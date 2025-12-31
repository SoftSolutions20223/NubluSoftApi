using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace NubluSoft.Extensions
{
    /// <summary>
    /// Extensiones para extraer información del ClaimsPrincipal (usuario autenticado)
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Obtiene el código del usuario desde los claims
        /// </summary>
        public static long GetUserId(this ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier)
                ?? principal.FindFirst("sub")
                ?? principal.FindFirst("Cod");

            if (claim != null && long.TryParse(claim.Value, out long userId))
            {
                return userId;
            }

            return 0;
        }

        /// <summary>
        /// Obtiene el nombre de usuario desde los claims
        /// </summary>
        public static string? GetUserName(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst("Usuario")?.Value
                ?? principal.Identity?.Name;
        }

        /// <summary>
        /// Obtiene el código de la entidad desde los claims
        /// </summary>
        public static long GetEntidadId(this ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst("Entidad")
                ?? principal.FindFirst("EntidadId");

            if (claim != null && long.TryParse(claim.Value, out long entidadId))
            {
                return entidadId;
            }

            return 0;
        }

        /// <summary>
        /// Obtiene el nombre completo del usuario desde los claims
        /// </summary>
        public static string? GetNombreCompleto(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("NombreCompleto")?.Value;
        }

        /// <summary>
        /// Obtiene el nombre de la entidad desde los claims
        /// </summary>
        public static string? GetNombreEntidad(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("NombreEntidad")?.Value;
        }

        /// <summary>
        /// Verifica si el usuario tiene un rol específico
        /// </summary>
        public static bool HasRole(this ClaimsPrincipal principal, string role)
        {
            return principal.IsInRole(role)
                || principal.HasClaim(ClaimTypes.Role, role)
                || principal.HasClaim("role", role);
        }

        /// <summary>
        /// Obtiene todos los roles del usuario
        /// </summary>
        public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
        {
            return principal.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(principal.FindAll("role").Select(c => c.Value))
                .Distinct();
        }

        /// <summary>
        /// Obtiene el SessionId desde los claims
        /// </summary>
        public static string? GetSessionId(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("SessionId")?.Value
                ?? principal.FindFirst("sid")?.Value;
        }
    }
}
