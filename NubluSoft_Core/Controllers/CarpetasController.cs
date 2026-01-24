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
    public class CarpetasController : ControllerBase
    {
        private readonly ICarpetasService _service;
        private readonly ILogger<CarpetasController> _logger;

        public CarpetasController(ICarpetasService service, ILogger<CarpetasController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene carpetas con filtros opcionales
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosCarpetasRequest? filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var carpetas = await _service.ObtenerPorEntidadAsync(entidadId, filtros);
            return Ok(carpetas);
        }

        /// <summary>
        /// Obtiene una carpeta por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var carpeta = await _service.ObtenerPorIdAsync(id);
            if (carpeta == null)
                return NotFound(new { Message = "Carpeta no encontrada" });

            return Ok(carpeta);
        }

        /// <summary>
        /// Obtiene los hijos directos de una carpeta
        /// </summary>
        [HttpGet("{id}/hijos")]
        public async Task<IActionResult> GetHijos(long id)
        {
            var hijos = await _service.ObtenerHijosAsync(id);
            return Ok(hijos);
        }

        /// <summary>
        /// Obtiene la ruta completa desde la raíz hasta la carpeta
        /// </summary>
        [HttpGet("{id}/ruta")]
        public async Task<IActionResult> GetRuta(long id)
        {
            var ruta = await _service.ObtenerRutaAsync(id);
            return Ok(ruta);
        }

        /// <summary>
        /// Obtiene estadísticas de una carpeta raíz (expedientes activos, documentos, usuarios con acceso, última modificación)
        /// </summary>
        [HttpGet("{id}/estadisticas")]
        public async Task<IActionResult> GetEstadisticas(long id)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var estadisticas = await _service.ObtenerEstadisticasAsync(id, entidadId);
            if (estadisticas == null)
                return NotFound(new { Message = "Carpeta no encontrada" });

            return Ok(estadisticas);
        }

        /// <summary>
        /// Obtiene el índice electrónico de un expediente (Acuerdo 002/2014 AGN).
        /// Solo disponible para expedientes (TipoCarpeta = 3).
        /// Incluye todos los archivos del expediente y sus subcarpetas genéricas.
        /// </summary>
        [HttpGet("{id}/indice-electronico")]
        public async Task<IActionResult> GetIndiceElectronico(long id)
        {
            var indice = await _service.ObtenerIndiceElectronicoAsync(id);

            if (indice == null)
                return NotFound(new { Message = "Expediente no encontrado o sin índice electrónico. Este endpoint solo funciona con expedientes (TipoCarpeta = 3)." });

            // Retornar el JSON directamente (ya viene como string JSON de PostgreSQL)
            return Content(indice, "application/json");
        }

        /// <summary>
        /// Obtiene la auditoría completa de un expediente.
        /// Solo disponible para expedientes (TipoCarpeta = 3).
        /// Incluye todas las acciones sobre el expediente, sus subcarpetas y archivos.
        /// </summary>
        [HttpGet("{id}/auditoria")]
        public async Task<IActionResult> GetAuditoria(long id)
        {
            var auditoria = await _service.ObtenerAuditoriaExpedienteAsync(id);

            if (!auditoria.Any())
                return NotFound(new { Message = "Expediente no encontrado o sin registros de auditoría. Este endpoint solo funciona con expedientes (TipoCarpeta = 3)." });

            return Ok(auditoria);
        }

        /// <summary>
        /// Obtiene el árbol de Series y Subseries
        /// </summary>
        [HttpGet("arbol")]
        public async Task<IActionResult> GetArbol([FromQuery] long? oficina = null)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var arbol = await _service.ObtenerArbolAsync(entidadId, oficina);
            return Ok(arbol);
        }

        /// <summary>
        /// Crea una nueva carpeta (usa función PostgreSQL F_CrearCarpeta)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearCarpetaRequest request)
        {
            _logger.LogInformation("Crear carpeta - Request recibido: Nombre={Nombre}, TipoCarpeta={Tipo}, CarpetaPadre={Padre}",
                request?.Nombre, request?.TipoCarpeta, request?.CarpetaPadre);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Crear carpeta - ModelState inválido: {Errors}", string.Join(", ", errors));
                return BadRequest(ModelState);
            }

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.CrearAsync(entidadId, usuarioId, request);

            if (!resultado.Exito)
            {
                _logger.LogWarning("Crear carpeta - Error de PostgreSQL: {Mensaje}", resultado.Mensaje);
                return BadRequest(new { resultado.Mensaje });
            }

            var carpeta = await _service.ObtenerPorIdAsync(resultado.CarpetaCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.CarpetaCod }, carpeta);
        }

        /// <summary>
        /// Actualiza una carpeta existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ActualizarCarpetaRequest request)
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
        /// Elimina una carpeta (usa función PostgreSQL F_EliminarCarpeta)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.EliminarAsync(id, usuarioId);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return NoContent();
        }

        /// <summary>
        /// Mueve una carpeta a otro destino (usa función PostgreSQL F_MoverCarpeta)
        /// </summary>
        [HttpPost("{id}/mover")]
        public async Task<IActionResult> Mover(long id, [FromBody] MoverCarpetaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.MoverAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje, resultado.CarpetaCod });
        }

        /// <summary>
        /// Copia una carpeta (usa función PostgreSQL F_CopiarCarpeta)
        /// </summary>
        [HttpPost("{id}/copiar")]
        public async Task<IActionResult> Copiar(long id, [FromBody] CopiarCarpetaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.CopiarAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            var carpeta = await _service.ObtenerPorIdAsync(resultado.CarpetaCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.CarpetaCod }, carpeta);
        }

        /// <summary>
        /// Cierra un expediente
        /// </summary>
        [HttpPost("{id}/cerrar")]
        public async Task<IActionResult> CerrarExpediente(long id, [FromBody] CerrarExpedienteRequest request)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (success, error) = await _service.CerrarExpedienteAsync(id, usuarioId, request);

            if (!success)
                return BadRequest(new { Message = error });

            return Ok(new { Message = "Expediente cerrado correctamente" });
        }

        /// <summary>
        /// Reabre un expediente cerrado
        /// </summary>
        [HttpPost("{id}/reabrir")]
        public async Task<IActionResult> ReabrirExpediente(long id)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (success, error) = await _service.ReabrirExpedienteAsync(id, usuarioId);

            if (!success)
                return BadRequest(new { Message = error });

            return Ok(new { Message = "Expediente reabierto correctamente" });
        }
    }
}