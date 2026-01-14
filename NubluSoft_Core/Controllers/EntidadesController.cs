using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    /// <summary>
    /// Controller para gestión de entidades del sistema
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EntidadesController : ControllerBase
    {
        private readonly IEntidadesService _service;
        private readonly ILogger<EntidadesController> _logger;

        public EntidadesController(
            IEntidadesService service,
            ILogger<EntidadesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene entidades con filtros y paginación
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ListaEntidadesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosEntidadesRequest? filtros)
        {
            var resultado = await _service.ObtenerEntidadesAsync(filtros);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene una entidad por su ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EntidadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id)
        {
            var entidad = await _service.ObtenerPorIdAsync(id);

            if (entidad == null)
                return NotFound(new { Message = "Entidad no encontrada" });

            return Ok(entidad);
        }

        /// <summary>
        /// Obtiene una entidad por su NIT
        /// </summary>
        [HttpGet("nit/{nit}")]
        [ProducesResponseType(typeof(EntidadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByNit(string nit)
        {
            var entidad = await _service.ObtenerPorNitAsync(nit);

            if (entidad == null)
                return NotFound(new { Message = "Entidad no encontrada" });

            return Ok(entidad);
        }

        /// <summary>
        /// Obtiene los planes asignados a una entidad
        /// </summary>
        [HttpGet("{id}/planes")]
        [ProducesResponseType(typeof(IEnumerable<PlanEntidadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPlanes(long id)
        {
            var planes = await _service.ObtenerPlanesEntidadAsync(id);
            return Ok(planes);
        }

        /// <summary>
        /// Obtiene resumen de entidades para dashboard
        /// </summary>
        [HttpGet("resumen")]
        [ProducesResponseType(typeof(ResumenEntidadesDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResumen()
        {
            var resumen = await _service.ObtenerResumenAsync();
            return Ok(resumen);
        }

        /// <summary>
        /// Verifica si un NIT ya está registrado
        /// </summary>
        [HttpGet("verificar-nit/{nit}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> VerificarNit(string nit, [FromQuery] long? excluirId)
        {
            var existe = await _service.ExisteNitAsync(nit, excluirId);
            return Ok(new { Existe = existe, Disponible = !existe });
        }

        // ==================== OPERACIONES ====================

        /// <summary>
        /// Crea una nueva entidad
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResultadoEntidadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CrearEntidadRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _service.CrearAsync(request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return CreatedAtAction(nameof(GetById), new { id = resultado.EntidadCod }, resultado);
        }

        /// <summary>
        /// Actualiza una entidad existente
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ResultadoEntidadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(long id, [FromBody] ActualizarEntidadRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _service.ActualizarAsync(id, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Extiende el plan de una entidad
        /// </summary>
        [HttpPost("{id}/extender-plan")]
        [ProducesResponseType(typeof(ResultadoEntidadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExtenderPlan(long id, [FromBody] ExtenderPlanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _service.ExtenderPlanAsync(id, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Asigna un plan a una entidad
        /// </summary>
        [HttpPost("{id}/asignar-plan")]
        [ProducesResponseType(typeof(ResultadoEntidadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AsignarPlan(long id, [FromBody] AsignarPlanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _service.AsignarPlanAsync(id, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Desactiva una entidad
        /// </summary>
        [HttpPost("{id}/desactivar")]
        [ProducesResponseType(typeof(ResultadoEntidadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Desactivar(long id)
        {
            var resultado = await _service.DesactivarAsync(id);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }
    }
}