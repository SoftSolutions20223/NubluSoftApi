using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NubluSoft_Core.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return Ok(new
            {
                Status = "OK",
                Service = "NubluSoft_Core",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}