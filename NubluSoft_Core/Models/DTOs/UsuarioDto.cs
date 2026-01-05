using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// DTO para crear un usuario
    /// </summary>
    public class CrearUsuarioRequest
    {
        [Required(ErrorMessage = "Los nombres son requeridos")]
        [MaxLength(200)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [MaxLength(200)]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [MaxLength(200)]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [MaxLength(200)]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "El documento es requerido")]
        [MaxLength(20)]
        public string Documento { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? TipoDocumento { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(100)]
        public string? Cargo { get; set; }

        public long? OficinaPredeterminada { get; set; }

        public bool NotificacionesCorreo { get; set; } = true;

        /// <summary>
        /// Roles a asignar al usuario
        /// </summary>
        public List<AsignarRolRequest>? Roles { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un usuario
    /// </summary>
    public class ActualizarUsuarioRequest
    {
        [Required(ErrorMessage = "Los nombres son requeridos")]
        [MaxLength(200)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [MaxLength(200)]
        public string Apellidos { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Documento { get; set; }

        [MaxLength(10)]
        public string? TipoDocumento { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(100)]
        public string? Cargo { get; set; }

        public long? OficinaPredeterminada { get; set; }

        public bool NotificacionesCorreo { get; set; } = true;

        public bool Estado { get; set; } = true;
    }

    /// <summary>
    /// DTO para cambiar contraseña
    /// </summary>
    public class CambiarContrasenaRequest
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        public string ContrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [MaxLength(200)]
        public string ContrasenaNueva { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación es requerida")]
        [Compare("ContrasenaNueva", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para resetear contraseña (admin)
    /// </summary>
    public class ResetearContrasenaRequest
    {
        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [MaxLength(200)]
        public string ContrasenaNueva { get; set; } = string.Empty;

        public bool DebeCambiarContrasena { get; set; } = true;
    }

    /// <summary>
    /// DTO para asignar rol a usuario
    /// </summary>
    public class AsignarRolRequest
    {
        [Required]
        public long Oficina { get; set; }

        [Required]
        public long Rol { get; set; }
    }
}