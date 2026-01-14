using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Signature.Extensions;
using NubluSoft_Signature.Helpers;
using NubluSoft_Signature.Models.DTOs;
using NubluSoft_Signature.Services;

namespace NubluSoft_Signature.Controllers
{
    /// <summary>
    /// Controller para gestión de certificados digitales
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CertificadosController : ControllerBase
    {
        private readonly ICertificadoService _certificadoService;
        private readonly ILogger<CertificadosController> _logger;

        public CertificadosController(
            ICertificadoService certificadoService,
            ILogger<CertificadosController> logger)
        {
            _certificadoService = certificadoService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el certificado activo del usuario actual
        /// </summary>
        [HttpGet("mi-certificado")]
        public async Task<IActionResult> ObtenerMiCertificado()
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var certificado = await _certificadoService.ObtenerCertificadoActivoAsync(
                entidadId.Value, usuarioId.Value);

            if (certificado == null)
                return NotFound(new { mensaje = "No tienes un certificado activo" });

            return Ok(certificado);
        }

        /// <summary>
        /// Genera un nuevo certificado digital
        /// </summary>
        [HttpPost("generar")]
        public async Task<IActionResult> Generar([FromBody] GenerarCertificadoRequest request)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var resultado = await _certificadoService.GenerarCertificadoAsync(
                entidadId.Value, usuarioId.Value, request.Contrasena);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Revoca el certificado activo
        /// </summary>
        [HttpDelete("revocar")]
        public async Task<IActionResult> Revocar([FromBody] RevocarCertificadoRequest request)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var resultado = await _certificadoService.RevocarCertificadoAsync(
                entidadId.Value, usuarioId.Value, request.Motivo, request.Contrasena);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Valida la complejidad de una contraseña
        /// </summary>
        [HttpPost("validar-contrasena")]
        public IActionResult ValidarContrasena([FromBody] ValidarContrasenaRequest request)
        {
            var errores = CryptoHelper.ValidarComplejidadContrasena(request.Contrasena);

            return Ok(new ValidarContrasenaResponse
            {
                Valida = errores.Count == 0,
                Mensaje = errores.Count == 0 ? "Contraseña válida" : "La contraseña no cumple los requisitos",
                Errores = errores
            });
        }

        /// <summary>
        /// Descarga el certificado público (.cer)
        /// </summary>
        [HttpGet("descargar")]
        public async Task<IActionResult> Descargar()
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var certBytes = await _certificadoService.DescargarCertificadoPublicoAsync(
                entidadId.Value, usuarioId.Value);

            if (certBytes == null)
                return NotFound(new { mensaje = "No tienes un certificado activo" });

            return File(certBytes, "application/x-x509-ca-cert", "certificado.cer");
        }
    }

    /// <summary>
    /// Request para validar contraseña (solo complejidad)
    /// </summary>
    public class ValidarContrasenaRequest
    {
        public string Contrasena { get; set; } = string.Empty;
    }
}