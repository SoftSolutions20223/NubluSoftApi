using System.Security.Claims;

namespace NubluSoft_NavIndex.Extensions
{
    /// <summary>
    /// Extensiones para extraer claims del usuario autenticado
    /// DEBE coincidir con los claims generados por el Gateway
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub")
                ?? user.FindFirst("Cod")
                ?? user.FindFirst("userId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        public static long GetEntidadId(this ClaimsPrincipal user)
        {
            // Buscar todos los posibles nombres de claim para entidad
            var claim = user.FindFirst("Entidad")      // Gateway usa este
                ?? user.FindFirst("entidad")           // Core usa este
                ?? user.FindFirst("entidadId")
                ?? user.FindFirst("EntidadId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        public static string? GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("Usuario")?.Value
                ?? user.FindFirst("name")?.Value
                ?? user.Identity?.Name;
        }

        public static string GetSessionId(this ClaimsPrincipal user)
        {
            return user.FindFirst("SessionId")?.Value
                ?? user.FindFirst("session_id")?.Value
                ?? user.FindFirst("sid")?.Value
                ?? string.Empty;
        }

        public static string GetNombreCompleto(this ClaimsPrincipal user)
        {
            return user.FindFirst("NombreCompleto")?.Value
                ?? user.FindFirst("nombre_completo")?.Value
                ?? string.Empty;
        }

        public static long GetOficinaId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("Oficina")
                ?? user.FindFirst("oficinaId")
                ?? user.FindFirst("OficinaId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }
    }
}