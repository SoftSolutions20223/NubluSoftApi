using System.Security.Claims;

namespace NubluSoft_Signature.Extensions
{
    /// <summary>
    /// Extensiones para obtener claims del usuario autenticado
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Obtiene el ID del usuario desde los claims
        /// </summary>
        public static long? GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub")
                ?? user.FindFirst("UserId")
                ?? user.FindFirst("Cod");

            if (claim != null && long.TryParse(claim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Obtiene el ID de la entidad desde los claims
        /// </summary>
        public static long? GetEntidadId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("Entidad") ?? user.FindFirst("entidad");

            if (claim != null && long.TryParse(claim.Value, out var entidadId))
            {
                return entidadId;
            }

            return null;
        }

        /// <summary>
        /// Obtiene el nombre de usuario desde los claims
        /// </summary>
        public static string? GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("Usuario")?.Value
                ?? user.FindFirst("name")?.Value;
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
        /// Obtiene el nombre completo del usuario
        /// </summary>
        public static string? GetFullName(this ClaimsPrincipal user)
        {
            var nombres = user.FindFirst("Nombres")?.Value;
            var apellidos = user.FindFirst("Apellidos")?.Value;

            if (!string.IsNullOrEmpty(nombres) && !string.IsNullOrEmpty(apellidos))
            {
                return $"{nombres} {apellidos}";
            }

            return nombres ?? apellidos ?? user.GetUserName();
        }
    }
}