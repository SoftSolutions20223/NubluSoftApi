using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Extensions;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    /// <summary>
    /// Controller para gestión de notificaciones de usuario
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _service;
        private readonly ILogger<NotificacionesController> _logger;

        public NotificacionesController(
            INotificacionService service,
            ILogger<NotificacionesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene las notificaciones del usuario con filtros y paginación
        /// </summary>
        /// <param name="filtros">Filtros opcionales</param>
        /// <returns>Lista paginada de notificaciones</returns>
        [HttpGet]
        [ProducesResponseType(typeof(NotificacionesListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosNotificacionesRequest? filtros)
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario o entidad" });

            try
            {
                var resultado = await _service.ObtenerNotificacionesAsync(entidadId, usuarioId, filtros);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo notificaciones");
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene una notificación específica por su ID
        /// </summary>
        /// <param name="id">ID de la notificación</param>
        /// <returns>Detalle de la notificación</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(NotificacionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(long id)
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario o entidad" });

            try
            {
                var notificacion = await _service.ObtenerPorIdAsync(entidadId, usuarioId, id);

                if (notificacion == null)
                    return NotFound(new { Message = "Notificación no encontrada" });

                return Ok(notificacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo notificación {NotificacionId}", id);
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene el contador de notificaciones no leídas
        /// </summary>
        /// <returns>Contadores por prioridad</returns>
        [HttpGet("contador")]
        [ProducesResponseType(typeof(ContadorNotificacionesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetContador()
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario o entidad" });

            try
            {
                var contador = await _service.ObtenerContadorAsync(entidadId, usuarioId);
                return Ok(contador);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo contador de notificaciones");
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene solo el número de notificaciones no leídas (endpoint ligero)
        /// </summary>
        /// <returns>Número de no leídas</returns>
        [HttpGet("no-leidas/count")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNoLeidasCount()
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario o entidad" });

            try
            {
                var contador = await _service.ObtenerContadorAsync(entidadId, usuarioId);
                return Ok(new { Count = contador.NoLeidas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo contador de no leídas");
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene los tipos de notificación disponibles
        /// </summary>
        /// <returns>Lista de tipos de notificación</returns>
        [HttpGet("tipos")]
        [ProducesResponseType(typeof(IEnumerable<TipoNotificacionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTipos()
        {
            try
            {
                var tipos = await _service.ObtenerTiposNotificacionAsync();
                return Ok(tipos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tipos de notificación");
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Marca una notificación como leída
        /// </summary>
        /// <param name="id">ID de la notificación</param>
        /// <returns>Resultado de la operación</returns>
        [HttpPut("{id}/marcar-leida")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarcarLeida(long id)
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario o entidad" });

            try
            {
                var resultado = await _service.MarcarComoLeidaAsync(entidadId, usuarioId, id);

                if (!resultado)
                    return NotFound(new { Message = "Notificación no encontrada o ya está leída" });

                return Ok(new { Message = "Notificación marcada como leída" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando notificación {NotificacionId} como leída", id);
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Marca todas las notificaciones del usuario como leídas
        /// </summary>
        /// <returns>Cantidad de notificaciones marcadas</returns>
        [HttpPut("marcar-todas-leidas")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario o entidad" });

            try
            {
                var cantidad = await _service.MarcarTodasComoLeidasAsync(entidadId, usuarioId);
                return Ok(new { Message = "Notificaciones marcadas como leídas", Cantidad = cantidad });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando todas las notificaciones como leídas");
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Marca múltiples notificaciones como leídas
        /// </summary>
        /// <param name="request">IDs de las notificaciones</param>
        /// <returns>Cantidad de notificaciones marcadas</returns>
        [HttpPut("marcar-leidas")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarcarVariasLeidas([FromBody] MarcarLeidasRequest request)
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario o entidad" });

            try
            {
                int cantidad;

                if (request.MarcarTodas)
                {
                    cantidad = await _service.MarcarTodasComoLeidasAsync(entidadId, usuarioId);
                }
                else if (request.NotificacionIds != null && request.NotificacionIds.Count > 0)
                {
                    cantidad = await _service.MarcarVariasComoLeidasAsync(entidadId, usuarioId, request.NotificacionIds);
                }
                else
                {
                    return BadRequest(new { Message = "Debe especificar notificaciones o marcar todas" });
                }

                return Ok(new { Message = "Notificaciones marcadas como leídas", Cantidad = cantidad });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando notificaciones como leídas");
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Elimina (oculta) una notificación
        /// </summary>
        /// <param name="id">ID de la notificación</param>
        /// <returns>Resultado de la operación</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(long id)
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario o entidad" });

            try
            {
                var resultado = await _service.EliminarAsync(entidadId, usuarioId, id);

                if (!resultado)
                    return NotFound(new { Message = "Notificación no encontrada" });

                return Ok(new { Message = "Notificación eliminada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando notificación {NotificacionId}", id);
                return StatusCode(500, new { Message = "Error interno del servidor" });
            }
        }
    }
}