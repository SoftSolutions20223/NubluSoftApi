using System.Security.Claims;

namespace NubluSoft_Core.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                     ?? user.FindFirst("sub");
            return claim != null ? long.Parse(claim.Value) : 0;
        }

        public static string GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("unique_name")?.Value
                ?? string.Empty;
        }

        public static long GetEntidadId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("entidad");
            return claim != null ? long.Parse(claim.Value) : 0;
        }

        public static string GetNombreCompleto(this ClaimsPrincipal user)
        {
            return user.FindFirst("nombre_completo")?.Value ?? string.Empty;
        }

        public static string GetSessionId(this ClaimsPrincipal user)
        {
            return user.FindFirst("session_id")?.Value ?? string.Empty;
        }
    }
}