using Dapper;
using NubluSoft_Core.Models.DTOs;
using Npgsql;

namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Implementación del servicio de notificaciones
    /// </summary>
    public class NotificacionService : INotificacionService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<NotificacionService> _logger;

        public NotificacionService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<NotificacionService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<NotificacionesListResponse> ObtenerNotificacionesAsync(
            long entidadId,
            long usuarioId,
            FiltrosNotificacionesRequest? filtros = null)
        {
            filtros ??= new FiltrosNotificacionesRequest();

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Construir WHERE dinámico
                var whereConditions = new List<string>
                {
                    @"n.""Entidad"" = @EntidadId",
                    @"n.""UsuarioDestino"" = @UsuarioId",
                    @"n.""Estado"" != 'ELIMINADA'"
                };

                if (filtros.SoloNoLeidas == true)
                    whereConditions.Add(@"n.""FechaLeido"" IS NULL");

                if (!string.IsNullOrEmpty(filtros.TipoNotificacion))
                    whereConditions.Add(@"n.""TipoNotificacion"" = @TipoNotificacion");

                if (!string.IsNullOrEmpty(filtros.Prioridad))
                    whereConditions.Add(@"n.""Prioridad"" = @Prioridad");

                if (filtros.FechaDesde.HasValue)
                    whereConditions.Add(@"n.""FechaCreacion"" >= @FechaDesde");

                if (filtros.FechaHasta.HasValue)
                    whereConditions.Add(@"n.""FechaCreacion"" <= @FechaHasta");

                var whereClause = string.Join(" AND ", whereConditions);

                // Query para total de registros
                var countSql = $@"
                    SELECT COUNT(*) 
                    FROM documentos.""Notificaciones"" n
                    WHERE {whereClause}";

                var totalRegistros = await connection.ExecuteScalarAsync<int>(countSql, new
                {
                    EntidadId = entidadId,
                    UsuarioId = usuarioId,
                    filtros.TipoNotificacion,
                    filtros.Prioridad,
                    filtros.FechaDesde,
                    filtros.FechaHasta
                });

                // Query para no leídas
                var noLeidasSql = @"
                    SELECT COUNT(*) 
                    FROM documentos.""Notificaciones""
                    WHERE ""Entidad"" = @EntidadId 
                      AND ""UsuarioDestino"" = @UsuarioId
                      AND ""FechaLeido"" IS NULL
                      AND ""Estado"" != 'ELIMINADA'";

                var noLeidasCount = await connection.ExecuteScalarAsync<int>(noLeidasSql, new
                {
                    EntidadId = entidadId,
                    UsuarioId = usuarioId
                });

                // Query principal con paginación
                var offset = (filtros.Pagina - 1) * filtros.TamañoPagina;

                var sql = $@"
                    SELECT 
                        n.""Cod"",
                        n.""TipoNotificacion"",
                        t.""Nombre"" AS ""TipoNotificacionNombre"",
                        t.""Icono"",
                        t.""Color"",
                        n.""Titulo"",
                        n.""Mensaje"",
                        n.""Prioridad"",
                        n.""FechaCreacion"",
                        n.""FechaLeido"",
                        CASE WHEN n.""FechaLeido"" IS NULL THEN true ELSE false END AS ""NoLeido"",
                        n.""Estado"",
                        uo.""NombreCompleto"" AS ""UsuarioOrigenNombre"",
                        n.""RadicadoRef"",
                        r.""NumeroRadicado"",
                        n.""ArchivoRef"",
                        a.""Nombre"" AS ""ArchivoNombre"",
                        n.""SolicitudFirmaRef"",
                        n.""UrlAccion"",
                        t.""RequiereAccion""
                    FROM documentos.""Notificaciones"" n
                    INNER JOIN documentos.""Tipos_Notificacion"" t ON n.""TipoNotificacion"" = t.""Cod""
                    LEFT JOIN documentos.""Usuarios"" uo ON n.""UsuarioOrigen"" = uo.""Cod"" AND n.""Entidad"" = uo.""Entidad""
                    LEFT JOIN documentos.""Radicados"" r ON n.""RadicadoRef"" = r.""Cod"" AND n.""Entidad"" = r.""Entidad""
                    LEFT JOIN documentos.""Archivos"" a ON n.""ArchivoRef"" = a.""Cod"" AND n.""Entidad"" = a.""Entidad""
                    WHERE {whereClause}
                    ORDER BY n.""FechaCreacion"" DESC
                    LIMIT @Limite OFFSET @Offset";

                var notificaciones = await connection.QueryAsync<NotificacionResponse>(sql, new
                {
                    EntidadId = entidadId,
                    UsuarioId = usuarioId,
                    filtros.TipoNotificacion,
                    filtros.Prioridad,
                    filtros.FechaDesde,
                    filtros.FechaHasta,
                    Limite = filtros.TamañoPagina,
                    Offset = offset
                });

                return new NotificacionesListResponse
                {
                    Notificaciones = notificaciones.ToList(),
                    TotalRegistros = totalRegistros,
                    PaginaActual = filtros.Pagina,
                    TotalPaginas = (int)Math.Ceiling((double)totalRegistros / filtros.TamañoPagina),
                    NoLeidasCount = noLeidasCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo notificaciones para usuario {UsuarioId}", usuarioId);
                throw;
            }
        }

        public async Task<NotificacionResponse?> ObtenerPorIdAsync(
            long entidadId,
            long usuarioId,
            long notificacionId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                    SELECT 
                        n.""Cod"",
                        n.""TipoNotificacion"",
                        t.""Nombre"" AS ""TipoNotificacionNombre"",
                        t.""Icono"",
                        t.""Color"",
                        n.""Titulo"",
                        n.""Mensaje"",
                        n.""Prioridad"",
                        n.""FechaCreacion"",
                        n.""FechaLeido"",
                        CASE WHEN n.""FechaLeido"" IS NULL THEN true ELSE false END AS ""NoLeido"",
                        n.""Estado"",
                        uo.""NombreCompleto"" AS ""UsuarioOrigenNombre"",
                        n.""RadicadoRef"",
                        r.""NumeroRadicado"",
                        n.""ArchivoRef"",
                        a.""Nombre"" AS ""ArchivoNombre"",
                        n.""SolicitudFirmaRef"",
                        n.""UrlAccion"",
                        t.""RequiereAccion""
                    FROM documentos.""Notificaciones"" n
                    INNER JOIN documentos.""Tipos_Notificacion"" t ON n.""TipoNotificacion"" = t.""Cod""
                    LEFT JOIN documentos.""Usuarios"" uo ON n.""UsuarioOrigen"" = uo.""Cod"" AND n.""Entidad"" = uo.""Entidad""
                    LEFT JOIN documentos.""Radicados"" r ON n.""RadicadoRef"" = r.""Cod"" AND n.""Entidad"" = r.""Entidad""
                    LEFT JOIN documentos.""Archivos"" a ON n.""ArchivoRef"" = a.""Cod"" AND n.""Entidad"" = a.""Entidad""
                    WHERE n.""Cod"" = @NotificacionId
                      AND n.""Entidad"" = @EntidadId
                      AND n.""UsuarioDestino"" = @UsuarioId";

                return await connection.QueryFirstOrDefaultAsync<NotificacionResponse>(sql, new
                {
                    NotificacionId = notificacionId,
                    EntidadId = entidadId,
                    UsuarioId = usuarioId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo notificación {NotificacionId}", notificacionId);
                throw;
            }
        }

        public async Task<ContadorNotificacionesResponse> ObtenerContadorAsync(
            long entidadId,
            long usuarioId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                    SELECT 
                        COUNT(*) FILTER (WHERE ""FechaLeido"" IS NULL) AS ""NoLeidas"",
                        COUNT(*) FILTER (WHERE ""FechaLeido"" IS NULL AND ""Prioridad"" = 'ALTA') AS ""Alta"",
                        COUNT(*) FILTER (WHERE ""FechaLeido"" IS NULL AND ""Prioridad"" = 'MEDIA') AS ""Media"",
                        COUNT(*) FILTER (WHERE ""FechaLeido"" IS NULL AND ""Prioridad"" = 'BAJA') AS ""Baja""
                    FROM documentos.""Notificaciones""
                    WHERE ""Entidad"" = @EntidadId 
                      AND ""UsuarioDestino"" = @UsuarioId
                      AND ""Estado"" != 'ELIMINADA'";

                var resultado = await connection.QueryFirstOrDefaultAsync<ContadorNotificacionesResponse>(sql, new
                {
                    EntidadId = entidadId,
                    UsuarioId = usuarioId
                });

                return resultado ?? new ContadorNotificacionesResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo contador para usuario {UsuarioId}", usuarioId);
                throw;
            }
        }

        public async Task<IEnumerable<TipoNotificacionResponse>> ObtenerTiposNotificacionAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                    SELECT 
                        ""Cod"",
                        ""Nombre"",
                        ""Descripcion"",
                        ""Icono"",
                        ""Color"",
                        ""RequiereAccion""
                    FROM documentos.""Tipos_Notificacion""
                    WHERE ""Estado"" = true
                    ORDER BY ""Nombre""";

                return await connection.QueryAsync<TipoNotificacionResponse>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tipos de notificación");
                throw;
            }
        }

        // ==================== ACCIONES ====================

        public async Task<bool> MarcarComoLeidaAsync(
            long entidadId,
            long usuarioId,
            long notificacionId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Usar la función PostgreSQL
                const string sql = @"
                    SELECT documentos.""F_MarcarNotificacionLeida""(
                        @NotificacionId, @UsuarioId, @EntidadId
                    )";

                var resultado = await connection.ExecuteScalarAsync<bool>(sql, new
                {
                    NotificacionId = notificacionId,
                    UsuarioId = usuarioId,
                    EntidadId = entidadId
                });

                if (resultado)
                {
                    _logger.LogInformation(
                        "Notificación {NotificacionId} marcada como leída por usuario {UsuarioId}",
                        notificacionId, usuarioId);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando notificación {NotificacionId} como leída", notificacionId);
                throw;
            }
        }

        public async Task<int> MarcarTodasComoLeidasAsync(
            long entidadId,
            long usuarioId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Usar la función PostgreSQL
                const string sql = @"
                    SELECT documentos.""F_MarcarTodasNotificacionesLeidas""(
                        @UsuarioId, @EntidadId
                    )";

                var cantidad = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    UsuarioId = usuarioId,
                    EntidadId = entidadId
                });

                _logger.LogInformation(
                    "Usuario {UsuarioId} marcó {Cantidad} notificaciones como leídas",
                    usuarioId, cantidad);

                return cantidad;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando todas las notificaciones como leídas para usuario {UsuarioId}", usuarioId);
                throw;
            }
        }

        public async Task<int> MarcarVariasComoLeidasAsync(
            long entidadId,
            long usuarioId,
            List<long> notificacionIds)
        {
            if (notificacionIds == null || notificacionIds.Count == 0)
                return 0;

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE documentos.""Notificaciones""
                    SET ""FechaLeido"" = NOW(),
                        ""Estado"" = 'LEIDA'
                    WHERE ""Cod"" = ANY(@Ids)
                      AND ""Entidad"" = @EntidadId
                      AND ""UsuarioDestino"" = @UsuarioId
                      AND ""FechaLeido"" IS NULL";

                var cantidad = await connection.ExecuteAsync(sql, new
                {
                    Ids = notificacionIds.ToArray(),
                    EntidadId = entidadId,
                    UsuarioId = usuarioId
                });

                _logger.LogInformation(
                    "Usuario {UsuarioId} marcó {Cantidad} notificaciones específicas como leídas",
                    usuarioId, cantidad);

                return cantidad;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando notificaciones como leídas para usuario {UsuarioId}", usuarioId);
                throw;
            }
        }

        public async Task<bool> EliminarAsync(
            long entidadId,
            long usuarioId,
            long notificacionId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE documentos.""Notificaciones""
                    SET ""Estado"" = 'ELIMINADA'
                    WHERE ""Cod"" = @NotificacionId
                      AND ""Entidad"" = @EntidadId
                      AND ""UsuarioDestino"" = @UsuarioId";

                var affected = await connection.ExecuteAsync(sql, new
                {
                    NotificacionId = notificacionId,
                    EntidadId = entidadId,
                    UsuarioId = usuarioId
                });

                if (affected > 0)
                {
                    _logger.LogInformation(
                        "Notificación {NotificacionId} eliminada por usuario {UsuarioId}",
                        notificacionId, usuarioId);
                }

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando notificación {NotificacionId}", notificacionId);
                throw;
            }
        }

        // ==================== CREACIÓN ====================

        public async Task<NotificacionResponse?> CrearNotificacionAsync(
            long entidadId,
            CrearNotificacionRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Usar la función PostgreSQL
                const string sql = @"
                    SELECT * FROM documentos.""F_CrearNotificacion""(
                        @Entidad,
                        @TipoNotificacion,
                        @UsuarioDestino,
                        @Titulo,
                        @Mensaje,
                        @Prioridad,
                        @UsuarioOrigen,
                        @RadicadoRef,
                        @ArchivoRef,
                        @SolicitudFirmaRef,
                        @UrlAccion,
                        @DatosAdicionales::jsonb
                    )";

                var resultado = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    Entidad = entidadId,
                    request.TipoNotificacion,
                    request.UsuarioDestino,
                    request.Titulo,
                    request.Mensaje,
                    request.Prioridad,
                    request.UsuarioOrigen,
                    request.RadicadoRef,
                    request.ArchivoRef,
                    request.SolicitudFirmaRef,
                    request.UrlAccion,
                    DatosAdicionales = request.DatosAdicionales != null
                        ? System.Text.Json.JsonSerializer.Serialize(request.DatosAdicionales)
                        : null
                });

                if (resultado?.cod != null)
                {
                    // Cast explícito para evitar error con dynamic y métodos de extensión
                    long notificacionId = (long)resultado.cod;

                    _logger.LogInformation(
                        "Notificación {NotificacionId} creada para usuario {UsuarioId}",
                        notificacionId, request.UsuarioDestino);

                    // Obtener la notificación completa para retornar
                    return await ObtenerPorIdAsync(entidadId, request.UsuarioDestino, notificacionId);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando notificación para usuario {UsuarioId}", request.UsuarioDestino);
                throw;
            }
        }

        // ==================== WEBSOCKET ====================

        public async Task<NotificacionWebSocketMessage?> ObtenerParaWebSocketAsync(
            long entidadId,
            long usuarioId,
            long notificacionId)
        {
            try
            {
                var notificacion = await ObtenerPorIdAsync(entidadId, usuarioId, notificacionId);

                if (notificacion == null)
                    return null;

                var contador = await ObtenerContadorAsync(entidadId, usuarioId);

                return new NotificacionWebSocketMessage
                {
                    Tipo = "NUEVA_NOTIFICACION",
                    Notificacion = notificacion,
                    TotalNoLeidas = contador.NoLeidas,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo notificación para WebSocket {NotificacionId}", notificacionId);
                throw;
            }
        }
    }
}