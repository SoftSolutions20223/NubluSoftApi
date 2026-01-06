using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Storage.Extensions;
using NubluSoft_Storage.Models.DTOs;
using NubluSoft_Storage.Services;

namespace NubluSoft_Storage.Controllers
{
    /// <summary>
    /// Controller principal para operaciones de almacenamiento
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<StorageController> _logger;

        public StorageController(
            IStorageService storageService,
            ILogger<StorageController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // ==================== UPLOAD ====================

        /// <summary>
        /// Sube un archivo
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(524288000)] // 500 MB
        public async Task<IActionResult> Upload(
            IFormFile file,
            [FromForm] long carpetaId,
            [FromForm] string? nombrePersonalizado = null,
            [FromForm] string? descripcion = null,
            [FromForm] long? tipoDocumental = null,
            [FromForm] DateTime? fechaDocumento = null,
            [FromForm] string? codigoDocumento = null,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No se proporcionó ningún archivo" });

            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var request = new UploadRequest
            {
                CarpetaId = carpetaId,
                NombrePersonalizado = nombrePersonalizado,
                Descripcion = descripcion,
                TipoDocumental = tipoDocumental,
                FechaDocumento = fechaDocumento,
                CodigoDocumento = codigoDocumento
            };

            var result = await _storageService.UploadFileAsync(file, request, usuarioId, entidadId, cancellationToken);

            if (!result.Exito)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Sube múltiples archivos
        /// </summary>
        [HttpPost("upload/multiple")]
        [RequestSizeLimit(1073741824)] // 1 GB total
        public async Task<IActionResult> UploadMultiple(
            IFormFileCollection files,
            [FromForm] long carpetaId,
            CancellationToken cancellationToken = default)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { Message = "No se proporcionaron archivos" });

            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var results = await _storageService.UploadMultipleAsync(files, carpetaId, usuarioId, entidadId, cancellationToken);

            var exitosos = results.Count(r => r.Exito);
            var fallidos = results.Count - exitosos;

            return Ok(new
            {
                Message = $"Subidos: {exitosos}, Fallidos: {fallidos}",
                TotalArchivos = results.Count,
                Exitosos = exitosos,
                Fallidos = fallidos,
                Resultados = results
            });
        }

        // ==================== DOWNLOAD ====================

        /// <summary>
        /// Obtiene URL firmada para descarga directa
        /// </summary>
        [HttpGet("download-url/{archivoId}")]
        public async Task<IActionResult> GetDownloadUrl(
            long archivoId,
            [FromQuery] int? expirationMinutes = null,
            CancellationToken cancellationToken = default)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var result = await _storageService.GetDownloadUrlAsync(archivoId, usuarioId, expirationMinutes, cancellationToken);

            if (!result.Exito)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Obtiene múltiples URLs firmadas
        /// </summary>
        [HttpPost("download-url/batch")]
        public async Task<IActionResult> GetBatchDownloadUrls(
            [FromBody] BatchSignedUrlRequest request,
            CancellationToken cancellationToken = default)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var result = await _storageService.GetBatchDownloadUrlsAsync(request, usuarioId, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Descarga archivo directamente (proxy)
        /// </summary>
        [HttpGet("download/{archivoId}")]
        public async Task<IActionResult> Download(
            long archivoId,
            CancellationToken cancellationToken = default)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var result = await _storageService.DownloadFileAsync(archivoId, usuarioId, cancellationToken);

            if (result == null)
                return NotFound(new { Message = "Archivo no encontrado" });

            return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
        }

        /// <summary>
        /// Obtiene información para descarga de carpeta
        /// </summary>
        [HttpGet("folder/{carpetaId}/info")]
        public async Task<IActionResult> GetFolderInfo(
            long carpetaId,
            CancellationToken cancellationToken = default)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var result = await _storageService.GetFolderDownloadInfoAsync(carpetaId, usuarioId, cancellationToken);

            if (result == null)
                return NotFound(new { Message = "Carpeta no encontrada o vacía" });

            return Ok(result);
        }

        /// <summary>
        /// Descarga carpeta completa como ZIP
        /// </summary>
        [HttpGet("folder/{carpetaId}/download")]
        public async Task DownloadFolder(
            long carpetaId,
            CancellationToken cancellationToken = default)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
            {
                Response.StatusCode = 401;
                return;
            }

            // Obtener info para el nombre del archivo
            var folderInfo = await _storageService.GetFolderDownloadInfoAsync(carpetaId, usuarioId, cancellationToken);

            if (folderInfo == null)
            {
                Response.StatusCode = 404;
                return;
            }

            // Configurar respuesta para streaming
            Response.ContentType = "application/zip";
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{folderInfo.NombreCarpeta}.zip\"");

            // Stream ZIP directamente al response
            await _storageService.DownloadFolderAsZipAsync(carpetaId, usuarioId, Response.Body, cancellationToken);
        }

        // ==================== VERSIONES ====================

        /// <summary>
        /// Crea nueva versión de un archivo
        /// </summary>
        [HttpPost("version")]
        [RequestSizeLimit(524288000)] // 500 MB
        public async Task<IActionResult> CreateVersion(
            IFormFile file,
            [FromForm] long archivoId,
            [FromForm] string? comentario = null,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No se proporcionó ningún archivo" });

            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var request = new CreateVersionRequest
            {
                ArchivoId = archivoId,
                Comentario = comentario
            };

            var result = await _storageService.CreateVersionAsync(file, request, usuarioId, cancellationToken);

            if (!result.Exito)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Obtiene historial de versiones de un archivo
        /// </summary>
        [HttpGet("version/{archivoId}/history")]
        public async Task<IActionResult> GetVersionHistory(
            long archivoId,
            CancellationToken cancellationToken = default)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var result = await _storageService.GetVersionHistoryAsync(archivoId, usuarioId, cancellationToken);

            if (result == null)
                return NotFound(new { Message = "Archivo no encontrado o sin versiones" });

            return Ok(result);
        }

        /// <summary>
        /// Obtiene URL de descarga para versión específica
        /// </summary>
        [HttpGet("version/{archivoId}/{version}/download-url")]
        public async Task<IActionResult> GetVersionDownloadUrl(
            long archivoId,
            int version,
            CancellationToken cancellationToken = default)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var result = await _storageService.GetVersionDownloadUrlAsync(archivoId, version, usuarioId, cancellationToken);

            if (!result.Exito)
                return NotFound(result);

            return Ok(result);
        }

        // ==================== GESTIÓN ====================

        /// <summary>
        /// Elimina un archivo
        /// </summary>
        [HttpDelete("{archivoId}")]
        public async Task<IActionResult> Delete(
            long archivoId,
            CancellationToken cancellationToken = default)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var result = await _storageService.DeleteFileAsync(archivoId, usuarioId, cancellationToken);

            if (!result.Exito)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Verifica si un archivo existe
        /// </summary>
        [HttpGet("{archivoId}/exists")]
        public async Task<IActionResult> Exists(
            long archivoId,
            CancellationToken cancellationToken = default)
        {
            var exists = await _storageService.FileExistsAsync(archivoId, cancellationToken);

            return Ok(new { ArchivoId = archivoId, Existe = exists });
        }

        /// <summary>
        /// Obtiene estadísticas de storage de la entidad
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
        {
            var entidadId = User.GetEntidadId();

            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad" });

            var stats = await _storageService.GetStorageStatsAsync(entidadId, cancellationToken);

            return Ok(stats);
        }
    }
}