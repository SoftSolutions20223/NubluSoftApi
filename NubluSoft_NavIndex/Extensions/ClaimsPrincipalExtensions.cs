using System.Security.Claims;

namespace NubluSoft_NavIndex.Extensions
{
    /// <summary>
    /// Extensiones para extraer claims del usuario autenticado
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub")
                ?? user.FindFirst("userId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        public static long GetEntidadId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("entidadId")
                ?? user.FindFirst("EntidadId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }

        public static string? GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("name")?.Value
                ?? user.Identity?.Name;
        }

        public static long GetOficinaId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("oficinaId")
                ?? user.FindFirst("OficinaId");

            return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
        }
    }
}