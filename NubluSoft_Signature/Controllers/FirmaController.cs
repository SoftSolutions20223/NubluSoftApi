using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly ICertificadoService _certificadoService;
        private readonly IPdfSignatureService _pdfSignatureService;
        private readonly IStorageClientService _storageClientService;
        private readonly IConfiguration _configuration;

        public FirmaController(
            IFirmaService firmaService,
            IOtpService otpService,
            ISolicitudFirmaService solicitudService,
            ICertificadoService certificadoService,
            IPdfSignatureService pdfSignatureService,
            IStorageClientService storageClientService,
            IConfiguration configuration,  // ← AGREGAR
            ILogger<FirmaController> logger)
        {
            _firmaService = firmaService;
            _otpService = otpService;
            _solicitudService = solicitudService;
            _certificadoService = certificadoService;
            _pdfSignatureService = pdfSignatureService;
            _storageClientService = storageClientService;
            _configuration = configuration;  // ← AGREGAR
            _logger = logger;
        }

        /// <summary>
        /// Firma un documento con certificado digital (Firma Avanzada)
        /// </summary>
        [HttpPost("{solicitudId}/certificado")]
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
                var archivoInfo = await ObtenerArchivoIdAsync(solicitudId);
                if (archivoInfo == null)
                {
                    return BadRequest(new { mensaje = "No se encontró el archivo asociado" });
                }

                // 5. Descargar PDF desde Storage
                var storageInfo = await _storageClientService.ObtenerInfoArchivoAsync(archivoInfo.Value);
                if (storageInfo?.Ruta == null)
                {
                    return BadRequest(new { mensaje = "No se pudo obtener información del archivo" });
                }

                var pdfOriginal = await _storageClientService.DescargarArchivoAsync(storageInfo.Ruta);
                if (pdfOriginal == null)
                {
                    return BadRequest(new { mensaje = "No se pudo descargar el documento" });
                }

                // 6. Firmar PDF
                var nombreFirmante = User.FindFirst("NombreCompleto")?.Value ?? "Usuario";
                var resultadoFirma = await _pdfSignatureService.FirmarPdfAsync(
                    pdfOriginal,
                    certificado,
                    nombreFirmante,
                    info.Asunto,
                    "Colombia");

                if (!resultadoFirma.Exito || resultadoFirma.PdfFirmado == null)
                {
                    return BadRequest(new { mensaje = resultadoFirma.Error ?? "Error al firmar el PDF" });
                }

                // 7. Subir PDF firmado
                var uploadResult = await _storageClientService.SubirVersionAsync(
                    archivoInfo.Value,
                    resultadoFirma.PdfFirmado,
                    "application/pdf");

                if (uploadResult == null || !uploadResult.Success)
                {
                    return BadRequest(new { mensaje = "Error al guardar el documento firmado" });
                }

                // 8. Obtener ID del certificado
                var certInfo = await _certificadoService.ObtenerCertificadoActivoAsync(
                    entidadId.Value, usuarioId.Value);

                // 9. Registrar firma en BD
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = Request.Headers.UserAgent.ToString();

                var resultado = await _firmaService.RegistrarFirmaAvanzadaAsync(
                    info.FirmanteId,
                    certInfo?.Cod ?? 0,
                    resultadoFirma.HashFinal ?? "",
                    ip,
                    userAgent);

                // 10. Registrar uso del certificado
                if (certInfo != null)
                {
                    await _certificadoService.RegistrarUsoAsync(certInfo.Cod);
                }

                if (!resultado.Exito)
                {
                    return BadRequest(resultado);
                }

                return Ok(new ResultadoFirmaAvanzadaResponse
                {
                    Exito = true,
                    Mensaje = resultado.Mensaje,
                    TipoFirma = "AVANZADA_CERTIFICADO",
                    SolicitudCompletada = resultado.SolicitudCompletada,
                    HashFinal = resultadoFirma.HashFinal,
                    CodigoVerificacion = resultado.CodigoVerificacion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en firma con certificado");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
            finally
            {
                // Liberar recursos del certificado
            }
        }

        /// <summary>
        /// Obtiene el ID del archivo asociado a una solicitud
        /// </summary>
        private async Task<long?> ObtenerArchivoIdAsync(long solicitudId)
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(
                    _configuration.GetConnectionString("PostgreSQL"));
                await connection.OpenAsync();

                var archivoId = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<long?>(connection, @"
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