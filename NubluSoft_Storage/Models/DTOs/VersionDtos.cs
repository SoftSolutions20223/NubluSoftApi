using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Storage.Models.DTOs
{
    /// <summary>
    /// Request para crear nueva versión de un archivo
    /// </summary>
    public class CreateVersionRequest
    {
        [Required(ErrorMessage = "El archivo es requerido")]
        public long ArchivoId { get; set; }

        [MaxLength(500)]
        public string? Comentario { get; set; }
    }

    /// <summary>
    /// Respuesta al crear nueva versión
    /// </summary>
    public class VersionResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long? ArchivoId { get; set; }
        public int? VersionNumero { get; set; }
        public string? Ruta { get; set; }
        public string? Hash { get; set; }
        public long? Tamano { get; set; }
        public DateTime? FechaCreacion { get; set; }
    }

    /// <summary>
    /// Información de una versión de archivo
    /// </summary>
    public class VersionInfo
    {
        public long ArchivoId { get; set; }
        public int Version { get; set; }
        public string Ruta { get; set; } = string.Empty;
        public string? Hash { get; set; }
        public long Tamano { get; set; }
        public DateTime FechaCreacion { get; set; }
        public long UsuarioCreador { get; set; }
        public string? NombreUsuario { get; set; }
        public string? Comentario { get; set; }
        public bool EsVersionActual { get; set; }
    }

    /// <summary>
    /// Lista de versiones de un archivo
    /// </summary>
    public class VersionHistoryResponse
    {
        public long ArchivoId { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public int VersionActual { get; set; }
        public int TotalVersiones { get; set; }
        public List<VersionInfo> Versiones { get; set; } = new();
    }
}