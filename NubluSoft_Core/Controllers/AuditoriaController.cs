using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Extensions;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    /// <summary>
    /// Controller para consulta de auditoría y trazabilidad del sistema
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditoriaController : ControllerBase
    {
        private readonly IAuditoriaService _service;
        private readonly ILogger<AuditoriaController> _logger;

        public AuditoriaController(
            IAuditoriaService service,
            ILogger<AuditoriaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ==================== HISTORIAL DE ACCIONES ====================

        /// <summary>
        /// Obtiene el historial de acciones con filtros y paginación
        /// </summary>
        [HttpGet("historial")]
        [ProducesResponseType(typeof(HistorialAccionesListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistorial([FromQuery] FiltrosAuditoriaRequest filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerHistorialAsync(entidadId, filtros);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene el historial de un registro específico
        /// </summary>
        [HttpGet("historial/{tabla}/{registroCod}")]
        [ProducesResponseType(typeof(IEnumerable<HistorialAccionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistorialRegistro(string tabla, long registroCod)
        {
            var resultado = await _service.ObtenerHistorialRegistroAsync(tabla, registroCod);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene las acciones realizadas por un usuario específico
        /// </summary>
        [HttpGet("historial/usuario/{usuarioId}")]
        [ProducesResponseType(typeof(HistorialAccionesListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAccionesUsuario(long usuarioId, [FromQuery] FiltrosAuditoriaRequest filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerAccionesUsuarioAsync(entidadId, usuarioId, filtros);
            return Ok(resultado);
        }

        // ==================== ARCHIVOS ELIMINADOS ====================

        /// <summary>
        /// Obtiene los archivos eliminados con filtros y paginación
        /// </summary>
        [HttpGet("archivos-eliminados")]
        [ProducesResponseType(typeof(ArchivosEliminadosListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetArchivosEliminados([FromQuery] FiltrosArchivosEliminadosRequest filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerArchivosEliminadosAsync(entidadId, filtros);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene un archivo eliminado por su código original
        /// </summary>
        [HttpGet("archivos-eliminados/{cod}")]
        [ProducesResponseType(typeof(ArchivoEliminadoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetArchivoEliminado(long cod)
        {
            var resultado = await _service.ObtenerArchivoEliminadoAsync(cod);
            if (resultado == null)
                return NotFound(new { Message = "Archivo eliminado no encontrado" });

            return Ok(resultado);
        }

        // ==================== CARPETAS ELIMINADAS ====================

        /// <summary>
        /// Obtiene las carpetas eliminadas con filtros y paginación
        /// </summary>
        [HttpGet("carpetas-eliminadas")]
        [ProducesResponseType(typeof(CarpetasEliminadasListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCarpetasEliminadas([FromQuery] FiltrosCarpetasEliminadasRequest filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerCarpetasEliminadasAsync(entidadId, filtros);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene una carpeta eliminada por su código original
        /// </summary>
        [HttpGet("carpetas-eliminadas/{cod}")]
        [ProducesResponseType(typeof(CarpetaEliminadaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCarpetaEliminada(long cod)
        {
            var resultado = await _service.ObtenerCarpetaEliminadaAsync(cod);
            if (resultado == null)
                return NotFound(new { Message = "Carpeta eliminada no encontrada" });

            return Ok(resultado);
        }

        // ==================== ERROR LOG ====================

        /// <summary>
        /// Obtiene los errores del sistema con filtros y paginación
        /// </summary>
        [HttpGet("errores")]
        [ProducesResponseType(typeof(ErrorLogListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetErrores([FromQuery] FiltrosErrorLogRequest filtros)
        {
            var resultado = await _service.ObtenerErroresAsync(filtros);
            return Ok(resultado);
        }

        // ==================== RESUMEN / DASHBOARD ====================

        /// <summary>
        /// Obtiene un resumen de auditoría para el dashboard
        /// </summary>
        [HttpGet("resumen")]
        [ProducesResponseType(typeof(ResumenAuditoriaDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResumen()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerResumenAsync(entidadId);
            return Ok(resultado);
        }
    }
}