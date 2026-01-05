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
    public class TransferenciasController : ControllerBase
    {
        private readonly ITransferenciasService _service;
        private readonly ILogger<TransferenciasController> _logger;

        public TransferenciasController(ITransferenciasService service, ILogger<TransferenciasController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene transferencias con filtros
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosTransferenciasRequest filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var transferencias = await _service.ObtenerConFiltrosAsync(entidadId, filtros);
            return Ok(transferencias);
        }

        /// <summary>
        /// Obtiene una transferencia por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var transferencia = await _service.ObtenerPorIdAsync(id);
            if (transferencia == null)
                return NotFound(new { Message = "Transferencia no encontrada" });

            return Ok(transferencia);
        }

        /// <summary>
        /// Obtiene el detalle (expedientes) de una transferencia
        /// </summary>
        [HttpGet("{id}/detalle")]
        public async Task<IActionResult> GetDetalle(long id)
        {
            var detalle = await _service.ObtenerDetalleAsync(id);
            return Ok(detalle);
        }

        /// <summary>
        /// Obtiene expedientes candidatos para transferencia
        /// </summary>
        [HttpGet("expedientes-candidatos")]
        public async Task<IActionResult> GetExpedientesCandidatos([FromQuery] FiltrosExpedientesCandidatosRequest filtros)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var expedientes = await _service.ObtenerExpedientesCandidatosAsync(entidadId, filtros);
            return Ok(expedientes);
        }

        // ==================== CRUD ====================

        /// <summary>
        /// Crea una nueva transferencia (usa función PostgreSQL F_CrearTransferencia)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearTransferenciaRequest request)
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

            var transferencia = await _service.ObtenerPorIdAsync(resultado.TransferenciaCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.TransferenciaCod }, new
            {
                transferencia,
                resultado.NumeroTransferencia,
                resultado.Mensaje
            });
        }

        /// <summary>
        /// Elimina una transferencia en borrador
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var (success, error) = await _service.EliminarAsync(id);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        // ==================== GESTIÓN DE EXPEDIENTES ====================

        /// <summary>
        /// Agrega un expediente a la transferencia
        /// </summary>
        [HttpPost("{id}/expedientes")]
        public async Task<IActionResult> AgregarExpediente(long id, [FromBody] AgregarExpedienteTransferenciaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.AgregarExpedienteAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }

        /// <summary>
        /// Agrega múltiples expedientes a la transferencia
        /// </summary>
        [HttpPost("{id}/expedientes/multiple")]
        public async Task<IActionResult> AgregarMultiplesExpedientes(long id, [FromBody] AgregarMultiplesExpedientesRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultados = new List<object>();
            foreach (var expediente in request.Expedientes)
            {
                var resultado = await _service.AgregarExpedienteAsync(id, usuarioId, expediente);
                resultados.Add(new { expediente.Expediente, resultado.Exito, resultado.Mensaje });
            }

            return Ok(resultados);
        }

        /// <summary>
        /// Remueve un expediente de la transferencia
        /// </summary>
        [HttpDelete("{id}/expedientes/{expedienteId}")]
        public async Task<IActionResult> RemoverExpediente(long id, long expedienteId)
        {
            var (success, error) = await _service.RemoverExpedienteAsync(id, expedienteId);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        // ==================== FLUJO ====================

        /// <summary>
        /// Envía la transferencia a revisión
        /// </summary>
        [HttpPost("{id}/enviar")]
        public async Task<IActionResult> Enviar(long id, [FromBody] EnviarTransferenciaRequest? request)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.EnviarAsync(id, usuarioId, request ?? new EnviarTransferenciaRequest());

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }

        /// <summary>
        /// Recibe/aprueba la transferencia
        /// </summary>
        [HttpPost("{id}/recibir")]
        public async Task<IActionResult> Recibir(long id, [FromBody] RecibirTransferenciaRequest? request)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.RecibirAsync(id, usuarioId, request ?? new RecibirTransferenciaRequest());

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }

        /// <summary>
        /// Rechaza la transferencia
        /// </summary>
        [HttpPost("{id}/rechazar")]
        public async Task<IActionResult> Rechazar(long id, [FromBody] RechazarTransferenciaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.RechazarAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }

        /// <summary>
        /// Ejecuta la transferencia (mueve expedientes al archivo destino)
        /// </summary>
        [HttpPost("{id}/ejecutar")]
        public async Task<IActionResult> Ejecutar(long id)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.EjecutarAsync(id, usuarioId);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }
    }
}