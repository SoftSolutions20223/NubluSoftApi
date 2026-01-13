using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Signature.Services;

namespace NubluSoft_Signature.Controllers
{
    /// <summary>
    /// Controller para health checks
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IPostgresConnectionFactory connectionFactory,
            ILogger<HealthController> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Health check básico
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "NubluSoft_Signature",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Health check detallado con verificación de dependencias
        /// </summary>
        [HttpGet("detailed")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetailed()
        {
            var checks = new Dictionary<string, object>();
            var isHealthy = true;

            // Verificar PostgreSQL
            try
            {
                await using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync();

                checks["postgresql"] = new { status = "Healthy", latency = "OK" };
            }
            catch (Exception ex)
            {
                checks["postgresql"] = new { status = "Unhealthy", error = ex.Message };
                isHealthy = false;
                _logger.LogError(ex, "Health check PostgreSQL falló");
            }

            return Ok(new
            {
                status = isHealthy ? "Healthy" : "Unhealthy",
                service = "NubluSoft_Signature",
                timestamp = DateTime.UtcNow,
                checks
            });
        }
    }
}