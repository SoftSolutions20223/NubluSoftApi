using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Signature.Extensions;
using NubluSoft_Signature.Models.DTOs;
using NubluSoft_Signature.Services;
using Dapper;

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
        private readonly ICertificadoService _certificadoService;
        private readonly IPdfSignatureService _pdfSignatureService;
        private readonly IStorageClientService _storageClientService;
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FirmaController> _logger;

        public FirmaController(
            IFirmaService firmaService,
            IOtpService otpService,
            ISolicitudFirmaService solicitudService,
            ICertificadoService certificadoService,
            IPdfSignatureService pdfSignatureService,
            IStorageClientService storageClientService,
            IPostgresConnectionFactory connectionFactory,
            IConfiguration configuration,
            ILogger<FirmaController> logger)
        {
            _firmaService = firmaService;
            _otpService = otpService;
            _solicitudService = solicitudService;
            _certificadoService = certificadoService;
            _pdfSignatureService = pdfSignatureService;
            _storageClientService = storageClientService;
            _connectionFactory = connectionFactory;
            _configuration = configuration;
            _logger = logger;
        }

        #region Métodos Helper

        /// <summary>
        /// Obtiene el token JWT del header Authorization
        /// IMPORTANTE: Necesario para comunicación con Storage que requiere autenticación
        /// </summary>
        private string? GetAuthToken()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader.Substring("Bearer ".Length).Trim();
        }

        /// <summary>
        /// Obtiene el ID del archivo asociado a una solicitud
        /// </summary>
        private async Task<long?> ObtenerArchivoIdAsync(long solicitudId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var archivoId = await connection.QueryFirstOrDefaultAsync<long?>(@"
                    SELECT ""Archivo"" 
                    FROM documentos.""Solicitudes_Firma""
                    WHERE ""Cod"" = @SolicitudId",
                    new { SolicitudId = solicitudId });

                return archivoId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ArchivoId para solicitud {SolicitudId}", solicitudId);
                return null;
            }
        }

        #endregion

        #region Información de Documento

        /// <summary>
        /// Obtiene información del documento a firmar
        /// </summary>
        [HttpGet("{solicitudId}/info")]
        [ProducesResponseType(typeof(InfoDocumentoFirmaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        /// Verifica si el usuario puede firmar una solicitud específica
        /// </summary>
        [HttpGet("{solicitudId}/puede-firmar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> VerificarPuedeFirmar(long solicitudId)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var (puedeFirmar, firmanteId, mensaje) = await _solicitudService.VerificarPuedeFirmarAsync(
                entidadId.Value, usuarioId.Value, solicitudId);

            return Ok(new
            {
                puedeFirmar,
                firmanteId,
                mensaje
            });
        }

        #endregion

        #region Firma Simple con OTP

        /// <summary>
        /// Genera y envía un código OTP para firma simple
        /// </summary>
        [HttpPost("{solicitudId}/otp/generar")]
        [ProducesResponseType(typeof(GenerarOtpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        /// Valida el código OTP y registra la firma simple
        /// </summary>
        [HttpPost("{solicitudId}/otp/validar")]
        [ProducesResponseType(typeof(ResultadoFirmaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

            // 2. Registrar firma simple
            var resultadoFirma = await _firmaService.RegistrarFirmaSimpleAsync(
                firmanteId.Value, ip, userAgent);

            if (!resultadoFirma.Exito)
                return BadRequest(resultadoFirma);

            return Ok(resultadoFirma);
        }

        #endregion

        #region Firma Avanzada con Certificado

        /// <summary>
        /// Firma un documento con certificado digital (Firma Avanzada)
        /// Requiere que el usuario tenga un certificado activo y proporcione su contraseña
        /// </summary>
        [HttpPost("{solicitudId}/certificado")]
        [ProducesResponseType(typeof(ResultadoFirmaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FirmarConCertificado(
            long solicitudId,
            [FromBody] FirmarConCertificadoRequest request)
        {
            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (!usuarioId.HasValue || !entidadId.HasValue)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            try
            {
                // *** CORRECCIÓN CRÍTICA: Obtener token para llamadas a Storage ***
                var authToken = GetAuthToken();

                // 1. Verificar permisos
                var puedeFirmar = await _solicitudService.VerificarPuedeFirmarAsync(
                    entidadId.Value, usuarioId.Value, solicitudId);

                if (!puedeFirmar.PuedeFirmar)
                {
                    return BadRequest(new { mensaje = puedeFirmar.Mensaje });
                }

                // 2. Obtener certificado desencriptado
                var certificado = await _certificadoService.ObtenerCertificadoParaFirmarAsync(
                    entidadId.Value, usuarioId.Value, request.Contrasena);

                if (certificado == null)
                {
                    return BadRequest(new { mensaje = "Contraseña incorrecta o certificado no disponible" });
                }

                // 3. Obtener información del documento
                var info = await _firmaService.ObtenerInfoDocumentoAsync(
                    entidadId.Value, usuarioId.Value, solicitudId);

                if (info == null)
                {
                    return NotFound(new { mensaje = "Solicitud no encontrada" });
                }

                // 4. Obtener información del archivo para descargar
                var archivoId = await ObtenerArchivoIdAsync(solicitudId);
                if (!archivoId.HasValue)
                {
                    return BadRequest(new { mensaje = "No se encontró el archivo asociado" });
                }

                // 5. Obtener info del archivo desde Storage (PASANDO TOKEN)
                var storageInfo = await _storageClientService.ObtenerInfoArchivoAsync(
                    archivoId.Value,
                    authToken);  // ← CORRECCIÓN: Pasar token

                if (storageInfo?.Ruta == null)
                {
                    return BadRequest(new { mensaje = "No se pudo obtener información del archivo" });
                }

                // 6. Descargar PDF desde Storage (PASANDO TOKEN)
                var pdfOriginal = await _storageClientService.DescargarArchivoAsync(
                    storageInfo.Ruta,
                    authToken);  // ← CORRECCIÓN: Pasar token

                if (pdfOriginal == null)
                {
                    return BadRequest(new { mensaje = "No se pudo descargar el documento" });
                }

                // 7. Firmar PDF con certificado
                var nombreFirmante = User.FindFirst("NombreCompleto")?.Value
                    ?? User.FindFirst("Nombres")?.Value
                    ?? "Usuario";

                var resultadoFirmaPdf = await _pdfSignatureService.FirmarPdfAsync(
                    pdfOriginal,
                    certificado,
                    nombreFirmante,
                    info.Asunto,
                    "Colombia");

                if (!resultadoFirmaPdf.Exito || resultadoFirmaPdf.PdfFirmado == null)
                {
                    return BadRequest(new { mensaje = resultadoFirmaPdf.Error ?? "Error al firmar el PDF" });
                }

                // 8. Subir PDF firmado a Storage (PASANDO TOKEN)
                var uploadResult = await _storageClientService.SubirVersionAsync(
                    archivoId.Value,
                    resultadoFirmaPdf.PdfFirmado,
                    "application/pdf",
                    authToken);  // ← CORRECCIÓN: Pasar token

                if (uploadResult == null || !uploadResult.Success)
                {
                    return BadRequest(new { mensaje = "Error al guardar el documento firmado" });
                }

                // 9. Obtener ID del certificado para registrar en BD
                var certInfo = await _certificadoService.ObtenerCertificadoActivoAsync(
                    entidadId.Value, usuarioId.Value);

                // 10. Registrar firma avanzada en BD
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = Request.Headers.UserAgent.ToString();

                var resultado = await _firmaService.RegistrarFirmaAvanzadaAsync(
                    info.FirmanteId,
                    certInfo?.Cod ?? 0,
                    resultadoFirmaPdf.HashFinal ?? "",
                    ip,
                    userAgent);

                // 11. Registrar uso del certificado
                if (certInfo != null)
                {
                    await _certificadoService.RegistrarUsoAsync(certInfo.Cod);
                }

                _logger.LogInformation(
                    "Firma avanzada completada para solicitud {SolicitudId} por usuario {UsuarioId}",
                    solicitudId, usuarioId);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error firmando documento con certificado. Solicitud: {SolicitudId}",
                    solicitudId);
                return StatusCode(500, new { mensaje = "Error interno al firmar el documento" });
            }
        }

        #endregion

        #region Rechazar Firma

        /// <summary>
        /// Rechaza una solicitud de firma
        /// </summary>
        [HttpPost("{solicitudId}/rechazar")]
        [ProducesResponseType(typeof(ResultadoFirmaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        #endregion
    }
}