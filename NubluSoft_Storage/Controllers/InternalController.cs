using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Storage.Services;

namespace NubluSoft_Storage.Controllers
{
    /// <summary>
    /// Controller para operaciones internas (solo otros microservicios)
    /// NO exponer en Gateway al frontend
    /// </summary>
    [ApiController]
    [Route("internal/[controller]")]
    [Authorize] // Los microservicios pasan el token del usuario
    public class InternalController : ControllerBase
    {
        private readonly IGcsStorageService _gcsService;
        private readonly ILogger<InternalController> _logger;

        public InternalController(
            IGcsStorageService gcsService,
            ILogger<InternalController> logger)
        {
            _gcsService = gcsService;
            _logger = logger;
        }

        /// <summary>
        /// Genera URL firmada para upload directo a GCS
        /// Solo para uso interno (Core → Storage)
        /// </summary>
        [HttpPost("signed-url/upload")]
        public IActionResult GenerateUploadUrl([FromBody] GenerateUploadUrlRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var expiration = TimeSpan.FromMinutes(request.ExpirationMinutes ?? 30);

                var signedUrl = _gcsService.GenerateSignedUploadUrl(
                    request.ObjectName,
                    request.ContentType,
                    expiration);

                _logger.LogInformation(
                    "URL de upload generada para: {ObjectName}, expira en {Minutes} minutos",
                    request.ObjectName, expiration.TotalMinutes);

                return Ok(new GenerateUploadUrlResponse
                {
                    Success = true,
                    UploadUrl = signedUrl,
                    ObjectName = request.ObjectName,
                    ExpiresAt = DateTime.UtcNow.Add(expiration),
                    ExpiresInSeconds = (int)expiration.TotalSeconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando URL de upload para {ObjectName}", request.ObjectName);
                return StatusCode(500, new GenerateUploadUrlResponse
                {
                    Success = false,
                    Error = "Error al generar URL de upload"
                });
            }
        }

        /// <summary>
        /// Genera URL firmada para descarga
        /// Solo para uso interno (Core → Storage)
        /// </summary>
        [HttpPost("signed-url/download")]
        public IActionResult GenerateDownloadUrl([FromBody] GenerateDownloadUrlRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var expiration = TimeSpan.FromMinutes(request.ExpirationMinutes ?? 15);

                var signedUrl = _gcsService.GenerateSignedDownloadUrl(
                    request.ObjectName,
                    expiration,
                    request.DownloadFileName);

                return Ok(new GenerateDownloadUrlResponse
                {
                    Success = true,
                    DownloadUrl = signedUrl,
                    ExpiresAt = DateTime.UtcNow.Add(expiration),
                    ExpiresInSeconds = (int)expiration.TotalSeconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando URL de descarga para {ObjectName}", request.ObjectName);
                return StatusCode(500, new GenerateDownloadUrlResponse
                {
                    Success = false,
                    Error = "Error al generar URL de descarga"
                });
            }
        }

        /// <summary>
        /// Verifica si un archivo existe en GCS
        /// Solo para uso interno
        /// </summary>
        [HttpGet("exists")]
        public async Task<IActionResult> CheckExists([FromQuery] string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return BadRequest(new { Error = "objectName es requerido" });

            try
            {
                var exists = await _gcsService.ExistsAsync(objectName);

                return Ok(new
                {
                    ObjectName = objectName,
                    Exists = exists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando existencia de {ObjectName}", objectName);
                return StatusCode(500, new { Error = "Error al verificar archivo" });
            }
        }

        /// <summary>
        /// Obtiene información de un archivo en GCS
        /// Solo para uso interno
        /// </summary>
        [HttpGet("file-info")]
        public async Task<IActionResult> GetFileInfo([FromQuery] string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return BadRequest(new { Error = "objectName es requerido" });

            try
            {
                var fileInfo = await _gcsService.GetFileInfoAsync(objectName);

                if (fileInfo == null)
                    return NotFound(new { Error = "Archivo no encontrado", ObjectName = objectName });

                return Ok(new
                {
                    Success = true,
                    FileInfo = fileInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo info de {ObjectName}", objectName);
                return StatusCode(500, new { Error = "Error al obtener información del archivo" });
            }
        }

        /// <summary>
        /// Elimina un archivo de GCS
        /// Solo para uso interno
        /// </summary>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromQuery] string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return BadRequest(new { Error = "objectName es requerido" });

            try
            {
                var deleted = await _gcsService.DeleteAsync(objectName);

                return Ok(new
                {
                    Success = deleted,
                    ObjectName = objectName,
                    Message = deleted ? "Archivo eliminado" : "Archivo no encontrado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando {ObjectName}", objectName);
                return StatusCode(500, new { Error = "Error al eliminar archivo" });
            }
        }
    }

    // ==================== DTOs ====================

    public class GenerateUploadUrlRequest
    {
        public string ObjectName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public int? ExpirationMinutes { get; set; }
    }

    public class GenerateUploadUrlResponse
    {
        public bool Success { get; set; }
        public string? UploadUrl { get; set; }
        public string? ObjectName { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string? Error { get; set; }
    }

    public class GenerateDownloadUrlRequest
    {
        public string ObjectName { get; set; } = string.Empty;
        public string? DownloadFileName { get; set; }
        public int? ExpirationMinutes { get; set; }
    }

    public class GenerateDownloadUrlResponse
    {
        public bool Success { get; set; }
        public string? DownloadUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string? Error { get; set; }
    }
}