namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Cliente para comunicación interna con NubluSoft_Storage
    /// </summary>
    public interface IStorageClientService
    {
        /// <summary>
        /// Solicita URL firmada para upload directo a GCS
        /// </summary>
        Task<StorageUploadUrlResult> GetUploadUrlAsync(
            string objectName,
            string contentType,
            int? expirationMinutes = null,
            string? authToken = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Solicita URL firmada para descarga
        /// </summary>
        Task<StorageDownloadUrlResult> GetDownloadUrlAsync(
            string objectName,
            string? downloadFileName = null,
            int? expirationMinutes = null,
            string? authToken = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si un archivo existe en GCS
        /// </summary>
        Task<bool> FileExistsAsync(
            string objectName,
            string? authToken = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene información de un archivo en GCS
        /// </summary>
        Task<StorageFileInfo?> GetFileInfoAsync(
            string objectName,
            string? authToken = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un archivo de GCS
        /// </summary>
        Task<bool> DeleteFileAsync(
            string objectName,
            string? authToken = null,
            CancellationToken cancellationToken = default);
    }

    // ==================== DTOs ====================

    public class StorageUploadUrlResult
    {
        public bool Success { get; set; }
        public string? UploadUrl { get; set; }
        public string? ObjectName { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string? Error { get; set; }
    }

    public class StorageDownloadUrlResult
    {
        public bool Success { get; set; }
        public string? DownloadUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string? Error { get; set; }
    }

    public class StorageFileInfo
    {
        public string ObjectName { get; set; } = string.Empty;
        public string? Bucket { get; set; }
        public long Size { get; set; }
        public string? ContentType { get; set; }
        public string? Md5Hash { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}