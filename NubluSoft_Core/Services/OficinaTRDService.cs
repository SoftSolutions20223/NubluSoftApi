using Dapper;
using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    public class OficinaTRDService : IOficinaTRDService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<OficinaTRDService> _logger;

        public OficinaTRDService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<OficinaTRDService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<IEnumerable<OficinaTRDDto>> ObtenerAsignacionesAsync(
            long entidadId,
            FiltrosOficinaTRDRequest? filtros = null)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var whereClause = @"WHERE ot.""Entidad"" = @EntidadId";
                var parameters = new DynamicParameters();
                parameters.Add("EntidadId", entidadId);

                if (filtros != null)
                {
                    if (filtros.OficinaId.HasValue)
                    {
                        whereClause += @" AND ot.""Oficina"" = @OficinaId";
                        parameters.Add("OficinaId", filtros.OficinaId.Value);
                    }

                    if (filtros.TRDId.HasValue)
                    {
                        whereClause += @" AND ot.""TRD"" = @TRDId";
                        parameters.Add("TRDId", filtros.TRDId.Value);
                    }

                    if (filtros.SoloActivas == true)
                    {
                        whereClause += @" AND ot.""Estado"" = true";
                    }
                }

                var sql = $@"
                    SELECT 
                        ot.""Cod"",
                        ot.""Entidad"",
                        ot.""Oficina"",
                        o.""Nombre"" AS ""NombreOficina"",
                        ot.""TRD"",
                        t.""Nombre"" AS ""NombreTRD"",
                        t.""Codigo"" AS ""CodigoTRD"",
                        t.""Tipo"" AS ""TipoTRD"",
                        ot.""PuedeEditar"",
                        ot.""PuedeEliminar"",
                        ot.""FechaAsignacion"",
                        ot.""Estado""
                    FROM documentos.""Oficinas_TRD"" ot
                    INNER JOIN documentos.""Oficinas"" o ON ot.""Oficina"" = o.""Cod"" AND ot.""Entidad"" = o.""Entidad""
                    INNER JOIN documentos.""Tablas_Retencion_Documental"" t ON ot.""TRD"" = t.""Cod""
                    {whereClause}
                    ORDER BY o.""Nombre"", t.""Codigo""";

                return await connection.QueryAsync<OficinaTRDDto>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo asignaciones Oficina-TRD");
                return Enumerable.Empty<OficinaTRDDto>();
            }
        }

        public async Task<IEnumerable<TRDConPermisosDto>> ObtenerTRDsDeOficinaAsync(
            long entidadId,
            long oficinaId)
        {
            const string sql = @"
                SELECT 
                    t.""Cod"",
                    t.""Codigo"",
                    t.""Nombre"",
                    t.""Tipo"",
                    t.""TRDPadre"",
                    tp.""Nombre"" AS ""NombrePadre"",
                    true AS ""TieneAcceso"",
                    ot.""PuedeEditar"",
                    ot.""PuedeEliminar""
                FROM documentos.""Oficinas_TRD"" ot
                INNER JOIN documentos.""Tablas_Retencion_Documental"" t ON ot.""TRD"" = t.""Cod""
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" tp ON t.""TRDPadre"" = tp.""Cod""
                WHERE ot.""Entidad"" = @EntidadId 
                  AND ot.""Oficina"" = @OficinaId 
                  AND ot.""Estado"" = true
                  AND t.""Estado"" = true
                ORDER BY t.""Codigo""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<TRDConPermisosDto>(sql, new { EntidadId = entidadId, OficinaId = oficinaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo TRDs de oficina {OficinaId}", oficinaId);
                return Enumerable.Empty<TRDConPermisosDto>();
            }
        }

        public async Task<IEnumerable<OficinaTRDDto>> ObtenerOficinasConTRDAsync(
            long entidadId,
            long trdId)
        {
            var filtros = new FiltrosOficinaTRDRequest { TRDId = trdId, SoloActivas = true };
            return await ObtenerAsignacionesAsync(entidadId, filtros);
        }

        public async Task<ResumenPermisosOficinaDto> ObtenerResumenOficinaAsync(
            long entidadId,
            long oficinaId)
        {
            var resumen = new ResumenPermisosOficinaDto { OficinaId = oficinaId };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener nombre de oficina
                resumen.NombreOficina = await connection.ExecuteScalarAsync<string>(@"
                    SELECT ""Nombre"" FROM documentos.""Oficinas"" 
                    WHERE ""Cod"" = @OficinaId AND ""Entidad"" = @EntidadId",
                    new { OficinaId = oficinaId, EntidadId = entidadId }) ?? string.Empty;

                // Estadísticas
                var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT 
                        COUNT(*) AS ""Total"",
                        COUNT(*) FILTER (WHERE t.""Tipo"" = 'Serie') AS ""Series"",
                        COUNT(*) FILTER (WHERE t.""Tipo"" = 'Subserie') AS ""Subseries"",
                        COUNT(*) FILTER (WHERE ot.""PuedeEditar"" = true) AS ""ConEditar"",
                        COUNT(*) FILTER (WHERE ot.""PuedeEliminar"" = true) AS ""ConEliminar""
                    FROM documentos.""Oficinas_TRD"" ot
                    INNER JOIN documentos.""Tablas_Retencion_Documental"" t ON ot.""TRD"" = t.""Cod""
                    WHERE ot.""Entidad"" = @EntidadId 
                      AND ot.""Oficina"" = @OficinaId 
                      AND ot.""Estado"" = true",
                    new { EntidadId = entidadId, OficinaId = oficinaId });

                if (stats != null)
                {
                    resumen.TotalTRDsAsignadas = (int)(stats.Total ?? 0);
                    resumen.SeriesAsignadas = (int)(stats.Series ?? 0);
                    resumen.SubseriesAsignadas = (int)(stats.Subseries ?? 0);
                    resumen.ConPermisoEditar = (int)(stats.ConEditar ?? 0);
                    resumen.ConPermisoEliminar = (int)(stats.ConEliminar ?? 0);
                }

                // Lista de TRDs
                resumen.TRDs = await ObtenerTRDsDeOficinaAsync(entidadId, oficinaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen de oficina {OficinaId}", oficinaId);
            }

            return resumen;
        }

        public async Task<IEnumerable<TRDConPermisosDto>> ObtenerTRDsConEstadoAccesoAsync(
            long entidadId,
            long oficinaId)
        {
            const string sql = @"
                SELECT 
                    t.""Cod"",
                    t.""Codigo"",
                    t.""Nombre"",
                    t.""Tipo"",
                    t.""TRDPadre"",
                    tp.""Nombre"" AS ""NombrePadre"",
                    CASE WHEN ot.""Cod"" IS NOT NULL THEN true ELSE false END AS ""TieneAcceso"",
                    COALESCE(ot.""PuedeEditar"", false) AS ""PuedeEditar"",
                    COALESCE(ot.""PuedeEliminar"", false) AS ""PuedeEliminar""
                FROM documentos.""Tablas_Retencion_Documental"" t
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" tp ON t.""TRDPadre"" = tp.""Cod""
                LEFT JOIN documentos.""Oficinas_TRD"" ot 
                    ON t.""Cod"" = ot.""TRD"" 
                    AND ot.""Oficina"" = @OficinaId 
                    AND ot.""Entidad"" = @EntidadId
                    AND ot.""Estado"" = true
                WHERE t.""Entidad"" = @EntidadId AND t.""Estado"" = true
                ORDER BY t.""Codigo""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<TRDConPermisosDto>(sql, new { EntidadId = entidadId, OficinaId = oficinaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo TRDs con estado de acceso");
                return Enumerable.Empty<TRDConPermisosDto>();
            }
        }

        // ==================== VERIFICACIÓN DE ACCESO ====================

        public async Task<VerificacionAccesoCarpetaDto> VerificarAccesoCarpetaAsync(
            long entidadId,
            long oficinaId,
            long carpetaId)
        {
            const string sql = @"
                SELECT 
                    CASE WHEN ot.""Cod"" IS NOT NULL THEN true ELSE false END AS ""TieneAcceso"",
                    COALESCE(ot.""PuedeEditar"", false) AS ""PuedeEditar"",
                    COALESCE(ot.""PuedeEliminar"", false) AS ""PuedeEliminar"",
                    c.""TRD"" AS ""CarpetaTRDCod"",
                    t.""Cod"" AS ""TRDCod"",
                    t.""Nombre"" AS ""TRDNombre"",
                    t.""Codigo"" AS ""TRDCodigo""
                FROM documentos.""Carpetas"" c
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" t ON c.""TRD"" = t.""Cod""
                LEFT JOIN documentos.""Oficinas_TRD"" ot 
                    ON t.""Cod"" = ot.""TRD"" 
                    AND ot.""Oficina"" = @OficinaId 
                    AND ot.""Entidad"" = @EntidadId
                    AND ot.""Estado"" = true
                WHERE c.""Cod"" = @CarpetaId AND c.""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<VerificacionAccesoCarpetaDto>(
                    sql,
                    new { OficinaId = oficinaId, EntidadId = entidadId, CarpetaId = carpetaId });

                if (resultado == null)
                {
                    return new VerificacionAccesoCarpetaDto
                    {
                        TieneAcceso = false,
                        Mensaje = "Carpeta no encontrada"
                    };
                }

                resultado.Mensaje = resultado.TieneAcceso ? "Acceso permitido" : "La oficina no tiene acceso a esta carpeta";
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando acceso a carpeta {CarpetaId}", carpetaId);
                return new VerificacionAccesoCarpetaDto
                {
                    TieneAcceso = false,
                    Mensaje = "Error al verificar acceso: " + ex.Message
                };
            }
        }

        public async Task<VerificacionAccesoCarpetaDto> VerificarAccesoTRDAsync(
            long entidadId,
            long oficinaId,
            long trdId)
        {
            const string sql = @"
                SELECT 
                    ot.""TRD"" AS ""TRDCod"",
                    t.""Nombre"" AS ""TRDNombre"",
                    t.""Codigo"" AS ""TRDCodigo"",
                    ot.""PuedeEditar"",
                    ot.""PuedeEliminar""
                FROM documentos.""Oficinas_TRD"" ot
                INNER JOIN documentos.""Tablas_Retencion_Documental"" t ON ot.""TRD"" = t.""Cod""
                WHERE ot.""Entidad"" = @EntidadId 
                  AND ot.""Oficina"" = @OficinaId 
                  AND ot.""TRD"" = @TRDId
                  AND ot.""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<VerificacionAccesoCarpetaDto>(
                    sql,
                    new { EntidadId = entidadId, OficinaId = oficinaId, TRDId = trdId });

                if (resultado == null)
                {
                    return new VerificacionAccesoCarpetaDto
                    {
                        TieneAcceso = false,
                        Mensaje = "La oficina no tiene acceso a esta TRD"
                    };
                }

                resultado.TieneAcceso = true;
                resultado.Mensaje = "Acceso permitido";
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando acceso a TRD {TRDId}", trdId);
                return new VerificacionAccesoCarpetaDto
                {
                    TieneAcceso = false,
                    Mensaje = "Error al verificar acceso: " + ex.Message
                };
            }
        }

        // ==================== ASIGNACIÓN ====================

        public async Task<ResultadoOficinaTRDDto> AsignarTRDAsync(
            long entidadId,
            AsignarTRDAOficinaRequest request)
        {
            const string sql = @"
                SELECT ""Exito"", ""Mensaje""
                FROM documentos.""F_AsignarTRDOficina""(
                    @TRDId, @OficinaId, @EntidadId, @PuedeEditar, @PuedeEliminar
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoFuncionDto>(
                    sql,
                    new
                    {
                        request.OficinaId,
                        request.TRDId,
                        EntidadId = entidadId,
                        request.PuedeEditar,
                        request.PuedeEliminar
                    });

                if (resultado == null || !resultado.Exito)
                {
                    return new ResultadoOficinaTRDDto
                    {
                        Exito = false,
                        Mensaje = resultado?.Mensaje ?? "Error al asignar TRD"
                    };
                }

                // Obtener la asignación creada
                var asignaciones = await ObtenerAsignacionesAsync(entidadId,
                    new FiltrosOficinaTRDRequest { OficinaId = request.OficinaId, TRDId = request.TRDId });

                return new ResultadoOficinaTRDDto
                {
                    Exito = true,
                    Mensaje = resultado.Mensaje ?? "TRD asignada correctamente",
                    Asignacion = asignaciones.FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando TRD {TRDId} a oficina {OficinaId}", request.TRDId, request.OficinaId);
                return new ResultadoOficinaTRDDto
                {
                    Exito = false,
                    Mensaje = "Error al asignar TRD: " + ex.Message
                };
            }
        }

        public async Task<ResultadoAsignacionMasivaDto> AsignarTRDsMasivoAsync(
            long entidadId,
            AsignarTRDsMasivoRequest request)
        {
            var resultado = new ResultadoAsignacionMasivaDto();

            foreach (var trd in request.TRDs)
            {
                var asignacion = await AsignarTRDAsync(entidadId, new AsignarTRDAOficinaRequest
                {
                    OficinaId = request.OficinaId,
                    TRDId = trd.TRDId,
                    PuedeEditar = trd.PuedeEditar,
                    PuedeEliminar = trd.PuedeEliminar
                });

                if (asignacion.Exito)
                {
                    resultado.Asignadas++;
                }
                else
                {
                    resultado.Omitidas++;
                    resultado.Errores.Add($"TRD {trd.TRDId}: {asignacion.Mensaje}");
                }
            }

            resultado.Exito = resultado.Asignadas > 0;
            resultado.Mensaje = $"Se asignaron {resultado.Asignadas} TRDs. {resultado.Omitidas} omitidas.";

            return resultado;
        }

        public async Task<ResultadoOficinaTRDDto> ActualizarPermisosAsync(
            long entidadId,
            long oficinaId,
            long trdId,
            ActualizarPermisosOficinaTRDRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Oficinas_TRD""
                SET ""PuedeEditar"" = @PuedeEditar, ""PuedeEliminar"" = @PuedeEliminar
                WHERE ""Entidad"" = @EntidadId AND ""Oficina"" = @OficinaId AND ""TRD"" = @TRDId AND ""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    EntidadId = entidadId,
                    OficinaId = oficinaId,
                    TRDId = trdId,
                    request.PuedeEditar,
                    request.PuedeEliminar
                });

                if (affected == 0)
                {
                    return new ResultadoOficinaTRDDto
                    {
                        Exito = false,
                        Mensaje = "Asignación no encontrada"
                    };
                }

                var asignaciones = await ObtenerAsignacionesAsync(entidadId,
                    new FiltrosOficinaTRDRequest { OficinaId = oficinaId, TRDId = trdId });

                return new ResultadoOficinaTRDDto
                {
                    Exito = true,
                    Mensaje = "Permisos actualizados correctamente",
                    Asignacion = asignaciones.FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando permisos");
                return new ResultadoOficinaTRDDto
                {
                    Exito = false,
                    Mensaje = "Error al actualizar permisos: " + ex.Message
                };
            }
        }

        public async Task<ResultadoOficinaTRDDto> RevocarTRDAsync(
            long entidadId,
            long oficinaId,
            long trdId)
        {
            const string sql = @"
                UPDATE documentos.""Oficinas_TRD""
                SET ""Estado"" = false
                WHERE ""Entidad"" = @EntidadId AND ""Oficina"" = @OficinaId AND ""TRD"" = @TRDId AND ""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql,
                    new { EntidadId = entidadId, OficinaId = oficinaId, TRDId = trdId });

                if (affected == 0)
                {
                    return new ResultadoOficinaTRDDto
                    {
                        Exito = false,
                        Mensaje = "Asignación no encontrada"
                    };
                }

                return new ResultadoOficinaTRDDto
                {
                    Exito = true,
                    Mensaje = "Acceso revocado correctamente"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revocando TRD {TRDId} de oficina {OficinaId}", trdId, oficinaId);
                return new ResultadoOficinaTRDDto
                {
                    Exito = false,
                    Mensaje = "Error al revocar TRD: " + ex.Message
                };
            }
        }

        public async Task<ResultadoAsignacionMasivaDto> RevocarTodosAsync(
            long entidadId,
            long oficinaId)
        {
            var resultado = new ResultadoAsignacionMasivaDto();

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(@"
                    UPDATE documentos.""Oficinas_TRD""
                    SET ""Estado"" = false
                    WHERE ""Entidad"" = @EntidadId AND ""Oficina"" = @OficinaId AND ""Estado"" = true",
                    new { EntidadId = entidadId, OficinaId = oficinaId });

                resultado.Exito = true;
                resultado.Asignadas = affected;
                resultado.Mensaje = $"Se revocaron {affected} asignaciones.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revocando todos los accesos de oficina {OficinaId}", oficinaId);
                resultado.Exito = false;
                resultado.Mensaje = "Error al revocar accesos: " + ex.Message;
            }

            return resultado;
        }

        // ==================== CLASES AUXILIARES ====================

        private class ResultadoFuncionDto
        {
            public bool Exito { get; set; }
            public string? Mensaje { get; set; }
        }
    }
}