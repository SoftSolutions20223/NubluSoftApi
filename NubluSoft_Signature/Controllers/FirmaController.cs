using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Signature.Extensions;
using NubluSoft_Signature.Models.DTOs;
using NubluSoft_Signature.Services;

namespace NubluSoft_Signature.Controllers
{
    /// <summary>
    /// Controller para el proceso de firma electrónica
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FirmaController : ControllerBase
    {
        private readonly ISolicitudFirmaService _solicitudService;
        private readonly IOtpService _otpService;
        private readonly IFirmaService _firmaService;
        private readonly ILogger<FirmaController> _logger;

        public FirmaController(
            ISolicitudFirmaService solicitudService,
            IOtpService otpService,
            IFirmaService firmaService,
            ILogger<FirmaController> logger)
        {
            _solicitudService = solicitudService;
            _otpService = otpService;
            _firmaService = firmaService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene información del documento a firmar
        /// </summary>
        [HttpGet("{solicitudId}/info")]
        public async Task<IActionResult> ObtenerInfoDocumento(long solicitudId)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var info = await _firmaService.ObtenerInfoDocumentoAsync(
                entidadId.Value, usuarioId.Value, solicitudId);

            if (info == null)
                return NotFound(new { mensaje = "Solicitud no encontrada o no tienes permisos" });

            return Ok(info);
        }

        /// <summary>
        /// Genera y envía un código OTP para firma simple
        /// </summary>
        [HttpPost("{solicitudId}/otp/generar")]
        public async Task<IActionResult> GenerarOtp(long solicitudId, [FromBody] GenerarOtpRequest request)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            // Verificar que puede firmar
            var (puedeFirmar, firmanteId, mensaje) = await _solicitudService.VerificarPuedeFirmarAsync(
                entidadId.Value, usuarioId.Value, solicitudId);

            if (!puedeFirmar || !firmanteId.HasValue)
                return BadRequest(new { mensaje = mensaje ?? "No puedes firmar esta solicitud" });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers.UserAgent.ToString();

            var resultado = await _otpService.GenerarYEnviarAsync(
                firmanteId.Value, request.Medio, ip, userAgent);

            if (!resultado.Enviado)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Valida el código OTP y registra la firma
        /// </summary>
        [HttpPost("{solicitudId}/otp/validar")]
        public async Task<IActionResult> ValidarOtpYFirmar(long solicitudId, [FromBody] ValidarOtpRequest request)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            // Verificar que puede firmar
            var (puedeFirmar, firmanteId, mensaje) = await _solicitudService.VerificarPuedeFirmarAsync(
                entidadId.Value, usuarioId.Value, solicitudId);

            if (!puedeFirmar || !firmanteId.HasValue)
                return BadRequest(new { mensaje = mensaje ?? "No puedes firmar esta solicitud" });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers.UserAgent.ToString();

            // 1. Validar OTP
            var validacion = await _otpService.ValidarAsync(firmanteId.Value, request.Codigo, ip);

            if (!validacion.Valido)
                return BadRequest(validacion);

            // 2. Registrar firma
            var resultadoFirma = await _firmaService.RegistrarFirmaSimpleAsync(
                firmanteId.Value, ip, userAgent);

            if (!resultadoFirma.Exito)
                return BadRequest(resultadoFirma);

            return Ok(resultadoFirma);
        }

        /// <summary>
        /// Rechaza una solicitud de firma
        /// </summary>
        [HttpPost("{solicitudId}/rechazar")]
        public async Task<IActionResult> Rechazar(long solicitudId, [FromBody] RechazarFirmaRequest request)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            // Verificar que puede firmar (también puede rechazar)
            var (puedeFirmar, firmanteId, mensaje) = await _solicitudService.VerificarPuedeFirmarAsync(
                entidadId.Value, usuarioId.Value, solicitudId);

            if (!puedeFirmar || !firmanteId.HasValue)
                return BadRequest(new { mensaje = mensaje ?? "No puedes rechazar esta solicitud" });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers.UserAgent.ToString();

            var resultado = await _firmaService.RechazarFirmaAsync(
                firmanteId.Value, request.Motivo, ip, userAgent);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }
    }
}