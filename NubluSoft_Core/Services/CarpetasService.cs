using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class CarpetasService : ICarpetasService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<CarpetasService> _logger;

        public CarpetasService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<CarpetasService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<IEnumerable<Carpeta>> ObtenerPorEntidadAsync(long entidadId, FiltrosCarpetasRequest? filtros = null)
        {
            var sql = @"
                SELECT 
                    c.""Cod"", c.""CodSerie"", c.""CodSubSerie"", c.""Estado"", c.""EstadoCarpeta"",
                    c.""Nombre"", c.""Descripcion"", c.""Copia"", c.""CarpetaOriginal"", c.""CarpetaPadre"",
                    c.""FechaCreacion"", c.""Delegado"", c.""TipoCarpeta"", c.""NivelVisualizacion"",
                    c.""SerieRaiz"", c.""TRD"", c.""FechaCierre"", c.""NumeroFolios"", c.""Soporte"",
                    c.""FrecuenciaConsulta"", c.""UbicacionFisica"", c.""Observaciones"", c.""CreadoPor"",
                    c.""ModificadoPor"", c.""FechaModificacion"", c.""Tomo"", c.""TotalTomos"",
                    c.""CodigoExpediente"", c.""PalabrasClave"", c.""FechaDocumentoInicial"", c.""FechaDocumentoFinal"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta"",
                    ec.""Nombre"" AS ""NombreEstadoCarpeta"",
                    cp.""Nombre"" AS ""NombreCarpetaPadre"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreCreadoPor"",
                    t.""Nombre"" AS ""NombreTRD"",
                    t.""Codigo"" AS ""CodigoTRD""
                FROM documentos.""Carpetas"" c
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON c.""TipoCarpeta"" = tc.""Cod"" AND tc.""Estado"" = true
                LEFT JOIN documentos.""Estados_Carpetas"" ec ON c.""EstadoCarpeta"" = ec.""Cod""
                LEFT JOIN documentos.""Carpetas"" cp ON c.""CarpetaPadre"" = cp.""Cod"" AND cp.""Estado"" = true
                LEFT JOIN documentos.""Usuarios"" u ON c.""CreadoPor"" = u.""Cod""
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" t ON c.""TRD"" = t.""Cod""
                WHERE 1=1";

            var parameters = new DynamicParameters();

            // Aplicar filtros
            if (filtros?.SoloActivas == true)
            {
                sql += @" AND c.""Estado"" = true";
            }

            if (filtros?.CarpetaPadre.HasValue == true)
            {
                sql += @" AND c.""CarpetaPadre"" = @CarpetaPadre";
                parameters.Add("CarpetaPadre", filtros.CarpetaPadre.Value);
            }

            if (filtros?.TipoCarpeta.HasValue == true)
            {
                sql += @" AND c.""TipoCarpeta"" = @TipoCarpeta";
                parameters.Add("TipoCarpeta", filtros.TipoCarpeta.Value);
            }

            if (filtros?.SerieRaiz.HasValue == true)
            {
                sql += @" AND c.""SerieRaiz"" = @SerieRaiz";
                parameters.Add("SerieRaiz", filtros.SerieRaiz.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtros?.Busqueda))
            {
                sql += @" AND (c.""Nombre"" ILIKE @Busqueda OR c.""Descripcion"" ILIKE @Busqueda OR c.""PalabrasClave"" ILIKE @Busqueda)";
                parameters.Add("Busqueda", $"%{filtros.Busqueda}%");
            }

            sql += @" ORDER BY c.""TipoCarpeta"", c.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Carpeta>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo carpetas");
                return Enumerable.Empty<Carpeta>();
            }
        }

        public async Task<IEnumerable<Carpeta>> ObtenerHijosAsync(long carpetaPadreId)
        {
            const string sql = @"
                SELECT 
                    c.""Cod"", c.""Nombre"", c.""Descripcion"", c.""TipoCarpeta"", c.""Estado"",
                    c.""EstadoCarpeta"", c.""FechaCreacion"", c.""FechaCierre"", c.""CarpetaPadre"",
                    c.""SerieRaiz"", c.""TRD"", c.""NumeroFolios"", c.""CreadoPor"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta"",
                    ec.""Nombre"" AS ""NombreEstadoCarpeta"",
                    (SELECT COUNT(*) FROM documentos.""Archivos"" a WHERE a.""Carpeta"" = c.""Cod"" AND a.""Estado"" = true) AS ""CantidadArchivos"",
                    (SELECT COUNT(*) FROM documentos.""Carpetas"" h WHERE h.""CarpetaPadre"" = c.""Cod"" AND h.""Estado"" = true) AS ""CantidadSubcarpetas""
                FROM documentos.""Carpetas"" c
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON c.""TipoCarpeta"" = tc.""Cod"" AND tc.""Estado"" = true
                LEFT JOIN documentos.""Estados_Carpetas"" ec ON c.""EstadoCarpeta"" = ec.""Cod""
                WHERE c.""CarpetaPadre"" = @CarpetaPadreId AND c.""Estado"" = true
                ORDER BY c.""TipoCarpeta"", c.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Carpeta>(sql, new { CarpetaPadreId = carpetaPadreId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo hijos de carpeta {CarpetaId}", carpetaPadreId);
                return Enumerable.Empty<Carpeta>();
            }
        }

        public async Task<Carpeta?> ObtenerPorIdAsync(long carpetaId)
        {
            const string sql = @"
                SELECT 
                    c.""Cod"", c.""CodSerie"", c.""CodSubSerie"", c.""Estado"", c.""EstadoCarpeta"",
                    c.""Nombre"", c.""Descripcion"", c.""Copia"", c.""CarpetaOriginal"", c.""CarpetaPadre"",
                    c.""FechaCreacion"", c.""Delegado"", c.""TipoCarpeta"", c.""NivelVisualizacion"",
                    c.""SerieRaiz"", c.""TRD"", c.""FechaCierre"", c.""NumeroFolios"", c.""Soporte"",
                    c.""FrecuenciaConsulta"", c.""UbicacionFisica"", c.""Observaciones"", c.""CreadoPor"",
                    c.""ModificadoPor"", c.""FechaModificacion"", c.""Tomo"", c.""TotalTomos"",
                    c.""CodigoExpediente"", c.""PalabrasClave"", c.""FechaDocumentoInicial"", c.""FechaDocumentoFinal"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta"",
                    ec.""Nombre"" AS ""NombreEstadoCarpeta"",
                    cp.""Nombre"" AS ""NombreCarpetaPadre"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreCreadoPor"",
                    t.""Nombre"" AS ""NombreTRD"",
                    t.""Codigo"" AS ""CodigoTRD""
                FROM documentos.""Carpetas"" c
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON c.""TipoCarpeta"" = tc.""Cod"" AND tc.""Estado"" = true
                LEFT JOIN documentos.""Estados_Carpetas"" ec ON c.""EstadoCarpeta"" = ec.""Cod""
                LEFT JOIN documentos.""Carpetas"" cp ON c.""CarpetaPadre"" = cp.""Cod"" AND cp.""Estado"" = true
                LEFT JOIN documentos.""Usuarios"" u ON c.""CreadoPor"" = u.""Cod""
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" t ON c.""TRD"" = t.""Cod""
                WHERE c.""Cod"" = @CarpetaId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<Carpeta>(sql, new { CarpetaId = carpetaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo carpeta {CarpetaId}", carpetaId);
                return null;
            }
        }

        public async Task<IEnumerable<Carpeta>> ObtenerRutaAsync(long carpetaId)
        {
            const string sql = @"
                WITH RECURSIVE ruta AS (
                    SELECT ""Cod"", ""Nombre"", ""TipoCarpeta"", ""CarpetaPadre"", 1 as nivel
                    FROM documentos.""Carpetas""
                    WHERE ""Cod"" = @CarpetaId AND ""Estado"" = true
                    
                    UNION ALL
                    
                    SELECT c.""Cod"", c.""Nombre"", c.""TipoCarpeta"", c.""CarpetaPadre"", r.nivel + 1
                    FROM documentos.""Carpetas"" c
                    INNER JOIN ruta r ON c.""Cod"" = r.""CarpetaPadre""
                    WHERE c.""Estado"" = true
                )
                SELECT ""Cod"", ""Nombre"", ""TipoCarpeta"", ""CarpetaPadre""
                FROM ruta
                ORDER BY nivel DESC";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Carpeta>(sql, new { CarpetaId = carpetaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ruta de carpeta {CarpetaId}", carpetaId);
                return Enumerable.Empty<Carpeta>();
            }
        }

        public async Task<IEnumerable<CarpetaArbol>> ObtenerArbolAsync(long entidadId, long? oficina = null)
        {
            // Obtener Series y Subseries (TipoCarpeta 1 y 2) como raíces del árbol
            const string sql = @"
                SELECT 
                    c.""Cod"", c.""Nombre"", c.""Descripcion"", c.""TipoCarpeta"", c.""Estado"",
                    c.""CarpetaPadre"", c.""SerieRaiz"", c.""TRD"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta"",
                    t.""Codigo"" AS ""CodigoTRD"",
                    (SELECT COUNT(*) FROM documentos.""Carpetas"" h WHERE h.""CarpetaPadre"" = c.""Cod"" AND h.""Estado"" = true) AS ""CantidadSubcarpetas""
                FROM documentos.""Carpetas"" c
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON c.""TipoCarpeta"" = tc.""Cod"" AND tc.""Estado"" = true
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" t ON c.""TRD"" = t.""Cod""
                WHERE c.""Estado"" = true AND c.""TipoCarpeta"" IN (1, 2)
                ORDER BY c.""TipoCarpeta"", t.""Codigo"", c.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var carpetas = await connection.QueryAsync<CarpetaArbol>(sql);
                return ConstruirArbol(carpetas.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo árbol de carpetas");
                return Enumerable.Empty<CarpetaArbol>();
            }
        }

        // ==================== OPERACIONES CRUD ====================

        public async Task<ResultadoCarpeta> CrearAsync(long entidadId, long usuarioId, CrearCarpetaRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_CrearCarpeta""(
                    p_Nombre := @Nombre,
                    p_Descripcion := @Descripcion,
                    p_TipoCarpeta := @TipoCarpeta,
                    p_CarpetaPadre := @CarpetaPadre,
                    p_TRD := @TRD,
                    p_Oficina := @Oficina,
                    p_Entidad := @Entidad,
                    p_Usuario := @Usuario,
                    p_NivelVisualizacion := @NivelVisualizacion,
                    p_Soporte := @Soporte,
                    p_FrecuenciaConsulta := @FrecuenciaConsulta,
                    p_UbicacionFisica := @UbicacionFisica,
                    p_Observaciones := @Observaciones,
                    p_CodigoExpediente := @CodigoExpediente,
                    p_PalabrasClave := @PalabrasClave
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoCarpeta>(sql, new
                {
                    request.Nombre,
                    request.Descripcion,
                    request.TipoCarpeta,
                    request.CarpetaPadre,
                    request.TRD,
                    request.Oficina,
                    Entidad = entidadId,
                    Usuario = usuarioId,
                    request.NivelVisualizacion,
                    request.Soporte,
                    request.FrecuenciaConsulta,
                    request.UbicacionFisica,
                    request.Observaciones,
                    request.CodigoExpediente,
                    request.PalabrasClave
                });

                return resultado ?? new ResultadoCarpeta { Exito = false, Mensaje = "Error al crear la carpeta" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando carpeta");
                return new ResultadoCarpeta { Exito = false, Mensaje = "Error al crear la carpeta: " + ex.Message };
            }
        }

        public async Task<(bool Success, string? Error)> ActualizarAsync(long carpetaId, long usuarioId, ActualizarCarpetaRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Carpetas"" SET
                    ""Nombre"" = @Nombre,
                    ""Descripcion"" = @Descripcion,
                    ""NivelVisualizacion"" = @NivelVisualizacion,
                    ""Soporte"" = @Soporte,
                    ""FrecuenciaConsulta"" = @FrecuenciaConsulta,
                    ""UbicacionFisica"" = @UbicacionFisica,
                    ""Observaciones"" = @Observaciones,
                    ""PalabrasClave"" = @PalabrasClave,
                    ""NumeroFolios"" = @NumeroFolios,
                    ""Tomo"" = @Tomo,
                    ""TotalTomos"" = @TotalTomos,
                    ""ModificadoPor"" = @ModificadoPor,
                    ""FechaModificacion"" = @FechaModificacion
                WHERE ""Cod"" = @CarpetaId AND ""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    CarpetaId = carpetaId,
                    request.Nombre,
                    request.Descripcion,
                    request.NivelVisualizacion,
                    request.Soporte,
                    request.FrecuenciaConsulta,
                    request.UbicacionFisica,
                    request.Observaciones,
                    request.PalabrasClave,
                    request.NumeroFolios,
                    request.Tomo,
                    request.TotalTomos,
                    ModificadoPor = usuarioId,
                    FechaModificacion = DateTime.Now
                });

                if (affected == 0)
                    return (false, "Carpeta no encontrada");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando carpeta {CarpetaId}", carpetaId);
                return (false, "Error al actualizar la carpeta: " + ex.Message);
            }
        }

        public async Task<ResultadoCarpeta> EliminarAsync(long carpetaId, long usuarioId)
        {
            const string sql = @"SELECT * FROM documentos.""F_EliminarCarpeta""(@CarpetaId, @UsuarioId)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoCarpeta>(sql,
                    new { CarpetaId = carpetaId, UsuarioId = usuarioId });

                return resultado ?? new ResultadoCarpeta { Exito = false, Mensaje = "Error al eliminar la carpeta" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando carpeta {CarpetaId}", carpetaId);
                return new ResultadoCarpeta { Exito = false, Mensaje = "Error al eliminar la carpeta: " + ex.Message };
            }
        }

        public async Task<ResultadoCarpeta> MoverAsync(long carpetaId, long usuarioId, MoverCarpetaRequest request)
        {
            const string sql = @"SELECT * FROM documentos.""F_MoverCarpeta""(@CarpetaId, @CarpetaDestino, @UsuarioId)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoCarpeta>(sql,
                    new { CarpetaId = carpetaId, request.CarpetaDestino, UsuarioId = usuarioId });

                return resultado ?? new ResultadoCarpeta { Exito = false, Mensaje = "Error al mover la carpeta" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moviendo carpeta {CarpetaId}", carpetaId);
                return new ResultadoCarpeta { Exito = false, Mensaje = "Error al mover la carpeta: " + ex.Message };
            }
        }

        public async Task<ResultadoCarpeta> CopiarAsync(long carpetaId, long usuarioId, CopiarCarpetaRequest request)
        {
            const string sql = @"SELECT * FROM documentos.""F_CopiarCarpeta""(@CarpetaId, @CarpetaDestino, @NuevoNombre, @IncluirArchivos, @UsuarioId)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoCarpeta>(sql,
                    new { CarpetaId = carpetaId, request.CarpetaDestino, request.NuevoNombre, request.IncluirArchivos, UsuarioId = usuarioId });

                return resultado ?? new ResultadoCarpeta { Exito = false, Mensaje = "Error al copiar la carpeta" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copiando carpeta {CarpetaId}", carpetaId);
                return new ResultadoCarpeta { Exito = false, Mensaje = "Error al copiar la carpeta: " + ex.Message };
            }
        }

        // ==================== OPERACIONES DE EXPEDIENTE ====================

        public async Task<(bool Success, string? Error)> CerrarExpedienteAsync(long carpetaId, long usuarioId, CerrarExpedienteRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Carpetas"" SET
                    ""EstadoCarpeta"" = 2, -- Cerrado
                    ""FechaCierre"" = @FechaCierre,
                    ""NumeroFolios"" = COALESCE(@NumeroFolios, ""NumeroFolios""),
                    ""Observaciones"" = COALESCE(@Observaciones, ""Observaciones""),
                    ""ModificadoPor"" = @UsuarioId,
                    ""FechaModificacion"" = @FechaCierre
                WHERE ""Cod"" = @CarpetaId 
                  AND ""Estado"" = true 
                  AND ""TipoCarpeta"" = 3 -- Solo expedientes
                  AND ""EstadoCarpeta"" = 1 -- Solo si está abierto";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    CarpetaId = carpetaId,
                    UsuarioId = usuarioId,
                    FechaCierre = DateTime.Now,
                    request.NumeroFolios,
                    request.Observaciones
                });

                if (affected == 0)
                    return (false, "Expediente no encontrado o ya está cerrado");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cerrando expediente {CarpetaId}", carpetaId);
                return (false, "Error al cerrar el expediente: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ReabrirExpedienteAsync(long carpetaId, long usuarioId)
        {
            const string sql = @"
                UPDATE documentos.""Carpetas"" SET
                    ""EstadoCarpeta"" = 1, -- Abierto
                    ""FechaCierre"" = NULL,
                    ""ModificadoPor"" = @UsuarioId,
                    ""FechaModificacion"" = @Fecha
                WHERE ""Cod"" = @CarpetaId 
                  AND ""Estado"" = true 
                  AND ""TipoCarpeta"" = 3 -- Solo expedientes
                  AND ""EstadoCarpeta"" = 2 -- Solo si está cerrado";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    CarpetaId = carpetaId,
                    UsuarioId = usuarioId,
                    Fecha = DateTime.Now
                });

                if (affected == 0)
                    return (false, "Expediente no encontrado o no está cerrado");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reabriendo expediente {CarpetaId}", carpetaId);
                return (false, "Error al reabrir el expediente: " + ex.Message);
            }
        }

        // ==================== HELPERS ====================

        private List<CarpetaArbol> ConstruirArbol(List<CarpetaArbol> carpetas)
        {
            var lookup = carpetas.ToDictionary(c => c.Cod);
            var raices = new List<CarpetaArbol>();

            foreach (var carpeta in carpetas)
            {
                if (carpeta.CarpetaPadre.HasValue && lookup.ContainsKey(carpeta.CarpetaPadre.Value))
                {
                    lookup[carpeta.CarpetaPadre.Value].Hijos.Add(carpeta);
                }
                else
                {
                    raices.Add(carpeta);
                }
            }

            return raices;
        }
    }
}