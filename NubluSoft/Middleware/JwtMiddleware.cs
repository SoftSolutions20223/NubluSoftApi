using NubluSoft.Extensions;
using NubluSoft.Services;

namespace NubluSoft.Middleware
{
    /// <summary>
    /// Middleware para validar sesiones en Redis además del JWT
    /// </summary>
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IRedisSessionService sessionService)
        {
            // Si el usuario está autenticado, verificar que la sesión exista en Redis
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var sessionId = context.User.GetSessionId();

                if (!string.IsNullOrEmpty(sessionId))
                {
                    var sesionValida = await sessionService.SesionValidaAsync(sessionId);

                    if (!sesionValida)
                    {
                        _logger.LogWarning("Sesión no encontrada en Redis: {SessionId}", sessionId);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            Success = false,
                            Message = "Sesión expirada o inválida"
                        });
                        return;
                    }

                    // Actualizar última actividad (fire and forget)
                    _ = sessionService.ActualizarActividadAsync(sessionId);
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extensión para registrar el middleware
    /// </summary>
    public static class JwtMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtSessionValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtMiddleware>();
        }
    }
}