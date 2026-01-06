namespace NubluSoft_Storage.Models.DTOs
{
    /// <summary>
    /// Respuesta con URL firmada para descarga directa
    /// </summary>
    public class SignedUrlResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long? Size { get; set; }
        public int ExpiresInSeconds { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Respuesta para descarga de carpeta como ZIP
    /// </summary>
    public class FolderDownloadInfo
    {
        public long CarpetaId { get; set; }
        public string NombreCarpeta { get; set; } = string.Empty;
        public int TotalArchivos { get; set; }
        public long TamanoTotalBytes { get; set; }
        public string TamanoFormateado { get; set; } = string.Empty;
        public List<ArchivoEnCarpeta> Archivos { get; set; } = new();
    }

    /// <summary>
    /// Información de archivo dentro de una carpeta
    /// </summary>
    public class ArchivoEnCarpeta
    {
        public long ArchivoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string RutaRelativa { get; set; } = string.Empty;
        public string? GcsObjectName { get; set; }
        public long Tamano { get; set; }
        public string? ContentType { get; set; }
    }

    /// <summary>
    /// Request para obtener múltiples URLs firmadas
    /// </summary>
    public class BatchSignedUrlRequest
    {
        public List<long> ArchivoIds { get; set; } = new();
        public int? ExpirationMinutes { get; set; }
    }

    /// <summary>
    /// Respuesta con múltiples URLs firmadas
    /// </summary>
    public class BatchSignedUrlResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public List<SignedUrlItem> Urls { get; set; } = new();
    }

    /// <summary>
    /// Item individual de URL firmada
    /// </summary>
    public class SignedUrlItem
    {
        public long ArchivoId { get; set; }
        public string? Url { get; set; }
        public string? FileName { get; set; }
        public string? Error { get; set; }
    }
}