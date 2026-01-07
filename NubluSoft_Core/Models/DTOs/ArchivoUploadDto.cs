using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// Request para iniciar un upload directo a GCS
    /// </summary>
    public class IniciarUploadRequest
    {
        /// <summary>
        /// ID de la carpeta destino
        /// </summary>
        [Required(ErrorMessage = "La carpeta es requerida")]
        public long CarpetaId { get; set; }

        /// <summary>
        /// Nombre del archivo
        /// </summary>
        [Required(ErrorMessage = "El nombre del archivo es requerido")]
        [MaxLength(200)]
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Tipo MIME del archivo
        /// </summary>
        [Required(ErrorMessage = "El tipo de contenido es requerido")]
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Tamaño del archivo en bytes
        /// </summary>
        [Required(ErrorMessage = "El tamaño es requerido")]
        [Range(1, 524288000, ErrorMessage = "El tamaño debe ser entre 1 byte y 500 MB")]
        public long TamanoBytes { get; set; }

        /// <summary>
        /// Descripción del archivo (opcional)
        /// </summary>
        [MaxLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Tipo documental del catálogo (opcional)
        /// </summary>
        public long? TipoDocumental { get; set; }

        /// <summary>
        /// Fecha del documento (opcional)
        /// </summary>
        public DateTime? FechaDocumento { get; set; }

        /// <summary>
        /// Código único del documento (opcional)
        /// </summary>
        [MaxLength(50)]
        public string? CodigoDocumento { get; set; }
    }

    /// <summary>
    /// Respuesta al iniciar upload con URL firmada
    /// </summary>
    public class IniciarUploadResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// ID del archivo creado (en estado PENDIENTE)
        /// </summary>
        public long? ArchivoId { get; set; }

        /// <summary>
        /// URL firmada para subir directamente a GCS
        /// </summary>
        public string? UploadUrl { get; set; }

        /// <summary>
        /// Nombre del objeto en GCS
        /// </summary>
        public string? ObjectName { get; set; }

        /// <summary>
        /// Fecha/hora de expiración de la URL
        /// </summary>
        public DateTime? UrlExpiraEn { get; set; }

        /// <summary>
        /// Segundos hasta que expire la URL
        /// </summary>
        public int? SegundosParaExpirar { get; set; }

        /// <summary>
        /// Content-Type que debe usar el frontend al subir
        /// </summary>
        public string? ContentType { get; set; }
    }

    /// <summary>
    /// Request para confirmar que el upload se completó
    /// </summary>
    public class ConfirmarUploadRequest
    {
        /// <summary>
        /// Hash SHA256 del archivo (calculado por el frontend)
        /// </summary>
        public string? Hash { get; set; }

        /// <summary>
        /// Tamaño real del archivo subido
        /// </summary>
        public long? TamanoReal { get; set; }
    }

    /// <summary>
    /// Respuesta al confirmar upload
    /// </summary>
    public class ConfirmarUploadResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long? ArchivoId { get; set; }
        public string? Nombre { get; set; }
        public string? Ruta { get; set; }
        public long? Tamano { get; set; }
        public string? Hash { get; set; }
    }

    /// <summary>
    /// Request para cancelar un upload pendiente
    /// </summary>
    public class CancelarUploadRequest
    {
        /// <summary>
        /// Motivo de la cancelación (opcional)
        /// </summary>
        [MaxLength(200)]
        public string? Motivo { get; set; }
    }

    /// <summary>
    /// Respuesta con URL de descarga
    /// </summary>
    public class DescargarArchivoResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long? ArchivoId { get; set; }
        public string? Nombre { get; set; }
        public string? DownloadUrl { get; set; }
        public DateTime? UrlExpiraEn { get; set; }
        public int? SegundosParaExpirar { get; set; }
        public long? Tamano { get; set; }
        public string? ContentType { get; set; }
    }
}