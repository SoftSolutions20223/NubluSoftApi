using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Storage.Services;


namespace NubluSoft_Storage.Controllers
{
    /// <summary>
    /// Controller para health checks
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IGcsStorageService _gcsService;

        public HealthController(
            ILogger<HealthController> logger,
            IGcsStorageService gcsService)
        {
            _logger = logger;
            _gcsService = gcsService;
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
                Status = "Healthy",
                Service = "NubluSoft_Storage",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            });
        }

        /// <summary>
        /// Health check detallado (incluye verificación de GCS)
        /// </summary>
        [HttpGet("detailed")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetailed()
        {
            var gcsStatus = "Unknown";
            var gcsMessage = "";

            try
            {
                // Verificar conectividad con GCS
                var testObject = $"health-check-{Guid.NewGuid():N}";
                var exists = await _gcsService.ExistsAsync(testObject);
                gcsStatus = "Healthy";
                gcsMessage = $"Bucket: {_gcsService.BucketName}";
            }
            catch (Exception ex)
            {
                gcsStatus = "Unhealthy";
                gcsMessage = ex.Message;
                _logger.LogWarning(ex, "GCS health check failed");
            }

            var isHealthy = gcsStatus == "Healthy";

            return isHealthy ? Ok(new
            {
                Status = "Healthy",
                Service = "NubluSoft_Storage",
                Timestamp = DateTime.UtcNow,
                Components = new
                {
                    GoogleCloudStorage = new { Status = gcsStatus, Message = gcsMessage }
                }
            }) : StatusCode(503, new
            {
                Status = "Unhealthy",
                Service = "NubluSoft_Storage",
                Timestamp = DateTime.UtcNow,
                Components = new
                {
                    GoogleCloudStorage = new { Status = gcsStatus, Message = gcsMessage }
                }
            });
        }
    }
}