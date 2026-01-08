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
                // 1. Intentar obtener de caché
                var cached = await _cacheService.ObtenerIndiceComprimidoAsync(entidadId, carpetaId);
                if (cached != null)
                {
                    return cached;
                }

                // 2. Generar desde PostgreSQL
                var indice = await GenerarIndiceDesdeDbAsync(carpetaId);
                if (indice == null)
                {
                    return null;
                }

                // 3. Serializar y comprimir
                var json = JsonConvert.SerializeObject(indice, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss"
                });

                var comprimido = ComprimirGzip(json);

                // 4. Guardar en caché
                await _cacheService.GuardarIndiceComprimidoAsync(entidadId, carpetaId, comprimido);

                return comprimido;
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

        #region Métodos Privados

        /// <summary>
        /// Genera la estructura documental desde PostgreSQL
        /// Incluye: Series, Subseries, y Expedientes/Genéricas hijos directos de Serie/Subserie
        /// </summary>
        private async Task<EstructuraDocumental> GenerarEstructuraDesdeDbAsync(long entidadId)
        {
            // Obtener carpetas que van en la estructura:
            // - Todas las Series (TipoCarpeta = 1)
            // - Todas las Subseries (TipoCarpeta = 2)
            // - Expedientes y Genéricas (TipoCarpeta 3 y 4) cuyo padre es Serie o Subserie
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
                WHERE c.""Estado"" = true
                AND (
                    -- Series y Subseries siempre
                    c.""TipoCarpeta"" IN (1, 2)
                    OR
                    -- Expedientes y Genéricas cuyo padre es Serie o Subserie
                    (c.""TipoCarpeta"" IN (3, 4) AND c.""CarpetaPadre"" IN (
                        SELECT ""Cod"" FROM documentos.""Carpetas"" 
                        WHERE ""TipoCarpeta"" IN (1, 2) AND ""Estado"" = true
                    ))
                )
                ORDER BY c.""TipoCarpeta"", t.""Codigo"", c.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var nodos = (await connection.QueryAsync<NodoNavegacion>(sql)).ToList();

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
        /// Incluye subcarpetas y archivos recursivamente
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

            const string sqlSubcarpetas = @"
                WITH RECURSIVE subcarpetas AS (
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
                        c.""TRD""
                    FROM documentos.""Carpetas"" c
                    WHERE c.""CarpetaPadre"" = @CarpetaId AND c.""Estado"" = true
                    
                    UNION ALL
                    
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
                        c.""TRD""
                    FROM documentos.""Carpetas"" c
                    INNER JOIN subcarpetas s ON c.""CarpetaPadre"" = s.""Cod""
                    WHERE c.""Estado"" = true
                )
                SELECT 
                    s.*,
                    t.""Codigo"" AS ""CodigoTRD"",
                    tc.""Nombre"" AS ""NombreTipoCarpeta""
                FROM subcarpetas s
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" t ON s.""TRD"" = t.""Cod""
                LEFT JOIN documentos.""Tipos_Carpetas"" tc ON s.""TipoCarpeta"" = tc.""Cod""
                ORDER BY s.""TipoCarpeta"", s.""Nombre""";

            const string sqlArchivos = @"
                WITH RECURSIVE todas_carpetas AS (
                    SELECT ""Cod""
                    FROM documentos.""Carpetas""
                    WHERE ""Cod"" = @CarpetaId AND ""Estado"" = true
                    
                    UNION ALL
                    
                    SELECT c.""Cod""
                    FROM documentos.""Carpetas"" c
                    INNER JOIN todas_carpetas tc ON c.""CarpetaPadre"" = tc.""Cod""
                    WHERE c.""Estado"" = true
                )
                SELECT 
                    a.""Cod"",
                    a.""Nombre"",
                    a.""Carpeta"",
                    a.""TipoArchivo"",
                    a.""Formato"",
                    a.""NumeroHojas"",
                    a.""Duracion"",
                    a.""Tamano"",
                    a.""Estado"",
                    a.""Indice"",
                    ta.""Nombre"" AS ""NombreTipoArchivo""
                FROM documentos.""Archivos"" a
                INNER JOIN todas_carpetas tc ON a.""Carpeta"" = tc.""Cod""
                LEFT JOIN documentos.""Tipos_Archivos"" ta ON a.""TipoArchivo"" = ta.""Cod""
                WHERE a.""Estado"" = true
                ORDER BY a.""Carpeta"", a.""Indice"", a.""Nombre""";

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