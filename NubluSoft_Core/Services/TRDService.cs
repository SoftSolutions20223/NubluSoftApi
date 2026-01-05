using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class TRDService : ITRDService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<TRDService> _logger;

        public TRDService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<TRDService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS TRD ====================

        public async Task<IEnumerable<TRD>> ObtenerPorEntidadAsync(long entidadId, FiltrosTRDRequest? filtros = null)
        {
            var sql = @"
                SELECT 
                    t.""Cod"", t.""Codigo"", t.""Nombre"", t.""Descripcion"", t.""TRDPadre"",
                    t.""Entidad"", t.""TiempoGestion"", t.""TiempoCentral"", t.""DisposicionFinal"",
                    t.""Procedimiento"", t.""Estado"", t.""FechaCreacion"", t.""CreadoPor"",
                    t.""FechaModificacion"", t.""ModificadoPor"",
                    tp.""Nombre"" AS ""NombreTRDPadre"",
                    tp.""Codigo"" AS ""CodigoTRDPadre"",
                    df.""Nombre"" AS ""NombreDisposicionFinal"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreCreadoPor""
                FROM documentos.""Tablas_Retencion_Documental"" t
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" tp ON t.""TRDPadre"" = tp.""Cod""
                LEFT JOIN documentos.""Disposiciones_Finales"" df ON t.""DisposicionFinal"" = df.""Cod""
                LEFT JOIN documentos.""Usuarios"" u ON t.""CreadoPor"" = u.""Cod""
                WHERE t.""Entidad"" = @EntidadId";

            var parameters = new DynamicParameters();
            parameters.Add("EntidadId", entidadId);

            if (filtros?.SoloActivas == true)
            {
                sql += @" AND t.""Estado"" = true";
            }

            if (filtros?.SoloSeries == true)
            {
                sql += @" AND t.""TRDPadre"" IS NULL";
            }

            if (filtros?.SoloSubseries == true)
            {
                sql += @" AND t.""TRDPadre"" IS NOT NULL";
            }

            if (filtros?.TRDPadre.HasValue == true)
            {
                sql += @" AND t.""TRDPadre"" = @TRDPadre";
                parameters.Add("TRDPadre", filtros.TRDPadre.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtros?.Busqueda))
            {
                sql += @" AND (t.""Nombre"" ILIKE @Busqueda OR t.""Codigo"" ILIKE @Busqueda OR t.""Descripcion"" ILIKE @Busqueda)";
                parameters.Add("Busqueda", $"%{filtros.Busqueda}%");
            }

            sql += @" ORDER BY t.""Codigo"", t.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<TRD>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo TRDs de entidad {EntidadId}", entidadId);
                return Enumerable.Empty<TRD>();
            }
        }

        public async Task<TRD?> ObtenerPorIdAsync(long trdId)
        {
            const string sql = @"
                SELECT 
                    t.""Cod"", t.""Codigo"", t.""Nombre"", t.""Descripcion"", t.""TRDPadre"",
                    t.""Entidad"", t.""TiempoGestion"", t.""TiempoCentral"", t.""DisposicionFinal"",
                    t.""Procedimiento"", t.""Estado"", t.""FechaCreacion"", t.""CreadoPor"",
                    t.""FechaModificacion"", t.""ModificadoPor"",
                    tp.""Nombre"" AS ""NombreTRDPadre"",
                    tp.""Codigo"" AS ""CodigoTRDPadre"",
                    df.""Nombre"" AS ""NombreDisposicionFinal"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreCreadoPor""
                FROM documentos.""Tablas_Retencion_Documental"" t
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" tp ON t.""TRDPadre"" = tp.""Cod""
                LEFT JOIN documentos.""Disposiciones_Finales"" df ON t.""DisposicionFinal"" = df.""Cod""
                LEFT JOIN documentos.""Usuarios"" u ON t.""CreadoPor"" = u.""Cod""
                WHERE t.""Cod"" = @TRDId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<TRD>(sql, new { TRDId = trdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo TRD {TRDId}", trdId);
                return null;
            }
        }

        public async Task<IEnumerable<TRDArbol>> ObtenerArbolAsync(long entidadId)
        {
            const string sql = @"
                SELECT 
                    t.""Cod"", t.""Codigo"", t.""Nombre"", t.""Descripcion"", t.""TRDPadre"",
                    t.""Entidad"", t.""TiempoGestion"", t.""TiempoCentral"", t.""DisposicionFinal"",
                    t.""Procedimiento"", t.""Estado"",
                    df.""Nombre"" AS ""NombreDisposicionFinal"",
                    (SELECT COUNT(*) FROM documentos.""Tablas_Retencion_Documental"" s WHERE s.""TRDPadre"" = t.""Cod"" AND s.""Estado"" = true) AS ""CantidadSubseries"",
                    (SELECT COUNT(*) FROM documentos.""Oficinas_TRD"" ot WHERE ot.""TRD"" = t.""Cod"" AND ot.""Estado"" = true) AS ""CantidadOficinasAsignadas""
                FROM documentos.""Tablas_Retencion_Documental"" t
                LEFT JOIN documentos.""Disposiciones_Finales"" df ON t.""DisposicionFinal"" = df.""Cod""
                WHERE t.""Entidad"" = @EntidadId AND t.""Estado"" = true
                ORDER BY t.""Codigo"", t.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var trds = await connection.QueryAsync<TRDArbol>(sql, new { EntidadId = entidadId });
                return ConstruirArbol(trds.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo árbol TRD de entidad {EntidadId}", entidadId);
                return Enumerable.Empty<TRDArbol>();
            }
        }

        public async Task<IEnumerable<TRD>> ObtenerSubseriesAsync(long trdPadreId)
        {
            const string sql = @"
                SELECT 
                    t.""Cod"", t.""Codigo"", t.""Nombre"", t.""Descripcion"", t.""TRDPadre"",
                    t.""Entidad"", t.""TiempoGestion"", t.""TiempoCentral"", t.""DisposicionFinal"",
                    t.""Procedimiento"", t.""Estado"",
                    df.""Nombre"" AS ""NombreDisposicionFinal""
                FROM documentos.""Tablas_Retencion_Documental"" t
                LEFT JOIN documentos.""Disposiciones_Finales"" df ON t.""DisposicionFinal"" = df.""Cod""
                WHERE t.""TRDPadre"" = @TRDPadreId AND t.""Estado"" = true
                ORDER BY t.""Codigo"", t.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<TRD>(sql, new { TRDPadreId = trdPadreId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo subseries de TRD {TRDPadreId}", trdPadreId);
                return Enumerable.Empty<TRD>();
            }
        }

        // ==================== CRUD TRD ====================

        public async Task<ResultadoTRD> CrearAsync(long entidadId, long usuarioId, CrearTRDRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_CrearTRD""(
                    p_Codigo := @Codigo,
                    p_Nombre := @Nombre,
                    p_Descripcion := @Descripcion,
                    p_TRDPadre := @TRDPadre,
                    p_Entidad := @Entidad,
                    p_TiempoGestion := @TiempoGestion,
                    p_TiempoCentral := @TiempoCentral,
                    p_DisposicionFinal := @DisposicionFinal,
                    p_Procedimiento := @Procedimiento,
                    p_Usuario := @Usuario
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoTRD>(sql, new
                {
                    request.Codigo,
                    request.Nombre,
                    request.Descripcion,
                    request.TRDPadre,
                    Entidad = entidadId,
                    request.TiempoGestion,
                    request.TiempoCentral,
                    request.DisposicionFinal,
                    request.Procedimiento,
                    Usuario = usuarioId
                });

                return resultado ?? new ResultadoTRD { Exito = false, Mensaje = "Error al crear la TRD" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando TRD");
                return new ResultadoTRD { Exito = false, Mensaje = "Error al crear la TRD: " + ex.Message };
            }
        }

        public async Task<(bool Success, string? Error)> ActualizarAsync(long trdId, long usuarioId, ActualizarTRDRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Tablas_Retencion_Documental"" SET
                    ""Codigo"" = @Codigo,
                    ""Nombre"" = @Nombre,
                    ""Descripcion"" = @Descripcion,
                    ""TiempoGestion"" = @TiempoGestion,
                    ""TiempoCentral"" = @TiempoCentral,
                    ""DisposicionFinal"" = @DisposicionFinal,
                    ""Procedimiento"" = @Procedimiento,
                    ""Estado"" = @Estado,
                    ""FechaModificacion"" = @FechaModificacion,
                    ""ModificadoPor"" = @ModificadoPor
                WHERE ""Cod"" = @TRDId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    TRDId = trdId,
                    request.Codigo,
                    request.Nombre,
                    request.Descripcion,
                    request.TiempoGestion,
                    request.TiempoCentral,
                    request.DisposicionFinal,
                    request.Procedimiento,
                    request.Estado,
                    FechaModificacion = DateTime.Now,
                    ModificadoPor = usuarioId
                });

                if (affected == 0)
                    return (false, "TRD no encontrada");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando TRD {TRDId}", trdId);
                return (false, "Error al actualizar la TRD: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> EliminarAsync(long trdId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que no tenga subseries activas
                var tieneSubseries = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""Cod"" FROM documentos.""Tablas_Retencion_Documental"" 
                      WHERE ""TRDPadre"" = @TRDId AND ""Estado"" = true LIMIT 1",
                    new { TRDId = trdId });

                if (tieneSubseries.HasValue)
                    return (false, "No se puede eliminar una Serie que tiene Subseries activas");

                // Verificar que no tenga carpetas asociadas
                var tieneCarpetas = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""Cod"" FROM documentos.""Carpetas"" 
                      WHERE ""TRD"" = @TRDId AND ""Estado"" = true LIMIT 1",
                    new { TRDId = trdId });

                if (tieneCarpetas.HasValue)
                    return (false, "No se puede eliminar una TRD que tiene carpetas asociadas");

                // Soft delete
                var affected = await connection.ExecuteAsync(
                    @"UPDATE documentos.""Tablas_Retencion_Documental"" SET ""Estado"" = false WHERE ""Cod"" = @TRDId",
                    new { TRDId = trdId });

                if (affected == 0)
                    return (false, "TRD no encontrada");

                // Desactivar asignaciones
                await connection.ExecuteAsync(
                    @"UPDATE documentos.""Oficinas_TRD"" SET ""Estado"" = false WHERE ""TRD"" = @TRDId",
                    new { TRDId = trdId });

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando TRD {TRDId}", trdId);
                return (false, "Error al eliminar la TRD: " + ex.Message);
            }
        }

        // ==================== ASIGNACIONES OFICINA-TRD ====================

        public async Task<IEnumerable<OficinaTRD>> ObtenerOficinasAsignadasAsync(long trdId)
        {
            const string sql = @"
                SELECT 
                    ot.""Entidad"", ot.""Cod"", ot.""Oficina"", ot.""TRD"",
                    ot.""PuedeEditar"", ot.""PuedeEliminar"", ot.""Estado"",
                    ot.""FechaAsignacion"", ot.""AsignadoPor"",
                    o.""Nombre"" AS ""NombreOficina"",
                    o.""Codigo"" AS ""CodigoOficina"",
                    t.""Nombre"" AS ""NombreTRD"",
                    t.""Codigo"" AS ""CodigoTRD"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreAsignadoPor""
                FROM documentos.""Oficinas_TRD"" ot
                INNER JOIN documentos.""Oficinas"" o ON ot.""Oficina"" = o.""Cod"" AND ot.""Entidad"" = o.""Entidad""
                INNER JOIN documentos.""Tablas_Retencion_Documental"" t ON ot.""TRD"" = t.""Cod""
                LEFT JOIN documentos.""Usuarios"" u ON ot.""AsignadoPor"" = u.""Cod""
                WHERE ot.""TRD"" = @TRDId AND ot.""Estado"" = true
                ORDER BY o.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<OficinaTRD>(sql, new { TRDId = trdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo oficinas asignadas a TRD {TRDId}", trdId);
                return Enumerable.Empty<OficinaTRD>();
            }
        }

        public async Task<IEnumerable<OficinaTRD>> ObtenerTRDsPorOficinaAsync(long entidadId, long oficinaId)
        {
            const string sql = @"
                SELECT 
                    ot.""Entidad"", ot.""Cod"", ot.""Oficina"", ot.""TRD"",
                    ot.""PuedeEditar"", ot.""PuedeEliminar"", ot.""Estado"",
                    o.""Nombre"" AS ""NombreOficina"",
                    t.""Nombre"" AS ""NombreTRD"",
                    t.""Codigo"" AS ""CodigoTRD""
                FROM documentos.""Oficinas_TRD"" ot
                INNER JOIN documentos.""Oficinas"" o ON ot.""Oficina"" = o.""Cod"" AND ot.""Entidad"" = o.""Entidad""
                INNER JOIN documentos.""Tablas_Retencion_Documental"" t ON ot.""TRD"" = t.""Cod""
                WHERE ot.""Entidad"" = @EntidadId AND ot.""Oficina"" = @OficinaId AND ot.""Estado"" = true
                ORDER BY t.""Codigo"", t.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<OficinaTRD>(sql, new { EntidadId = entidadId, OficinaId = oficinaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo TRDs de oficina {OficinaId}", oficinaId);
                return Enumerable.Empty<OficinaTRD>();
            }
        }

        public async Task<ResultadoTRD> AsignarOficinaAsync(long trdId, long entidadId, long usuarioId, AsignarTRDOficinaRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_AsignarTRDOficina""(
                    p_TRD := @TRD,
                    p_Oficina := @Oficina,
                    p_Entidad := @Entidad,
                    p_PuedeEditar := @PuedeEditar,
                    p_PuedeEliminar := @PuedeEliminar,
                    p_Usuario := @Usuario
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoTRD>(sql, new
                {
                    TRD = trdId,
                    request.Oficina,
                    Entidad = entidadId,
                    request.PuedeEditar,
                    request.PuedeEliminar,
                    Usuario = usuarioId
                });

                return resultado ?? new ResultadoTRD { Exito = false, Mensaje = "Error al asignar la oficina" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando oficina {OficinaId} a TRD {TRDId}", request.Oficina, trdId);
                return new ResultadoTRD { Exito = false, Mensaje = "Error al asignar la oficina: " + ex.Message };
            }
        }

        public async Task<(bool Success, string? Error)> RevocarOficinaAsync(long trdId, long entidadId, long oficinaId)
        {
            const string sql = @"SELECT * FROM documentos.""F_RevocarTRDOficina""(@TRD, @Oficina, @Entidad)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoTRD>(sql,
                    new { TRD = trdId, Oficina = oficinaId, Entidad = entidadId });

                if (resultado == null || !resultado.Exito)
                    return (false, resultado?.Mensaje ?? "Error al revocar la asignación");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revocando oficina {OficinaId} de TRD {TRDId}", oficinaId, trdId);
                return (false, "Error al revocar la asignación: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ActualizarPermisosAsync(long trdId, long entidadId, long oficinaId, ActualizarPermisosTRDRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Oficinas_TRD"" SET
                    ""PuedeEditar"" = @PuedeEditar,
                    ""PuedeEliminar"" = @PuedeEliminar
                WHERE ""TRD"" = @TRD AND ""Entidad"" = @Entidad AND ""Oficina"" = @Oficina AND ""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    TRD = trdId,
                    Entidad = entidadId,
                    Oficina = oficinaId,
                    request.PuedeEditar,
                    request.PuedeEliminar
                });

                if (affected == 0)
                    return (false, "Asignación no encontrada");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando permisos de oficina {OficinaId} en TRD {TRDId}", oficinaId, trdId);
                return (false, "Error al actualizar los permisos: " + ex.Message);
            }
        }

        // ==================== HELPERS ====================

        private List<TRDArbol> ConstruirArbol(List<TRDArbol> trds)
        {
            var lookup = trds.ToDictionary(t => t.Cod);
            var raices = new List<TRDArbol>();

            foreach (var trd in trds)
            {
                if (trd.TRDPadre.HasValue && lookup.ContainsKey(trd.TRDPadre.Value))
                {
                    lookup[trd.TRDPadre.Value].Subseries.Add(trd);
                }
                else
                {
                    raices.Add(trd);
                }
            }

            return raices;
        }
    }
}