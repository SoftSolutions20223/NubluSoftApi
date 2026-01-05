using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NubluSoft.Configuration;
using NubluSoft.Models.DTOs;
using StackExchange.Redis;

namespace NubluSoft.Services
{
    /// <summary>
    /// Servicio para gestión de sesiones en Redis
    /// </summary>
    public class RedisSessionService : IRedisSessionService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly RedisSettings _settings;
        private readonly ILogger<RedisSessionService> _logger;
        private readonly string _prefix;

        public RedisSessionService(
            IConnectionMultiplexer redis,
            IOptions<RedisSettings> settings,
            ILogger<RedisSessionService> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _settings = settings.Value;
            _logger = logger;
            _prefix = _settings.InstanceName;
        }

        // Keys en Redis:
        // session:{sessionId} -> UserSessionDto (JSON)
        // user_sessions:{usuarioId} -> Set de sessionIds
        // refresh_token:{refreshToken} -> sessionId

        /// <inheritdoc />
        public async Task GuardarSesionAsync(UserSessionDto session)
        {
            try
            {
                var sessionKey = $"{_prefix}session:{session.SessionId}";
                var userSessionsKey = $"{_prefix}user_sessions:{session.Cod}";
                var refreshTokenKey = $"{_prefix}refresh_token:{session.RefreshToken}";

                var sessionJson = JsonConvert.SerializeObject(session);
                var expiration = TimeSpan.FromMinutes(_settings.SessionExpirationMinutes);

                // Guardar sesión
                await _db.StringSetAsync(sessionKey, sessionJson, expiration);

                // Agregar a set de sesiones del usuario
                await _db.SetAddAsync(userSessionsKey, session.SessionId);
                await _db.KeyExpireAsync(userSessionsKey, expiration);

                // Mapear refresh token a session id
                await _db.StringSetAsync(refreshTokenKey, session.SessionId,
    TimeSpan.FromDays(7));

                _logger.LogDebug("Sesión guardada en Redis: {SessionId} para usuario: {Usuario}",
                    session.SessionId, session.Usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando sesión en Redis: {SessionId}", session.SessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<UserSessionDto?> ObtenerSesionAsync(string sessionId)
        {
            try
            {
                var sessionKey = $"{_prefix}session:{sessionId}";
                var sessionJson = await _db.StringGetAsync(sessionKey);

                if (sessionJson.IsNullOrEmpty)
                {
                    _logger.LogDebug("Sesión no encontrada en Redis: {SessionId}", sessionId);
                    return null;
                }

                return JsonConvert.DeserializeObject<UserSessionDto>(sessionJson!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo sesión de Redis: {SessionId}", sessionId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<UserSessionDto?> ObtenerSesionPorRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var refreshTokenKey = $"{_prefix}refresh_token:{refreshToken}";
                var sessionId = await _db.StringGetAsync(refreshTokenKey);

                if (sessionId.IsNullOrEmpty)
                {
                    _logger.LogDebug("Refresh token no encontrado en Redis");
                    return null;
                }

                return await ObtenerSesionAsync(sessionId!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo sesión por refresh token");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task ActualizarActividadAsync(string sessionId)
        {
            try
            {
                var session = await ObtenerSesionAsync(sessionId);
                if (session != null)
                {
                    session.LastActivity = DateTime.UtcNow;
                    await GuardarSesionAsync(session);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando actividad de sesión: {SessionId}", sessionId);
            }
        }

        /// <inheritdoc />
        public async Task EliminarSesionAsync(string sessionId)
        {
            try
            {
                var session = await ObtenerSesionAsync(sessionId);
                if (session == null) return;

                var sessionKey = $"{_prefix}session:{sessionId}";
                var userSessionsKey = $"{_prefix}user_sessions:{session.Cod}";
                var refreshTokenKey = $"{_prefix}refresh_token:{session.RefreshToken}";

                // Eliminar todas las keys relacionadas
                await _db.KeyDeleteAsync(sessionKey);
                await _db.SetRemoveAsync(userSessionsKey, sessionId);
                await _db.KeyDeleteAsync(refreshTokenKey);

                _logger.LogDebug("Sesión eliminada de Redis: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando sesión de Redis: {SessionId}", sessionId);
            }
        }

        /// <inheritdoc />
        public async Task EliminarTodasSesionesUsuarioAsync(long usuarioId)
        {
            try
            {
                var userSessionsKey = $"{_prefix}user_sessions:{usuarioId}";
                var sessionIds = await _db.SetMembersAsync(userSessionsKey);

                foreach (var sessionId in sessionIds)
                {
                    await EliminarSesionAsync(sessionId!);
                }

                await _db.KeyDeleteAsync(userSessionsKey);

                _logger.LogDebug("Todas las sesiones eliminadas para usuario: {UsuarioId}", usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando sesiones del usuario: {UsuarioId}", usuarioId);
            }
        }

        /// <inheritdoc />
        public async Task<bool> SesionValidaAsync(string sessionId)
        {
            try
            {
                var sessionKey = $"{_prefix}session:{sessionId}";
                return await _db.KeyExistsAsync(sessionKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando sesión: {SessionId}", sessionId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<UserSessionDto>> ObtenerSesionesUsuarioAsync(long usuarioId)
        {
            try
            {
                var userSessionsKey = $"{_prefix}user_sessions:{usuarioId}";
                var sessionIds = await _db.SetMembersAsync(userSessionsKey);
                var sessions = new List<UserSessionDto>();

                foreach (var sessionId in sessionIds)
                {
                    var session = await ObtenerSesionAsync(sessionId!);
                    if (session != null)
                    {
                        sessions.Add(session);
                    }
                }

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo sesiones del usuario: {UsuarioId}", usuarioId);
                return new List<UserSessionDto>();
            }
        }
    }
}
