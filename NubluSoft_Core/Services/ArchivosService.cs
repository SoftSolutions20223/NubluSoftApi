using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class ArchivosService : IArchivosService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly IStorageClientService _storageClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ArchivosService> _logger;

        public ArchivosService(
            IPostgresConnectionFactory connectionFactory,
            IStorageClientService storageClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ArchivosService> logger)
        {
            _connectionFactory = connectionFactory;
            _storageClient = storageClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string? GetAuthToken()
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            return authHeader?.Replace("Bearer ", "");
        }

        // ==================== CONSULTAS ====================

        public async Task<IEnumerable<Archivo>> ObtenerPorCarpetaAsync(long carpetaId)
        {
            // NOTA: La tabla usa "Tamaño" con tilde y "FechaIncorporacion"
            const string sql = @"
                SELECT
                    a.""Cod"", a.""Nombre"", a.""Ruta"", a.""Carpeta"", a.""Estado"", a.""Indice"",
                    a.""FechaIncorporacion"" AS ""FechaSubida"",
                    a.""FechaDocumento"",
                    a.""CodigoDocumento"",
                    a.""Descripcion"",
                    a.""SubidoPor"", a.""TipoArchivo"", a.""TipoDocumental"", a.""OrigenDocumento"",
                    a.""PaginaInicio"", a.""PaginaFin"", a.""Hash"",
                    a.""Tamaño""::bigint AS ""Tamano"",
                    a.""Version"",
                    a.""TipoFirma"", a.""Firmado"", a.""Observaciones"" AS ""MetadatosAdicionales"",
                    a.""ContentType"", a.""EstadoUpload"",
                    c.""Nombre"" AS ""NombreCarpeta"",
                    u.""Nombres"" || ' ' || COALESCE(u.""Apellidos"", '') AS ""NombreSubidoPor"",
                    ta.""Nombre"" AS ""NombreTipoArchivo"",
                    td.""Nombre"" AS ""NombreTipoDocumental"",
                    od.""Nombre"" AS ""NombreOrigenDocumento"",
                    CASE
                        WHEN POSITION('.' IN a.""Nombre"") > 0
                        THEN LOWER(SUBSTRING(a.""Nombre"" FROM '\.([^.]+)$'))
                        ELSE NULL
                    END AS ""Extension""
                FROM documentos.""Archivos"" a
                LEFT JOIN documentos.""Carpetas"" c ON a.""Carpeta"" = c.""Cod"" AND c.""Estado"" = true
                LEFT JOIN usuarios.""Usuarios"" u ON a.""SubidoPor"" = u.""Cod""
                LEFT JOIN documentos.""Tipos_Archivos"" ta ON a.""TipoArchivo"" = ta.""Cod""
                LEFT JOIN documentos.""Tipos_Documentales"" td ON a.""TipoDocumental"" = td.""Cod""
                LEFT JOIN documentos.""Origenes_Documentos"" od ON a.""OrigenDocumento"" = od.""Cod""::text
                WHERE a.""Carpeta"" = @CarpetaId AND a.""Estado"" = true
                ORDER BY a.""Indice"", a.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Archivo>(sql, new { CarpetaId = carpetaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo archivos de carpeta {CarpetaId}", carpetaId);
                return Enumerable.Empty<Archivo>();
            }
        }

        public async Task<IEnumerable<Archivo>> ObtenerConFiltrosAsync(FiltrosArchivosRequest filtros)
        {
            // NOTA: La tabla usa "Tamaño" con tilde y "FechaIncorporacion"
            var sql = @"
                SELECT
                    a.""Cod"", a.""Nombre"", a.""Ruta"", a.""Carpeta"", a.""Estado"", a.""Indice"",
                    a.""FechaIncorporacion"" AS ""FechaSubida"",
                    a.""FechaDocumento"",
                    a.""CodigoDocumento"",
                    a.""Descripcion"",
                    a.""SubidoPor"", a.""TipoArchivo"", a.""TipoDocumental"", a.""OrigenDocumento"",
                    a.""PaginaInicio"", a.""PaginaFin"", a.""Hash"",
                    a.""Tamaño""::bigint AS ""Tamano"",
                    a.""Version"",
                    a.""ContentType"", a.""EstadoUpload"",
                    c.""Nombre"" AS ""NombreCarpeta"",
                    u.""Nombres"" || ' ' || COALESCE(u.""Apellidos"", '') AS ""NombreSubidoPor"",
                    td.""Nombre"" AS ""NombreTipoDocumental""
                FROM documentos.""Archivos"" a
                LEFT JOIN documentos.""Carpetas"" c ON a.""Carpeta"" = c.""Cod"" AND c.""Estado"" = true
                LEFT JOIN usuarios.""Usuarios"" u ON a.""SubidoPor"" = u.""Cod""
                LEFT JOIN documentos.""Tipos_Documentales"" td ON a.""TipoDocumental"" = td.""Cod""
                WHERE 1=1";

            var parameters = new DynamicParameters();

            if (filtros.SoloActivos)
            {
                sql += @" AND a.""Estado"" = true";
            }

            if (filtros.Carpeta.HasValue)
            {
                sql += @" AND a.""Carpeta"" = @Carpeta";
                parameters.Add("Carpeta", filtros.Carpeta.Value);
            }

            if (filtros.TipoDocumental.HasValue)
            {
                sql += @" AND a.""TipoDocumental"" = @TipoDocumental";
                parameters.Add("TipoDocumental", filtros.TipoDocumental.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtros.Busqueda))
            {
                sql += @" AND (a.""Nombre"" ILIKE @Busqueda OR a.""Descripcion"" ILIKE @Busqueda OR a.""CodigoDocumento"" ILIKE @Busqueda)";
                parameters.Add("Busqueda", $"%{filtros.Busqueda}%");
            }

            if (filtros.FechaDesde.HasValue)
            {
                sql += @" AND a.""FechaIncorporacion"" >= @FechaDesde";
                parameters.Add("FechaDesde", filtros.FechaDesde.Value);
            }

            if (filtros.FechaHasta.HasValue)
            {
                sql += @" AND a.""FechaIncorporacion"" <= @FechaHasta";
                parameters.Add("FechaHasta", filtros.FechaHasta.Value.AddDays(1));
            }

            sql += @" ORDER BY a.""FechaIncorporacion"" DESC, a.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Archivo>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo archivos con filtros");
                return Enumerable.Empty<Archivo>();
            }
        }

        public async Task<Archivo?> ObtenerPorIdAsync(long archivoId)
        {
            // NOTA: La tabla usa "Tamaño" con tilde y "FechaIncorporacion"
            const string sql = @"
                SELECT
                    a.""Cod"", a.""Nombre"", a.""Ruta"", a.""Carpeta"", a.""Estado"", a.""Indice"",
                    a.""FechaIncorporacion"" AS ""FechaSubida"",
                    a.""FechaDocumento"",
                    a.""CodigoDocumento"",
                    a.""Descripcion"",
                    a.""SubidoPor"", a.""TipoArchivo"", a.""TipoDocumental"", a.""OrigenDocumento"",
                    a.""PaginaInicio"", a.""PaginaFin"", a.""Hash"",
                    a.""Tamaño""::bigint AS ""Tamano"",
                    a.""Version"",
                    a.""TipoFirma"", a.""Firmado"", a.""Observaciones"" AS ""MetadatosAdicionales"",
                    a.""ContentType"", a.""EstadoUpload"",
                    c.""Nombre"" AS ""NombreCarpeta"",
                    u.""Nombres"" || ' ' || COALESCE(u.""Apellidos"", '') AS ""NombreSubidoPor"",
                    ta.""Nombre"" AS ""NombreTipoArchivo"",
                    td.""Nombre"" AS ""NombreTipoDocumental"",
                    od.""Nombre"" AS ""NombreOrigenDocumento"",
                    CASE
                        WHEN POSITION('.' IN a.""Nombre"") > 0
                        THEN LOWER(SUBSTRING(a.""Nombre"" FROM '\.([^.]+)$'))
                        ELSE NULL
                    END AS ""Extension""
                FROM documentos.""Archivos"" a
                LEFT JOIN documentos.""Carpetas"" c ON a.""Carpeta"" = c.""Cod"" AND c.""Estado"" = true
                LEFT JOIN usuarios.""Usuarios"" u ON a.""SubidoPor"" = u.""Cod""
                LEFT JOIN documentos.""Tipos_Archivos"" ta ON a.""TipoArchivo"" = ta.""Cod""
                LEFT JOIN documentos.""Tipos_Documentales"" td ON a.""TipoDocumental"" = td.""Cod""
                LEFT JOIN documentos.""Origenes_Documentos"" od ON a.""OrigenDocumento"" = od.""Cod""::text
                WHERE a.""Cod"" = @ArchivoId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<Archivo>(sql, new { ArchivoId = archivoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo archivo {ArchivoId}: {Message}", archivoId, ex.Message);
                return null;
            }
        }

        public async Task<IEnumerable<VersionArchivo>> ObtenerVersionesAsync(long archivoId)
        {
            // NOTA: La tabla usa "Tamaño" con tilde y "FechaVersion"
            const string sql = @"
                SELECT
                    v.""Cod"", v.""Archivo"", v.""Version"", v.""Ruta"", v.""Hash"",
                    v.""Tamaño""::bigint AS ""Tamano"",
                    v.""FechaVersion"" AS ""FechaCreacion"",
                    v.""CreadoPor"", v.""Comentario"", v.""EsVersionActual"",
                    u.""Nombres"" || ' ' || COALESCE(u.""Apellidos"", '') AS ""NombreCreadoPor""
                FROM documentos.""Versiones_Archivos"" v
                LEFT JOIN usuarios.""Usuarios"" u ON v.""CreadoPor"" = u.""Cod""
                WHERE v.""Archivo"" = @ArchivoId
                ORDER BY v.""Version"" DESC";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<VersionArchivo>(sql, new { ArchivoId = archivoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo versiones de archivo {ArchivoId}", archivoId);
                return Enumerable.Empty<VersionArchivo>();
            }
        }

        // ==================== OPERACIONES CRUD ====================

        public async Task<ResultadoArchivo> CrearAsync(long usuarioId, CrearArchivoRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_CrearArchivo""(
                    p_Nombre := @Nombre,
                    p_Ruta := @Ruta,
                    p_Carpeta := @Carpeta,
                    p_Usuario := @Usuario,
                    p_Descripcion := @Descripcion,
                    p_FechaDocumento := @FechaDocumento,
                    p_CodigoDocumento := @CodigoDocumento,
                    p_TipoArchivo := @TipoArchivo,
                    p_TipoDocumental := @TipoDocumental,
                    p_OrigenDocumento := @OrigenDocumento,
                    p_PaginaInicio := @PaginaInicio,
                    p_PaginaFin := @PaginaFin,
                    p_Hash := @Hash,
                    p_Tamano := @Tamano,
                    p_MetadatosAdicionales := @MetadatosAdicionales
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoArchivo>(sql, new
                {
                    request.Nombre,
                    request.Ruta,
                    request.Carpeta,
                    Usuario = usuarioId,
                    request.Descripcion,
                    request.FechaDocumento,
                    request.CodigoDocumento,
                    request.TipoArchivo,
                    request.TipoDocumental,
                    request.OrigenDocumento,
                    request.PaginaInicio,
                    request.PaginaFin,
                    request.Hash,
                    request.Tamano,
                    request.MetadatosAdicionales
                });

                return resultado ?? new ResultadoArchivo { Exito = false, Mensaje = "Error al crear el archivo" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando archivo");
                return new ResultadoArchivo { Exito = false, Mensaje = "Error al crear el archivo: " + ex.Message };
            }
        }

        public async Task<(bool Success, string? Error)> ActualizarAsync(long archivoId, long usuarioId, ActualizarArchivoRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Archivos"" SET
                    ""Nombre"" = @Nombre,
                    ""Descripcion"" = @Descripcion,
                    ""FechaDocumento"" = @FechaDocumento,
                    ""CodigoDocumento"" = @CodigoDocumento,
                    ""TipoDocumental"" = @TipoDocumental,
                    ""OrigenDocumento"" = @OrigenDocumento,
                    ""PaginaInicio"" = @PaginaInicio,
                    ""PaginaFin"" = @PaginaFin,
                    ""MetadatosAdicionales"" = @MetadatosAdicionales
                WHERE ""Cod"" = @ArchivoId AND ""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    ArchivoId = archivoId,
                    request.Nombre,
                    request.Descripcion,
                    request.FechaDocumento,
                    request.CodigoDocumento,
                    request.TipoDocumental,
                    request.OrigenDocumento,
                    request.PaginaInicio,
                    request.PaginaFin,
                    request.MetadatosAdicionales
                });

                if (affected == 0)
                    return (false, "Archivo no encontrado");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando archivo {ArchivoId}", archivoId);
                return (false, "Error al actualizar el archivo: " + ex.Message);
            }
        }

        public async Task<ResultadoArchivo> EliminarAsync(long archivoId, long usuarioId)
        {
            const string sqlRuta = @"SELECT ""Ruta"" FROM documentos.""Archivos"" WHERE ""Cod"" = @ArchivoId AND ""Estado"" = true";
            const string sqlEliminar = @"SELECT * FROM documentos.""F_EliminarArchivo""(@ArchivoId, @UsuarioId)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 1. Obtener ruta GCS antes del soft delete
                var rutaGcs = await connection.QueryFirstOrDefaultAsync<string>(sqlRuta,
                    new { ArchivoId = archivoId });

                // 2. Ejecutar soft delete en BD
                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoArchivo>(sqlEliminar,
                    new { ArchivoId = archivoId, UsuarioId = usuarioId });

                if (resultado?.Exito != true)
                {
                    return resultado ?? new ResultadoArchivo { Exito = false, Mensaje = "Error al eliminar el archivo" };
                }

                // 3. Eliminar de GCS (en background, sin bloquear)
                if (!string.IsNullOrEmpty(rutaGcs))
                {
                    var authToken = GetAuthToken();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _storageClient.DeleteFileAsync(rutaGcs, authToken);
                            _logger.LogInformation("Archivo eliminado de GCS: {Ruta}", rutaGcs);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "No se pudo eliminar archivo de GCS: {Ruta}", rutaGcs);
                        }
                    });
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando archivo {ArchivoId}", archivoId);
                return new ResultadoArchivo { Exito = false, Mensaje = "Error al eliminar el archivo: " + ex.Message };
            }
        }

        public async Task<ResultadoArchivo> MoverAsync(long archivoId, long usuarioId, long carpetaDestinoId)
        {
            const string sqlValidar = @"
                SELECT c.""Cod"", c.""TipoCarpeta"", c.""EstadoCarpeta""
                FROM documentos.""Carpetas"" c
                WHERE c.""Cod"" = @CarpetaId AND c.""Estado"" = true";

            const string sqlMover = @"
                UPDATE documentos.""Archivos""
                SET ""Carpeta"" = @CarpetaDestino, ""FechaModificacion"" = NOW()
                WHERE ""Cod"" = @ArchivoId AND ""Estado"" = true
                RETURNING ""Cod""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 1. Validar carpeta destino
                var carpeta = await connection.QueryFirstOrDefaultAsync<dynamic>(sqlValidar,
                    new { CarpetaId = carpetaDestinoId });

                if (carpeta == null)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "Carpeta destino no encontrada" };
                }

                // Solo expedientes (3) y genéricas (4) pueden contener archivos
                if (carpeta.TipoCarpeta != 3 && carpeta.TipoCarpeta != 4)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "Solo se pueden mover archivos a Expedientes o Carpetas Genéricas" };
                }

                if (carpeta.EstadoCarpeta == 2)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "La carpeta destino está cerrada" };
                }

                // 2. Mover archivo
                var moved = await connection.QueryFirstOrDefaultAsync<long?>(sqlMover,
                    new { ArchivoId = archivoId, CarpetaDestino = carpetaDestinoId });

                if (moved == null)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "Archivo no encontrado" };
                }

                _logger.LogInformation("Archivo {ArchivoId} movido a carpeta {CarpetaId} por usuario {UsuarioId}",
                    archivoId, carpetaDestinoId, usuarioId);

                return new ResultadoArchivo
                {
                    Exito = true,
                    ArchivoCod = archivoId,
                    Mensaje = "Archivo movido exitosamente"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moviendo archivo {ArchivoId}", archivoId);
                return new ResultadoArchivo { Exito = false, Mensaje = "Error al mover el archivo: " + ex.Message };
            }
        }

        public async Task<ResultadoArchivo> CopiarAsync(long archivoId, long usuarioId, long carpetaDestinoId, string? nuevoNombre = null)
        {
            const string sqlObtener = @"
                SELECT ""Cod"", ""Nombre"", ""Ruta"", ""Descripcion"", ""Hash"", ""Tamaño"",
                       ""ContentType"", ""TipoArchivo"", ""TipoDocumental"", ""OrigenDocumento"",
                       ""PaginaInicio"", ""PaginaFin"", ""FechaDocumento"", ""CodigoDocumento"",
                       ""Carpeta""
                FROM documentos.""Archivos""
                WHERE ""Cod"" = @ArchivoId AND ""Estado"" = true";

            const string sqlValidar = @"
                SELECT c.""Cod"", c.""TipoCarpeta"", c.""EstadoCarpeta""
                FROM documentos.""Carpetas"" c
                WHERE c.""Cod"" = @CarpetaId AND c.""Estado"" = true";

            const string sqlInsertar = @"
                INSERT INTO documentos.""Archivos"" (
                    ""Cod"", ""Nombre"", ""Ruta"", ""Descripcion"", ""Hash"", ""Tamaño"",
                    ""ContentType"", ""TipoArchivo"", ""TipoDocumental"", ""OrigenDocumento"",
                    ""PaginaInicio"", ""PaginaFin"", ""FechaDocumento"", ""CodigoDocumento"",
                    ""Carpeta"", ""SubidoPor"", ""Estado"", ""EstadoUpload"", ""Version""
                ) VALUES (
                    documentos.""F_SiguienteCod""('Archivos', NULL),
                    @Nombre, @Ruta, @Descripcion, @Hash, @Tamano,
                    @ContentType, @TipoArchivo, @TipoDocumental, @OrigenDocumento,
                    @PaginaInicio, @PaginaFin, @FechaDocumento, @CodigoDocumento,
                    @Carpeta, @Usuario, true, 'COMPLETADO', 1
                ) RETURNING ""Cod""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 1. Obtener archivo origen
                var archivoOrigen = await connection.QueryFirstOrDefaultAsync<dynamic>(sqlObtener,
                    new { ArchivoId = archivoId });

                if (archivoOrigen == null)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "Archivo no encontrado" };
                }

                // 2. Validar carpeta destino
                var carpeta = await connection.QueryFirstOrDefaultAsync<dynamic>(sqlValidar,
                    new { CarpetaId = carpetaDestinoId });

                if (carpeta == null)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "Carpeta destino no encontrada" };
                }

                if (carpeta.TipoCarpeta != 3 && carpeta.TipoCarpeta != 4)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "Solo se pueden copiar archivos a Expedientes o Carpetas Genéricas" };
                }

                if (carpeta.EstadoCarpeta == 2)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "La carpeta destino está cerrada" };
                }

                // 3. Copiar archivo en GCS con nueva ruta
                string rutaOrigen = archivoOrigen.Ruta;
                var extension = Path.GetExtension(rutaOrigen);
                var basePath = rutaOrigen[..^extension.Length];
                var timestamp = DateTime.UtcNow.Ticks;
                var nuevaRuta = $"{basePath}_copy_{timestamp}{extension}";

                var authToken = GetAuthToken();
                var copiado = await _storageClient.CopyFileAsync(rutaOrigen, nuevaRuta, authToken);

                if (!copiado)
                {
                    return new ResultadoArchivo { Exito = false, Mensaje = "No se pudo copiar el archivo en el storage" };
                }

                // 4. Crear nuevo registro en BD
                var nombreArchivo = nuevoNombre ?? archivoOrigen.Nombre;
                var nuevoArchivoCod = await connection.QueryFirstAsync<long>(sqlInsertar, new
                {
                    Nombre = nombreArchivo,
                    Ruta = nuevaRuta,
                    Descripcion = archivoOrigen.Descripcion,
                    Hash = archivoOrigen.Hash,
                    Tamano = archivoOrigen.Tamaño,
                    ContentType = archivoOrigen.ContentType,
                    TipoArchivo = archivoOrigen.TipoArchivo,
                    TipoDocumental = archivoOrigen.TipoDocumental,
                    OrigenDocumento = archivoOrigen.OrigenDocumento,
                    PaginaInicio = archivoOrigen.PaginaInicio,
                    PaginaFin = archivoOrigen.PaginaFin,
                    FechaDocumento = archivoOrigen.FechaDocumento,
                    CodigoDocumento = archivoOrigen.CodigoDocumento,
                    Carpeta = carpetaDestinoId,
                    Usuario = usuarioId
                });

                _logger.LogInformation("Archivo {ArchivoOrigen} copiado a {NuevoArchivo} en carpeta {CarpetaId}",
                    archivoId, nuevoArchivoCod, carpetaDestinoId);

                return new ResultadoArchivo
                {
                    Exito = true,
                    ArchivoCod = nuevoArchivoCod,
                    Mensaje = "Archivo copiado exitosamente"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copiando archivo {ArchivoId}", archivoId);
                return new ResultadoArchivo { Exito = false, Mensaje = "Error al copiar el archivo: " + ex.Message };
            }
        }

        // ==================== VERSIONAMIENTO ====================

        public async Task<ResultadoArchivo> CrearVersionAsync(long archivoId, long usuarioId, CrearVersionRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_CrearVersionArchivo""(
                    p_Archivo := @ArchivoId,
                    p_Ruta := @Ruta,
                    p_Hash := @Hash,
                    p_Tamano := @Tamano,
                    p_Usuario := @Usuario,
                    p_Comentario := @Comentario
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoArchivo>(sql, new
                {
                    ArchivoId = archivoId,
                    request.Ruta,
                    request.Hash,
                    request.Tamano,
                    Usuario = usuarioId,
                    request.Comentario
                });

                return resultado ?? new ResultadoArchivo { Exito = false, Mensaje = "Error al crear la versión" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando versión de archivo {ArchivoId}", archivoId);
                return new ResultadoArchivo { Exito = false, Mensaje = "Error al crear la versión: " + ex.Message };
            }
        }

        public async Task<ResultadoArchivo> RestaurarVersionAsync(long archivoId, long usuarioId, RestaurarVersionRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_RestaurarVersionArchivo""(
                    p_Archivo := @ArchivoId,
                    p_Version := @Version,
                    p_Usuario := @Usuario,
                    p_Comentario := @Comentario
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoArchivo>(sql, new
                {
                    ArchivoId = archivoId,
                    request.Version,
                    Usuario = usuarioId,
                    request.Comentario
                });

                return resultado ?? new ResultadoArchivo { Exito = false, Mensaje = "Error al restaurar la versión" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restaurando versión de archivo {ArchivoId}", archivoId);
                return new ResultadoArchivo { Exito = false, Mensaje = "Error al restaurar la versión: " + ex.Message };
            }
        }

        // ==================== UPLOAD DIRECTO (Opción C) ====================

        public async Task<IniciarUploadResponse> IniciarUploadAsync(
            long usuarioId,
            long entidadId,
            IniciarUploadRequest request,
            string authToken)
        {
            try
            {
                // 1. Validar que la carpeta existe y el usuario tiene permisos
                var carpetaValida = await ValidarCarpetaParaUploadAsync(request.CarpetaId, usuarioId, entidadId);
                if (!carpetaValida.Valida)
                {
                    return new IniciarUploadResponse
                    {
                        Exito = false,
                        Mensaje = carpetaValida.Error ?? "Carpeta no válida para upload"
                    };
                }

                // 2. Generar nombre único del objeto en GCS
                var objectName = GenerarObjectName(entidadId, request.CarpetaId, request.NombreArchivo);

                // 3. Solicitar URL firmada a Storage
                var urlResult = await _storageClient.GetUploadUrlAsync(
                    objectName,
                    request.ContentType,
                    expirationMinutes: 30,
                    authToken);

                if (!urlResult.Success || string.IsNullOrEmpty(urlResult.UploadUrl))
                {
                    _logger.LogError("Error obteniendo URL de upload: {Error}", urlResult.Error);
                    return new IniciarUploadResponse
                    {
                        Exito = false,
                        Mensaje = urlResult.Error ?? "Error al generar URL de upload"
                    };
                }

                // 4. Crear registro en BD con estado PENDIENTE
                var archivoId = await CrearArchivoPendienteAsync(
                    request.NombreArchivo,
                    objectName,
                    request.TamanoBytes,
                    request.ContentType,
                    request.CarpetaId,
                    usuarioId,
                    request.Descripcion,
                    request.TipoDocumental,
                    request.FechaDocumento,
                    request.CodigoDocumento);

                if (archivoId == null)
                {
                    return new IniciarUploadResponse
                    {
                        Exito = false,
                        Mensaje = "Error al crear registro de archivo"
                    };
                }

                // Determinar qué ContentType usar - preferir el de Storage (es el que se firmó)
                var contentTypeParaFrontend = urlResult.ContentType ?? request.ContentType;

                _logger.LogInformation(
                    "[UPLOAD DEBUG] Upload iniciado: ArchivoId={ArchivoId}, Object={ObjectName}, Usuario={Usuario}",
                    archivoId, objectName, usuarioId);
                _logger.LogInformation(
                    "[UPLOAD DEBUG] ContentTypes - Request: '{RequestCT}', Storage: '{StorageCT}', Final: '{FinalCT}'",
                    request.ContentType, urlResult.ContentType ?? "(NULL)", contentTypeParaFrontend);
                _logger.LogInformation(
                    "[UPLOAD DEBUG] ContentType Hex: {Hex}, Length: {Len}",
                    urlResult.ContentTypeHex ?? "(NULL)", urlResult.ContentTypeLength);

                return new IniciarUploadResponse
                {
                    Exito = true,
                    Mensaje = "Upload iniciado. Use la URL proporcionada para subir el archivo.",
                    ArchivoId = archivoId,
                    UploadUrl = urlResult.UploadUrl,
                    ObjectName = objectName,
                    UrlExpiraEn = urlResult.ExpiresAt,
                    SegundosParaExpirar = urlResult.ExpiresInSeconds,
                    // IMPORTANTE: Usar el ContentType de Storage, que es exactamente el que se usó para firmar la URL
                    ContentType = contentTypeParaFrontend,
                    // Información de debugging para diagnosticar SignatureDoesNotMatch
                    ContentTypeHex = urlResult.ContentTypeHex,
                    ContentTypeLength = urlResult.ContentTypeLength
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando upload para carpeta {CarpetaId}", request.CarpetaId);
                return new IniciarUploadResponse
                {
                    Exito = false,
                    Mensaje = $"Error al iniciar upload: {ex.Message}"
                };
            }
        }

        public async Task<ConfirmarUploadResponse> ConfirmarUploadAsync(
            long archivoId,
            long usuarioId,
            ConfirmarUploadRequest request,
            string authToken)
        {
            try
            {
                // 1. Obtener el archivo pendiente
                var archivo = await ObtenerArchivoPendienteAsync(archivoId);
                if (archivo == null)
                {
                    _logger.LogWarning(
                        "[CONFIRMAR DEBUG] Archivo no encontrado o ya confirmado: ArchivoId={ArchivoId}",
                        archivoId);
                    return new ConfirmarUploadResponse
                    {
                        Exito = false,
                        Mensaje = "Archivo no encontrado o ya fue confirmado"
                    };
                }

                _logger.LogInformation(
                    "[CONFIRMAR DEBUG] Archivo pendiente encontrado: ArchivoId={ArchivoId}, Nombre='{Nombre}', Ruta='{Ruta}'",
                    archivo.ArchivoId, archivo.Nombre, archivo.Ruta);

                // 2. Verificar que el archivo existe en GCS
                _logger.LogInformation(
                    "[CONFIRMAR DEBUG] Verificando existencia en GCS con ruta: '{Ruta}'",
                    archivo.Ruta);

                var existeEnGcs = await _storageClient.FileExistsAsync(archivo.Ruta, authToken);

                _logger.LogInformation(
                    "[CONFIRMAR DEBUG] Resultado verificación GCS: Exists={Exists}",
                    existeEnGcs);

                if (!existeEnGcs)
                {
                    _logger.LogError(
                        "[CONFIRMAR DEBUG] Archivo NO encontrado en GCS. Ruta buscada: '{Ruta}'",
                        archivo.Ruta);
                    return new ConfirmarUploadResponse
                    {
                        Exito = false,
                        Mensaje = $"El archivo no se encontró en el storage. Ruta: {archivo.Ruta}"
                    };
                }

                // 3. Obtener información del archivo en GCS
                var fileInfo = await _storageClient.GetFileInfoAsync(archivo.Ruta, authToken);

                // 4. Actualizar registro en BD: Estado = ACTIVO
                var actualizado = await ConfirmarArchivoEnBdAsync(
                    archivoId,
                    usuarioId,
                    request.Hash ?? fileInfo?.Md5Hash,
                    request.TamanoReal ?? fileInfo?.Size ?? archivo.Tamano);

                if (!actualizado)
                {
                    return new ConfirmarUploadResponse
                    {
                        Exito = false,
                        Mensaje = "Error al confirmar archivo en base de datos"
                    };
                }

                _logger.LogInformation(
                    "Upload confirmado: ArchivoId={ArchivoId}, Size={Size}, Usuario={Usuario}",
                    archivoId, fileInfo?.Size ?? archivo.Tamano, usuarioId);

                return new ConfirmarUploadResponse
                {
                    Exito = true,
                    Mensaje = "Archivo subido y confirmado exitosamente",
                    ArchivoId = archivoId,
                    Nombre = archivo.Nombre,
                    Ruta = archivo.Ruta,
                    Tamano = request.TamanoReal ?? fileInfo?.Size ?? archivo.Tamano,
                    Hash = request.Hash ?? fileInfo?.Md5Hash
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirmando upload de archivo {ArchivoId}", archivoId);
                return new ConfirmarUploadResponse
                {
                    Exito = false,
                    Mensaje = $"Error al confirmar upload: {ex.Message}"
                };
            }
        }

        public async Task<ResultadoArchivo> CancelarUploadAsync(
            long archivoId,
            long usuarioId,
            string? motivo = null,
            string? authToken = null)
        {
            try
            {
                // 1. Obtener el archivo pendiente
                var archivo = await ObtenerArchivoPendienteAsync(archivoId);
                if (archivo == null)
                {
                    return new ResultadoArchivo
                    {
                        Exito = false,
                        Mensaje = "Archivo no encontrado o no está pendiente"
                    };
                }

                // 2. Intentar eliminar de GCS (por si se subió parcialmente)
                if (!string.IsNullOrEmpty(authToken))
                {
                    await _storageClient.DeleteFileAsync(archivo.Ruta, authToken);
                }

                // 3. Eliminar registro de BD
                const string sql = @"
                DELETE FROM documentos.""Archivos""
                WHERE ""Cod"" = @ArchivoId AND ""EstadoUpload"" = 'PENDIENTE'";

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var deleted = await connection.ExecuteAsync(sql, new { ArchivoId = archivoId });

                if (deleted > 0)
                {
                    _logger.LogInformation(
                        "Upload cancelado: ArchivoId={ArchivoId}, Motivo={Motivo}, Usuario={Usuario}",
                        archivoId, motivo ?? "No especificado", usuarioId);

                    return new ResultadoArchivo
                    {
                        Exito = true,
                        Mensaje = "Upload cancelado exitosamente"
                    };
                }

                return new ResultadoArchivo
                {
                    Exito = false,
                    Mensaje = "No se pudo cancelar el upload"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando upload de archivo {ArchivoId}", archivoId);
                return new ResultadoArchivo
                {
                    Exito = false,
                    Mensaje = $"Error al cancelar upload: {ex.Message}"
                };
            }
        }

        public async Task<DescargarArchivoResponse> ObtenerUrlDescargaAsync(
            long archivoId,
            long usuarioId,
            string authToken)
        {
            try
            {
                // 1. Obtener archivo
                var archivo = await ObtenerPorIdAsync(archivoId);
                if (archivo == null)
                {
                    return new DescargarArchivoResponse
                    {
                        Exito = false,
                        Mensaje = "Archivo no encontrado"
                    };
                }

                // 2. Solicitar URL de descarga
                var urlResult = await _storageClient.GetDownloadUrlAsync(
                    archivo.Ruta,
                    archivo.Nombre,
                    expirationMinutes: 15,
                    authToken);

                if (!urlResult.Success || string.IsNullOrEmpty(urlResult.DownloadUrl))
                {
                    return new DescargarArchivoResponse
                    {
                        Exito = false,
                        Mensaje = urlResult.Error ?? "Error al generar URL de descarga"
                    };
                }

                return new DescargarArchivoResponse
                {
                    Exito = true,
                    Mensaje = "URL de descarga generada",
                    ArchivoId = archivoId,
                    Nombre = archivo.Nombre,
                    DownloadUrl = urlResult.DownloadUrl,
                    UrlExpiraEn = urlResult.ExpiresAt,
                    SegundosParaExpirar = urlResult.ExpiresInSeconds,
                    Tamano = archivo.Tamano,
                    ContentType = archivo.ContentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo URL de descarga para archivo {ArchivoId}", archivoId);
                return new DescargarArchivoResponse
                {
                    Exito = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        // ==================== MÉTODOS PRIVADOS PARA UPLOAD ====================

        private string GenerarObjectName(long entidadId, long carpetaId, string nombreArchivo)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            var extension = Path.GetExtension(nombreArchivo);
            var nombreBase = Path.GetFileNameWithoutExtension(nombreArchivo);

            // Sanitizar nombre - reemplazar espacios y caracteres problemáticos
            var nombreLimpio = nombreBase
                .Replace(" ", "_")           // Espacios por guion bajo
                .Replace("%", "_")           // Porcentaje puede causar problemas de encoding
                .Replace("#", "_")           // Numeral
                .Replace("&", "_")           // Ampersand
                .Replace("?", "_")           // Interrogación
                .Replace("+", "_");          // Más

            // Remover caracteres inválidos de archivo
            nombreLimpio = string.Join("_", nombreLimpio.Split(Path.GetInvalidFileNameChars()));

            // Limitar longitud
            if (nombreLimpio.Length > 50) nombreLimpio = nombreLimpio[..50];

            return $"entidad_{entidadId}/carpeta_{carpetaId}/{timestamp}_{guid}_{nombreLimpio}{extension}";
        }

        private async Task<(bool Valida, string? Error)> ValidarCarpetaParaUploadAsync(
            long carpetaId, long usuarioId, long entidadId)
        {
            const string sql = @"
            SELECT 
                c.""Cod"",
                c.""Estado"",
                c.""Entidad"",
                c.""EstadoCarpeta""
            FROM documentos.""Carpetas"" c
            WHERE c.""Cod"" = @CarpetaId";

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var carpeta = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { CarpetaId = carpetaId });

            if (carpeta == null)
                return (false, "Carpeta no encontrada");

            if (carpeta.Estado == false)
                return (false, "La carpeta está eliminada");

            if (carpeta.Entidad != entidadId)
                return (false, "No tiene acceso a esta carpeta");

            // Verificar si la carpeta está cerrada (EstadoCarpeta = 2 típicamente)
            if (carpeta.EstadoCarpeta == 2)
                return (false, "La carpeta está cerrada, no se pueden agregar archivos");

            return (true, null);
        }

        private async Task<long?> CrearArchivoPendienteAsync(
            string nombre,
            string ruta,
            long tamano,
            string contentType,
            long carpetaId,
            long usuarioId,
            string? descripcion,
            long? tipoDocumental,
            DateTime? fechaDocumento,
            string? codigoDocumento)
        {
            // Nota: La tabla Archivos usa columnas con nombres específicos:
            // - "SubidoPor" (no UsuarioCreador)
            // - "FechaIncorporacion" (tiene DEFAULT CURRENT_TIMESTAMP)
            // - "Tamaño" con tilde (es VARCHAR, no BIGINT)
            // - "Version" (no VersionActual)
            // - "Cod" es NOT NULL y debe generarse con F_SiguienteCod
            const string sql = @"
            INSERT INTO documentos.""Archivos"" (
                ""Cod"", ""Nombre"", ""Ruta"", ""Tamaño"", ""ContentType"", ""Carpeta"",
                ""SubidoPor"", ""Estado"", ""EstadoUpload"",
                ""Descripcion"", ""TipoDocumental"", ""FechaDocumento"", ""CodigoDocumento"",
                ""Version""
            ) VALUES (
                documentos.""F_SiguienteCod""('Archivos', NULL),
                @Nombre, @Ruta, @Tamano, @ContentType, @Carpeta,
                @Usuario, true, 'PENDIENTE',
                @Descripcion, @TipoDocumental, @FechaDocumento, @CodigoDocumento,
                1
            ) RETURNING ""Cod""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var archivoId = await connection.QueryFirstAsync<long>(sql, new
                {
                    Nombre = nombre,
                    Ruta = ruta,
                    Tamano = tamano.ToString(), // Convertir a string porque "Tamaño" es VARCHAR(50)
                    ContentType = contentType,
                    Carpeta = carpetaId,
                    Usuario = usuarioId,
                    Descripcion = descripcion,
                    TipoDocumental = tipoDocumental,
                    FechaDocumento = fechaDocumento,
                    CodigoDocumento = codigoDocumento
                });

                return archivoId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando archivo pendiente");
                return null;
            }
        }

        private async Task<ArchivosPendiente?> ObtenerArchivoPendienteAsync(long archivoId)
        {
            const string sql = @"
            SELECT
                ""Cod"" as ArchivoId,
                ""Nombre"",
                ""Ruta"",
                ""Tamaño"" as Tamano,
                ""ContentType"",
                ""Carpeta"" as CarpetaId
            FROM documentos.""Archivos""
            WHERE ""Cod"" = @ArchivoId AND ""EstadoUpload"" = 'PENDIENTE'";

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<ArchivosPendiente>(sql, new { ArchivoId = archivoId });
        }

        private async Task<bool> ConfirmarArchivoEnBdAsync(
            long archivoId,
            long usuarioId,
            string? hash,
            long tamano)
        {
            // Nota: La tabla Archivos usa:
            // - "Tamaño" con tilde (es VARCHAR, no BIGINT)
            // - No tiene columna "UsuarioModificador"
            const string sql = @"
            UPDATE documentos.""Archivos""
            SET
                ""EstadoUpload"" = 'COMPLETADO',
                ""Hash"" = @Hash,
                ""Tamaño"" = @Tamano,
                ""FechaModificacion"" = NOW()
            WHERE ""Cod"" = @ArchivoId AND ""EstadoUpload"" = 'PENDIENTE'";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var updated = await connection.ExecuteAsync(sql, new
                {
                    ArchivoId = archivoId,
                    Hash = hash,
                    Tamano = tamano.ToString() // Convertir a string porque "Tamaño" es VARCHAR(50)
                });

                return updated > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirmando archivo {ArchivoId}", archivoId);
                return false;
            }
        }

        /// <summary>
        /// DTO interno para archivos pendientes
        /// </summary>
        private class ArchivosPendiente
        {
            public long ArchivoId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Ruta { get; set; } = string.Empty;
            public long Tamano { get; set; }
            public string? ContentType { get; set; }
            public long CarpetaId { get; set; }
        }

        // ==================== DIAGNÓSTICO ====================

        public async Task<IEnumerable<dynamic>> ListarTodosParaDiagnosticoAsync(int limit = 20)
        {
            // Consulta directa sin mapeo de entidad para evitar problemas con "Tamaño"
            const string sql = @"
                SELECT
                    ""Cod"",
                    ""Nombre"",
                    ""Ruta"",
                    ""Estado"",
                    ""EstadoUpload"",
                    ""ContentType"",
                    ""Tamaño"" as Tamano,
                    ""Carpeta"",
                    ""SubidoPor"",
                    ""FechaIncorporacion""
                FROM documentos.""Archivos""
                ORDER BY ""Cod"" DESC
                LIMIT @Limit";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<dynamic>(sql, new { Limit = limit });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando archivos para diagnóstico");
                return Enumerable.Empty<dynamic>();
            }
        }
    }
}