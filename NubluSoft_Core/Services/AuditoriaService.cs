using Dapper;
using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<AuditoriaService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== HISTORIAL DE ACCIONES ====================

        public async Task<HistorialAccionesListResponse> ObtenerHistorialAsync(
            long entidadId,
            FiltrosAuditoriaRequest filtros)
        {
            var response = new HistorialAccionesListResponse
            {
                Pagina = filtros.Pagina,
                PorPagina = filtros.PorPagina
            };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var whereClause = "WHERE 1=1";
                var parameters = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(filtros.Tabla))
                {
                    whereClause += @" AND h.""Tabla"" = @Tabla";
                    parameters.Add("Tabla", filtros.Tabla);
                }

                if (filtros.RegistroCod.HasValue)
                {
                    whereClause += @" AND h.""RegistroCod"" = @RegistroCod";
                    parameters.Add("RegistroCod", filtros.RegistroCod.Value);
                }

                if (filtros.UsuarioId.HasValue)
                {
                    whereClause += @" AND h.""Usuario"" = @UsuarioId";
                    parameters.Add("UsuarioId", filtros.UsuarioId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filtros.Accion))
                {
                    whereClause += @" AND h.""Accion"" = @Accion";
                    parameters.Add("Accion", filtros.Accion);
                }

                if (filtros.FechaDesde.HasValue)
                {
                    whereClause += @" AND h.""Fecha"" >= @FechaDesde";
                    parameters.Add("FechaDesde", filtros.FechaDesde.Value);
                }

                if (filtros.FechaHasta.HasValue)
                {
                    whereClause += @" AND h.""Fecha"" <= @FechaHasta";
                    parameters.Add("FechaHasta", filtros.FechaHasta.Value);
                }

                // Contar total
                var countSql = $@"
                    SELECT COUNT(*) 
                    FROM documentos.""Historial_Acciones"" h
                    LEFT JOIN documentos.""Usuarios"" u ON h.""Usuario"" = u.""Cod""
                    {whereClause}";

                response.TotalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // Obtener página
                var offset = (filtros.Pagina - 1) * filtros.PorPagina;
                parameters.Add("Limit", filtros.PorPagina);
                parameters.Add("Offset", offset);

                var sql = $@"
                    SELECT 
                        h.""Cod"",
                        h.""Tabla"",
                        h.""RegistroCod"",
                        h.""Accion"",
                        h.""Usuario"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreUsuario"",
                        h.""Fecha"",
                        h.""IP"",
                        h.""DetalleAnterior"",
                        h.""DetalleNuevo"",
                        h.""Observaciones""
                    FROM documentos.""Historial_Acciones"" h
                    LEFT JOIN documentos.""Usuarios"" u ON h.""Usuario"" = u.""Cod""
                    {whereClause}
                    ORDER BY h.""Fecha"" DESC
                    LIMIT @Limit OFFSET @Offset";

                response.Items = await connection.QueryAsync<HistorialAccionDto>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de acciones");
            }

            return response;
        }

        public async Task<IEnumerable<HistorialAccionDto>> ObtenerHistorialRegistroAsync(
            string tabla,
            long registroCod)
        {
            const string sql = @"
                SELECT 
                    h.""Cod"",
                    h.""Tabla"",
                    h.""RegistroCod"",
                    h.""Accion"",
                    h.""Usuario"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreUsuario"",
                    h.""Fecha"",
                    h.""IP"",
                    h.""DetalleAnterior"",
                    h.""DetalleNuevo"",
                    h.""Observaciones""
                FROM documentos.""Historial_Acciones"" h
                LEFT JOIN documentos.""Usuarios"" u ON h.""Usuario"" = u.""Cod""
                WHERE h.""Tabla"" = @Tabla AND h.""RegistroCod"" = @RegistroCod
                ORDER BY h.""Fecha"" DESC";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<HistorialAccionDto>(sql, new { Tabla = tabla, RegistroCod = registroCod });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial del registro {Tabla}/{RegistroCod}", tabla, registroCod);
                return Enumerable.Empty<HistorialAccionDto>();
            }
        }

        public async Task<HistorialAccionesListResponse> ObtenerAccionesUsuarioAsync(
            long entidadId,
            long usuarioId,
            FiltrosAuditoriaRequest filtros)
        {
            filtros.UsuarioId = usuarioId;
            return await ObtenerHistorialAsync(entidadId, filtros);
        }

        // ==================== ARCHIVOS ELIMINADOS ====================

        public async Task<ArchivosEliminadosListResponse> ObtenerArchivosEliminadosAsync(
            long entidadId,
            FiltrosArchivosEliminadosRequest filtros)
        {
            var response = new ArchivosEliminadosListResponse
            {
                Pagina = filtros.Pagina,
                PorPagina = filtros.PorPagina
            };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var whereClause = "WHERE 1=1";
                var parameters = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(filtros.Nombre))
                {
                    whereClause += @" AND ae.""Nombre"" ILIKE @Nombre";
                    parameters.Add("Nombre", $"%{filtros.Nombre}%");
                }

                if (filtros.EliminadoPor.HasValue)
                {
                    whereClause += @" AND ae.""EliminadoPor"" = @EliminadoPor";
                    parameters.Add("EliminadoPor", filtros.EliminadoPor.Value);
                }

                if (filtros.FechaDesde.HasValue)
                {
                    whereClause += @" AND ae.""FechaEliminacion"" >= @FechaDesde";
                    parameters.Add("FechaDesde", filtros.FechaDesde.Value);
                }

                if (filtros.FechaHasta.HasValue)
                {
                    whereClause += @" AND ae.""FechaEliminacion"" <= @FechaHasta";
                    parameters.Add("FechaHasta", filtros.FechaHasta.Value);
                }

                // Contar total
                var countSql = $@"
                    SELECT COUNT(*) 
                    FROM documentos.""Archivos_Eliminados"" ae
                    {whereClause}";

                response.TotalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // Obtener página
                var offset = (filtros.Pagina - 1) * filtros.PorPagina;
                parameters.Add("Limit", filtros.PorPagina);
                parameters.Add("Offset", offset);

                var sql = $@"
                    SELECT 
                        ae.""Cod"",
                        ae.""Nombre"",
                        ae.""Ruta"",
                        ae.""FechaEliminacion"",
                        ae.""Carpeta"",
                        ae.""CarpetaCod"",
                        ae.""Formato"",
                        ae.""EliminadoPor"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreEliminadoPor"",
                        ae.""MotivoEliminacion"",
                        ae.""Hash"",
                        ae.""Tamaño""
                    FROM documentos.""Archivos_Eliminados"" ae
                    LEFT JOIN documentos.""Usuarios"" u ON ae.""EliminadoPor"" = u.""Cod""
                    {whereClause}
                    ORDER BY ae.""FechaEliminacion"" DESC
                    LIMIT @Limit OFFSET @Offset";

                response.Items = await connection.QueryAsync<ArchivoEliminadoDto>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo archivos eliminados");
            }

            return response;
        }

        public async Task<ArchivoEliminadoDto?> ObtenerArchivoEliminadoAsync(long archivoCod)
        {
            const string sql = @"
                SELECT 
                    ae.""Cod"",
                    ae.""Nombre"",
                    ae.""Ruta"",
                    ae.""FechaEliminacion"",
                    ae.""Carpeta"",
                    ae.""CarpetaCod"",
                    ae.""Formato"",
                    ae.""EliminadoPor"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreEliminadoPor"",
                    ae.""MotivoEliminacion"",
                    ae.""Hash"",
                    ae.""Tamaño""
                FROM documentos.""Archivos_Eliminados"" ae
                LEFT JOIN documentos.""Usuarios"" u ON ae.""EliminadoPor"" = u.""Cod""
                WHERE ae.""Cod"" = @Cod";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<ArchivoEliminadoDto>(sql, new { Cod = archivoCod });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo archivo eliminado {Cod}", archivoCod);
                return null;
            }
        }

        // ==================== CARPETAS ELIMINADAS ====================

        public async Task<CarpetasEliminadasListResponse> ObtenerCarpetasEliminadasAsync(
            long entidadId,
            FiltrosCarpetasEliminadasRequest filtros)
        {
            var response = new CarpetasEliminadasListResponse
            {
                Pagina = filtros.Pagina,
                PorPagina = filtros.PorPagina
            };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var whereClause = "WHERE 1=1";
                var parameters = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(filtros.Nombre))
                {
                    whereClause += @" AND ce.""Nombre"" ILIKE @Nombre";
                    parameters.Add("Nombre", $"%{filtros.Nombre}%");
                }

                if (filtros.TipoCarpeta.HasValue)
                {
                    whereClause += @" AND ce.""TipoCarpeta"" = @TipoCarpeta";
                    parameters.Add("TipoCarpeta", filtros.TipoCarpeta.Value);
                }

                if (filtros.EliminadoPor.HasValue)
                {
                    whereClause += @" AND ce.""EliminadoPor"" = @EliminadoPor";
                    parameters.Add("EliminadoPor", filtros.EliminadoPor.Value);
                }

                if (filtros.FechaDesde.HasValue)
                {
                    whereClause += @" AND ce.""FechaEliminacion"" >= @FechaDesde";
                    parameters.Add("FechaDesde", filtros.FechaDesde.Value);
                }

                if (filtros.FechaHasta.HasValue)
                {
                    whereClause += @" AND ce.""FechaEliminacion"" <= @FechaHasta";
                    parameters.Add("FechaHasta", filtros.FechaHasta.Value);
                }

                // Contar total
                var countSql = $@"
                    SELECT COUNT(*) 
                    FROM documentos.""Carpetas_Eliminadas"" ce
                    {whereClause}";

                response.TotalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // Obtener página
                var offset = (filtros.Pagina - 1) * filtros.PorPagina;
                parameters.Add("Limit", filtros.PorPagina);
                parameters.Add("Offset", offset);

                var sql = $@"
                    SELECT 
                        ce.""Cod"",
                        ce.""Nombre"",
                        ce.""TipoCarpeta"",
                        tc.""Nombre"" AS ""NombreTipoCarpeta"",
                        ce.""CarpetaPadre"",
                        ce.""SerieRaiz"",
                        ce.""FechaCreacion"",
                        ce.""FechaEliminacion"",
                        ce.""EliminadoPor"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreEliminadoPor"",
                        ce.""MotivoEliminacion"",
                        ce.""NumeroFolios"",
                        ce.""NumeroArchivos""
                    FROM documentos.""Carpetas_Eliminadas"" ce
                    LEFT JOIN documentos.""Usuarios"" u ON ce.""EliminadoPor"" = u.""Cod""
                    LEFT JOIN documentos.""Tipos_Carpetas"" tc ON ce.""TipoCarpeta"" = tc.""Cod""
                    {whereClause}
                    ORDER BY ce.""FechaEliminacion"" DESC
                    LIMIT @Limit OFFSET @Offset";

                response.Items = await connection.QueryAsync<CarpetaEliminadaDto>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo carpetas eliminadas");
            }

            return response;
        }

        public async Task<CarpetaEliminadaDto?> ObtenerCarpetaEliminadaAsync(long carpetaCod)
        {
            const string sql = @"
                SELECT 
                    ce.""Cod"",
                    ce.""Nombre"",
                    ce.""TipoCarpeta"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta"",
                    ce.""CarpetaPadre"",
                    ce.""SerieRaiz"",
                    ce.""FechaCreacion"",
                    ce.""FechaEliminacion"",
                    ce.""EliminadoPor"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreEliminadoPor"",
                    ce.""MotivoEliminacion"",
                    ce.""NumeroFolios"",
                    ce.""NumeroArchivos""
                FROM documentos.""Carpetas_Eliminadas"" ce
                LEFT JOIN documentos.""Usuarios"" u ON ce.""EliminadoPor"" = u.""Cod""
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON ce.""TipoCarpeta"" = tc.""Cod""
                WHERE ce.""Cod"" = @Cod";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<CarpetaEliminadaDto>(sql, new { Cod = carpetaCod });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo carpeta eliminada {Cod}", carpetaCod);
                return null;
            }
        }

        // ==================== ERROR LOG ====================

        public async Task<ErrorLogListResponse> ObtenerErroresAsync(FiltrosErrorLogRequest filtros)
        {
            var response = new ErrorLogListResponse
            {
                Pagina = filtros.Pagina,
                PorPagina = filtros.PorPagina
            };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var whereClause = "WHERE 1=1";
                var parameters = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(filtros.ProcedureName))
                {
                    whereClause += @" AND ""ProcedureName"" ILIKE @ProcedureName";
                    parameters.Add("ProcedureName", $"%{filtros.ProcedureName}%");
                }

                if (filtros.FechaDesde.HasValue)
                {
                    whereClause += @" AND ""ErrorDate"" >= @FechaDesde";
                    parameters.Add("FechaDesde", filtros.FechaDesde.Value);
                }

                if (filtros.FechaHasta.HasValue)
                {
                    whereClause += @" AND ""ErrorDate"" <= @FechaHasta";
                    parameters.Add("FechaHasta", filtros.FechaHasta.Value);
                }

                // Contar total
                var countSql = $@"SELECT COUNT(*) FROM documentos.""ErrorLog"" {whereClause}";
                response.TotalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // Obtener página
                var offset = (filtros.Pagina - 1) * filtros.PorPagina;
                parameters.Add("Limit", filtros.PorPagina);
                parameters.Add("Offset", offset);

                var sql = $@"
                    SELECT 
                        ""Id"",
                        ""ProcedureName"",
                        ""Parameters"",
                        ""ErrorMessage"",
                        ""ErrorDate"",
                        ""ErrorNumber"",
                        ""ErrorLine"",
                        ""ErrorSeverity""
                    FROM documentos.""ErrorLog""
                    {whereClause}
                    ORDER BY ""ErrorDate"" DESC
                    LIMIT @Limit OFFSET @Offset";

                response.Items = await connection.QueryAsync<ErrorLogDto>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo log de errores");
            }

            return response;
        }

        // ==================== RESUMEN / DASHBOARD ====================

        public async Task<ResumenAuditoriaDto> ObtenerResumenAsync(long entidadId)
        {
            var resumen = new ResumenAuditoriaDto();

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Acciones de hoy
                resumen.TotalAccionesHoy = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM documentos.""Historial_Acciones""
                    WHERE ""Fecha"" >= CURRENT_DATE");

                // Acciones de la semana
                resumen.TotalAccionesSemana = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM documentos.""Historial_Acciones""
                    WHERE ""Fecha"" >= CURRENT_DATE - INTERVAL '7 days'");

                // Total archivos eliminados
                resumen.TotalArchivosEliminados = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM documentos.""Archivos_Eliminados""");

                // Total carpetas eliminadas
                resumen.TotalCarpetasEliminadas = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM documentos.""Carpetas_Eliminadas""");

                // Total errores (últimos 30 días)
                resumen.TotalErrores = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM documentos.""ErrorLog""
                    WHERE ""ErrorDate"" >= CURRENT_DATE - INTERVAL '30 days'");

                // Acciones por tabla (últimos 7 días)
                resumen.AccionesPorTabla = await connection.QueryAsync<AccionesPorTablaDto>(@"
                    SELECT ""Tabla"", COUNT(*) AS ""Total""
                    FROM documentos.""Historial_Acciones""
                    WHERE ""Fecha"" >= CURRENT_DATE - INTERVAL '7 days'
                    GROUP BY ""Tabla""
                    ORDER BY ""Total"" DESC
                    LIMIT 10");

                // Acciones por usuario (últimos 7 días)
                resumen.AccionesPorUsuario = await connection.QueryAsync<AccionesPorUsuarioDto>(@"
                    SELECT 
                        h.""Usuario"" AS ""UsuarioId"",
                        COALESCE(u.""Nombres"" || ' ' || u.""Apellidos"", 'Sistema') AS ""NombreUsuario"",
                        COUNT(*) AS ""Total""
                    FROM documentos.""Historial_Acciones"" h
                    LEFT JOIN documentos.""Usuarios"" u ON h.""Usuario"" = u.""Cod""
                    WHERE h.""Fecha"" >= CURRENT_DATE - INTERVAL '7 days'
                    GROUP BY h.""Usuario"", u.""Nombres"", u.""Apellidos""
                    ORDER BY ""Total"" DESC
                    LIMIT 10");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen de auditoría");
            }

            return resumen;
        }

        // ==================== REGISTRO MANUAL ====================

        public async Task<bool> RegistrarAccionAsync(
            string tabla,
            long registroCod,
            string accion,
            long usuarioId,
            string? ip = null,
            string? detalleAnterior = null,
            string? detalleNuevo = null,
            string? observaciones = null)
        {
            const string sql = @"
                INSERT INTO documentos.""Historial_Acciones"" 
                (""Tabla"", ""RegistroCod"", ""Accion"", ""Usuario"", ""Fecha"", ""IP"", ""DetalleAnterior"", ""DetalleNuevo"", ""Observaciones"")
                VALUES 
                (@Tabla, @RegistroCod, @Accion, @Usuario, CURRENT_TIMESTAMP, @IP, @DetalleAnterior, @DetalleNuevo, @Observaciones)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    Tabla = tabla,
                    RegistroCod = registroCod,
                    Accion = accion,
                    Usuario = usuarioId,
                    IP = ip,
                    DetalleAnterior = detalleAnterior,
                    DetalleNuevo = detalleNuevo,
                    Observaciones = observaciones
                });

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando acción en historial");
                return false;
            }
        }
    }
}