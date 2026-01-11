using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NubluSoft.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "NubluSoft Gateway",
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed(
            [FromServices] StackExchange.Redis.IConnectionMultiplexer redis)
        {
            var redisOk = false;
            try
            {
                var db = redis.GetDatabase();
                await db.PingAsync();
                redisOk = true;
            }
            catch { }

            return Ok(new
            {
                Status = redisOk ? "Healthy" : "Degraded",
                Service = "NubluSoft Gateway",
                Timestamp = DateTime.UtcNow,
                Dependencies = new
                {
                    Redis = redisOk ? "Connected" : "Disconnected"
                }
            });
        }
    }
}