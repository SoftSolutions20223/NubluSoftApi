using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Implementación del cliente HTTP para comunicación con NubluSoft_Storage
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

        public async Task<StorageUploadUrlResult> GetUploadUrlAsync(
            string objectName,
            string contentType,
            int? expirationMinutes = null,
            string? authToken = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "[UPLOAD DEBUG Core→Storage] Solicitando URL. ObjectName: {ObjectName}, ContentType enviado: '{ContentType}'",
                    objectName, contentType);

                var request = new
                {
                    ObjectName = objectName,
                    ContentType = contentType,
                    ExpirationMinutes = expirationMinutes ?? 30
                };

                var response = await SendPostAsync<StorageUploadUrlResult>(
                    "/internal/Internal/signed-url/upload",
                    request,
                    authToken,
                    cancellationToken);

                if (response != null)
                {
                    _logger.LogInformation(
                        "[UPLOAD DEBUG Core←Storage] Respuesta recibida. Success: {Success}, ContentType recibido: '{ContentType}', URL: {HasUrl}",
                        response.Success, response.ContentType, !string.IsNullOrEmpty(response.UploadUrl));
                }

                return response ?? new StorageUploadUrlResult
                {
                    Success = false,
                    Error = "No se recibió respuesta del servicio de storage"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error solicitando URL de upload para {ObjectName}", objectName);
                return new StorageUploadUrlResult
                {
                    Success = false,
                    Error = $"Error de comunicación con Storage: {ex.Message}"
                };
            }
        }

        public async Task<StorageDownloadUrlResult> GetDownloadUrlAsync(
            string objectName,
            string? downloadFileName = null,
            int? expirationMinutes = null,
            string? authToken = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    ObjectName = objectName,
                    DownloadFileName = downloadFileName,
                    ExpirationMinutes = expirationMinutes ?? 15
                };

                var response = await SendPostAsync<StorageDownloadUrlResult>(
                    "/internal/Internal/signed-url/download",
                    request,
                    authToken,
                    cancellationToken);

                return response ?? new StorageDownloadUrlResult
                {
                    Success = false,
                    Error = "No se recibió respuesta del servicio de storage"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error solicitando URL de descarga para {ObjectName}", objectName);
                return new StorageDownloadUrlResult
                {
                    Success = false,
                    Error = $"Error de comunicación con Storage: {ex.Message}"
                };
            }
        }

        public async Task<bool> FileExistsAsync(
            string objectName,
            string? authToken = null,
            CancellationToken cancellationToken = default)
        {
            // Retry con delay para manejar latencia de propagación de GCS
            // A veces el archivo tarda unos segundos en ser visible después del upload
            const int maxRetries = 3;
            const int delayMs = 1500; // 1.5 segundos entre reintentos

            _logger.LogInformation(
                "[EXISTS DEBUG] Verificando existencia de archivo: '{ObjectName}'",
                objectName);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var encodedName = Uri.EscapeDataString(objectName);
                    var endpoint = $"/internal/Internal/exists?objectName={encodedName}";

                    _logger.LogInformation(
                        "[EXISTS DEBUG] Intento {Attempt}/{Max} - Endpoint: {Endpoint}",
                        attempt, maxRetries, endpoint);

                    var response = await SendGetAsync<FileExistsResponse>(
                        endpoint,
                        authToken,
                        cancellationToken);

                    var exists = response?.Exists ?? false;

                    _logger.LogInformation(
                        "[EXISTS DEBUG] Intento {Attempt} - Respuesta: Exists={Exists}, ObjectName={ResponseObj}",
                        attempt, exists, response?.ObjectName ?? "(null)");

                    if (exists)
                    {
                        return true;
                    }

                    // Si no existe y quedan reintentos, esperar antes del siguiente
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning(
                            "[EXISTS DEBUG] Archivo no encontrado en intento {Attempt}, esperando {Delay}ms antes de reintentar...",
                            attempt, delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[EXISTS DEBUG] Error en intento {Attempt} verificando existencia de {ObjectName}",
                        attempt, objectName);

                    if (attempt == maxRetries)
                    {
                        return false;
                    }

                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            _logger.LogWarning(
                "[EXISTS DEBUG] Archivo no encontrado después de {Max} intentos: {ObjectName}",
                maxRetries, objectName);
            return false;
        }

        public async Task<StorageFileInfo?> GetFileInfoAsync(
            string objectName,
            string? authToken = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await SendGetAsync<FileInfoResponse>(
                    $"/internal/Internal/file-info?objectName={Uri.EscapeDataString(objectName)}",
                    authToken,
                    cancellationToken);

                return response?.FileInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo info de {ObjectName}", objectName);
                return null;
            }
        }

        public async Task<bool> DeleteFileAsync(
            string objectName,
            string? authToken = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var requestMessage = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"/internal/Internal/delete?objectName={Uri.EscapeDataString(objectName)}");

                if (!string.IsNullOrEmpty(authToken))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }

                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var result = JsonSerializer.Deserialize<DeleteResponse>(content, _jsonOptions);
                    return result?.Success ?? false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando {ObjectName}", objectName);
                return false;
            }
        }

        // ==================== HELPERS ====================

        private async Task<T?> SendPostAsync<T>(
            string endpoint,
            object data,
            string? authToken,
            CancellationToken cancellationToken) where T : class
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(authToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }

            _logger.LogWarning("Storage respondió con {StatusCode} para {Endpoint}",
                response.StatusCode, endpoint);

            return null;
        }

        private async Task<T?> SendGetAsync<T>(
            string endpoint,
            string? authToken,
            CancellationToken cancellationToken) where T : class
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);

            if (!string.IsNullOrEmpty(authToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }

            return null;
        }

        // ==================== Response DTOs ====================

        private class FileExistsResponse
        {
            public string? ObjectName { get; set; }
            public bool Exists { get; set; }
        }

        private class FileInfoResponse
        {
            public bool Success { get; set; }
            public StorageFileInfo? FileInfo { get; set; }
        }

        private class DeleteResponse
        {
            public bool Success { get; set; }
            public string? ObjectName { get; set; }
            public string? Message { get; set; }
        }
    }
}