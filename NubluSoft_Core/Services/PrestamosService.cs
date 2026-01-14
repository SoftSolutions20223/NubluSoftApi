using Dapper;
using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    public class PrestamosService : IPrestamosService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<PrestamosService> _logger;

        public PrestamosService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<PrestamosService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<ListaPrestamosResponse> ObtenerPrestamosAsync(
            long entidadId,
            FiltrosPrestamosRequest? filtros = null)
        {
            var response = new ListaPrestamosResponse
            {
                PaginaActual = filtros?.Pagina ?? 1,
                PorPagina = filtros?.PorPagina ?? 20
            };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var whereClause = @"WHERE c.""Entidad"" = @EntidadId";
                var parameters = new DynamicParameters();
                parameters.Add("EntidadId", entidadId);

                if (filtros != null)
                {
                    if (filtros.CarpetaId.HasValue)
                    {
                        whereClause += @" AND p.""Carpeta"" = @CarpetaId";
                        parameters.Add("CarpetaId", filtros.CarpetaId.Value);
                    }

                    if (filtros.SolicitadoPor.HasValue)
                    {
                        whereClause += @" AND p.""SolicitadoPor"" = @SolicitadoPor";
                        parameters.Add("SolicitadoPor", filtros.SolicitadoPor.Value);
                    }

                    if (!string.IsNullOrEmpty(filtros.Estado))
                    {
                        whereClause += @" AND p.""Estado"" = @Estado";
                        parameters.Add("Estado", filtros.Estado);
                    }

                    if (filtros.FechaDesde.HasValue)
                    {
                        whereClause += @" AND p.""FechaSolicitud"" >= @FechaDesde";
                        parameters.Add("FechaDesde", filtros.FechaDesde.Value);
                    }

                    if (filtros.FechaHasta.HasValue)
                    {
                        whereClause += @" AND p.""FechaSolicitud"" <= @FechaHasta";
                        parameters.Add("FechaHasta", filtros.FechaHasta.Value);
                    }

                    if (filtros.SoloVencidos == true)
                    {
                        whereClause += @" AND p.""Estado"" = 'PRESTADO' AND p.""FechaDevolucionEsperada"" < CURRENT_TIMESTAMP";
                    }

                    if (filtros.SoloPendientes == true)
                    {
                        whereClause += @" AND p.""Estado"" IN ('SOLICITADO', 'AUTORIZADO', 'PRESTADO')";
                    }
                }

                // Contar total
                var countSql = $@"
                    SELECT COUNT(*)
                    FROM documentos.""Prestamos_Expedientes"" p
                    INNER JOIN documentos.""Carpetas"" c ON p.""Carpeta"" = c.""Cod""
                    {whereClause}";

                response.TotalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
                response.TotalPaginas = (int)Math.Ceiling((double)response.TotalItems / response.PorPagina);

                // Obtener página
                var offset = (response.PaginaActual - 1) * response.PorPagina;
                parameters.Add("Offset", offset);
                parameters.Add("Limit", response.PorPagina);

                var sql = $@"
                    SELECT 
                        p.""Cod"",
                        p.""Carpeta"",
                        c.""Nombre"" AS ""NombreCarpeta"",
                        c.""CodigoExpediente"",
                        p.""SolicitadoPor"",
                        us.""Nombres"" || ' ' || COALESCE(us.""Apellidos"", '') AS ""NombreSolicitante"",
                        p.""AutorizadoPor"",
                        ua.""Nombres"" || ' ' || COALESCE(ua.""Apellidos"", '') AS ""NombreAutorizador"",
                        p.""FechaSolicitud"",
                        p.""FechaAutorizacion"",
                        p.""FechaPrestamo"",
                        p.""FechaDevolucionEsperada"",
                        p.""FechaDevolucionReal"",
                        p.""Estado"",
                        p.""Motivo"",
                        p.""Observaciones"",
                        p.""ObservacionesDevolucion"",
                        CASE 
                            WHEN p.""FechaPrestamo"" IS NOT NULL AND p.""FechaDevolucionReal"" IS NULL
                            THEN EXTRACT(DAY FROM (CURRENT_TIMESTAMP - p.""FechaPrestamo""))::INT
                            WHEN p.""FechaPrestamo"" IS NOT NULL AND p.""FechaDevolucionReal"" IS NOT NULL
                            THEN EXTRACT(DAY FROM (p.""FechaDevolucionReal"" - p.""FechaPrestamo""))::INT
                            ELSE NULL
                        END AS ""DiasEnPrestamo"",
                        CASE 
                            WHEN p.""Estado"" = 'PRESTADO' AND p.""FechaDevolucionEsperada"" < CURRENT_TIMESTAMP
                            THEN EXTRACT(DAY FROM (CURRENT_TIMESTAMP - p.""FechaDevolucionEsperada""))::INT
                            ELSE NULL
                        END AS ""DiasVencido"",
                        CASE 
                            WHEN p.""Estado"" = 'PRESTADO' AND p.""FechaDevolucionEsperada"" < CURRENT_TIMESTAMP
                            THEN true ELSE false
                        END AS ""EstaVencido""
                    FROM documentos.""Prestamos_Expedientes"" p
                    INNER JOIN documentos.""Carpetas"" c ON p.""Carpeta"" = c.""Cod""
                    LEFT JOIN usuarios.""Usuarios"" us ON p.""SolicitadoPor"" = us.""Cod""
                    LEFT JOIN usuarios.""Usuarios"" ua ON p.""AutorizadoPor"" = ua.""Cod""
                    {whereClause}
                    ORDER BY p.""FechaSolicitud"" DESC
                    OFFSET @Offset LIMIT @Limit";

                response.Prestamos = await connection.QueryAsync<PrestamoDto>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo préstamos");
            }

            return response;
        }

        public async Task<PrestamoDto?> ObtenerPorIdAsync(long prestamoCod)
        {
            const string sql = @"
                SELECT 
                    p.""Cod"",
                    p.""Carpeta"",
                    c.""Nombre"" AS ""NombreCarpeta"",
                    c.""CodigoExpediente"",
                    p.""SolicitadoPor"",
                    us.""Nombres"" || ' ' || COALESCE(us.""Apellidos"", '') AS ""NombreSolicitante"",
                    p.""AutorizadoPor"",
                    ua.""Nombres"" || ' ' || COALESCE(ua.""Apellidos"", '') AS ""NombreAutorizador"",
                    p.""FechaSolicitud"",
                    p.""FechaAutorizacion"",
                    p.""FechaPrestamo"",
                    p.""FechaDevolucionEsperada"",
                    p.""FechaDevolucionReal"",
                    p.""Estado"",
                    p.""Motivo"",
                    p.""Observaciones"",
                    p.""ObservacionesDevolucion"",
                    CASE 
                        WHEN p.""FechaPrestamo"" IS NOT NULL AND p.""FechaDevolucionReal"" IS NULL
                        THEN EXTRACT(DAY FROM (CURRENT_TIMESTAMP - p.""FechaPrestamo""))::INT
                        ELSE NULL
                    END AS ""DiasEnPrestamo"",
                    CASE 
                        WHEN p.""Estado"" = 'PRESTADO' AND p.""FechaDevolucionEsperada"" < CURRENT_TIMESTAMP
                        THEN EXTRACT(DAY FROM (CURRENT_TIMESTAMP - p.""FechaDevolucionEsperada""))::INT
                        ELSE NULL
                    END AS ""DiasVencido"",
                    CASE 
                        WHEN p.""Estado"" = 'PRESTADO' AND p.""FechaDevolucionEsperada"" < CURRENT_TIMESTAMP
                        THEN true ELSE false
                    END AS ""EstaVencido""
                FROM documentos.""Prestamos_Expedientes"" p
                INNER JOIN documentos.""Carpetas"" c ON p.""Carpeta"" = c.""Cod""
                LEFT JOIN usuarios.""Usuarios"" us ON p.""SolicitadoPor"" = us.""Cod""
                LEFT JOIN usuarios.""Usuarios"" ua ON p.""AutorizadoPor"" = ua.""Cod""
                WHERE p.""Cod"" = @PrestamoCod";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<PrestamoDto>(sql, new { PrestamoCod = prestamoCod });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo préstamo {PrestamoCod}", prestamoCod);
                return null;
            }
        }

        public async Task<IEnumerable<PrestamoDto>> ObtenerPorExpedienteAsync(long carpetaId)
        {
            var filtros = new FiltrosPrestamosRequest { CarpetaId = carpetaId, PorPagina = 100 };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener entidad de la carpeta
                var entidadId = await connection.ExecuteScalarAsync<long?>(
                    @"SELECT ""Entidad"" FROM documentos.""Carpetas"" WHERE ""Cod"" = @CarpetaId",
                    new { CarpetaId = carpetaId });

                if (!entidadId.HasValue)
                    return Enumerable.Empty<PrestamoDto>();

                var response = await ObtenerPrestamosAsync(entidadId.Value, filtros);
                return response.Prestamos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo préstamos de expediente {CarpetaId}", carpetaId);
                return Enumerable.Empty<PrestamoDto>();
            }
        }

        public async Task<IEnumerable<PrestamoDto>> ObtenerMisSolicitudesAsync(
            long entidadId,
            long usuarioId)
        {
            var filtros = new FiltrosPrestamosRequest { SolicitadoPor = usuarioId, PorPagina = 50 };
            var response = await ObtenerPrestamosAsync(entidadId, filtros);
            return response.Prestamos;
        }

        public async Task<IEnumerable<PrestamoDto>> ObtenerPendientesAutorizacionAsync(long entidadId)
        {
            var filtros = new FiltrosPrestamosRequest { Estado = EstadosPrestamo.Solicitado, PorPagina = 100 };
            var response = await ObtenerPrestamosAsync(entidadId, filtros);
            return response.Prestamos;
        }

        public async Task<IEnumerable<PrestamoDto>> ObtenerVencidosAsync(long entidadId)
        {
            var filtros = new FiltrosPrestamosRequest { SoloVencidos = true, PorPagina = 100 };
            var response = await ObtenerPrestamosAsync(entidadId, filtros);
            return response.Prestamos;
        }

        public async Task<ResumenPrestamosDto> ObtenerResumenAsync(long entidadId)
        {
            var resumen = new ResumenPrestamosDto();

            const string sql = @"
                SELECT 
                    COUNT(*) FILTER (WHERE p.""Estado"" = 'SOLICITADO') AS ""TotalSolicitados"",
                    COUNT(*) FILTER (WHERE p.""Estado"" = 'AUTORIZADO') AS ""TotalAutorizados"",
                    COUNT(*) FILTER (WHERE p.""Estado"" = 'PRESTADO') AS ""TotalPrestados"",
                    COUNT(*) FILTER (WHERE p.""Estado"" = 'DEVUELTO') AS ""TotalDevueltos"",
                    COUNT(*) FILTER (WHERE p.""Estado"" = 'RECHAZADO') AS ""TotalRechazados"",
                    COUNT(*) FILTER (WHERE p.""Estado"" = 'PRESTADO' AND p.""FechaDevolucionEsperada"" < CURRENT_TIMESTAMP) AS ""TotalVencidos"",
                    COUNT(*) FILTER (WHERE p.""Estado"" = 'SOLICITADO') AS ""PendientesAutorizacion"",
                    COUNT(*) FILTER (WHERE p.""Estado"" = 'PRESTADO') AS ""PendientesDevolucion""
                FROM documentos.""Prestamos_Expedientes"" p
                INNER JOIN documentos.""Carpetas"" c ON p.""Carpeta"" = c.""Cod""
                WHERE c.""Entidad"" = @EntidadId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { EntidadId = entidadId });

                if (stats != null)
                {
                    resumen.TotalSolicitados = (int)(stats.TotalSolicitados ?? 0);
                    resumen.TotalAutorizados = (int)(stats.TotalAutorizados ?? 0);
                    resumen.TotalPrestados = (int)(stats.TotalPrestados ?? 0);
                    resumen.TotalDevueltos = (int)(stats.TotalDevueltos ?? 0);
                    resumen.TotalRechazados = (int)(stats.TotalRechazados ?? 0);
                    resumen.TotalVencidos = (int)(stats.TotalVencidos ?? 0);
                    resumen.PendientesAutorizacion = (int)(stats.PendientesAutorizacion ?? 0);
                    resumen.PendientesDevolucion = (int)(stats.PendientesDevolucion ?? 0);
                }

                // Obtener préstamos vencidos
                resumen.PrestamosVencidos = await ObtenerVencidosAsync(entidadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen de préstamos");
            }

            return resumen;
        }

        public async Task<(bool EstaPrestado, PrestamoDto? PrestamoActivo)> VerificarDisponibilidadAsync(long carpetaId)
        {
            const string sql = @"
                SELECT p.""Cod""
                FROM documentos.""Prestamos_Expedientes"" p
                WHERE p.""Carpeta"" = @CarpetaId 
                  AND p.""Estado"" IN ('SOLICITADO', 'AUTORIZADO', 'PRESTADO')
                LIMIT 1";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var prestamoCod = await connection.ExecuteScalarAsync<long?>(sql, new { CarpetaId = carpetaId });

                if (!prestamoCod.HasValue)
                    return (false, null);

                var prestamo = await ObtenerPorIdAsync(prestamoCod.Value);
                return (true, prestamo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando disponibilidad de expediente {CarpetaId}", carpetaId);
                return (false, null);
            }
        }

        // ==================== OPERACIONES ====================

        public async Task<ResultadoPrestamoDto> SolicitarPrestamoAsync(
            long entidadId,
            long usuarioId,
            SolicitarPrestamoRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que el expediente existe y pertenece a la entidad
                var carpetaValida = await connection.ExecuteScalarAsync<bool>(@"
                    SELECT EXISTS(
                        SELECT 1 FROM documentos.""Carpetas"" 
                        WHERE ""Cod"" = @CarpetaId AND ""Entidad"" = @EntidadId AND ""Estado"" = true
                        AND ""TipoCarpeta"" = 'Expediente'
                    )",
                    new { request.CarpetaId, EntidadId = entidadId });

                if (!carpetaValida)
                {
                    return new ResultadoPrestamoDto
                    {
                        Exito = false,
                        Mensaje = "Expediente no encontrado o no válido para préstamo"
                    };
                }

                // Verificar disponibilidad
                var (estaPrestado, prestamoActivo) = await VerificarDisponibilidadAsync(request.CarpetaId);
                if (estaPrestado)
                {
                    return new ResultadoPrestamoDto
                    {
                        Exito = false,
                        Mensaje = $"El expediente ya tiene un préstamo activo (Estado: {prestamoActivo?.Estado})",
                        Prestamo = prestamoActivo
                    };
                }

                // Obtener siguiente código
                var nuevoCod = await connection.ExecuteScalarAsync<long>(
                    @"SELECT COALESCE(MAX(""Cod""), 0) + 1 FROM documentos.""Prestamos_Expedientes""");

                // Crear solicitud
                const string insertSql = @"
                    INSERT INTO documentos.""Prestamos_Expedientes"" (
                        ""Cod"", ""Carpeta"", ""SolicitadoPor"", ""FechaSolicitud"",
                        ""FechaDevolucionEsperada"", ""Estado"", ""Motivo"", ""Observaciones""
                    ) VALUES (
                        @Cod, @Carpeta, @SolicitadoPor, CURRENT_TIMESTAMP,
                        @FechaDevolucionEsperada, 'SOLICITADO', @Motivo, @Observaciones
                    )";

                await connection.ExecuteAsync(insertSql, new
                {
                    Cod = nuevoCod,
                    Carpeta = request.CarpetaId,
                    SolicitadoPor = usuarioId,
                    FechaDevolucionEsperada = request.FechaDevolucionEsperada ?? DateTime.Now.AddDays(15),
                    request.Motivo,
                    request.Observaciones
                });

                var prestamo = await ObtenerPorIdAsync(nuevoCod);

                return new ResultadoPrestamoDto
                {
                    Exito = true,
                    Mensaje = "Solicitud de préstamo creada exitosamente",
                    PrestamoCod = nuevoCod,
                    Prestamo = prestamo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error solicitando préstamo");
                return new ResultadoPrestamoDto
                {
                    Exito = false,
                    Mensaje = "Error al solicitar préstamo: " + ex.Message
                };
            }
        }

        public async Task<ResultadoPrestamoDto> AutorizarPrestamoAsync(
            long prestamoCod,
            long usuarioId,
            AutorizarPrestamoRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar estado actual
                var estadoActual = await connection.ExecuteScalarAsync<string?>(
                    @"SELECT ""Estado"" FROM documentos.""Prestamos_Expedientes"" WHERE ""Cod"" = @PrestamoCod",
                    new { PrestamoCod = prestamoCod });

                if (estadoActual == null)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = "Préstamo no encontrado" };
                }

                if (estadoActual != EstadosPrestamo.Solicitado)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = $"El préstamo no está en estado SOLICITADO (Estado actual: {estadoActual})" };
                }

                if (request.Autorizar)
                {
                    // Autorizar
                    const string sql = @"
                        UPDATE documentos.""Prestamos_Expedientes""
                        SET ""Estado"" = 'AUTORIZADO',
                            ""AutorizadoPor"" = @UsuarioId,
                            ""FechaAutorizacion"" = CURRENT_TIMESTAMP,
                            ""FechaDevolucionEsperada"" = CURRENT_TIMESTAMP + INTERVAL '1 day' * @DiasPrestamo,
                            ""Observaciones"" = COALESCE(""Observaciones"" || E'\n', '') || @Observaciones
                        WHERE ""Cod"" = @PrestamoCod";

                    await connection.ExecuteAsync(sql, new
                    {
                        PrestamoCod = prestamoCod,
                        UsuarioId = usuarioId,
                        request.DiasPrestamo,
                        Observaciones = string.IsNullOrEmpty(request.Observaciones)
                            ? $"Autorizado por {usuarioId}"
                            : $"Autorizado: {request.Observaciones}"
                    });

                    var prestamo = await ObtenerPorIdAsync(prestamoCod);
                    return new ResultadoPrestamoDto
                    {
                        Exito = true,
                        Mensaje = $"Préstamo autorizado por {request.DiasPrestamo} días",
                        PrestamoCod = prestamoCod,
                        Prestamo = prestamo
                    };
                }
                else
                {
                    // Rechazar
                    if (string.IsNullOrEmpty(request.MotivoRechazo))
                    {
                        return new ResultadoPrestamoDto { Exito = false, Mensaje = "El motivo de rechazo es requerido" };
                    }

                    const string sql = @"
                        UPDATE documentos.""Prestamos_Expedientes""
                        SET ""Estado"" = 'RECHAZADO',
                            ""AutorizadoPor"" = @UsuarioId,
                            ""FechaAutorizacion"" = CURRENT_TIMESTAMP,
                            ""Observaciones"" = COALESCE(""Observaciones"" || E'\n', '') || @MotivoRechazo
                        WHERE ""Cod"" = @PrestamoCod";

                    await connection.ExecuteAsync(sql, new
                    {
                        PrestamoCod = prestamoCod,
                        UsuarioId = usuarioId,
                        MotivoRechazo = $"RECHAZADO: {request.MotivoRechazo}"
                    });

                    var prestamo = await ObtenerPorIdAsync(prestamoCod);
                    return new ResultadoPrestamoDto
                    {
                        Exito = true,
                        Mensaje = "Solicitud de préstamo rechazada",
                        PrestamoCod = prestamoCod,
                        Prestamo = prestamo
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error autorizando préstamo {PrestamoCod}", prestamoCod);
                return new ResultadoPrestamoDto
                {
                    Exito = false,
                    Mensaje = "Error al autorizar préstamo: " + ex.Message
                };
            }
        }

        public async Task<ResultadoPrestamoDto> RegistrarEntregaAsync(
            long prestamoCod,
            long usuarioId,
            RegistrarEntregaRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar estado
                var estadoActual = await connection.ExecuteScalarAsync<string?>(
                    @"SELECT ""Estado"" FROM documentos.""Prestamos_Expedientes"" WHERE ""Cod"" = @PrestamoCod",
                    new { PrestamoCod = prestamoCod });

                if (estadoActual == null)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = "Préstamo no encontrado" };
                }

                if (estadoActual != EstadosPrestamo.Autorizado)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = $"El préstamo debe estar AUTORIZADO para registrar entrega (Estado actual: {estadoActual})" };
                }

                const string sql = @"
                    UPDATE documentos.""Prestamos_Expedientes""
                    SET ""Estado"" = 'PRESTADO',
                        ""FechaPrestamo"" = CURRENT_TIMESTAMP,
                        ""Observaciones"" = COALESCE(""Observaciones"" || E'\n', '') || @Observaciones
                    WHERE ""Cod"" = @PrestamoCod";

                await connection.ExecuteAsync(sql, new
                {
                    PrestamoCod = prestamoCod,
                    Observaciones = string.IsNullOrEmpty(request.Observaciones)
                        ? $"Entregado el {DateTime.Now:dd/MM/yyyy HH:mm}"
                        : $"Entregado: {request.Observaciones}"
                });

                var prestamo = await ObtenerPorIdAsync(prestamoCod);
                return new ResultadoPrestamoDto
                {
                    Exito = true,
                    Mensaje = "Entrega del expediente registrada",
                    PrestamoCod = prestamoCod,
                    Prestamo = prestamo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando entrega {PrestamoCod}", prestamoCod);
                return new ResultadoPrestamoDto
                {
                    Exito = false,
                    Mensaje = "Error al registrar entrega: " + ex.Message
                };
            }
        }

        public async Task<ResultadoPrestamoDto> RegistrarDevolucionAsync(
            long prestamoCod,
            long usuarioId,
            RegistrarDevolucionRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar estado
                var estadoActual = await connection.ExecuteScalarAsync<string?>(
                    @"SELECT ""Estado"" FROM documentos.""Prestamos_Expedientes"" WHERE ""Cod"" = @PrestamoCod",
                    new { PrestamoCod = prestamoCod });

                if (estadoActual == null)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = "Préstamo no encontrado" };
                }

                if (estadoActual != EstadosPrestamo.Prestado)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = $"El préstamo debe estar PRESTADO para registrar devolución (Estado actual: {estadoActual})" };
                }

                const string sql = @"
                    UPDATE documentos.""Prestamos_Expedientes""
                    SET ""Estado"" = 'DEVUELTO',
                        ""FechaDevolucionReal"" = CURRENT_TIMESTAMP,
                        ""ObservacionesDevolucion"" = @ObservacionesDevolucion
                    WHERE ""Cod"" = @PrestamoCod";

                var observaciones = request.ObservacionesDevolucion ?? "";
                if (!request.EnBuenEstado)
                {
                    observaciones = "⚠️ DEVUELTO CON OBSERVACIONES: " + observaciones;
                }

                await connection.ExecuteAsync(sql, new
                {
                    PrestamoCod = prestamoCod,
                    ObservacionesDevolucion = observaciones
                });

                var prestamo = await ObtenerPorIdAsync(prestamoCod);
                return new ResultadoPrestamoDto
                {
                    Exito = true,
                    Mensaje = "Devolución del expediente registrada",
                    PrestamoCod = prestamoCod,
                    Prestamo = prestamo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando devolución {PrestamoCod}", prestamoCod);
                return new ResultadoPrestamoDto
                {
                    Exito = false,
                    Mensaje = "Error al registrar devolución: " + ex.Message
                };
            }
        }

        public async Task<ResultadoPrestamoDto> CancelarSolicitudAsync(
            long prestamoCod,
            long usuarioId,
            CancelarPrestamoRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que el usuario es el solicitante y el estado permite cancelación
                var prestamo = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT ""SolicitadoPor"", ""Estado""
                    FROM documentos.""Prestamos_Expedientes"" 
                    WHERE ""Cod"" = @PrestamoCod",
                    new { PrestamoCod = prestamoCod });

                if (prestamo == null)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = "Préstamo no encontrado" };
                }

                if ((long)prestamo.SolicitadoPor != usuarioId)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = "Solo el solicitante puede cancelar la solicitud" };
                }

                if ((string)prestamo.Estado != EstadosPrestamo.Solicitado)
                {
                    return new ResultadoPrestamoDto { Exito = false, Mensaje = $"Solo se pueden cancelar solicitudes en estado SOLICITADO (Estado actual: {prestamo.Estado})" };
                }

                const string sql = @"
                    UPDATE documentos.""Prestamos_Expedientes""
                    SET ""Estado"" = 'CANCELADO',
                        ""Observaciones"" = COALESCE(""Observaciones"" || E'\n', '') || @Motivo
                    WHERE ""Cod"" = @PrestamoCod";

                await connection.ExecuteAsync(sql, new
                {
                    PrestamoCod = prestamoCod,
                    Motivo = $"CANCELADO: {request.Motivo}"
                });

                var resultado = await ObtenerPorIdAsync(prestamoCod);
                return new ResultadoPrestamoDto
                {
                    Exito = true,
                    Mensaje = "Solicitud cancelada",
                    PrestamoCod = prestamoCod,
                    Prestamo = resultado
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando solicitud {PrestamoCod}", prestamoCod);
                return new ResultadoPrestamoDto
                {
                    Exito = false,
                    Mensaje = "Error al cancelar solicitud: " + ex.Message
                };
            }
        }
    }
}