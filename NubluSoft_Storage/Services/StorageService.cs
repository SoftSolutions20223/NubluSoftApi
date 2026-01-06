using Dapper;
using Microsoft.Extensions.Options;
using NubluSoft_Storage.Configuration;
using NubluSoft_Storage.Helpers;
using NubluSoft_Storage.Models.DTOs;
using NubluSoft_Storage.Services;
using System.IO.Compression;

namespace NubluSoft_Storage.Services
{
    /// <summary>
    /// Implementación del servicio orquestador de storage
    /// Coordina operaciones entre GCS y PostgreSQL
    /// </summary>
    public class StorageService : IStorageService
    {
        private readonly IGcsStorageService _gcsService;
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly GcsSettings _gcsSettings;
        private readonly ILogger<StorageService> _logger;

        public StorageService(
            IGcsStorageService gcsService,
            IPostgresConnectionFactory connectionFactory,
            IOptions<GcsSettings> gcsSettings,
            ILogger<StorageService> logger)
        {
            _gcsService = gcsService;
            _connectionFactory = connectionFactory;
            _gcsSettings = gcsSettings.Value;
            _logger = logger;
        }

        // ==================== UPLOAD ====================

        public async Task<UploadResponse> UploadFileAsync(
            IFormFile file,
            UploadRequest request,
            long usuarioId,
            long entidadId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validar tamaño
                if (file.Length > _gcsSettings.MaxFileSizeBytes)
                {
                    return new UploadResponse
                    {
                        Exito = false,
                        Mensaje = $"El archivo excede el tamaño máximo permitido ({_gcsSettings.MaxFileSizeMB} MB)"
                    };
                }

                // Generar nombre único en GCS
                var fileName = request.NombrePersonalizado ?? file.FileName;
                var extension = Path.GetExtension(fileName);
                var objectName = GenerateObjectName(entidadId, request.CarpetaId, fileName);
                var contentType = file.ContentType ?? MimeTypeHelper.GetMimeType(fileName);

                _logger.LogInformation("Subiendo archivo: {FileName} -> {ObjectName}, Size: {Size}",
                    fileName, objectName, file.Length);

                // Subir a GCS con streaming
                await using var stream = file.OpenReadStream();
                var gcsResult = await _gcsService.UploadStreamingAsync(
                    stream,
                    objectName,
                    contentType,
                    file.Length,
                    new Dictionary<string, string>
                    {
                        { "originalFileName", fileName },
                        { "uploadedBy", usuarioId.ToString() },
                        { "entidadId", entidadId.ToString() },
                        { "carpetaId", request.CarpetaId.ToString() }
                    },
                    cancellationToken);

                if (!gcsResult.Success)
                {
                    return new UploadResponse
                    {
                        Exito = false,
                        Mensaje = gcsResult.Error ?? "Error al subir archivo a storage"
                    };
                }

                // Registrar en base de datos
                var archivoId = await RegistrarArchivoEnBdAsync(
                    fileName,
                    objectName,
                    gcsResult.Size,
                    gcsResult.Hash,
                    contentType,
                    request,
                    usuarioId,
                    cancellationToken);

                if (archivoId == null)
                {
                    // Rollback: eliminar de GCS si falla BD
                    await _gcsService.DeleteAsync(objectName, cancellationToken);
                    return new UploadResponse
                    {
                        Exito = false,
                        Mensaje = "Error al registrar archivo en base de datos"
                    };
                }

                return new UploadResponse
                {
                    Exito = true,
                    Mensaje = "Archivo subido exitosamente",
                    ArchivoId = archivoId,
                    Nombre = fileName,
                    Ruta = objectName,
                    Tamano = gcsResult.Size,
                    Hash = gcsResult.Hash,
                    ContentType = contentType,
                    FechaSubida = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo {FileName}", file.FileName);
                return new UploadResponse
                {
                    Exito = false,
                    Mensaje = $"Error al subir archivo: {ex.Message}"
                };
            }
        }

        public async Task<List<UploadResponse>> UploadMultipleAsync(
            IFormFileCollection files,
            long carpetaId,
            long usuarioId,
            long entidadId,
            CancellationToken cancellationToken = default)
        {
            var results = new List<UploadResponse>();

            foreach (var file in files)
            {
                var request = new UploadRequest { CarpetaId = carpetaId };
                var result = await UploadFileAsync(file, request, usuarioId, entidadId, cancellationToken);
                results.Add(result);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            return results;
        }

        // ==================== DOWNLOAD ====================

        public async Task<SignedUrlResponse> GetDownloadUrlAsync(
            long archivoId,
            long usuarioId,
            int? expirationMinutes = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var archivo = await ObtenerArchivoAsync(archivoId, cancellationToken);

                if (archivo == null)
                {
                    return new SignedUrlResponse
                    {
                        Exito = false,
                        Mensaje = "Archivo no encontrado"
                    };
                }

                var expiration = TimeSpan.FromMinutes(expirationMinutes ?? _gcsSettings.SignedUrlExpiration);
                var signedUrl = _gcsService.GenerateSignedDownloadUrl(
                    archivo.Ruta,
                    expiration,
                    archivo.Nombre);

                return new SignedUrlResponse
                {
                    Exito = true,
                    Mensaje = "URL generada exitosamente",
                    Url = signedUrl,
                    FileName = archivo.Nombre,
                    ContentType = archivo.ContentType,
                    Size = archivo.Tamano,
                    ExpiresInSeconds = (int)expiration.TotalSeconds,
                    ExpiresAt = DateTime.UtcNow.Add(expiration)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL de descarga para archivo {ArchivoId}", archivoId);
                return new SignedUrlResponse
                {
                    Exito = false,
                    Mensaje = $"Error al generar URL de descarga: {ex.Message}"
                };
            }
        }

        public async Task<BatchSignedUrlResponse> GetBatchDownloadUrlsAsync(
            BatchSignedUrlRequest request,
            long usuarioId,
            CancellationToken cancellationToken = default)
        {
            var urls = new List<SignedUrlItem>();

            foreach (var archivoId in request.ArchivoIds)
            {
                var result = await GetDownloadUrlAsync(archivoId, usuarioId, request.ExpirationMinutes, cancellationToken);

                urls.Add(new SignedUrlItem
                {
                    ArchivoId = archivoId,
                    Url = result.Url,
                    FileName = result.FileName,
                    Error = result.Exito ? null : result.Mensaje
                });
            }

            return new BatchSignedUrlResponse
            {
                Exito = true,
                Mensaje = $"Se generaron {urls.Count(u => u.Url != null)} URLs",
                Urls = urls
            };
        }

        public async Task<(Stream Stream, string FileName, string ContentType, long Size)?> DownloadFileAsync(
            long archivoId,
            long usuarioId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var archivo = await ObtenerArchivoAsync(archivoId, cancellationToken);

                if (archivo == null)
                    return null;

                var stream = await _gcsService.DownloadAsStreamAsync(archivo.Ruta, cancellationToken);

                return (stream, archivo.Nombre, archivo.ContentType ?? "application/octet-stream", archivo.Tamano);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar archivo {ArchivoId}", archivoId);
                return null;
            }
        }

        public async Task<FolderDownloadInfo?> GetFolderDownloadInfoAsync(
            long carpetaId,
            long usuarioId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                WITH RECURSIVE carpetas_recursivas AS (
                    SELECT ""Cod"", ""Nombre"", ""CarpetaPadre"", ""Nombre"" as ruta
                    FROM documentos.""Carpetas""
                    WHERE ""Cod"" = @CarpetaId AND ""Estado"" = true
                    
                    UNION ALL
                    
                    SELECT c.""Cod"", c.""Nombre"", c.""CarpetaPadre"", 
                           cr.ruta || '/' || c.""Nombre""
                    FROM documentos.""Carpetas"" c
                    INNER JOIN carpetas_recursivas cr ON c.""CarpetaPadre"" = cr.""Cod""
                    WHERE c.""Estado"" = true
                )
                SELECT 
                    a.""Cod"" as ArchivoId,
                    a.""Nombre"",
                    cr.ruta || '/' || a.""Nombre"" as RutaRelativa,
                    a.""Ruta"" as GcsObjectName,
                    COALESCE(a.""Tamano"", 0) as Tamano,
                    a.""ContentType""
                FROM documentos.""Archivos"" a
                INNER JOIN carpetas_recursivas cr ON a.""Carpeta"" = cr.""Cod""
                WHERE a.""Estado"" = true
                ORDER BY cr.ruta, a.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                var archivos = (await connection.QueryAsync<ArchivoEnCarpeta>(sql, new { CarpetaId = carpetaId })).ToList();

                if (!archivos.Any())
                    return null;

                // Obtener nombre de la carpeta
                var carpetaNombre = await connection.QueryFirstOrDefaultAsync<string>(
                    @"SELECT ""Nombre"" FROM documentos.""Carpetas"" WHERE ""Cod"" = @CarpetaId",
                    new { CarpetaId = carpetaId });

                var totalBytes = archivos.Sum(a => a.Tamano);

                return new FolderDownloadInfo
                {
                    CarpetaId = carpetaId,
                    NombreCarpeta = carpetaNombre ?? $"Carpeta_{carpetaId}",
                    TotalArchivos = archivos.Count,
                    TamanoTotalBytes = totalBytes,
                    TamanoFormateado = FileSizeHelper.FormatSize(totalBytes),
                    Archivos = archivos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener info de carpeta {CarpetaId}", carpetaId);
                return null;
            }
        }

        public async Task DownloadFolderAsZipAsync(
            long carpetaId,
            long usuarioId,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            var folderInfo = await GetFolderDownloadInfoAsync(carpetaId, usuarioId, cancellationToken);

            if (folderInfo == null || !folderInfo.Archivos.Any())
            {
                throw new InvalidOperationException("Carpeta vacía o no encontrada");
            }

            _logger.LogInformation("Generando ZIP para carpeta {CarpetaId}: {Count} archivos, {Size}",
                carpetaId, folderInfo.TotalArchivos, folderInfo.TamanoFormateado);

            using var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true);

            foreach (var archivo in folderInfo.Archivos)
            {
                if (string.IsNullOrEmpty(archivo.GcsObjectName))
                    continue;

                try
                {
                    var entry = archive.CreateEntry(archivo.RutaRelativa, CompressionLevel.Optimal);

                    await using var entryStream = entry.Open();
                    await _gcsService.DownloadToStreamAsync(archivo.GcsObjectName, entryStream, cancellationToken);

                    _logger.LogDebug("Agregado al ZIP: {Path}", archivo.RutaRelativa);
                }
                catch (FileNotFoundException)
                {
                    _logger.LogWarning("Archivo no encontrado en GCS, omitiendo: {Path}", archivo.GcsObjectName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error agregando archivo al ZIP: {Path}", archivo.RutaRelativa);
                }

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            _logger.LogInformation("ZIP generado exitosamente para carpeta {CarpetaId}", carpetaId);
        }

        // ==================== VERSIONES ====================

        public async Task<VersionResponse> CreateVersionAsync(
            IFormFile file,
            CreateVersionRequest request,
            long usuarioId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtener archivo actual
                var archivo = await ObtenerArchivoAsync(request.ArchivoId, cancellationToken);

                if (archivo == null)
                {
                    return new VersionResponse
                    {
                        Exito = false,
                        Mensaje = "Archivo no encontrado"
                    };
                }

                // Generar nuevo nombre para la versión
                var newVersion = archivo.VersionActual + 1;
                var extension = Path.GetExtension(archivo.Nombre);
                var baseName = Path.GetFileNameWithoutExtension(archivo.Ruta);
                var newObjectName = $"{baseName}_v{newVersion}{extension}";

                var contentType = file.ContentType ?? archivo.ContentType ?? "application/octet-stream";

                // Subir nueva versión a GCS
                await using var stream = file.OpenReadStream();
                var gcsResult = await _gcsService.UploadStreamingAsync(
                    stream,
                    newObjectName,
                    contentType,
                    file.Length,
                    new Dictionary<string, string>
                    {
                        { "archivoId", request.ArchivoId.ToString() },
                        { "version", newVersion.ToString() },
                        { "uploadedBy", usuarioId.ToString() }
                    },
                    cancellationToken);

                if (!gcsResult.Success)
                {
                    return new VersionResponse
                    {
                        Exito = false,
                        Mensaje = gcsResult.Error ?? "Error al subir nueva versión"
                    };
                }

                // Registrar versión en BD
                var versionId = await RegistrarVersionEnBdAsync(
                    request.ArchivoId,
                    newObjectName,
                    gcsResult.Size,
                    gcsResult.Hash,
                    request.Comentario,
                    usuarioId,
                    cancellationToken);

                if (versionId == null)
                {
                    await _gcsService.DeleteAsync(newObjectName, cancellationToken);
                    return new VersionResponse
                    {
                        Exito = false,
                        Mensaje = "Error al registrar versión en base de datos"
                    };
                }

                return new VersionResponse
                {
                    Exito = true,
                    Mensaje = "Nueva versión creada exitosamente",
                    ArchivoId = request.ArchivoId,
                    VersionNumero = newVersion,
                    Ruta = newObjectName,
                    Hash = gcsResult.Hash,
                    Tamano = gcsResult.Size,
                    FechaCreacion = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear versión para archivo {ArchivoId}", request.ArchivoId);
                return new VersionResponse
                {
                    Exito = false,
                    Mensaje = $"Error al crear versión: {ex.Message}"
                };
            }
        }

        public async Task<VersionHistoryResponse?> GetVersionHistoryAsync(
            long archivoId,
            long usuarioId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT 
                    v.""Archivo"" as ArchivoId,
                    v.""Version"",
                    v.""Ruta"",
                    v.""Hash"",
                    v.""Tamano"",
                    v.""FechaCreacion"",
                    v.""UsuarioCreador"",
                    u.""Nombre"" || ' ' || COALESCE(u.""Apellidos"", '') as NombreUsuario,
                    v.""Comentario"",
                    CASE WHEN a.""VersionActual"" = v.""Version"" THEN true ELSE false END as EsVersionActual
                FROM documentos.""Versiones_Archivos"" v
                INNER JOIN documentos.""Archivos"" a ON v.""Archivo"" = a.""Cod""
                LEFT JOIN usuarios.""Usuarios"" u ON v.""UsuarioCreador"" = u.""Cod""
                WHERE v.""Archivo"" = @ArchivoId
                ORDER BY v.""Version"" DESC";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                var versiones = (await connection.QueryAsync<VersionInfo>(sql, new { ArchivoId = archivoId })).ToList();

                if (!versiones.Any())
                    return null;

                var archivo = await ObtenerArchivoAsync(archivoId, cancellationToken);

                return new VersionHistoryResponse
                {
                    ArchivoId = archivoId,
                    NombreArchivo = archivo?.Nombre ?? string.Empty,
                    VersionActual = archivo?.VersionActual ?? 1,
                    TotalVersiones = versiones.Count,
                    Versiones = versiones
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de versiones de archivo {ArchivoId}", archivoId);
                return null;
            }
        }

        public async Task<SignedUrlResponse> GetVersionDownloadUrlAsync(
            long archivoId,
            int version,
            long usuarioId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT ""Ruta"", ""Tamano""
                FROM documentos.""Versiones_Archivos""
                WHERE ""Archivo"" = @ArchivoId AND ""Version"" = @Version";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                var versionInfo = await connection.QueryFirstOrDefaultAsync<(string Ruta, long Tamano)>(sql,
                    new { ArchivoId = archivoId, Version = version });

                if (string.IsNullOrEmpty(versionInfo.Ruta))
                {
                    return new SignedUrlResponse
                    {
                        Exito = false,
                        Mensaje = "Versión no encontrada"
                    };
                }

                var archivo = await ObtenerArchivoAsync(archivoId, cancellationToken);
                var expiration = TimeSpan.FromMinutes(_gcsSettings.SignedUrlExpiration);

                var signedUrl = _gcsService.GenerateSignedDownloadUrl(
                    versionInfo.Ruta,
                    expiration,
                    $"{Path.GetFileNameWithoutExtension(archivo?.Nombre)}_v{version}{Path.GetExtension(archivo?.Nombre)}");

                return new SignedUrlResponse
                {
                    Exito = true,
                    Url = signedUrl,
                    Size = versionInfo.Tamano,
                    ExpiresInSeconds = (int)expiration.TotalSeconds,
                    ExpiresAt = DateTime.UtcNow.Add(expiration)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL para versión {Version} de archivo {ArchivoId}", version, archivoId);
                return new SignedUrlResponse
                {
                    Exito = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        // ==================== GESTIÓN ====================

        public async Task<StorageResult> DeleteFileAsync(
            long archivoId,
            long usuarioId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var archivo = await ObtenerArchivoAsync(archivoId, cancellationToken);

                if (archivo == null)
                {
                    return StorageResult.Failure("Archivo no encontrado");
                }

                // Eliminar de GCS
                var gcsDeleted = await _gcsService.DeleteAsync(archivo.Ruta, cancellationToken);

                // Marcar como eliminado en BD (soft delete)
                const string sql = @"
                    UPDATE documentos.""Archivos"" 
                    SET ""Estado"" = false, ""FechaEliminacion"" = NOW(), ""EliminadoPor"" = @UsuarioId
                    WHERE ""Cod"" = @ArchivoId";

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);
                await connection.ExecuteAsync(sql, new { ArchivoId = archivoId, UsuarioId = usuarioId });

                _logger.LogInformation("Archivo eliminado: {ArchivoId}, GCS: {GcsDeleted}", archivoId, gcsDeleted);

                return StorageResult.Success("Archivo eliminado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar archivo {ArchivoId}", archivoId);
                return StorageResult.Failure($"Error al eliminar archivo: {ex.Message}");
            }
        }

        public async Task<bool> FileExistsAsync(
            long archivoId,
            CancellationToken cancellationToken = default)
        {
            var archivo = await ObtenerArchivoAsync(archivoId, cancellationToken);
            return archivo != null;
        }

        public async Task<StorageStats> GetStorageStatsAsync(
            long entidadId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT 
                    COUNT(*) as TotalArchivos,
                    COALESCE(SUM(""Tamano""), 0) as TotalBytes
                FROM documentos.""Archivos"" a
                INNER JOIN documentos.""Carpetas"" c ON a.""Carpeta"" = c.""Cod""
                WHERE c.""Entidad"" = @EntidadId AND a.""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                var stats = await connection.QueryFirstAsync<(long TotalArchivos, long TotalBytes)>(sql,
                    new { EntidadId = entidadId });

                return new StorageStats
                {
                    EntidadId = entidadId,
                    TotalArchivos = stats.TotalArchivos,
                    TotalBytes = stats.TotalBytes,
                    TotalFormateado = FileSizeHelper.FormatSize(stats.TotalBytes)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de storage para entidad {EntidadId}", entidadId);
                return new StorageStats { EntidadId = entidadId };
            }
        }

        // ==================== MÉTODOS PRIVADOS ====================

        private string GenerateObjectName(long entidadId, long carpetaId, string fileName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            var safeFileName = SanitizeFileName(fileName);

            return $"entidad_{entidadId}/carpeta_{carpetaId}/{timestamp}_{guid}_{safeFileName}";
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Length > 100 ? sanitized[..100] : sanitized;
        }

        private async Task<ArchivoInfo?> ObtenerArchivoAsync(long archivoId, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT 
                    ""Cod"" as ArchivoId,
                    ""Nombre"",
                    ""Ruta"",
                    COALESCE(""Tamano"", 0) as Tamano,
                    ""ContentType"",
                    COALESCE(""VersionActual"", 1) as VersionActual
                FROM documentos.""Archivos""
                WHERE ""Cod"" = @ArchivoId AND ""Estado"" = true";

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<ArchivoInfo>(sql, new { ArchivoId = archivoId });
        }

        private async Task<long?> RegistrarArchivoEnBdAsync(
            string nombre,
            string ruta,
            long tamano,
            string? hash,
            string? contentType,
            UploadRequest request,
            long usuarioId,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                INSERT INTO documentos.""Archivos"" (
                    ""Nombre"", ""Carpeta"", ""Ruta"", ""Tamano"", ""Hash"", ""ContentType"",
                    ""Descripcion"", ""FechaDocumento"", ""CodigoDocumento"", ""TipoDocumental"",
                    ""MetadatosAdicionales"", ""FechaCreacion"", ""UsuarioCreador"", ""Estado"", ""VersionActual""
                ) VALUES (
                    @Nombre, @Carpeta, @Ruta, @Tamano, @Hash, @ContentType,
                    @Descripcion, @FechaDocumento, @CodigoDocumento, @TipoDocumental,
                    @MetadatosAdicionales, NOW(), @UsuarioCreador, true, 1
                ) RETURNING ""Cod""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                var archivoId = await connection.QueryFirstAsync<long>(sql, new
                {
                    Nombre = nombre,
                    Carpeta = request.CarpetaId,
                    Ruta = ruta,
                    Tamano = tamano,
                    Hash = hash,
                    ContentType = contentType,
                    request.Descripcion,
                    request.FechaDocumento,
                    request.CodigoDocumento,
                    request.TipoDocumental,
                    request.MetadatosAdicionales,
                    UsuarioCreador = usuarioId
                });

                return archivoId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar archivo en BD");
                return null;
            }
        }

        private async Task<long?> RegistrarVersionEnBdAsync(
            long archivoId,
            string ruta,
            long tamano,
            string? hash,
            string? comentario,
            long usuarioId,
            CancellationToken cancellationToken)
        {
            const string sqlInsertVersion = @"
                INSERT INTO documentos.""Versiones_Archivos"" (
                    ""Archivo"", ""Version"", ""Ruta"", ""Tamano"", ""Hash"", 
                    ""Comentario"", ""FechaCreacion"", ""UsuarioCreador""
                ) 
                SELECT 
                    @ArchivoId, 
                    COALESCE(MAX(""Version""), 0) + 1,
                    @Ruta, @Tamano, @Hash, @Comentario, NOW(), @UsuarioCreador
                FROM documentos.""Versiones_Archivos""
                WHERE ""Archivo"" = @ArchivoId
                RETURNING ""Version""";

            const string sqlUpdateArchivo = @"
                UPDATE documentos.""Archivos"" 
                SET ""VersionActual"" = @Version, ""Ruta"" = @Ruta, ""Tamano"" = @Tamano, ""Hash"" = @Hash
                WHERE ""Cod"" = @ArchivoId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                var newVersion = await connection.QueryFirstAsync<int>(sqlInsertVersion, new
                {
                    ArchivoId = archivoId,
                    Ruta = ruta,
                    Tamano = tamano,
                    Hash = hash,
                    Comentario = comentario,
                    UsuarioCreador = usuarioId
                }, transaction);

                await connection.ExecuteAsync(sqlUpdateArchivo, new
                {
                    ArchivoId = archivoId,
                    Version = newVersion,
                    Ruta = ruta,
                    Tamano = tamano,
                    Hash = hash
                }, transaction);

                await transaction.CommitAsync(cancellationToken);

                return newVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar versión en BD para archivo {ArchivoId}", archivoId);
                return null;
            }
        }

        /// <summary>
        /// DTO interno para información de archivo
        /// </summary>
        private class ArchivoInfo
        {
            public long ArchivoId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Ruta { get; set; } = string.Empty;
            public long Tamano { get; set; }
            public string? ContentType { get; set; }
            public int VersionActual { get; set; }
        }
    }
}
