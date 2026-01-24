using Dapper;
using Newtonsoft.Json;
using NubluSoft_NavIndex.Models.DTOs;
using System.IO.Compression;
using System.Text;

namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Implementación del servicio principal de NavIndex
    /// </summary>
    public class NavIndexService : INavIndexService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly IRedisCacheService _cacheService;
        private readonly ILogger<NavIndexService> _logger;

        public NavIndexService(
            IPostgresConnectionFactory connectionFactory,
            IRedisCacheService cacheService,
            ILogger<NavIndexService> logger)
        {
            _connectionFactory = connectionFactory;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<byte[]> ObtenerEstructuraComprimidaAsync(long entidadId)
        {
            try
            {
                // 1. Intentar obtener de caché
                var cached = await _cacheService.ObtenerEstructuraComprimidaAsync(entidadId);
                if (cached != null)
                {
                    return cached;
                }

                // 2. Generar desde PostgreSQL
                var estructura = await GenerarEstructuraDesdeDbAsync(entidadId);

                // 3. Serializar y comprimir
                var json = JsonConvert.SerializeObject(estructura, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss"
                });

                var comprimido = ComprimirGzip(json);

                // 4. Guardar en caché
                await _cacheService.GuardarEstructuraComprimidaAsync(entidadId, comprimido, estructura.Version);

                _logger.LogInformation(
                    "Estructura generada para entidad {EntidadId}: {TotalNodos} nodos, {SizeOriginal} bytes JSON, {SizeComprimido} bytes comprimido",
                    entidadId, estructura.TotalNodos, Encoding.UTF8.GetByteCount(json), comprimido.Length);

                return comprimido;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estructura para entidad {EntidadId}", entidadId);
                throw;
            }
        }

        public async Task<VersionEstructura> ObtenerVersionAsync(long entidadId)
        {
            try
            {
                var versionCached = await _cacheService.ObtenerVersionAsync(entidadId);

                if (versionCached != null)
                {
                    return new VersionEstructura
                    {
                        EntidadId = entidadId,
                        Version = versionCached,
                        UltimaActualizacion = DateTime.UtcNow
                    };
                }

                // Si no hay caché, generar la estructura para obtener versión
                var estructura = await GenerarEstructuraDesdeDbAsync(entidadId);

                return new VersionEstructura
                {
                    EntidadId = entidadId,
                    Version = estructura.Version,
                    UltimaActualizacion = estructura.GeneradoEn,
                    TotalNodos = estructura.TotalNodos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo versión para entidad {EntidadId}", entidadId);
                throw;
            }
        }

        public async Task<byte[]?> ObtenerIndiceComprimidoAsync(long entidadId, long carpetaId)
        {
            try
            {
                // Siempre consultar desde PostgreSQL (sin caché)
                // Esto evita problemas de sincronización cuando se crean/modifican carpetas
                var indice = await GenerarIndiceDesdeDbAsync(carpetaId);
                if (indice == null)
                {
                    return null;
                }

                var json = JsonConvert.SerializeObject(indice, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss"
                });

                return ComprimirGzip(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo índice para carpeta {CarpetaId}", carpetaId);
                throw;
            }
        }

        public async Task<NodoNavegacion?> ObtenerNodoAsync(long carpetaId)
        {
            const string sql = @"
                SELECT 
                    c.""Cod"",
                    c.""Nombre"",
                    c.""CodSerie"",
                    c.""CodSubSerie"",
                    c.""Estado"",
                    c.""CarpetaPadre"",
                    c.""FechaCreacion"",
                    c.""SerieRaiz"",
                    c.""NivelVisualizacion"",
                    c.""TipoCarpeta"",
                    c.""Delegado"",
                    c.""TRD"",
                    t.""Codigo"" AS ""CodigoTRD"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta""
                FROM documentos.""Carpetas"" c
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" t ON c.""TRD"" = t.""Cod""
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON c.""TipoCarpeta"" = tc.""Cod""
                WHERE c.""Cod"" = @CarpetaId AND c.""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<NodoNavegacion>(sql, new { CarpetaId = carpetaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo nodo {CarpetaId}", carpetaId);
                return null;
            }
        }

        public async Task<ResultadoNavIndex> RegenerarEstructuraAsync(long entidadId)
        {
            try
            {
                await _cacheService.InvalidarCacheAsync(entidadId);
                await ObtenerEstructuraComprimidaAsync(entidadId);

                return new ResultadoNavIndex
                {
                    Exito = true,
                    Mensaje = "Estructura regenerada exitosamente"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerando estructura para entidad {EntidadId}", entidadId);
                return new ResultadoNavIndex
                {
                    Exito = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        public async Task InvalidarCacheAsync(long entidadId)
        {
            await _cacheService.InvalidarCacheAsync(entidadId);
        }

        public async Task InvalidarIndiceCacheAsync(long entidadId, long carpetaId)
        {
            await _cacheService.InvalidarIndiceAsync(entidadId, carpetaId);
        }

        public async Task<bool> RegenerarConLockAsync(long entidadId)
        {
            // Duración máxima del lock (debe ser mayor al tiempo de regeneración)
            var lockDuration = TimeSpan.FromSeconds(30);

            // Intentar adquirir lock
            var lockToken = await _cacheService.AdquirirLockRegeneracionAsync(entidadId, lockDuration);

            if (lockToken == null)
            {
                // Otro proceso ya está regenerando, salir sin hacer nada
                _logger.LogDebug("Regeneración omitida para entidad {EntidadId} - otro proceso está regenerando", entidadId);
                return false;
            }

            try
            {
                _logger.LogInformation("Iniciando regeneración proactiva para entidad {EntidadId}", entidadId);

                // Generar nueva estructura desde PostgreSQL
                var estructura = await GenerarEstructuraDesdeDbAsync(entidadId);

                // Serializar y comprimir
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(estructura, new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss"
                });

                var comprimido = ComprimirGzip(json);

                // Guardar en caché (sobrescribe el anterior atómicamente)
                await _cacheService.GuardarEstructuraComprimidaAsync(entidadId, comprimido, estructura.Version);

                _logger.LogInformation(
                    "Regeneración completada para entidad {EntidadId}: {TotalNodos} nodos, {Size} bytes",
                    entidadId, estructura.TotalNodos, comprimido.Length);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en regeneración proactiva para entidad {EntidadId}", entidadId);
                return false;
            }
            finally
            {
                // Siempre liberar el lock
                await _cacheService.LiberarLockRegeneracionAsync(entidadId, lockToken);
            }
        }

        #region Métodos Privados

        /// <summary>
        /// Genera la estructura documental desde PostgreSQL
        /// Incluye: Series, Subseries, Genéricas en raíz, Expedientes/Genéricas hijos directos de Serie/Subserie,
        /// y también hijos directos de Genéricas raíz.
        /// Para Series (TipoCarpeta = 1) y Genéricas raíz incluye estadísticas.
        /// </summary>
        private async Task<EstructuraDocumental> GenerarEstructuraDesdeDbAsync(long entidadId)
        {
            // Obtener carpetas que van en la estructura:
            // - Todas las Series (TipoCarpeta = 1) con estadísticas
            // - Todas las Subseries (TipoCarpeta = 2)
            // - Genéricas en la raíz (TipoCarpeta = 4 y CarpetaPadre IS NULL) con estadísticas
            // - Expedientes y Genéricas cuyo padre es Serie o Subserie
            // - Expedientes y Genéricas cuyo padre es una Genérica raíz
            const string sql = @"
                SELECT
                    c.""Cod"",
                    c.""Nombre"",
                    c.""CodSerie"",
                    c.""CodSubSerie"",
                    c.""Estado"",
                    c.""CarpetaPadre"",
                    c.""FechaCreacion"",
                    c.""SerieRaiz"",
                    c.""NivelVisualizacion"",
                    c.""TipoCarpeta"",
                    c.""Delegado"",
                    c.""TRD"",
                    t.""Codigo"" AS ""CodigoTRD"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta"",

                    -- Estadísticas para carpetas raíz: Series (TipoCarpeta = 1) y Genéricas raíz (TipoCarpeta = 4 sin padre)
                    CASE WHEN c.""TipoCarpeta"" = 1 OR (c.""TipoCarpeta"" = 4 AND c.""CarpetaPadre"" IS NULL)
                         THEN stats.""ExpedientesActivos"" END AS ""ExpedientesActivos"",
                    CASE WHEN c.""TipoCarpeta"" = 1 OR (c.""TipoCarpeta"" = 4 AND c.""CarpetaPadre"" IS NULL)
                         THEN stats.""DocumentosTotales"" END AS ""DocumentosTotales"",
                    CASE WHEN c.""TipoCarpeta"" = 1 OR (c.""TipoCarpeta"" = 4 AND c.""CarpetaPadre"" IS NULL)
                         THEN stats.""UsuariosConAcceso"" END AS ""UsuariosConAcceso"",
                    CASE WHEN c.""TipoCarpeta"" = 1 OR (c.""TipoCarpeta"" = 4 AND c.""CarpetaPadre"" IS NULL)
                         THEN stats.""UltimaModificacion"" END AS ""UltimaModificacion""
                FROM documentos.""Carpetas"" c
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" t ON c.""TRD"" = t.""Cod""
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON c.""TipoCarpeta"" = tc.""Cod""
                LEFT JOIN LATERAL (
                    SELECT
                        -- Expedientes activos (TipoCarpeta = 3, EstadoCarpeta = 1 Abierto)
                        -- Para Series: busca por SerieRaiz, para Genéricas raíz: busca por CarpetaPadre recursivo
                        (SELECT COUNT(*)
                         FROM documentos.""Carpetas"" sub
                         WHERE (sub.""SerieRaiz"" = c.""Cod"" OR sub.""CarpetaPadre"" = c.""Cod"")
                           AND sub.""TipoCarpeta"" = 3
                           AND sub.""EstadoCarpeta"" = 1
                           AND sub.""Estado"" = true) AS ""ExpedientesActivos"",

                        -- Documentos totales en la carpeta y sus subcarpetas
                        (SELECT COUNT(*)
                         FROM documentos.""Archivos"" a
                         INNER JOIN documentos.""Carpetas"" ca ON a.""Carpeta"" = ca.""Cod""
                         WHERE (ca.""SerieRaiz"" = c.""Cod"" OR ca.""CarpetaPadre"" = c.""Cod"" OR ca.""Cod"" = c.""Cod"")
                           AND a.""Estado"" = true
                           AND ca.""Estado"" = true) AS ""DocumentosTotales"",

                        -- Usuarios con acceso (para Series usa TRD, para Genéricas cuenta usuarios de la entidad)
                        (SELECT COUNT(DISTINCT ru.""Usuario"")
                         FROM documentos.""Roles_Usuarios"" ru
                         INNER JOIN documentos.""Oficinas_TRD"" ot ON ru.""Oficina"" = ot.""Oficina""
                         WHERE (c.""TRD"" IS NOT NULL AND ot.""TRD"" = c.""TRD"" OR c.""TRD"" IS NULL)
                           AND ot.""Estado"" = true
                           AND ot.""Entidad"" = @EntidadId
                           AND ru.""Estado"" = true) AS ""UsuariosConAcceso"",

                        -- Última modificación (carpetas o archivos)
                        GREATEST(
                            c.""FechaModificacion"",
                            (SELECT MAX(sub.""FechaModificacion"")
                             FROM documentos.""Carpetas"" sub
                             WHERE (sub.""SerieRaiz"" = c.""Cod"" OR sub.""CarpetaPadre"" = c.""Cod"")
                               AND sub.""Estado"" = true),
                            (SELECT MAX(a.""FechaModificacion"")
                             FROM documentos.""Archivos"" a
                             INNER JOIN documentos.""Carpetas"" ca ON a.""Carpeta"" = ca.""Cod""
                             WHERE (ca.""SerieRaiz"" = c.""Cod"" OR ca.""CarpetaPadre"" = c.""Cod"" OR ca.""Cod"" = c.""Cod"")
                               AND a.""Estado"" = true AND ca.""Estado"" = true)
                        ) AS ""UltimaModificacion""
                ) stats ON c.""TipoCarpeta"" = 1 OR (c.""TipoCarpeta"" = 4 AND c.""CarpetaPadre"" IS NULL)
                WHERE c.""Estado"" = true
                AND c.""Entidad"" = @EntidadId
                AND (
                    -- Series y Subseries siempre
                    c.""TipoCarpeta"" IN (1, 2)
                    OR
                    -- Genéricas en la raíz (sin padre)
                    (c.""TipoCarpeta"" = 4 AND c.""CarpetaPadre"" IS NULL)
                    OR
                    -- Expedientes y Genéricas cuyo padre es Serie o Subserie
                    (c.""TipoCarpeta"" IN (3, 4) AND c.""CarpetaPadre"" IN (
                        SELECT ""Cod"" FROM documentos.""Carpetas""
                        WHERE ""TipoCarpeta"" IN (1, 2) AND ""Estado"" = true AND ""Entidad"" = @EntidadId
                    ))
                    OR
                    -- Expedientes y Genéricas cuyo padre es una Genérica raíz
                    (c.""TipoCarpeta"" IN (3, 4) AND c.""CarpetaPadre"" IN (
                        SELECT ""Cod"" FROM documentos.""Carpetas""
                        WHERE ""TipoCarpeta"" = 4 AND ""CarpetaPadre"" IS NULL AND ""Estado"" = true AND ""Entidad"" = @EntidadId
                    ))
                )
                ORDER BY c.""TipoCarpeta"", t.""Codigo"", c.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var nodos = (await connection.QueryAsync<NodoNavegacion>(sql, new { EntidadId = entidadId })).ToList();

                var version = DateTime.UtcNow.Ticks.ToString();

                return new EstructuraDocumental
                {
                    EntidadId = entidadId,
                    Version = version,
                    GeneradoEn = DateTime.UtcNow,
                    TotalNodos = nodos.Count,
                    Nodos = nodos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando estructura desde DB para entidad {EntidadId}", entidadId);
                throw;
            }
        }

        /// <summary>
        /// Genera el índice (contenido) de una carpeta
        /// Incluye solo subcarpetas hijas directas y archivos de la carpeta actual
        /// </summary>
        private async Task<IndiceContenido?> GenerarIndiceDesdeDbAsync(long carpetaId)
        {
            const string sqlCarpeta = @"
                SELECT 
                    ""Cod"" AS CarpetaId,
                    ""Nombre"",
                    ""TipoCarpeta"",
                    ""FechaCreacion""
                FROM documentos.""Carpetas""
                WHERE ""Cod"" = @CarpetaId AND ""Estado"" = true";

            // Solo hijas directas, sin recursividad
            const string sqlSubcarpetas = @"
                SELECT
                    c.""Cod"",
                    c.""Nombre"",
                    c.""CodSerie"",
                    c.""CodSubSerie"",
                    c.""Estado"",
                    c.""CarpetaPadre"",
                    c.""FechaCreacion"",
                    c.""SerieRaiz"",
                    c.""NivelVisualizacion"",
                    c.""TipoCarpeta"",
                    c.""Delegado"",
                    c.""TRD"",
                    t.""Codigo"" AS ""CodigoTRD"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta""
                FROM documentos.""Carpetas"" c
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" t ON c.""TRD"" = t.""Cod""
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON c.""TipoCarpeta"" = tc.""Cod""
                WHERE c.""CarpetaPadre"" = @CarpetaId AND c.""Estado"" = true
                ORDER BY c.""TipoCarpeta"", c.""Nombre""";

            // Solo archivos de la carpeta actual, sin recursividad
            const string sqlArchivos = @"
                SELECT
                    a.""Cod"",
                    a.""Nombre"",
                    a.""Carpeta"",
                    a.""TipoArchivo"",
                    a.""Formato"",
                    a.""NumeroHojas"",
                    a.""Duracion"",
                    a.""Tamaño"" AS ""Tamano"",
                    a.""Estado"",
                    a.""Indice"",
                    ta.""Nombre"" AS ""NombreTipoArchivo""
                FROM documentos.""Archivos"" a
                LEFT JOIN documentos.""Tipos_Archivos"" ta ON a.""TipoArchivo"" = ta.""Cod""
                WHERE a.""Carpeta"" = @CarpetaId AND a.""Estado"" = true
                ORDER BY a.""Indice"", a.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener info de la carpeta principal
                var carpeta = await connection.QueryFirstOrDefaultAsync<IndiceContenido>(sqlCarpeta, new { CarpetaId = carpetaId });
                if (carpeta == null)
                {
                    return null;
                }

                // Obtener subcarpetas recursivamente
                var subcarpetas = (await connection.QueryAsync<NodoNavegacion>(sqlSubcarpetas, new { CarpetaId = carpetaId })).ToList();
                carpeta.Subcarpetas = subcarpetas;

                // Obtener archivos de todas las carpetas
                var archivos = (await connection.QueryAsync<ArchivoNavegacion>(sqlArchivos, new { CarpetaId = carpetaId })).ToList();
                carpeta.Archivos = archivos;

                return carpeta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando índice desde DB para carpeta {CarpetaId}", carpetaId);
                throw;
            }
        }

        /// <summary>
        /// Comprime string a gzip
        /// </summary>
        private byte[] ComprimirGzip(string texto)
        {
            var bytes = Encoding.UTF8.GetBytes(texto);

            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }

            return memoryStream.ToArray();
        }

        #endregion
    }
}