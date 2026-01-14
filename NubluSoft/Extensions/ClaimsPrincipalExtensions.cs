using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace NubluSoft.Extensions
{
    /// <summary>
    /// Extensiones para extraer claims del usuario autenticado.
    /// IMPORTANTE: Debe coincidir con los claims generados por el Gateway en JwtService.
    /// 
    /// Claims generados por Gateway:
    /// - ClaimTypes.NameIdentifier = Cod del usuario
    /// - ClaimTypes.Name = Usuario (login)
    /// - "Cod" = Cod del usuario
    /// - "Usuario" = Usuario (login)
    /// - "Entidad" = ID de la entidad (PascalCase)
    /// - "NombreCompleto" = Nombre completo del usuario
    /// - "NombreEntidad" = Nombre de la entidad
    /// - "SessionId" = ID de sesión
    /// - ClaimTypes.Role = Roles del usuario
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Obtiene el ID (Cod) del usuario desde los claims
        /// </summary>
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("Cod")
                ?? user.FindFirst("sub")
                ?? user.FindFirst("UserId")
                ?? user.FindFirst("userId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        /// <summary>
        /// Obtiene el ID de la entidad desde los claims.
        /// El Gateway genera el claim como "Entidad" (PascalCase).
        /// </summary>
        public static long GetEntidadId(this ClaimsPrincipal user)
        {
            // Primero buscar "Entidad" (lo que genera el Gateway)
            // Luego fallbacks por compatibilidad
            var claim = user.FindFirst("Entidad")
                ?? user.FindFirst("entidad")
                ?? user.FindFirst("EntidadId")
                ?? user.FindFirst("entidadId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        /// <summary>
        /// Obtiene el nombre de usuario (login) desde los claims
        /// </summary>
        public static string GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("Usuario")?.Value
                ?? user.FindFirst("unique_name")?.Value
                ?? user.FindFirst("name")?.Value
                ?? user.Identity?.Name
                ?? string.Empty;
        }

        /// <summary>
        /// Obtiene el nombre completo del usuario desde los claims
        /// </summary>
        public static string GetNombreCompleto(this ClaimsPrincipal user)
        {
            return user.FindFirst("NombreCompleto")?.Value
                ?? user.FindFirst("nombre_completo")?.Value
                ?? string.Empty;
        }

        /// <summary>
        /// Obtiene el ID de sesión desde los claims
        /// </summary>
        public static string GetSessionId(this ClaimsPrincipal user)
        {
            return user.FindFirst("SessionId")?.Value
                ?? user.FindFirst("session_id")?.Value
                ?? user.FindFirst("sid")?.Value
                ?? string.Empty;
        }

        /// <summary>
        /// Obtiene el nombre de la entidad desde los claims
        /// </summary>
        public static string GetNombreEntidad(this ClaimsPrincipal user)
        {
            return user.FindFirst("NombreEntidad")?.Value
                ?? user.FindFirst("nombre_entidad")?.Value
                ?? string.Empty;
        }

        /// <summary>
        /// Obtiene el email del usuario desde los claims
        /// </summary>
        public static string? GetEmail(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value
                ?? user.FindFirst("Email")?.Value;
        }

        /// <summary>
        /// Obtiene el ID de la oficina del usuario desde los claims
        /// </summary>
        public static long GetOficinaId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("Oficina")
                ?? user.FindFirst("oficina")
                ?? user.FindFirst("OficinaId")
                ?? user.FindFirst("oficinaId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        /// <summary>
        /// Verifica si el usuario tiene un rol específico
        /// </summary>
        public static bool HasRole(this ClaimsPrincipal user, string role)
        {
            return user.IsInRole(role)
                || user.HasClaim(ClaimTypes.Role, role)
                || user.HasClaim("role", role);
        }

        /// <summary>
        /// Obtiene todos los roles del usuario
        /// </summary>
        public static IEnumerable<string> GetRoles(this ClaimsPrincipal user)
        {
            return user.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(user.FindAll("role").Select(c => c.Value))
                .Distinct();
        }
    }
}