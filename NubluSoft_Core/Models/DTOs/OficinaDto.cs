using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// DTO para crear una oficina
    /// </summary>
    public class CrearOficinaRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Codigo { get; set; }

        public long? OficinaPadre { get; set; }

        public long? Responsable { get; set; }

        public int? NivelJerarquico { get; set; }

        [MaxLength(20)]
        public string? Sigla { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(200)]
        public string? Ubicacion { get; set; }
    }

    /// <summary>
    /// DTO para actualizar una oficina
    /// </summary>
    public class ActualizarOficinaRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Codigo { get; set; }

        public long? OficinaPadre { get; set; }

        public long? Responsable { get; set; }

        public int? NivelJerarquico { get; set; }

        [MaxLength(20)]
        public string? Sigla { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(200)]
        public string? Ubicacion { get; set; }

        public bool Estado { get; set; } = true;
    }
}