using NubluSoft_Storage.Models.DTOs;

namespace NubluSoft_Storage.Services
{
    /// <summary>
    /// Servicio de bajo nivel para interactuar con Google Cloud Storage
    /// </summary>
    public interface IGcsStorageService
    {
        /// <summary>
        /// Sube un archivo usando streaming (sin cargar en memoria)
        /// </summary>
        Task<GcsUploadResult> UploadStreamingAsync(
            Stream stream,
            string objectName,
            string contentType,
            long? contentLength = null,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Descarga un archivo como stream
        /// </summary>
        Task<Stream> DownloadAsStreamAsync(
            string objectName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Descarga un archivo directamente a un stream de destino
        /// </summary>
        Task DownloadToStreamAsync(
            string objectName,
            Stream destination,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Genera una URL firmada para descarga directa
        /// </summary>
        string GenerateSignedDownloadUrl(
            string objectName,
            TimeSpan? expiration = null,
            string? downloadFileName = null);

        /// <summary>
        /// Genera una URL firmada para upload directo.
        /// Retorna la URL firmada y el Content-Type normalizado que fue usado para la firma.
        /// El cliente DEBE usar exactamente el ContentType retornado.
        /// </summary>
        SignedUploadUrlResult GenerateSignedUploadUrl(
            string objectName,
            string contentType,
            TimeSpan? expiration = null);

        /// <summary>
        /// Elimina un archivo
        /// </summary>
        Task<bool> DeleteAsync(
            string objectName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si un archivo existe
        /// </summary>
        Task<bool> ExistsAsync(
            string objectName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene información de un archivo
        /// </summary>
        Task<GcsFileInfo?> GetFileInfoAsync(
            string objectName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Copia un archivo dentro del bucket
        /// </summary>
        Task<bool> CopyAsync(
            string sourceObjectName,
            string destinationObjectName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista archivos con un prefijo
        /// </summary>
        IAsyncEnumerable<GcsFileInfo> ListFilesAsync(
            string prefix,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el nombre del bucket configurado
        /// </summary>
        string BucketName { get; }
    }

    /// <summary>
    /// Resultado de generación de URL firmada para upload
    /// </summary>
    public class SignedUploadUrlResult
    {
        /// <summary>
        /// URL firmada para subir el archivo directamente a GCS
        /// </summary>
        public string SignedUrl { get; set; } = string.Empty;

        /// <summary>
        /// Content-Type EXACTO que fue usado para firmar la URL.
        /// El cliente DEBE enviar este valor exacto en el header Content-Type.
        /// </summary>
        public string SignedContentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de upload a GCS
    /// </summary>
    public class GcsUploadResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string ObjectName { get; set; } = string.Empty;
        public string? MediaLink { get; set; }
        public string? SelfLink { get; set; }
        public long Size { get; set; }
        public string? Hash { get; set; }
        public string? Md5Hash { get; set; }
        public string? Crc32c { get; set; }
        public DateTime? Created { get; set; }

        public static GcsUploadResult Failure(string error) => new()
        {
            Success = false,
            Error = error
        };
    }
}