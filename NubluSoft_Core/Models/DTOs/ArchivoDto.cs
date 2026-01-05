using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// DTO para crear un archivo (metadatos, la subida física va por NubluSoft_Storage)
    /// </summary>
    public class CrearArchivoRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La carpeta es requerida")]
        public long Carpeta { get; set; }

        [Required(ErrorMessage = "La ruta es requerida")]
        public string Ruta { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public DateTime? FechaDocumento { get; set; }

        [MaxLength(50)]
        public string? CodigoDocumento { get; set; }

        public long? TipoArchivo { get; set; }

        public long? TipoDocumental { get; set; }

        [MaxLength(20)]
        public string? OrigenDocumento { get; set; }

        public int? PaginaInicio { get; set; }

        public int? PaginaFin { get; set; }

        public string? Hash { get; set; }

        public long? Tamano { get; set; }

        public string? MetadatosAdicionales { get; set; }
    }

    /// <summary>
    /// DTO para actualizar metadatos de un archivo
    /// </summary>
    public class ActualizarArchivoRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public DateTime? FechaDocumento { get; set; }

        [MaxLength(50)]
        public string? CodigoDocumento { get; set; }

        public long? TipoDocumental { get; set; }

        [MaxLength(20)]
        public string? OrigenDocumento { get; set; }

        public int? PaginaInicio { get; set; }

        public int? PaginaFin { get; set; }

        public string? MetadatosAdicionales { get; set; }
    }

    /// <summary>
    /// DTO para crear nueva versión de un archivo
    /// </summary>
    public class CrearVersionRequest
    {
        [Required(ErrorMessage = "La ruta es requerida")]
        public string Ruta { get; set; } = string.Empty;

        public string? Hash { get; set; }

        public long? Tamano { get; set; }

        [MaxLength(500)]
        public string? Comentario { get; set; }
    }

    /// <summary>
    /// DTO para restaurar una versión anterior
    /// </summary>
    public class RestaurarVersionRequest
    {
        [Required(ErrorMessage = "La versión es requerida")]
        public int Version { get; set; }

        [MaxLength(500)]
        public string? Comentario { get; set; }
    }

    /// <summary>
    /// Filtros para listar archivos
    /// </summary>
    public class FiltrosArchivosRequest
    {
        public long? Carpeta { get; set; }
        public long? TipoDocumental { get; set; }
        public string? Busqueda { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool SoloActivos { get; set; } = true;
    }
}