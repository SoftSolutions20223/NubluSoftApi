using Dapper;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class DatosEstaticosService : IDatosEstaticosService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<DatosEstaticosService> _logger;

        public DatosEstaticosService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<DatosEstaticosService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CARPETAS/ARCHIVOS ====================

        public async Task<IEnumerable<Catalogo>> ObtenerTiposCarpetasAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", ""Estado"" 
                FROM documentos.""Tipos_Carpetas"" 
                WHERE ""Estado"" = true 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<Catalogo>(sql, "Tipos_Carpetas");
        }

        public async Task<IEnumerable<Catalogo>> ObtenerEstadosCarpetasAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Estados_Carpetas"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<Catalogo>(sql, "Estados_Carpetas");
        }

        public async Task<IEnumerable<Catalogo>> ObtenerTiposArchivosAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", ""Estado"" 
                FROM documentos.""Tipos_Archivos"" 
                WHERE ""Estado"" = true 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<Catalogo>(sql, "Tipos_Archivos");
        }

        public async Task<IEnumerable<Catalogo>> ObtenerTiposDocumentalesAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", ""Estado"" 
                FROM documentos.""Tipos_Documentales"" 
                WHERE ""Estado"" = true 
                ORDER BY ""Nombre""";

            return await EjecutarConsultaAsync<Catalogo>(sql, "Tipos_Documentales");
        }

        public async Task<IEnumerable<Catalogo>> ObtenerNivelesVisualizacionAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Niveles_Visualizacion_Carpetas"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<Catalogo>(sql, "Niveles_Visualizacion_Carpetas");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerSoportesDocumentoAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Soportes_Documento"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Soportes_Documento");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerFrecuenciasConsultaAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Frecuencias_Consulta"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Frecuencias_Consulta");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerOrigenesDocumentosAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Origenes_Documentos"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Origenes_Documentos");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerDisposicionesFinalesAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Disposiciones_Finales"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Disposiciones_Finales");
        }

        // ==================== VENTANILLA ÚNICA ====================

        public async Task<IEnumerable<TipoSolicitud>> ObtenerTiposSolicitudAsync()
        {
            const string sql = @"
                SELECT 
                    ""Cod"", ""Nombre"", ""Descripcion"", 
                    ""DiasHabilesRespuesta"", ""DiasHabilesProrroga"",
                    ""EsCalendario"", ""RequiereRespuestaEscrita"", 
                    ""Normativa"", ""Estado""
                FROM documentos.""Tipos_Solicitud"" 
                WHERE ""Estado"" = true 
                ORDER BY ""Nombre""";

            return await EjecutarConsultaAsync<TipoSolicitud>(sql, "Tipos_Solicitud");
        }

        public async Task<IEnumerable<EstadoRadicado>> ObtenerEstadosRadicadoAsync()
        {
            const string sql = @"
                SELECT 
                    ""Cod"", ""Nombre"", ""Descripcion"", 
                    ""EsFinal"", ""Color"", ""Orden""
                FROM documentos.""Estados_Radicado"" 
                ORDER BY ""Orden"", ""Cod""";

            return await EjecutarConsultaAsync<EstadoRadicado>(sql, "Estados_Radicado");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerTiposComunicacionAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Tipos_Comunicacion"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Tipos_Comunicacion");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerMediosRecepcionAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", ""Estado"" 
                FROM documentos.""Medios_Recepcion"" 
                WHERE ""Estado"" = true 
                ORDER BY ""Nombre""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Medios_Recepcion");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerPrioridadesRadicadoAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Prioridades_Radicado"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Prioridades_Radicado");
        }

        // ==================== GENERALES ====================

        public async Task<IEnumerable<Catalogo>> ObtenerRolesAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Roles"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<Catalogo>(sql, "Roles");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerTiposDocumentoIdentidadAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Tipos_Documento_Identidad"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Tipos_Documento_Identidad");
        }

        public async Task<IEnumerable<CatalogoString>> ObtenerTiposFirmaAsync()
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", true AS ""Estado"" 
                FROM documentos.""Tipos_Firma"" 
                ORDER BY ""Cod""";

            return await EjecutarConsultaAsync<CatalogoString>(sql, "Tipos_Firma");
        }

        // ==================== HELPER ====================

        private async Task<IEnumerable<T>> EjecutarConsultaAsync<T>(string sql, string nombreTabla)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<T>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando {Tabla}", nombreTabla);
                return Enumerable.Empty<T>();
            }
        }
    }
}