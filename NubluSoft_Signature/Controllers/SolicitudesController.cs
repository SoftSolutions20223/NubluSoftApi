using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Signature.Extensions;
using NubluSoft_Signature.Models.DTOs;
using NubluSoft_Signature.Services;

namespace NubluSoft_Signature.Controllers
{
    /// <summary>
    /// Controller para gestión de solicitudes de firma
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SolicitudesController : ControllerBase
    {
        private readonly ISolicitudFirmaService _solicitudService;
        private readonly ILogger<SolicitudesController> _logger;

        public SolicitudesController(
            ISolicitudFirmaService solicitudService,
            ILogger<SolicitudesController> logger)
        {
            _solicitudService = solicitudService;
            _logger = logger;
        }

        /// <summary>
        /// Crea una nueva solicitud de firma
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearSolicitudRequest request)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var resultado = await _solicitudService.CrearSolicitudAsync(
                entidadId.Value, usuarioId.Value, request);

            if (!resultado.Exito)
                return BadRequest(new { mensaje = resultado.Mensaje });

            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene una solicitud por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(long id)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var solicitud = await _solicitudService.ObtenerPorIdAsync(
                entidadId.Value, id, usuarioId.Value);

            if (solicitud == null)
                return NotFound(new { mensaje = "Solicitud no encontrada" });

            return Ok(solicitud);
        }

        /// <summary>
        /// Obtiene las solicitudes pendientes de firma del usuario actual
        /// </summary>
        [HttpGet("pendientes")]
        public async Task<IActionResult> ObtenerPendientes()
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var solicitudes = await _solicitudService.ObtenerPendientesUsuarioAsync(
                entidadId.Value, usuarioId.Value);

            return Ok(solicitudes);
        }

        /// <summary>
        /// Obtiene las solicitudes creadas por el usuario actual
        /// </summary>
        [HttpGet("mis-solicitudes")]
        public async Task<IActionResult> ObtenerMisSolicitudes([FromQuery] string? estado = null)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var solicitudes = await _solicitudService.ObtenerMisSolicitudesAsync(
                entidadId.Value, usuarioId.Value, estado);

            return Ok(solicitudes);
        }

        /// <summary>
        /// Obtiene el resumen de solicitudes para dashboard
        /// </summary>
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumen()
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var resumen = await _solicitudService.ObtenerResumenAsync(
                entidadId.Value, usuarioId.Value);

            return Ok(resumen);
        }

        /// <summary>
        /// Cancela una solicitud de firma
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancelar(long id, [FromBody] CancelarSolicitudRequest request)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var resultado = await _solicitudService.CancelarSolicitudAsync(
                entidadId.Value, usuarioId.Value, id, request.Motivo);

            if (!resultado.Exito)
                return BadRequest(new { mensaje = resultado.Mensaje });

            return Ok(resultado);
        }

        /// <summary>
        /// Verifica si el usuario puede firmar una solicitud
        /// </summary>
        [HttpGet("{id}/puede-firmar")]
        public async Task<IActionResult> VerificarPuedeFirmar(long id)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var (puedeFirmar, firmanteId, mensaje) = await _solicitudService.VerificarPuedeFirmarAsync(
                entidadId.Value, usuarioId.Value, id);

            return Ok(new
            {
                puedeFirmar,
                firmanteId,
                mensaje
            });
        }
    }
}