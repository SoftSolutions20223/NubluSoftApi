using NubluSoft.Models.DTOs;
using NubluSoft.Models.Entities;
using System.Security.Claims;

namespace NubluSoft.Services
{
    /// <summary>
    /// Interfaz para el servicio de JWT
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Genera un token JWT para el usuario
        /// </summary>
        string GenerarToken(Usuario usuario, string? nombreEntidad, List<RolUsuarioInfo>? roles, string sessionId);

        /// <summary>
        /// Genera un refresh token
        /// </summary>
        string GenerarRefreshToken();

        /// <summary>
        /// Valida un token JWT y retorna los claims
        /// </summary>
        ClaimsPrincipal? ValidarToken(string token);

        /// <summary>
        /// Obtiene la fecha de expiración del token
        /// </summary>
        DateTime ObtenerExpiracionToken();

        /// <summary>
        /// Obtiene la fecha de expiración del refresh token
        /// </summary>
        DateTime ObtenerExpiracionRefreshToken();
    }
}