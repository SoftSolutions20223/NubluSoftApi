using Microsoft.Extensions.Options;
using NubluSoft_Signature.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del cliente HTTP para Storage
    /// </summary>
    public class StorageClientService : IStorageClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StorageClientService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public StorageClientService(
            HttpClient httpClient,
            ILogger<StorageClientService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<byte[]?> DescargarArchivoAsync(string ruta, string? authToken = null)
        {
            try
            {
                // Primero obtener URL de descarga
                var urlDescarga = await ObtenerUrlDescargaAsync(ruta, authToken);
                if (string.IsNullOrEmpty(urlDescarga))
                {
                    _logger.LogWarning("No se pudo obtener URL de descarga para {Ruta}", ruta);
                    return null;
                }

                // Descargar directamente desde GCS
                using var downloadClient = new HttpClient();
                var response = await downloadClient.GetAsync(urlDescarga);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error descargando archivo: {StatusCode}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando archivo {Ruta}", ruta);
                return null;
            }
        }

        public async Task<string?> ObtenerUrlDescargaAsync(string ruta, string? authToken = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/internal/Internal/signed-url/download");

                if (!string.IsNullOrEmpty(authToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }

                var body = new { ObjectName = ruta, ExpirationMinutes = 15 };
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error obteniendo URL de descarga: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DownloadUrlResponse>(content, _jsonOptions);

                return result?.SignedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo URL de descarga para {Ruta}", ruta);
                return null;
            }
        }

        public async Task<StorageUploadResult?> SubirVersionAsync(
            long archivoId,
            byte[] contenido,
            string contentType,
            string? authToken = null)
        {
            try
            {
                // 1. Obtener información del archivo actual
                var info = await ObtenerInfoArchivoAsync(archivoId, authToken);
                if (info == null)
                {
                    return new StorageUploadResult { Success = false, Error = "Archivo no encontrado" };
                }

                // 2. Solicitar URL de upload
                var uploadUrlRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/Internal/signed-url/upload");

                if (!string.IsNullOrEmpty(authToken))
                {
                    uploadUrlRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }

                var uploadBody = new
                {
                    ObjectName = info.Ruta,
                    ContentType = contentType,
                    ExpirationMinutes = 30
                };

                uploadUrlRequest.Content = new StringContent(
                    JsonSerializer.Serialize(uploadBody, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var uploadUrlResponse = await _httpClient.SendAsync(uploadUrlRequest);
                if (!uploadUrlResponse.IsSuccessStatusCode)
                {
                    return new StorageUploadResult { Success = false, Error = "Error obteniendo URL de upload" };
                }

                var uploadUrlContent = await uploadUrlResponse.Content.ReadAsStringAsync();
                var uploadUrlResult = JsonSerializer.Deserialize<UploadUrlResponse>(uploadUrlContent, _jsonOptions);

                if (string.IsNullOrEmpty(uploadUrlResult?.SignedUrl))
                {
                    return new StorageUploadResult { Success = false, Error = "URL de upload inválida" };
                }

                // 3. Subir archivo a GCS
                using var uploadClient = new HttpClient();
                var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrlResult.SignedUrl);
                uploadRequest.Content = new ByteArrayContent(contenido);
                uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                var gcsResponse = await uploadClient.SendAsync(uploadRequest);
                if (!gcsResponse.IsSuccessStatusCode)
                {
                    return new StorageUploadResult { Success = false, Error = "Error subiendo a GCS" };
                }

                // 4. Confirmar upload y actualizar hash
                var confirmRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/Internal/confirm-upload");

                if (!string.IsNullOrEmpty(authToken))
                {
                    confirmRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }

                var confirmBody = new
                {
                    ArchivoId = archivoId,
                    ObjectName = info.Ruta,
                    Size = contenido.Length
                };

                confirmRequest.Content = new StringContent(
                    JsonSerializer.Serialize(confirmBody, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var confirmResponse = await _httpClient.SendAsync(confirmRequest);
                var confirmContent = await confirmResponse.Content.ReadAsStringAsync();
                var confirmResult = JsonSerializer.Deserialize<ConfirmUploadResponse>(confirmContent, _jsonOptions);

                return new StorageUploadResult
                {
                    Success = confirmResult?.Success ?? false,
                    ObjectName = info.Ruta,
                    Hash = confirmResult?.Hash,
                    Size = contenido.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo versión del archivo {ArchivoId}", archivoId);
                return new StorageUploadResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<StorageFileInfo?> ObtenerInfoArchivoAsync(long archivoId, string? authToken = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/Internal/file-info/{archivoId}");

                if (!string.IsNullOrEmpty(authToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StorageFileInfo>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo info del archivo {ArchivoId}", archivoId);
                return null;
            }
        }

        // ==================== DTOs internos ====================

        private class DownloadUrlResponse
        {
            public bool Success { get; set; }
            public string? SignedUrl { get; set; }
        }

        private class UploadUrlResponse
        {
            public bool Success { get; set; }
            public string? SignedUrl { get; set; }
        }

        private class ConfirmUploadResponse
        {
            public bool Success { get; set; }
            public string? Hash { get; set; }
        }
    }
}