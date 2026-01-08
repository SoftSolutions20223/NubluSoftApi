using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_NavIndex.Extensions;
using NubluSoft_NavIndex.Services;

namespace NubluSoft_NavIndex.Controllers
{
    /// <summary>
    /// Controller para navegación documental
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NavegacionController : ControllerBase
    {
        private readonly INavIndexService _navIndexService;
        private readonly ILogger<NavegacionController> _logger;

        public NavegacionController(
            INavIndexService navIndexService,
            ILogger<NavegacionController> logger)
        {
            _navIndexService = navIndexService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la estructura documental completa (comprimida con gzip)
        /// </summary>
        [HttpGet("estructura")]
        public async Task<IActionResult> GetEstructura()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            try
            {
                _logger.LogInformation("Solicitando estructura para entidad {EntidadId}", entidadId);

                var datosComprimidos = await _navIndexService.ObtenerEstructuraComprimidaAsync(entidadId);

                // Retornar como gzip
                Response.Headers.Append("Content-Encoding", "gzip");
                Response.Headers.Append("Content-Type", "application/json");

                return File(datosComprimidos, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estructura para entidad {EntidadId}", entidadId);
                return StatusCode(500, new { Message = "Error interno obteniendo estructura" });
            }
        }

        /// <summary>
        /// Obtiene solo la versión de la estructura (para verificar si cambió)
        /// </summary>
        [HttpGet("estructura/version")]
        public async Task<IActionResult> GetVersion()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            try
            {
                var version = await _navIndexService.ObtenerVersionAsync(entidadId);
                return Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo versión para entidad {EntidadId}", entidadId);
                return StatusCode(500, new { Message = "Error interno obteniendo versión" });
            }
        }

        /// <summary>
        /// Obtiene el índice (contenido) de una carpeta específica (comprimido con gzip)
        /// </summary>
        [HttpGet("indice/{carpetaId}")]
        public async Task<IActionResult> GetIndice(long carpetaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            try
            {
                _logger.LogInformation("Solicitando índice para carpeta {CarpetaId}", carpetaId);

                var datosComprimidos = await _navIndexService.ObtenerIndiceComprimidoAsync(entidadId, carpetaId);

                if (datosComprimidos == null)
                    return NotFound(new { Message = "Carpeta no encontrada" });

                // Retornar como gzip
                Response.Headers.Append("Content-Encoding", "gzip");
                Response.Headers.Append("Content-Type", "application/json");

                return File(datosComprimidos, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo índice para carpeta {CarpetaId}", carpetaId);
                return StatusCode(500, new { Message = "Error interno obteniendo índice" });
            }
        }

        /// <summary>
        /// Obtiene un nodo específico por su ID
        /// </summary>
        [HttpGet("nodo/{carpetaId}")]
        public async Task<IActionResult> GetNodo(long carpetaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            try
            {
                var nodo = await _navIndexService.ObtenerNodoAsync(carpetaId);

                if (nodo == null)
                    return NotFound(new { Message = "Nodo no encontrado" });

                return Ok(nodo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo nodo {CarpetaId}", carpetaId);
                return StatusCode(500, new { Message = "Error interno obteniendo nodo" });
            }
        }

        /// <summary>
        /// Fuerza la regeneración de la estructura (uso interno/admin)
        /// </summary>
        [HttpPost("regenerar")]
        public async Task<IActionResult> Regenerar()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            try
            {
                _logger.LogInformation("Regenerando estructura para entidad {EntidadId}", entidadId);

                var resultado = await _navIndexService.RegenerarEstructuraAsync(entidadId);

                if (resultado.Exito)
                    return Ok(resultado);

                return BadRequest(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerando estructura para entidad {EntidadId}", entidadId);
                return StatusCode(500, new { Message = "Error interno regenerando estructura" });
            }
        }

        /// <summary>
        /// Invalida el caché de la estructura (uso interno)
        /// </summary>
        [HttpDelete("cache")]
        public async Task<IActionResult> InvalidarCache()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            try
            {
                await _navIndexService.InvalidarCacheAsync(entidadId);
                return Ok(new { Message = "Caché invalidado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidando caché para entidad {EntidadId}", entidadId);
                return StatusCode(500, new { Message = "Error interno invalidando caché" });
            }
        }
    }
}