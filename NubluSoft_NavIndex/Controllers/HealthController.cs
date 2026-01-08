using Microsoft.AspNetCore.Mvc;
using NubluSoft_NavIndex.Services;
using StackExchange.Redis;

namespace NubluSoft_NavIndex.Controllers
{
    /// <summary>
    /// Health checks para el servicio NavIndex
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<HealthController> _logger;
        private readonly IConfiguration _configuration;

        public HealthController(
            IPostgresConnectionFactory connectionFactory,
            IConnectionMultiplexer redis,
            ILogger<HealthController> logger,
            IConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _redis = redis;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Health check básico
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "NubluSoft_NavIndex",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Health check detallado con estado de dependencias
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            var postgresOk = await CheckPostgresAsync();
            var redisOk = CheckRedis();

            var isHealthy = postgresOk && redisOk;

            var result = new
            {
                Status = isHealthy ? "Healthy" : "Unhealthy",
                Service = "NubluSoft_NavIndex",
                Version = _configuration["ServiceInfo:Version"] ?? "1.0.0",
                Timestamp = DateTime.UtcNow,
                Dependencies = new
                {
                    PostgreSQL = new
                    {
                        Status = postgresOk ? "Connected" : "Disconnected",
                        Connection = _connectionFactory.GetConnectionInfo()
                    },
                    Redis = new
                    {
                        Status = redisOk ? "Connected" : "Disconnected",
                        Endpoint = _configuration["Redis:ConnectionString"]
                    }
                }
            };

            return isHealthy ? Ok(result) : StatusCode(503, result);
        }

        private async Task<bool> CheckPostgresAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando conexión a PostgreSQL");
                return false;
            }
        }

        private bool CheckRedis()
        {
            try
            {
                var db = _redis.GetDatabase();
                db.Ping();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando conexión a Redis");
                return false;
            }
        }
    }
}