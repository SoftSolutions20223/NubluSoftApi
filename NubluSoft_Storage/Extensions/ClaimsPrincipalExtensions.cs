using System.Security.Claims;

namespace NubluSoft_Storage.Extensions
{
    /// <summary>
    /// Extensiones para extraer claims del token JWT
    /// DEBE ser idéntico al Gateway y NubluSoft_Core
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Obtiene el ID del usuario (Cod)
        /// </summary>
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("Cod") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        /// <summary>
        /// Obtiene el ID de la entidad
        /// </summary>
        public static long GetEntidadId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("Entidad");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        /// <summary>
        /// Obtiene el ID de sesión
        /// </summary>
        public static string GetSessionId(this ClaimsPrincipal user)
        {
            return user.FindFirst("SessionId")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Obtiene el nombre de usuario
        /// </summary>
        public static string GetUsuario(this ClaimsPrincipal user)
        {
            return user.FindFirst("Usuario")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Obtiene el nombre completo
        /// </summary>
        public static string GetNombreCompleto(this ClaimsPrincipal user)
        {
            return user.FindFirst("NombreCompleto")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Obtiene el ID de la oficina del usuario
        /// </summary>
        public static long GetOficinaId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("Oficina");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        /// <summary>
        /// Verifica si el usuario tiene un rol específico
        /// </summary>
        public static bool HasRole(this ClaimsPrincipal user, string role)
        {
            return user.IsInRole(role) ||
                   user.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == role);
        }
    }
}