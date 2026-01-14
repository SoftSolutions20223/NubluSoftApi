using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Extensions;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    /// <summary>
    /// Controller para gestión de permisos de TRD por oficina
    /// </summary>
    [ApiController]
    [Route("api/oficinas-trd")]
    [Authorize]
    public class OficinaTRDController : ControllerBase
    {
        private readonly IOficinaTRDService _service;
        private readonly ILogger<OficinaTRDController> _logger;

        public OficinaTRDController(
            IOficinaTRDService service,
            ILogger<OficinaTRDController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene todas las asignaciones de TRD a oficinas
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OficinaTRDDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosOficinaTRDRequest? filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerAsignacionesAsync(entidadId, filtros);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene las TRDs asignadas a una oficina específica
        /// </summary>
        [HttpGet("oficina/{oficinaId}")]
        [ProducesResponseType(typeof(IEnumerable<TRDConPermisosDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTRDsDeOficina(long oficinaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerTRDsDeOficinaAsync(entidadId, oficinaId);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene las oficinas que tienen asignada una TRD específica
        /// </summary>
        [HttpGet("trd/{trdId}")]
        [ProducesResponseType(typeof(IEnumerable<OficinaTRDDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOficinasConTRD(long trdId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerOficinasConTRDAsync(entidadId, trdId);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene resumen de permisos de una oficina
        /// </summary>
        [HttpGet("oficina/{oficinaId}/resumen")]
        [ProducesResponseType(typeof(ResumenPermisosOficinaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResumenOficina(long oficinaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerResumenOficinaAsync(entidadId, oficinaId);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene todas las TRDs con indicador de si la oficina tiene acceso
        /// </summary>
        [HttpGet("oficina/{oficinaId}/todas-trd")]
        [ProducesResponseType(typeof(IEnumerable<TRDConPermisosDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTRDsConEstadoAcceso(long oficinaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerTRDsConEstadoAccesoAsync(entidadId, oficinaId);
            return Ok(resultado);
        }

        // ==================== VERIFICACIÓN DE ACCESO ====================

        /// <summary>
        /// Verifica si una oficina tiene acceso a una carpeta
        /// </summary>
        [HttpGet("verificar/carpeta/{carpetaId}")]
        [ProducesResponseType(typeof(VerificacionAccesoCarpetaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerificarAccesoCarpeta(long carpetaId, [FromQuery] long oficinaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.VerificarAccesoCarpetaAsync(entidadId, oficinaId, carpetaId);
            return Ok(resultado);
        }

        /// <summary>
        /// Verifica si una oficina tiene acceso a una TRD
        /// </summary>
        [HttpGet("verificar/trd/{trdId}")]
        [ProducesResponseType(typeof(VerificacionAccesoCarpetaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerificarAccesoTRD(long trdId, [FromQuery] long oficinaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.VerificarAccesoTRDAsync(entidadId, oficinaId, trdId);
            return Ok(resultado);
        }

        // ==================== ASIGNACIÓN ====================

        /// <summary>
        /// Asigna una TRD a una oficina
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResultadoOficinaTRDDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AsignarTRD([FromBody] AsignarTRDAOficinaRequest request)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.AsignarTRDAsync(entidadId, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return CreatedAtAction(nameof(GetTRDsDeOficina), new { oficinaId = request.OficinaId }, resultado);
        }

        /// <summary>
        /// Asigna múltiples TRDs a una oficina
        /// </summary>
        [HttpPost("masivo")]
        [ProducesResponseType(typeof(ResultadoAsignacionMasivaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AsignarTRDsMasivo([FromBody] AsignarTRDsMasivoRequest request)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.AsignarTRDsMasivoAsync(entidadId, request);
            return Ok(resultado);
        }

        /// <summary>
        /// Actualiza los permisos de una asignación existente
        /// </summary>
        [HttpPut("oficina/{oficinaId}/trd/{trdId}")]
        [ProducesResponseType(typeof(ResultadoOficinaTRDDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActualizarPermisos(
            long oficinaId,
            long trdId,
            [FromBody] ActualizarPermisosOficinaTRDRequest request)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ActualizarPermisosAsync(entidadId, oficinaId, trdId, request);

            if (!resultado.Exito)
                return NotFound(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Revoca el acceso de una oficina a una TRD
        /// </summary>
        [HttpDelete("oficina/{oficinaId}/trd/{trdId}")]
        [ProducesResponseType(typeof(ResultadoOficinaTRDDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevocarTRD(long oficinaId, long trdId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.RevocarTRDAsync(entidadId, oficinaId, trdId);

            if (!resultado.Exito)
                return NotFound(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Revoca todos los accesos de una oficina
        /// </summary>
        [HttpDelete("oficina/{oficinaId}/todos")]
        [ProducesResponseType(typeof(ResultadoAsignacionMasivaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> RevocarTodos(long oficinaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.RevocarTodosAsync(entidadId, oficinaId);
            return Ok(resultado);
        }
    }
}