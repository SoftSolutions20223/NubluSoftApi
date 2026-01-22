using Microsoft.Extensions.Options;
using NubluSoft_NavIndex.Configuration;
using StackExchange.Redis;

namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Implementación del servicio de caché en Redis
    /// </summary>
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly RedisSettings _settings;
        private readonly ILogger<RedisCacheService> _logger;

        // Prefijos de claves
        private const string PrefixEstructura = "navindex:estructura:";
        private const string PrefixVersion = "navindex:version:";
        private const string PrefixIndice = "navindex:indice:";
        private const string PrefixLock = "navindex:lock:";

        public RedisCacheService(
            IConnectionMultiplexer redis,
            IOptions<RedisSettings> settings,
            ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<byte[]?> ObtenerEstructuraComprimidaAsync(long entidadId)
        {
            try
            {
                var key = $"{PrefixEstructura}{entidadId}";
                var data = await _db.StringGetAsync(key);

                if (data.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache MISS para estructura entidad {EntidadId}", entidadId);
                    return null;
                }

                _logger.LogDebug("Cache HIT para estructura entidad {EntidadId}", entidadId);
                return (byte[])data!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estructura de Redis para entidad {EntidadId}", entidadId);
                return null;
            }
        }

        public async Task GuardarEstructuraComprimidaAsync(long entidadId, byte[] datosComprimidos, string version)
        {
            try
            {
                var keyEstructura = $"{PrefixEstructura}{entidadId}";
                var keyVersion = $"{PrefixVersion}{entidadId}";
                var expiracion = TimeSpan.FromMinutes(_settings.CacheExpirationMinutes);

                var batch = _db.CreateBatch();

                // Guardar estructura comprimida
                _ = batch.StringSetAsync(keyEstructura, datosComprimidos, expiracion);

                // Guardar versión
                _ = batch.StringSetAsync(keyVersion, version, expiracion);

                batch.Execute();

                _logger.LogInformation(
                    "Estructura guardada en Redis para entidad {EntidadId}, version {Version}, tamaño {Size} bytes",
                    entidadId, version, datosComprimidos.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando estructura en Redis para entidad {EntidadId}", entidadId);
            }
        }

        public async Task<string?> ObtenerVersionAsync(long entidadId)
        {
            try
            {
                var key = $"{PrefixVersion}{entidadId}";
                var version = await _db.StringGetAsync(key);

                return version.IsNullOrEmpty ? null : version.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo versión de Redis para entidad {EntidadId}", entidadId);
                return null;
            }
        }

        public async Task InvalidarCacheAsync(long entidadId)
        {
            try
            {
                var keyEstructura = $"{PrefixEstructura}{entidadId}";
                var keyVersion = $"{PrefixVersion}{entidadId}";

                var batch = _db.CreateBatch();
                _ = batch.KeyDeleteAsync(keyEstructura);
                _ = batch.KeyDeleteAsync(keyVersion);
                batch.Execute();

                _logger.LogInformation("Cache invalidado para entidad {EntidadId}", entidadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidando cache en Redis para entidad {EntidadId}", entidadId);
            }
        }

        public async Task<byte[]?> ObtenerIndiceComprimidoAsync(long entidadId, long carpetaId)
        {
            try
            {
                var key = $"{PrefixIndice}{entidadId}:{carpetaId}";
                var data = await _db.StringGetAsync(key);

                if (data.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache MISS para índice carpeta {CarpetaId}", carpetaId);
                    return null;
                }

                _logger.LogDebug("Cache HIT para índice carpeta {CarpetaId}", carpetaId);
                return (byte[])data!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo índice de Redis para carpeta {CarpetaId}", carpetaId);
                return null;
            }
        }

        public async Task GuardarIndiceComprimidoAsync(long entidadId, long carpetaId, byte[] datosComprimidos)
        {
            try
            {
                var key = $"{PrefixIndice}{entidadId}:{carpetaId}";
                var expiracion = TimeSpan.FromMinutes(_settings.CacheExpirationMinutes);

                await _db.StringSetAsync(key, datosComprimidos, expiracion);

                _logger.LogDebug(
                    "Índice guardado en Redis para carpeta {CarpetaId}, tamaño {Size} bytes",
                    carpetaId, datosComprimidos.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando índice en Redis para carpeta {CarpetaId}", carpetaId);
            }
        }

        public async Task InvalidarIndiceAsync(long entidadId, long carpetaId)
        {
            try
            {
                var key = $"{PrefixIndice}{entidadId}:{carpetaId}";
                await _db.KeyDeleteAsync(key);

                _logger.LogDebug("Índice invalidado para carpeta {CarpetaId}", carpetaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidando índice en Redis para carpeta {CarpetaId}", carpetaId);
            }
        }

        public async Task<string?> AdquirirLockRegeneracionAsync(long entidadId, TimeSpan lockDuration)
        {
            try
            {
                var key = $"{PrefixLock}{entidadId}";
                var token = Guid.NewGuid().ToString();

                // SET NX (solo si no existe) con expiración
                var acquired = await _db.StringSetAsync(key, token, lockDuration, When.NotExists);

                if (acquired)
                {
                    _logger.LogDebug("Lock adquirido para entidad {EntidadId}, token {Token}", entidadId, token);
                    return token;
                }

                _logger.LogDebug("Lock NO adquirido para entidad {EntidadId} (ya existe)", entidadId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adquiriendo lock para entidad {EntidadId}", entidadId);
                return null;
            }
        }

        public async Task LiberarLockRegeneracionAsync(long entidadId, string lockToken)
        {
            try
            {
                var key = $"{PrefixLock}{entidadId}";

                // Solo liberar si el token coincide (evita liberar lock de otro proceso)
                var script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                var result = await _db.ScriptEvaluateAsync(script,
                    new RedisKey[] { key },
                    new RedisValue[] { lockToken });

                if ((int)result == 1)
                {
                    _logger.LogDebug("Lock liberado para entidad {EntidadId}", entidadId);
                }
                else
                {
                    _logger.LogWarning("Lock NO liberado para entidad {EntidadId} (token no coincide o expiró)", entidadId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liberando lock para entidad {EntidadId}", entidadId);
            }
        }

        public async Task<bool> ExisteLockRegeneracionAsync(long entidadId)
        {
            try
            {
                var key = $"{PrefixLock}{entidadId}";
                return await _db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando lock para entidad {EntidadId}", entidadId);
                return false;
            }
        }
    }
}