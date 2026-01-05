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
    public class OficinasController : ControllerBase
    {
        private readonly IOficinasService _service;
        private readonly ILogger<OficinasController> _logger;

        public OficinasController(IOficinasService service, ILogger<OficinasController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las oficinas de la entidad del usuario autenticado
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var oficinas = await _service.ObtenerPorEntidadAsync(entidadId);
            return Ok(oficinas);
        }

        /// <summary>
        /// Obtiene las oficinas en estructura de árbol jerárquico
        /// </summary>
        [HttpGet("arbol")]
        public async Task<IActionResult> GetArbol()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var arbol = await _service.ObtenerArbolAsync(entidadId);
            return Ok(arbol);
        }

        /// <summary>
        /// Obtiene una oficina por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var oficina = await _service.ObtenerPorIdAsync(entidadId, id);
            if (oficina == null)
                return NotFound(new { Message = "Oficina no encontrada" });

            return Ok(oficina);
        }

        /// <summary>
        /// Crea una nueva oficina
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearOficinaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (oficina, error) = await _service.CrearAsync(entidadId, request);

            if (oficina == null)
                return BadRequest(new { Message = error });

            return CreatedAtAction(nameof(GetById), new { id = oficina.Cod }, oficina);
        }

        /// <summary>
        /// Actualiza una oficina existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ActualizarOficinaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.ActualizarAsync(entidadId, id, request);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        /// <summary>
        /// Elimina (soft delete) una oficina
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.EliminarAsync(entidadId, id);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }
    }
}