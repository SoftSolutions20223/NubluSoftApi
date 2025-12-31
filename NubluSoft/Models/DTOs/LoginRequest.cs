using System.ComponentModel.DataAnnotations;

namespace NubluSoft.Models.DTOs
{
    /// <summary>
    /// Request para iniciar sesión
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "El usuario es requerido")]
        [StringLength(200, ErrorMessage = "El usuario no puede exceder 200 caracteres")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(200, ErrorMessage = "La contraseña no puede exceder 200 caracteres")]
        public string Contraseña { get; set; } = string.Empty;
    }
}