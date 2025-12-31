using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft.Extensions;
using NubluSoft.Models.DTOs;
using NubluSoft.Services;

namespace NubluSoft.Controllers
{
    /// <summary>
    /// Controller de autenticación - Gateway
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly IRedisSessionService _sessionService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IJwtService jwtService,
            IRedisSessionService sessionService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _jwtService = jwtService;
            _sessionService = sessionService;
            _logger = logger;
        }

        /// <summary>
        /// Iniciar sesión
        /// </summary>
        /// <param name="request">Credenciales de usuario</param>
        /// <returns>Token JWT y datos del usuario</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validar modelo
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Datos de entrada inválidos"
                    });
                }

                // Validar credenciales contra PostgreSQL
                var (usuario, error) = await _authService.ValidarCredencialesAsync(
                    request.Usuario,
                    request.Contraseña);

                if (usuario == null)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = error ?? "Credenciales inválidas"
                    });
                }

                // Obtener roles del usuario
                var roles = await _authService.ObtenerRolesUsuarioAsync(usuario.Cod);

                // Generar SessionId único
                var sessionId = Guid.NewGuid().ToString();

                // Generar tokens
                var token = _jwtService.GenerarToken(usuario, usuario.NombreEntidad, roles, sessionId);
                var refreshToken = _jwtService.GenerarRefreshToken();
                var tokenExpiration = _jwtService.ObtenerExpiracionToken();
                var refreshExpiration = _jwtService.ObtenerExpiracionRefreshToken();

                // Crear sesión para Redis
                var session = new UserSessionDto
                {
                    SessionId = sessionId,
                    Cod = usuario.Cod,
                    Nombres = usuario.Nombres,
                    Apellidos = usuario.Apellidos,
                    Usuario = usuario.Usuario_,
                    Entidad = usuario.Entidad,
                    NombreEntidad = usuario.NombreEntidad,
                    Token = token,
                    RefreshToken = refreshToken,
                    TokenExpiration = tokenExpiration,
                    RefreshTokenExpiration = refreshExpiration,
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    Roles = roles
                };

                // Guardar sesión en Redis
                await _sessionService.GuardarSesionAsync(session);

                // Actualizar token en BD
                await _authService.ActualizarTokenUsuarioAsync(usuario.Cod, token);

                _logger.LogInformation("Login exitoso para usuario: {Usuario} desde IP: {IP}",
                    usuario.Usuario_, session.IpAddress);

                // Respuesta exitosa
                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Inicio de sesión exitoso",
                    Token = token,
                    RefreshToken = refreshToken,
                    TokenExpiration = tokenExpiration,
                    User = new UserInfo
                    {
                        Cod = usuario.Cod,
                        Nombres = usuario.Nombres,
                        Apellidos = usuario.Apellidos,
                        NombreCompleto = usuario.NombreCompleto,
                        Usuario = usuario.Usuario_,
                        Entidad = usuario.Entidad,
                        NombreEntidad = usuario.NombreEntidad,
                        DescripcionEstado = usuario.DescripcionEstado,
                        Roles = roles
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login para usuario: {Usuario}", request.Usuario);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Cerrar sesión
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var sessionId = User.GetSessionId();
                var userId = User.GetUserId();

                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Eliminar sesión de Redis
                    await _sessionService.EliminarSesionAsync(sessionId);
                }

                if (userId > 0)
                {
                    // Limpiar token en BD
                    await _authService.LimpiarTokenUsuarioAsync(userId);
                }

                _logger.LogInformation("Logout exitoso para usuario: {UserId}", userId);

                return Ok(new { Success = true, Message = "Sesión cerrada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en logout");
                return StatusCode(500, new { Success = false, Message = "Error al cerrar sesión" });
            }
        }

        /// <summary>
        /// Cerrar todas las sesiones del usuario
        /// </summary>
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            try
            {
                var userId = User.GetUserId();

                if (userId > 0)
                {
                    // Eliminar todas las sesiones de Redis
                    await _sessionService.EliminarTodasSesionesUsuarioAsync(userId);

                    // Limpiar token en BD
                    await _authService.LimpiarTokenUsuarioAsync(userId);
                }

                _logger.LogInformation("Logout de todas las sesiones para usuario: {UserId}", userId);

                return Ok(new { Success = true, Message = "Todas las sesiones han sido cerradas" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en logout-all");
                return StatusCode(500, new { Success = false, Message = "Error al cerrar sesiones" });
            }
        }

        /// <summary>
        /// Renovar token usando refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Datos de entrada inválidos"
                    });
                }

                // Buscar sesión por refresh token
                var session = await _sessionService.ObtenerSesionPorRefreshTokenAsync(request.RefreshToken);

                if (session == null)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Refresh token inválido o expirado"
                    });
                }

                // Verificar que el refresh token no haya expirado
                if (session.RefreshTokenExpiration < DateTime.UtcNow)
                {
                    await _sessionService.EliminarSesionAsync(session.SessionId);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Refresh token expirado, inicie sesión nuevamente"
                    });
                }

                // Obtener datos actualizados del usuario
                var (usuario, error) = await _authService.ValidarCredencialesAsync(
                    session.Usuario ?? string.Empty,
                    string.Empty);

                // Si no podemos validar, usar datos de la sesión
                var roles = session.Roles ?? new List<RolUsuarioInfo>();

                // Generar nuevo SessionId
                var newSessionId = Guid.NewGuid().ToString();

                // Generar nuevos tokens
                var newToken = _jwtService.GenerarToken(
                    new Models.Entities.Usuario
                    {
                        Cod = session.Cod,
                        Nombres = session.Nombres,
                        Apellidos = session.Apellidos,
                        Usuario_ = session.Usuario ?? string.Empty,
                        Entidad = session.Entidad
                    },
                    session.NombreEntidad,
                    roles,
                    newSessionId);

                var newRefreshToken = _jwtService.GenerarRefreshToken();
                var tokenExpiration = _jwtService.ObtenerExpiracionToken();
                var refreshExpiration = _jwtService.ObtenerExpiracionRefreshToken();

                // Eliminar sesión anterior
                await _sessionService.EliminarSesionAsync(session.SessionId);

                // Crear nueva sesión
                var newSession = new UserSessionDto
                {
                    SessionId = newSessionId,
                    Cod = session.Cod,
                    Nombres = session.Nombres,
                    Apellidos = session.Apellidos,
                    Usuario = session.Usuario,
                    Entidad = session.Entidad,
                    NombreEntidad = session.NombreEntidad,
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    TokenExpiration = tokenExpiration,
                    RefreshTokenExpiration = refreshExpiration,
                    LoginTime = session.LoginTime,
                    LastActivity = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    Roles = roles
                };

                // Guardar nueva sesión
                await _sessionService.GuardarSesionAsync(newSession);

                // Actualizar token en BD
                await _authService.ActualizarTokenUsuarioAsync(session.Cod, newToken);

                _logger.LogInformation("Token renovado para usuario: {Usuario}", session.Usuario);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Token renovado exitosamente",
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    TokenExpiration = tokenExpiration,
                    User = new UserInfo
                    {
                        Cod = session.Cod,
                        Nombres = session.Nombres,
                        Apellidos = session.Apellidos,
                        NombreCompleto = $"{session.Nombres} {session.Apellidos}".Trim(),
                        Usuario = session.Usuario,
                        Entidad = session.Entidad,
                        NombreEntidad = session.NombreEntidad,
                        Roles = roles
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renovando token");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Obtener información del usuario actual
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var sessionId = User.GetSessionId();

                if (string.IsNullOrEmpty(sessionId))
                {
                    return Unauthorized(new { Success = false, Message = "Sesión no válida" });
                }

                var session = await _sessionService.ObtenerSesionAsync(sessionId);

                if (session == null)
                {
                    return Unauthorized(new { Success = false, Message = "Sesión expirada" });
                }

                // Actualizar última actividad
                await _sessionService.ActualizarActividadAsync(sessionId);

                return Ok(new
                {
                    Success = true,
                    User = new UserInfo
                    {
                        Cod = session.Cod,
                        Nombres = session.Nombres,
                        Apellidos = session.Apellidos,
                        NombreCompleto = $"{session.Nombres} {session.Apellidos}".Trim(),
                        Usuario = session.Usuario,
                        Entidad = session.Entidad,
                        NombreEntidad = session.NombreEntidad,
                        Roles = session.Roles
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario actual");
                return StatusCode(500, new { Success = false, Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener sesiones activas del usuario
        /// </summary>
        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> GetActiveSessions()
        {
            try
            {
                var userId = User.GetUserId();
                var currentSessionId = User.GetSessionId();

                var sessions = await _sessionService.ObtenerSesionesUsuarioAsync(userId);

                var result = sessions.Select(s => new
                {
                    s.SessionId,
                    s.LoginTime,
                    s.LastActivity,
                    s.IpAddress,
                    s.UserAgent,
                    IsCurrentSession = s.SessionId == currentSessionId
                });

                return Ok(new { Success = true, Sessions = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo sesiones activas");
                return StatusCode(500, new { Success = false, Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Verificar si el token es válido (health check de autenticación)
        /// </summary>
        [HttpGet("verify")]
        [Authorize]
        public IActionResult VerifyToken()
        {
            return Ok(new
            {
                Success = true,
                Message = "Token válido",
                UserId = User.GetUserId(),
                UserName = User.GetUserName(),
                Entidad = User.GetEntidadId()
            });
        }
    }
}