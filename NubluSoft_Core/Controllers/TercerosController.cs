using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Extensions;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TercerosController : ControllerBase
    {
        private readonly ITercerosService _service;
        private readonly ILogger<TercerosController> _logger;

        public TercerosController(ITercerosService service, ILogger<TercerosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene terceros con filtros
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosTercerosRequest filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var terceros = await _service.ObtenerConFiltrosAsync(entidadId, filtros);
            return Ok(terceros);
        }

        /// <summary>
        /// Obtiene un tercero por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var tercero = await _service.ObtenerPorIdAsync(id);
            if (tercero == null)
                return NotFound(new { Message = "Tercero no encontrado" });

            return Ok(tercero);
        }

        /// <summary>
        /// Obtiene un tercero con estadísticas de radicados
        /// </summary>
        [HttpGet("{id}/estadisticas")]
        public async Task<IActionResult> GetConEstadisticas(long id)
        {
            var tercero = await _service.ObtenerConEstadisticasAsync(id);
            if (tercero == null)
                return NotFound(new { Message = "Tercero no encontrado" });

            return Ok(tercero);
        }

        /// <summary>
        /// Busca un tercero por tipo y número de documento
        /// </summary>
        [HttpGet("buscar")]
        public async Task<IActionResult> GetByDocumento([FromQuery] string tipoDocumento, [FromQuery] string documento)
        {
            if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(documento))
                return BadRequest(new { Message = "El tipo de documento y el documento son requeridos" });

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var tercero = await _service.ObtenerPorDocumentoAsync(entidadId, tipoDocumento, documento);
            if (tercero == null)
                return NotFound(new { Message = "Tercero no encontrado" });

            return Ok(tercero);
        }

        /// <summary>
        /// Obtiene los radicados de un tercero
        /// </summary>
        [HttpGet("{id}/radicados")]
        public async Task<IActionResult> GetRadicados(long id, [FromQuery] int? limite = 20)
        {
            var radicados = await _service.ObtenerRadicadosAsync(id, limite);
            return Ok(radicados);
        }

        /// <summary>
        /// Verifica si existe un tercero con el documento especificado
        /// </summary>
        [HttpGet("existe")]
        public async Task<IActionResult> ExisteDocumento([FromQuery] string tipoDocumento, [FromQuery] string documento)
        {
            if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(documento))
                return BadRequest(new { Message = "El tipo de documento y el documento son requeridos" });

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var existe = await _service.ExisteDocumentoAsync(entidadId, tipoDocumento, documento);
            return Ok(new { Existe = existe });
        }

        /// <summary>
        /// Crea un nuevo tercero
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearTerceroRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (tercero, error) = await _service.CrearAsync(entidadId, usuarioId, request);

            if (tercero == null)
                return BadRequest(new { Message = error });

            return CreatedAtAction(nameof(GetById), new { id = tercero.Cod }, tercero);
        }

        /// <summary>
        /// Actualiza un tercero existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ActualizarTerceroRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (success, error) = await _service.ActualizarAsync(id, usuarioId, request);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        /// <summary>
        /// Elimina un tercero (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var (success, error) = await _service.EliminarAsync(id);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }
    }
}