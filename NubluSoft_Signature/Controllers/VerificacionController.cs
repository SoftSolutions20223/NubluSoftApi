using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Signature.Extensions;
using NubluSoft_Signature.Services;

namespace NubluSoft_Signature.Controllers
{
    /// <summary>
    /// Controller para verificación de firmas
    /// El endpoint principal es PÚBLICO (sin autenticación)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VerificacionController : ControllerBase
    {
        private readonly IVerificacionService _verificacionService;
        private readonly ILogger<VerificacionController> _logger;

        public VerificacionController(
            IVerificacionService verificacionService,
            ILogger<VerificacionController> logger)
        {
            _verificacionService = verificacionService;
            _logger = logger;
        }

        /// <summary>
        /// Verifica una firma por su código de verificación
        /// ENDPOINT PÚBLICO - No requiere autenticación
        /// </summary>
        /// <param name="codigo">Código de verificación (ej: NUBLU-2026-ABC123)</param>
        /// <returns>Información pública de la firma</returns>
        [HttpGet("{codigo}")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarPorCodigo(string codigo)
        {
            _logger.LogInformation("Solicitud de verificación para código: {Codigo}", codigo);

            var resultado = await _verificacionService.VerificarPorCodigoAsync(codigo);

            // Siempre retornamos 200 OK, el campo "Valido" indica si es válido o no
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene el historial completo de una solicitud de firma
        /// ENDPOINT PRIVADO - Requiere autenticación
        /// </summary>
        /// <param name="solicitudId">ID de la solicitud</param>
        /// <returns>Historial con todas las evidencias</returns>
        [HttpGet("historial/{solicitudId}")]
        [Authorize]
        public async Task<IActionResult> ObtenerHistorial(long solicitudId)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var historial = await _verificacionService.ObtenerHistorialAsync(
                entidadId.Value, solicitudId);

            if (historial == null)
                return NotFound(new { mensaje = "Solicitud no encontrada" });

            return Ok(historial);
        }
    }
}