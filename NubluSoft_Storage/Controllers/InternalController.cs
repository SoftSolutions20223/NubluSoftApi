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
                _logger.LogInformation(
                    "[UPLOAD DEBUG Storage] Request recibido. ObjectName: {ObjectName}, ContentType del request: '{ContentType}'",
                    request.ObjectName, request.ContentType);

                var expiration = TimeSpan.FromMinutes(request.ExpirationMinutes ?? 30);

                // El servicio retorna tanto la URL como el ContentType normalizado que se usó para firmar
                var result = _gcsService.GenerateSignedUploadUrl(
                    request.ObjectName,
                    request.ContentType,
                    expiration);

                _logger.LogInformation(
                    "[UPLOAD DEBUG Storage] URL generada. ContentType del request: '{RequestCT}', ContentType firmado: '{SignedCT}', Son iguales: {Iguales}",
                    request.ContentType, result.SignedContentType, request.ContentType == result.SignedContentType);

                // Calcular bytes hex del Content-Type para debugging
                var contentTypeBytes = System.Text.Encoding.UTF8.GetBytes(result.SignedContentType);
                var contentTypeHex = BitConverter.ToString(contentTypeBytes);

                var response = new GenerateUploadUrlResponse
                {
                    Success = true,
                    UploadUrl = result.SignedUrl,
                    ObjectName = request.ObjectName,
                    // CRÍTICO: Retornar el ContentType EXACTO que se usó para firmar, no el del request
                    // El frontend DEBE usar este valor exacto al hacer el PUT a GCS
                    ContentType = result.SignedContentType,
                    ContentTypeHex = contentTypeHex,
                    ContentTypeLength = contentTypeBytes.Length,
                    ExpiresAt = DateTime.UtcNow.Add(expiration),
                    ExpiresInSeconds = (int)expiration.TotalSeconds
                };

                _logger.LogInformation(
                    "[UPLOAD DEBUG Storage] Respuesta a enviar. ContentType: '{ResponseCT}', Hex: {Hex}, Length: {Len}",
                    response.ContentType, contentTypeHex, contentTypeBytes.Length);

                return Ok(response);
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
                _logger.LogInformation(
                    "[EXISTS DEBUG Storage] Verificando existencia. ObjectName recibido: '{ObjectName}', Bucket: '{Bucket}'",
                    objectName, _gcsService.BucketName);

                var exists = await _gcsService.ExistsAsync(objectName);

                _logger.LogInformation(
                    "[EXISTS DEBUG Storage] Resultado: Exists={Exists} para '{ObjectName}'",
                    exists, objectName);

                return Ok(new
                {
                    ObjectName = objectName,
                    Exists = exists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EXISTS DEBUG Storage] Error verificando existencia de {ObjectName}", objectName);
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

        /// <summary>
        /// DIAGNÓSTICO: Verifica que las credenciales de GCS funcionan correctamente.
        /// Intenta listar objetos y subir un archivo de prueba.
        /// </summary>
        [HttpGet("diagnostico-gcs")]
        [AllowAnonymous] // Temporal para diagnóstico - REMOVER después
        public async Task<IActionResult> DiagnosticoGcs()
        {
            var diagnostico = new DiagnosticoGcsResponse
            {
                Timestamp = DateTime.UtcNow,
                BucketName = _gcsService.BucketName
            };

            try
            {
                // Test 1: Verificar que podemos listar objetos
                _logger.LogInformation("[DIAGNÓSTICO] Test 1: Listando objetos...");
                var objectCount = 0;
                await foreach (var obj in _gcsService.ListFilesAsync("entidad_1/", CancellationToken.None))
                {
                    objectCount++;
                    if (objectCount >= 3) break; // Solo listar máximo 3 para test rápido
                }
                diagnostico.ListarObjetosExito = true;
                diagnostico.ObjetosEncontrados = objectCount;
                _logger.LogInformation("[DIAGNÓSTICO] Test 1 OK: Encontrados {Count} objetos", objectCount);
            }
            catch (Exception ex)
            {
                diagnostico.ListarObjetosExito = false;
                diagnostico.ListarObjetosError = ex.Message;
                _logger.LogError(ex, "[DIAGNÓSTICO] Test 1 FALLÓ: Error listando objetos");
            }

            try
            {
                // Test 2: Subir archivo de prueba directamente (sin URL firmada)
                _logger.LogInformation("[DIAGNÓSTICO] Test 2: Subiendo archivo de prueba...");
                var testContent = $"Test file created at {DateTime.UtcNow:O} for credential verification";
                var testBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
                var testObjectName = $"_diagnostico/test_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";

                using var stream = new MemoryStream(testBytes);
                var uploadResult = await _gcsService.UploadStreamingAsync(
                    stream,
                    testObjectName,
                    "text/plain",
                    testBytes.Length);

                diagnostico.SubirArchivoExito = uploadResult.Success;
                diagnostico.ArchivoSubido = testObjectName;
                diagnostico.SubirArchivoError = uploadResult.Success ? null : uploadResult.Error;

                if (uploadResult.Success)
                {
                    _logger.LogInformation("[DIAGNÓSTICO] Test 2 OK: Archivo subido a {ObjectName}", testObjectName);

                    // Limpiar archivo de prueba
                    await _gcsService.DeleteAsync(testObjectName);
                    _logger.LogInformation("[DIAGNÓSTICO] Archivo de prueba eliminado");
                }
                else
                {
                    _logger.LogError("[DIAGNÓSTICO] Test 2 FALLÓ: {Error}", uploadResult.Error);
                }
            }
            catch (Exception ex)
            {
                diagnostico.SubirArchivoExito = false;
                diagnostico.SubirArchivoError = ex.Message;
                _logger.LogError(ex, "[DIAGNÓSTICO] Test 2 FALLÓ: Error subiendo archivo");
            }

            try
            {
                // Test 3: Generar URL firmada y mostrar componentes para debugging
                _logger.LogInformation("[DIAGNÓSTICO] Test 3: Generando URL firmada de prueba...");
                var testObjectName = $"_diagnostico/test_signed_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
                var result = _gcsService.GenerateSignedUploadUrl(testObjectName, "text/plain", TimeSpan.FromMinutes(5));

                diagnostico.UrlFirmadaGenerada = true;
                diagnostico.UrlFirmadaMuestra = result.SignedUrl;

                // Parsear la URL para mostrar componentes
                var uri = new Uri(result.SignedUrl);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                diagnostico.UrlComponentes = new UrlFirmadaComponentes
                {
                    Host = uri.Host,
                    Path = uri.AbsolutePath,
                    Algorithm = queryParams["X-Goog-Algorithm"],
                    Credential = queryParams["X-Goog-Credential"],
                    Date = queryParams["X-Goog-Date"],
                    Expires = queryParams["X-Goog-Expires"],
                    SignedHeaders = queryParams["X-Goog-SignedHeaders"],
                    SignatureLength = queryParams["X-Goog-Signature"]?.Length ?? 0
                };

                _logger.LogInformation("[DIAGNÓSTICO] Test 3 OK: URL firmada generada correctamente");
            }
            catch (Exception ex)
            {
                diagnostico.UrlFirmadaGenerada = false;
                diagnostico.UrlFirmadaError = ex.Message;
                _logger.LogError(ex, "[DIAGNÓSTICO] Test 3 FALLÓ: Error generando URL firmada");
            }

            // Resultado final
            diagnostico.TodosLosTestsExitosos =
                diagnostico.ListarObjetosExito &&
                diagnostico.SubirArchivoExito &&
                diagnostico.UrlFirmadaGenerada;

            if (diagnostico.TodosLosTestsExitosos)
            {
                diagnostico.Conclusion = "Las credenciales de GCS funcionan correctamente para operaciones directas. " +
                    "Si las URLs firmadas fallan en el navegador, el problema puede ser: " +
                    "1) La clave del service account fue rotada en GCP pero el archivo local es viejo, " +
                    "2) CORS del bucket, o " +
                    "3) Diferencias en cómo el navegador envía headers.";
            }
            else
            {
                diagnostico.Conclusion = "Hay problemas con las credenciales de GCS. Verifique: " +
                    "1) Que el archivo gcs-credentials.json es válido y actualizado, " +
                    "2) Que el service account tiene permisos en el bucket.";
            }

            return Ok(diagnostico);
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
        /// <summary>
        /// Content-Type EXACTO que se usó para firmar la URL.
        /// El cliente DEBE usar exactamente este valor (byte por byte) al hacer el PUT request a GCS.
        /// NO use el tipo del archivo (file.type), use ESTE valor.
        /// </summary>
        public string? ContentType { get; set; }
        /// <summary>
        /// Bytes hexadecimales del Content-Type firmado para verificación de debugging.
        /// Útil para diagnosticar errores SignatureDoesNotMatch.
        /// </summary>
        public string? ContentTypeHex { get; set; }
        /// <summary>
        /// Longitud exacta del Content-Type firmado en bytes.
        /// </summary>
        public int ContentTypeLength { get; set; }
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

    public class DiagnosticoGcsResponse
    {
        public DateTime Timestamp { get; set; }
        public string? BucketName { get; set; }

        // Test 1: Listar objetos
        public bool ListarObjetosExito { get; set; }
        public int ObjetosEncontrados { get; set; }
        public string? ListarObjetosError { get; set; }

        // Test 2: Subir archivo
        public bool SubirArchivoExito { get; set; }
        public string? ArchivoSubido { get; set; }
        public string? SubirArchivoError { get; set; }

        // Test 3: URL firmada
        public bool UrlFirmadaGenerada { get; set; }
        public string? UrlFirmadaMuestra { get; set; }
        public string? UrlFirmadaError { get; set; }
        public UrlFirmadaComponentes? UrlComponentes { get; set; }

        // Resultado final
        public bool TodosLosTestsExitosos { get; set; }
        public string? Conclusion { get; set; }
    }

    public class UrlFirmadaComponentes
    {
        public string? Host { get; set; }
        public string? Path { get; set; }
        public string? Algorithm { get; set; }
        public string? Credential { get; set; }
        public string? Date { get; set; }
        public string? Expires { get; set; }
        public string? SignedHeaders { get; set; }
        public int SignatureLength { get; set; }
    }
}