using NubluSoft_Storage.Models.DTOs;

namespace NubluSoft_Storage.Services
{
    /// <summary>
    /// Servicio orquestador de alto nivel para operaciones de storage
    /// Coordina entre GCS y PostgreSQL
    /// </summary>
    public interface IStorageService
    {
        // ==================== UPLOAD ====================

        /// <summary>
        /// Sube un archivo y registra metadatos en BD
        /// </summary>
        Task<UploadResponse> UploadFileAsync(
            IFormFile file,
            UploadRequest request,
            long usuarioId,
            long entidadId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sube múltiples archivos
        /// </summary>
        Task<List<UploadResponse>> UploadMultipleAsync(
            IFormFileCollection files,
            long carpetaId,
            long usuarioId,
            long entidadId,
            CancellationToken cancellationToken = default);

        // ==================== DOWNLOAD ====================

        /// <summary>
        /// Obtiene URL firmada para descarga directa
        /// </summary>
        Task<SignedUrlResponse> GetDownloadUrlAsync(
            long archivoId,
            long usuarioId,
            int? expirationMinutes = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene múltiples URLs firmadas
        /// </summary>
        Task<BatchSignedUrlResponse> GetBatchDownloadUrlsAsync(
            BatchSignedUrlRequest request,
            long usuarioId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Descarga archivo como stream (para proxy)
        /// </summary>
        Task<(Stream Stream, string FileName, string ContentType, long Size)?> DownloadFileAsync(
            long archivoId,
            long usuarioId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene información para descargar carpeta como ZIP
        /// </summary>
        Task<FolderDownloadInfo?> GetFolderDownloadInfoAsync(
            long carpetaId,
            long usuarioId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Descarga carpeta como ZIP streaming
        /// </summary>
        Task DownloadFolderAsZipAsync(
            long carpetaId,
            long usuarioId,
            Stream outputStream,
            CancellationToken cancellationToken = default);

        // ==================== VERSIONES ====================

        /// <summary>
        /// Crea nueva versión de un archivo
        /// </summary>
        Task<VersionResponse> CreateVersionAsync(
            IFormFile file,
            CreateVersionRequest request,
            long usuarioId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene historial de versiones
        /// </summary>
        Task<VersionHistoryResponse?> GetVersionHistoryAsync(
            long archivoId,
            long usuarioId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene URL de descarga para versión específica
        /// </summary>
        Task<SignedUrlResponse> GetVersionDownloadUrlAsync(
            long archivoId,
            int version,
            long usuarioId,
            CancellationToken cancellationToken = default);

        // ==================== GESTIÓN ====================

        /// <summary>
        /// Elimina un archivo (GCS + BD)
        /// </summary>
        Task<StorageResult> DeleteFileAsync(
            long archivoId,
            long usuarioId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si archivo existe
        /// </summary>
        Task<bool> FileExistsAsync(
            long archivoId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene estadísticas de storage de una entidad
        /// </summary>
        Task<StorageStats> GetStorageStatsAsync(
            long entidadId,
            CancellationToken cancellationToken = default);
    }
}