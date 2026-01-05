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
    public class TRDController : ControllerBase
    {
        private readonly ITRDService _service;
        private readonly ILogger<TRDController> _logger;

        public TRDController(ITRDService service, ILogger<TRDController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ==================== CONSULTAS TRD ====================

        /// <summary>
        /// Obtiene todas las TRD de la entidad con filtros opcionales
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosTRDRequest? filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var trds = await _service.ObtenerPorEntidadAsync(entidadId, filtros);
            return Ok(trds);
        }

        /// <summary>
        /// Obtiene una TRD por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var trd = await _service.ObtenerPorIdAsync(id);
            if (trd == null)
                return NotFound(new { Message = "TRD no encontrada" });

            return Ok(trd);
        }

        /// <summary>
        /// Obtiene el árbol de Series y Subseries
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
        /// Obtiene las Subseries de una Serie
        /// </summary>
        [HttpGet("{id}/subseries")]
        public async Task<IActionResult> GetSubseries(long id)
        {
            var subseries = await _service.ObtenerSubseriesAsync(id);
            return Ok(subseries);
        }

        // ==================== CRUD TRD ====================

        /// <summary>
        /// Crea una nueva Serie o Subserie (usa función PostgreSQL F_CrearTRD)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearTRDRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.CrearAsync(entidadId, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            var trd = await _service.ObtenerPorIdAsync(resultado.TRDCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.TRDCod }, trd);
        }

        /// <summary>
        /// Actualiza una TRD existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ActualizarTRDRequest request)
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
        /// Elimina una TRD (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var (success, error) = await _service.EliminarAsync(id);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        // ==================== ASIGNACIONES OFICINA-TRD ====================

        /// <summary>
        /// Obtiene las oficinas asignadas a una TRD
        /// </summary>
        [HttpGet("{id}/oficinas")]
        public async Task<IActionResult> GetOficinasAsignadas(long id)
        {
            var oficinas = await _service.ObtenerOficinasAsignadasAsync(id);
            return Ok(oficinas);
        }

        /// <summary>
        /// Obtiene las TRD asignadas a una oficina
        /// </summary>
        [HttpGet("oficina/{oficinaId}")]
        public async Task<IActionResult> GetTRDsPorOficina(long oficinaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var trds = await _service.ObtenerTRDsPorOficinaAsync(entidadId, oficinaId);
            return Ok(trds);
        }

        /// <summary>
        /// Asigna una oficina a una TRD (usa función PostgreSQL F_AsignarTRDOficina)
        /// </summary>
        [HttpPost("{id}/oficinas")]
        public async Task<IActionResult> AsignarOficina(long id, [FromBody] AsignarTRDOficinaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.AsignarOficinaAsync(id, entidadId, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }

        /// <summary>
        /// Asigna múltiples oficinas a una TRD
        /// </summary>
        [HttpPost("{id}/oficinas/multiple")]
        public async Task<IActionResult> AsignarMultiplesOficinas(long id, [FromBody] AsignarMultiplesOficinasRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultados = new List<object>();
            foreach (var oficina in request.Oficinas)
            {
                var resultado = await _service.AsignarOficinaAsync(id, entidadId, usuarioId, oficina);
                resultados.Add(new { oficina.Oficina, resultado.Exito, resultado.Mensaje });
            }

            return Ok(resultados);
        }

        /// <summary>
        /// Revoca la asignación de una oficina a una TRD
        /// </summary>
        [HttpDelete("{id}/oficinas/{oficinaId}")]
        public async Task<IActionResult> RevocarOficina(long id, long oficinaId)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.RevocarOficinaAsync(id, entidadId, oficinaId);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        /// <summary>
        /// Actualiza los permisos de una oficina en una TRD
        /// </summary>
        [HttpPut("{id}/oficinas/{oficinaId}/permisos")]
        public async Task<IActionResult> ActualizarPermisos(long id, long oficinaId, [FromBody] ActualizarPermisosTRDRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.ActualizarPermisosAsync(id, entidadId, oficinaId, request);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }
    }
}