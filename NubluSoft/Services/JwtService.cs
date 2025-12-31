using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NubluSoft.Configuration;
using NubluSoft.Models.DTOs;
using NubluSoft.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NubluSoft.Services
{
    /// <summary>
    /// Servicio para generación y validación de JWT
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<JwtService> _logger;
        private readonly SymmetricSecurityKey _signingKey;

        public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        }

        /// <inheritdoc />
        public string GenerarToken(Usuario usuario, string? nombreEntidad, List<RolUsuarioInfo>? roles, string sessionId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Cod.ToString()),
                new Claim(ClaimTypes.Name, usuario.Usuario_ ?? string.Empty),
                new Claim("Cod", usuario.Cod.ToString()),
                new Claim("Usuario", usuario.Usuario_ ?? string.Empty),
                new Claim("Entidad", usuario.Entidad.ToString()),
                new Claim("NombreCompleto", usuario.NombreCompleto),
                new Claim("NombreEntidad", nombreEntidad ?? string.Empty),
                new Claim("SessionId", sessionId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Agregar roles como claims
            if (roles != null)
            {
                foreach (var rol in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, rol.NombreRol ?? rol.Rol.ToString()));
                    claims.Add(new Claim("RolOficina", $"{rol.Rol}:{rol.Oficina}"));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = ObtenerExpiracionToken(),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogDebug("Token generado para usuario: {Usuario}, expira: {Expiracion}",
                usuario.Usuario_, tokenDescriptor.Expires);

            return tokenString;
        }

        /// <inheritdoc />
        public string GenerarRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <inheritdoc />
        public ClaimsPrincipal? ValidarToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Verificar que el token sea del tipo correcto
                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Token con algoritmo inválido");
                    return null;
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogDebug("Token expirado");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validando token");
                return null;
            }
        }

        /// <inheritdoc />
        public DateTime ObtenerExpiracionToken()
        {
            return DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
        }

        /// <inheritdoc />
        public DateTime ObtenerExpiracionRefreshToken()
        {
            return DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpirationDays);
        }
    }
}