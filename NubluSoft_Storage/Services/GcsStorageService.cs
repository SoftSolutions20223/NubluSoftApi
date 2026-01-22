using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using NubluSoft_Storage.Configuration;
using NubluSoft_Storage.Helpers;
using NubluSoft_Storage.Models.DTOs;
using System.Runtime.CompilerServices;
using System.Web;

namespace NubluSoft_Storage.Services
{
    /// <summary>
    /// Implementación del servicio de Google Cloud Storage
    /// Usa streaming real para evitar cargar archivos completos en memoria
    /// </summary>
    public class GcsStorageService : IGcsStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly UrlSigner _urlSigner;
        private readonly GcsSettings _settings;
        private readonly ILogger<GcsStorageService> _logger;

        public string BucketName => _settings.BucketName;

        public GcsStorageService(
            IOptions<GcsSettings> settings,
            ILogger<GcsStorageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            // Inicializar cliente de GCS
            GoogleCredential credential;

            if (!string.IsNullOrEmpty(_settings.CredentialsPath) && File.Exists(_settings.CredentialsPath))
            {
                // Usar archivo de credenciales específico
                credential = GoogleCredential.FromFile(_settings.CredentialsPath);
                _logger.LogInformation("GCS inicializado con credenciales desde archivo: {Path}", _settings.CredentialsPath);
            }
            else
            {
                // Usar Application Default Credentials (ADC)
                credential = GoogleCredential.GetApplicationDefault();
                _logger.LogInformation("GCS inicializado con Application Default Credentials");
            }

            _storageClient = StorageClient.Create(credential);

            // Usar la API moderna para crear UrlSigner
            // FromCredential maneja automáticamente ServiceAccountCredential y otros tipos
            var serviceAccountCredential = credential.UnderlyingCredential as ServiceAccountCredential;
            if (serviceAccountCredential != null)
            {
                _urlSigner = UrlSigner.FromCredential(serviceAccountCredential);
                _logger.LogInformation("UrlSigner inicializado con ServiceAccountCredential. Email: {Email}",
                    serviceAccountCredential.Id);
            }
            else
            {
                // Fallback para otros tipos de credenciales (ej: ADC con impersonation)
                _urlSigner = UrlSigner.FromCredential(credential);
                _logger.LogWarning("UrlSigner inicializado con GoogleCredential (fallback)");
            }

            _logger.LogInformation("GcsStorageService inicializado para bucket: {Bucket}", _settings.BucketName);
        }

        /// <summary>
        /// Sube un archivo usando streaming con cálculo de hash en paralelo
        /// </summary>
        public async Task<GcsUploadResult> UploadStreamingAsync(
            Stream stream,
            string objectName,
            string contentType,
            long? contentLength = null,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Iniciando upload streaming: {ObjectName}, ContentType: {ContentType}, Size: {Size}",
                    objectName, contentType, contentLength);

                // Wrap del stream para calcular hash mientras se sube
                using var hashStream = new HashCalculatingStream(stream, leaveOpen: true);

                // Configurar objeto
                var storageObject = new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = _settings.BucketName,
                    Name = objectName,
                    ContentType = contentType
                };

                // Agregar metadata si existe
                if (metadata != null && metadata.Count > 0)
                {
                    storageObject.Metadata = metadata;
                }

                // Opciones de upload
                var uploadOptions = new UploadObjectOptions
                {
                    ChunkSize = _settings.ChunkSizeBytes
                };

                // Subir con streaming
                var result = await _storageClient.UploadObjectAsync(
                    storageObject,
                    hashStream,
                    uploadOptions,
                    cancellationToken);

                var hash = hashStream.GetHashString();

                _logger.LogInformation("Upload completado: {ObjectName}, Size: {Size}, Hash: {Hash}",
                    objectName, hashStream.BytesRead, hash);

                return new GcsUploadResult
                {
                    Success = true,
                    ObjectName = result.Name,
                    MediaLink = result.MediaLink,
                    SelfLink = result.SelfLink,
                    Size = hashStream.BytesRead,
                    Hash = hash,
                    Md5Hash = result.Md5Hash,
                    Crc32c = result.Crc32c,
                    Created = result.TimeCreatedDateTimeOffset?.DateTime
                };
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError(ex, "Error de Google API al subir {ObjectName}: {Message}", objectName, ex.Message);
                return GcsUploadResult.Failure($"Error de Google Cloud Storage: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo {ObjectName}", objectName);
                return GcsUploadResult.Failure($"Error al subir archivo: {ex.Message}");
            }
        }

        /// <summary>
        /// Descarga un archivo como stream (para procesar en memoria)
        /// </summary>
        public async Task<Stream> DownloadAsStreamAsync(
            string objectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Descargando como stream: {ObjectName}", objectName);

                var memoryStream = new MemoryStream();
                await _storageClient.DownloadObjectAsync(
                    _settings.BucketName,
                    objectName,
                    memoryStream,
                    cancellationToken: cancellationToken);

                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Archivo no encontrado en GCS: {ObjectName}", objectName);
                throw new FileNotFoundException($"Archivo no encontrado: {objectName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar archivo {ObjectName}", objectName);
                throw;
            }
        }

        /// <summary>
        /// Descarga directamente a un stream destino (ideal para streaming HTTP o ZIP)
        /// </summary>
        public async Task DownloadToStreamAsync(
            string objectName,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Descargando a stream destino: {ObjectName}", objectName);

                await _storageClient.DownloadObjectAsync(
                    _settings.BucketName,
                    objectName,
                    destination,
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Descarga completada: {ObjectName}", objectName);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Archivo no encontrado en GCS: {ObjectName}", objectName);
                throw new FileNotFoundException($"Archivo no encontrado: {objectName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar archivo {ObjectName}", objectName);
                throw;
            }
        }

        /// <summary>
        /// Genera URL firmada para que el cliente descargue directamente de GCS
        /// </summary>
        public string GenerateSignedDownloadUrl(
            string objectName,
            TimeSpan? expiration = null,
            string? downloadFileName = null)
        {
            try
            {
                var exp = expiration ?? TimeSpan.FromMinutes(_settings.SignedUrlExpiration);

                // Crear el template de request
                var requestTemplate = UrlSigner.RequestTemplate
                    .FromBucket(_settings.BucketName)
                    .WithObjectName(objectName)
                    .WithHttpMethod(HttpMethod.Get);

                // Si se especifica nombre de descarga, usar query parameters
                if (!string.IsNullOrEmpty(downloadFileName))
                {
                    var encodedFileName = Uri.EscapeDataString(downloadFileName);
                    var queryParams = new Dictionary<string, IEnumerable<string>>
                    {
                        { "response-content-disposition", new[] { $"attachment; filename=\"{encodedFileName}\"" } }
                    };
                    requestTemplate = requestTemplate.WithQueryParameters(queryParams);
                }

                // Usar PathStyle explícitamente para consistencia con upload URLs
                var signingOptions = UrlSigner.Options
                    .FromDuration(exp)
                    .WithUrlStyle(Google.Cloud.Storage.V1.UrlSigner.UrlStyle.PathStyle);

                var signedUrl = _urlSigner.Sign(requestTemplate, signingOptions);

                _logger.LogDebug("URL firmada generada para: {ObjectName}, expira en {Expiration}",
                    objectName, exp);

                return signedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL firmada para {ObjectName}", objectName);
                throw;
            }
        }

        /// <summary>
        /// Genera URL firmada para upload directo desde el cliente.
        /// NOTA: Se probó sin firmar Content-Type para diagnosticar SignatureDoesNotMatch.
        /// Retorna tanto la URL como el Content-Type normalizado.
        /// </summary>
        public SignedUploadUrlResult GenerateSignedUploadUrl(
            string objectName,
            string contentType,
            TimeSpan? expiration = null)
        {
            try
            {
                var exp = expiration ?? TimeSpan.FromMinutes(_settings.SignedUrlExpiration);

                // Normalización del Content-Type (aunque no se firme, se retorna para metadata)
                var normalizedContentType = NormalizeContentType(contentType);

                _logger.LogInformation(
                    "[FIRMA DEBUG] Generando URL firmada SIN firmar Content-Type - Bucket: {Bucket}, ObjectName: {ObjectName}",
                    _settings.BucketName, objectName);
                _logger.LogInformation(
                    "[FIRMA DEBUG] ContentType (no firmado): '{ContentType}'",
                    normalizedContentType);

                // PRUEBA DE DIAGNÓSTICO: NO incluir Content-Type en la firma
                // El cliente enviará Content-Type, pero no estará en la firma verificada
                // Esto permite diagnosticar si el problema es el Content-Type o algo más
                var requestTemplate = UrlSigner.RequestTemplate
                    .FromBucket(_settings.BucketName)
                    .WithObjectName(objectName)
                    .WithHttpMethod(HttpMethod.Put);
                // SIN .WithContentHeaders() para no firmar Content-Type

                // IMPORTANTE: Usar PathStyle explícitamente para asegurar consistencia
                var signingOptions = UrlSigner.Options
                    .FromDuration(exp)
                    .WithUrlStyle(Google.Cloud.Storage.V1.UrlSigner.UrlStyle.PathStyle);

                var signedUrl = _urlSigner.Sign(requestTemplate, signingOptions);

                // Log detallado de todos los parámetros de firma para debugging
                var uri = new Uri(signedUrl);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                _logger.LogInformation(
                    "[FIRMA DEBUG] ===== URL FIRMADA GENERADA (SIN Content-Type) =====");
                _logger.LogInformation(
                    "[FIRMA DEBUG] Host: {Host}, Path: {Path}",
                    uri.Host, uri.AbsolutePath);
                _logger.LogInformation(
                    "[FIRMA DEBUG] Algorithm: {Algorithm}, Date: {Date}, Expires: {Expires}",
                    queryParams["X-Goog-Algorithm"], queryParams["X-Goog-Date"], queryParams["X-Goog-Expires"]);
                _logger.LogInformation(
                    "[FIRMA DEBUG] SignedHeaders: {SignedHeaders}",
                    queryParams["X-Goog-SignedHeaders"]);
                _logger.LogInformation(
                    "[FIRMA DEBUG] Credential: {Credential}",
                    queryParams["X-Goog-Credential"]);
                _logger.LogInformation(
                    "[FIRMA DEBUG] Signature length: {SignatureLength} chars",
                    queryParams["X-Goog-Signature"]?.Length ?? 0);
                _logger.LogInformation(
                    "[FIRMA DEBUG] ================================");

                _logger.LogInformation("URL firmada para upload generada (SIN Content-Type firmado): {ObjectName}, Expira en: {Expiration}",
                    objectName, exp);

                // Retornar la URL firmada junto con el Content-Type (para que el archivo se guarde con tipo correcto)
                return new SignedUploadUrlResult
                {
                    SignedUrl = signedUrl,
                    SignedContentType = normalizedContentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL firmada de upload para {ObjectName}", objectName);
                throw;
            }
        }

        /// <summary>
        /// Normaliza el Content-Type para evitar discrepancias en la firma GCS.
        /// Elimina espacios, parámetros adicionales (charset, boundary) y caracteres de control.
        /// </summary>
        private static string NormalizeContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return "application/octet-stream";

            // Remover caracteres de control y espacios invisibles
            var cleaned = new string(contentType.Where(c => !char.IsControl(c)).ToArray());

            // Trim espacios normales
            cleaned = cleaned.Trim();

            // Si el Content-Type tiene parámetros (ej: "text/plain; charset=utf-8"),
            // tomar solo la parte principal del MIME type
            var semicolonIndex = cleaned.IndexOf(';');
            if (semicolonIndex > 0)
            {
                cleaned = cleaned[..semicolonIndex].Trim();
            }

            // Si quedó vacío después de la limpieza, usar default
            if (string.IsNullOrEmpty(cleaned))
                return "application/octet-stream";

            return cleaned;
        }

        /// <summary>
        /// Elimina un archivo de GCS
        /// </summary>
        public async Task<bool> DeleteAsync(
            string objectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Eliminando archivo: {ObjectName}", objectName);

                await _storageClient.DeleteObjectAsync(
                    _settings.BucketName,
                    objectName,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Archivo eliminado: {ObjectName}", objectName);
                return true;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Archivo no encontrado al eliminar: {ObjectName}", objectName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar archivo {ObjectName}", objectName);
                return false;
            }
        }

        /// <summary>
        /// Verifica si un archivo existe
        /// </summary>
        public async Task<bool> ExistsAsync(
            string objectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "[EXISTS DEBUG GCS] Consultando GCS. Bucket: '{Bucket}', ObjectName: '{ObjectName}'",
                    _settings.BucketName, objectName);

                var obj = await _storageClient.GetObjectAsync(
                    _settings.BucketName,
                    objectName,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "[EXISTS DEBUG GCS] Archivo ENCONTRADO. Name: '{Name}', Size: {Size}, Created: {Created}",
                    obj.Name, obj.Size, obj.TimeCreatedDateTimeOffset);

                return true;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "[EXISTS DEBUG GCS] Archivo NO encontrado (404). Bucket: '{Bucket}', ObjectName: '{ObjectName}'",
                    _settings.BucketName, objectName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[EXISTS DEBUG GCS] Error al verificar existencia. Bucket: '{Bucket}', ObjectName: '{ObjectName}'",
                    _settings.BucketName, objectName);
                throw;
            }
        }

        /// <summary>
        /// Obtiene información de un archivo
        /// </summary>
        public async Task<GcsFileInfo?> GetFileInfoAsync(
            string objectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var obj = await _storageClient.GetObjectAsync(
                    _settings.BucketName,
                    objectName,
                    cancellationToken: cancellationToken);

                return new GcsFileInfo
                {
                    ObjectName = obj.Name,
                    Bucket = obj.Bucket,
                    MediaLink = obj.MediaLink,
                    SelfLink = obj.SelfLink,
                    Size = (long)(obj.Size ?? 0),
                    ContentType = obj.ContentType,
                    Md5Hash = obj.Md5Hash,
                    Crc32c = obj.Crc32c,
                    Created = obj.TimeCreatedDateTimeOffset?.DateTime,
                    Updated = obj.UpdatedDateTimeOffset?.DateTime,
                    Metadata = obj.Metadata?.ToDictionary(x => x.Key, x => x.Value)
                };
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener info de {ObjectName}", objectName);
                throw;
            }
        }

        /// <summary>
        /// Copia un archivo dentro del bucket
        /// </summary>
        public async Task<bool> CopyAsync(
            string sourceObjectName,
            string destinationObjectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Copiando {Source} a {Destination}", sourceObjectName, destinationObjectName);

                await _storageClient.CopyObjectAsync(
                    _settings.BucketName,
                    sourceObjectName,
                    _settings.BucketName,
                    destinationObjectName,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Archivo copiado de {Source} a {Destination}",
                    sourceObjectName, destinationObjectName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al copiar {Source} a {Destination}",
                    sourceObjectName, destinationObjectName);
                return false;
            }
        }

        /// <summary>
        /// Lista archivos con un prefijo (para listar carpetas)
        /// </summary>
        public async IAsyncEnumerable<GcsFileInfo> ListFilesAsync(
            string prefix,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listando archivos con prefijo: {Prefix}", prefix);

            var objects = _storageClient.ListObjectsAsync(_settings.BucketName, prefix);

            await foreach (var obj in objects.WithCancellation(cancellationToken))
            {
                yield return new GcsFileInfo
                {
                    ObjectName = obj.Name,
                    Bucket = obj.Bucket,
                    MediaLink = obj.MediaLink,
                    Size = (long)(obj.Size ?? 0),
                    ContentType = obj.ContentType,
                    Created = obj.TimeCreatedDateTimeOffset?.DateTime,
                    Updated = obj.UpdatedDateTimeOffset?.DateTime
                };
            }
        }
    }
}