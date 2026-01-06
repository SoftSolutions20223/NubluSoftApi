using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Storage.Models.DTOs
{
    /// <summary>
    /// Request para subir un archivo
    /// </summary>
    public class UploadRequest
    {
        /// <summary>
        /// ID de la carpeta destino
        /// </summary>
        [Required(ErrorMessage = "La carpeta es requerida")]
        public long CarpetaId { get; set; }

        /// <summary>
        /// Nombre personalizado (opcional, usa nombre original si no se especifica)
        /// </summary>
        [MaxLength(200)]
        public string? NombrePersonalizado { get; set; }

        /// <summary>
        /// Descripción del archivo
        /// </summary>
        [MaxLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Tipo documental (catálogo)
        /// </summary>
        public long? TipoDocumental { get; set; }

        /// <summary>
        /// Fecha del documento
        /// </summary>
        public DateTime? FechaDocumento { get; set; }

        /// <summary>
        /// Código único del documento
        /// </summary>
        [MaxLength(50)]
        public string? CodigoDocumento { get; set; }

        /// <summary>
        /// Metadatos adicionales en formato JSON
        /// </summary>
        public string? MetadatosAdicionales { get; set; }
    }

    /// <summary>
    /// Respuesta después de subir un archivo
    /// </summary>
    public class UploadResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long? ArchivoId { get; set; }
        public string? Nombre { get; set; }
        public string? Ruta { get; set; }
        public long? Tamano { get; set; }
        public string? Hash { get; set; }
        public string? ContentType { get; set; }
        public DateTime? FechaSubida { get; set; }
    }

    /// <summary>
    /// Request para iniciar upload resumible (archivos grandes)
    /// </summary>
    public class InitResumableUploadRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de contenido es requerido")]
        public string ContentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tamaño es requerido")]
        [Range(1, long.MaxValue, ErrorMessage = "El tamaño debe ser mayor a 0")]
        public long FileSize { get; set; }

        [Required(ErrorMessage = "La carpeta es requerida")]
        public long CarpetaId { get; set; }
    }

    /// <summary>
    /// Respuesta con URI para upload resumible
    /// </summary>
    public class ResumableUploadResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string? UploadId { get; set; }
        public string? UploadUri { get; set; }
        public int ChunkSizeBytes { get; set; }
        public DateTime? Expiration { get; set; }
    }
}