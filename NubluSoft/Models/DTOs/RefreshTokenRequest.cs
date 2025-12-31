using System.ComponentModel.DataAnnotations;

namespace NubluSoft.Models.DTOs
{
    /// <summary>
    /// Request para renovar el token
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "El refresh token es requerido")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}