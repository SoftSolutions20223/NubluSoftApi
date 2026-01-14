namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Cliente HTTP para comunicación con NubluSoft_Storage
    /// </summary>
    public interface IStorageClientService
    {
        /// <summary>
        /// Descarga un archivo desde Storage
        /// </summary>
        Task<byte[]?> DescargarArchivoAsync(string ruta, string? authToken = null);

        /// <summary>
        /// Obtiene una URL firmada para descargar
        /// </summary>
        Task<string?> ObtenerUrlDescargaAsync(string ruta, string? authToken = null);

        /// <summary>
        /// Sube una nueva versión de un archivo
        /// </summary>
        Task<StorageUploadResult?> SubirVersionAsync(
            long archivoId,
            byte[] contenido,
            string contentType,
            string? authToken = null);

        /// <summary>
        /// Obtiene información de un archivo
        /// </summary>
        Task<StorageFileInfo?> ObtenerInfoArchivoAsync(long archivoId, string? authToken = null);
    }

    // ==================== DTOs de Storage ====================

    public class StorageUploadResult
    {
        public bool Success { get; set; }
        public string? ObjectName { get; set; }
        public string? Hash { get; set; }
        public long? Size { get; set; }
        public string? Error { get; set; }
    }

    public class StorageFileInfo
    {
        public long Cod { get; set; }
        public string? Nombre { get; set; }
        public string? Ruta { get; set; }
        public string? Hash { get; set; }
        public string? ContentType { get; set; }
        public long? Tamano { get; set; }
    }
}