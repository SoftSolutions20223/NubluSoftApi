using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class ArchivosService : IArchivosService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<ArchivosService> _logger;

        public ArchivosService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<ArchivosService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<IEnumerable<Archivo>> ObtenerPorCarpetaAsync(long carpetaId)
        {
            const string sql = @"
                SELECT 
                    a.""Cod"", a.""Nombre"", a.""Ruta"", a.""Carpeta"", a.""Estado"", a.""Indice"",
                    a.""FechaSubida"", a.""FechaDocumento"", a.""CodigoDocumento"", a.""Descripcion"",
                    a.""SubidoPor"", a.""TipoArchivo"", a.""TipoDocumental"", a.""OrigenDocumento"",
                    a.""PaginaInicio"", a.""PaginaFin"", a.""Hash"", a.""Tamano"", a.""Version"",
                    a.""TipoFirma"", a.""Firmado"", a.""MetadatosAdicionales"",
                    c.""Nombre"" AS ""NombreCarpeta"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreSubidoPor"",
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
                LEFT JOIN documentos.""Usuarios"" u ON a.""SubidoPor"" = u.""Cod""
                LEFT JOIN documentos.""Tipos_Archivos"" ta ON a.""TipoArchivo"" = ta.""Cod""
                LEFT JOIN documentos.""Tipos_Documentales"" td ON a.""TipoDocumental"" = td.""Cod""
                LEFT JOIN documentos.""Origenes_Documentos"" od ON a.""OrigenDocumento"" = od.""Cod""
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
            var sql = @"
                SELECT 
                    a.""Cod"", a.""Nombre"", a.""Ruta"", a.""Carpeta"", a.""Estado"", a.""Indice"",
                    a.""FechaSubida"", a.""FechaDocumento"", a.""CodigoDocumento"", a.""Descripcion"",
                    a.""SubidoPor"", a.""TipoArchivo"", a.""TipoDocumental"", a.""OrigenDocumento"",
                    a.""PaginaInicio"", a.""PaginaFin"", a.""Hash"", a.""Tamano"", a.""Version"",
                    c.""Nombre"" AS ""NombreCarpeta"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreSubidoPor"",
                    td.""Nombre"" AS ""NombreTipoDocumental""
                FROM documentos.""Archivos"" a
                LEFT JOIN documentos.""Carpetas"" c ON a.""Carpeta"" = c.""Cod"" AND c.""Estado"" = true
                LEFT JOIN documentos.""Usuarios"" u ON a.""SubidoPor"" = u.""Cod""
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
                sql += @" AND a.""FechaSubida"" >= @FechaDesde";
                parameters.Add("FechaDesde", filtros.FechaDesde.Value);
            }

            if (filtros.FechaHasta.HasValue)
            {
                sql += @" AND a.""FechaSubida"" <= @FechaHasta";
                parameters.Add("FechaHasta", filtros.FechaHasta.Value.AddDays(1));
            }

            sql += @" ORDER BY a.""FechaSubida"" DESC, a.""Nombre""";

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
            const string sql = @"
                SELECT 
                    a.""Cod"", a.""Nombre"", a.""Ruta"", a.""Carpeta"", a.""Estado"", a.""Indice"",
                    a.""FechaSubida"", a.""FechaDocumento"", a.""CodigoDocumento"", a.""Descripcion"",
                    a.""SubidoPor"", a.""TipoArchivo"", a.""TipoDocumental"", a.""OrigenDocumento"",
                    a.""PaginaInicio"", a.""PaginaFin"", a.""Hash"", a.""Tamano"", a.""Version"",
                    a.""TipoFirma"", a.""Firmado"", a.""MetadatosAdicionales"",
                    c.""Nombre"" AS ""NombreCarpeta"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreSubidoPor"",
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
                LEFT JOIN documentos.""Usuarios"" u ON a.""SubidoPor"" = u.""Cod""
                LEFT JOIN documentos.""Tipos_Archivos"" ta ON a.""TipoArchivo"" = ta.""Cod""
                LEFT JOIN documentos.""Tipos_Documentales"" td ON a.""TipoDocumental"" = td.""Cod""
                LEFT JOIN documentos.""Origenes_Documentos"" od ON a.""OrigenDocumento"" = od.""Cod""
                WHERE a.""Cod"" = @ArchivoId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<Archivo>(sql, new { ArchivoId = archivoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo archivo {ArchivoId}", archivoId);
                return null;
            }
        }

        public async Task<IEnumerable<VersionArchivo>> ObtenerVersionesAsync(long archivoId)
        {
            const string sql = @"
                SELECT 
                    v.""Cod"", v.""Archivo"", v.""Version"", v.""Ruta"", v.""Hash"", v.""Tamano"",
                    v.""FechaCreacion"", v.""CreadoPor"", v.""Comentario"", v.""EsVersionActual"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreCreadoPor""
                FROM documentos.""Versiones_Archivos"" v
                LEFT JOIN documentos.""Usuarios"" u ON v.""CreadoPor"" = u.""Cod""
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
            const string sql = @"SELECT * FROM documentos.""F_EliminarArchivo""(@ArchivoId, @UsuarioId)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoArchivo>(sql,
                    new { ArchivoId = archivoId, UsuarioId = usuarioId });

                return resultado ?? new ResultadoArchivo { Exito = false, Mensaje = "Error al eliminar el archivo" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando archivo {ArchivoId}", archivoId);
                return new ResultadoArchivo { Exito = false, Mensaje = "Error al eliminar el archivo: " + ex.Message };
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
    }
}