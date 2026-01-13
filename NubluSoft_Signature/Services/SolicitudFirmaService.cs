using Dapper;
using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del servicio de solicitudes de firma
    /// </summary>
    public class SolicitudFirmaService : ISolicitudFirmaService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<SolicitudFirmaService> _logger;

        public SolicitudFirmaService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<SolicitudFirmaService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<OperacionFirmaResult> CrearSolicitudAsync(
            long entidadId,
            long usuarioId,
            CrearSolicitudRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1. Verificar que el archivo existe y obtener su hash
                    var archivo = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                        SELECT ""Cod"", ""Nombre"", ""Hash"", ""Carpeta""
                        FROM documentos.""Archivos""
                        WHERE ""Cod"" = @ArchivoId AND ""Entidad"" = @EntidadId AND ""Estado"" = true",
                        new { ArchivoId = request.ArchivoId, EntidadId = entidadId },
                        transaction);

                    if (archivo == null)
                    {
                        return new OperacionFirmaResult
                        {
                            Exito = false,
                            Mensaje = "El archivo no existe o no tiene permisos"
                        };
                    }

                    // 2. Verificar que no haya solicitud pendiente para el mismo archivo
                    var solicitudExistente = await connection.QueryFirstOrDefaultAsync<long?>(@"
                        SELECT ""Cod""
                        FROM documentos.""Solicitudes_Firma""
                        WHERE ""Archivo"" = @ArchivoId 
                          AND ""Entidad"" = @EntidadId 
                          AND ""Estado"" IN ('PENDIENTE', 'EN_PROCESO')",
                        new { ArchivoId = request.ArchivoId, EntidadId = entidadId },
                        transaction);

                    if (solicitudExistente.HasValue)
                    {
                        return new OperacionFirmaResult
                        {
                            Exito = false,
                            Mensaje = "Ya existe una solicitud de firma pendiente para este archivo"
                        };
                    }

                    // 3. Verificar que todos los firmantes existen
                    var usuariosIds = request.Firmantes.Select(f => f.UsuarioId).Distinct().ToList();
                    var usuariosExistentes = await connection.QueryAsync<long>(@"
                        SELECT ""Cod""
                        FROM documentos.""Usuarios""
                        WHERE ""Cod"" = ANY(@Usuarios) AND ""Entidad"" = @EntidadId AND ""Estado"" = true",
                        new { Usuarios = usuariosIds.ToArray(), EntidadId = entidadId },
                        transaction);

                    if (usuariosExistentes.Count() != usuariosIds.Count)
                    {
                        return new OperacionFirmaResult
                        {
                            Exito = false,
                            Mensaje = "Uno o más firmantes no existen o están inactivos"
                        };
                    }

                    // 4. Generar código de verificación
                    var codigoVerificacion = await connection.QueryFirstAsync<string>(@"
                        SELECT documentos.""F_GenerarCodigoVerificacion""()",
                        transaction: transaction);

                    // 5. Calcular fecha de vencimiento
                    var fechaVencimiento = DateTime.Now.AddDays(request.DiasVigencia);

                    // 6. Crear la solicitud
                    var solicitudId = await connection.QueryFirstAsync<long>(@"
                        INSERT INTO documentos.""Solicitudes_Firma"" (
                            ""Entidad"", ""Archivo"", ""TipoFirma"", ""OrdenSecuencial"",
                            ""SolicitadoPor"", ""FechaVencimiento"", ""Asunto"", ""Mensaje"",
                            ""HashOriginal"", ""CodigoVerificacion""
                        ) VALUES (
                            @EntidadId, @ArchivoId, @TipoFirma, @OrdenSecuencial,
                            @UsuarioId, @FechaVencimiento, @Asunto, @Mensaje,
                            @HashOriginal, @CodigoVerificacion
                        ) RETURNING ""Cod""",
                        new
                        {
                            EntidadId = entidadId,
                            ArchivoId = request.ArchivoId,
                            TipoFirma = request.TipoFirma,
                            OrdenSecuencial = request.OrdenSecuencial,
                            UsuarioId = usuarioId,
                            FechaVencimiento = fechaVencimiento,
                            Asunto = request.Asunto,
                            Mensaje = request.Mensaje,
                            HashOriginal = archivo.Hash ?? "PENDIENTE",
                            CodigoVerificacion = codigoVerificacion
                        },
                        transaction);

                    // 7. Crear los firmantes
                    foreach (var firmante in request.Firmantes)
                    {
                        // Determinar estado inicial
                        var estadoInicial = request.OrdenSecuencial && firmante.Orden > 1
                            ? "PENDIENTE"
                            : "NOTIFICADO";

                        await connection.ExecuteAsync(@"
                            INSERT INTO documentos.""Firmantes_Solicitud"" (
                                ""Solicitud"", ""Usuario"", ""Entidad"", ""Orden"",
                                ""RolFirmante"", ""Estado"", ""FechaNotificacion""
                            ) VALUES (
                                @SolicitudId, @UsuarioId, @EntidadId, @Orden,
                                @RolFirmante, @Estado, 
                                CASE WHEN @Estado = 'NOTIFICADO' THEN CURRENT_TIMESTAMP ELSE NULL END
                            )",
                            new
                            {
                                SolicitudId = solicitudId,
                                UsuarioId = firmante.UsuarioId,
                                EntidadId = entidadId,
                                Orden = firmante.Orden,
                                RolFirmante = firmante.RolFirmante,
                                Estado = estadoInicial
                            },
                            transaction);
                    }

                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Solicitud de firma {SolicitudId} creada por usuario {UsuarioId} para archivo {ArchivoId}",
                        solicitudId, usuarioId, request.ArchivoId);

                    return new OperacionFirmaResult
                    {
                        Exito = true,
                        Mensaje = "Solicitud creada exitosamente",
                        SolicitudId = solicitudId,
                        CodigoVerificacion = codigoVerificacion
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando solicitud de firma");
                return new OperacionFirmaResult
                {
                    Exito = false,
                    Mensaje = "Error al crear la solicitud: " + ex.Message
                };
            }
        }

        public async Task<SolicitudFirmaResponse?> ObtenerPorIdAsync(
            long entidadId,
            long solicitudId,
            long? usuarioActual = null)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener solicitud
                var solicitud = await connection.QueryFirstOrDefaultAsync<SolicitudFirmaResponse>(@"
                    SELECT 
                        s.""Cod"",
                        s.""Entidad"",
                        s.""Archivo"" AS ""ArchivoId"",
                        a.""Nombre"" AS ""NombreArchivo"",
                        s.""TipoFirma"",
                        s.""OrdenSecuencial"",
                        s.""Estado"",
                        s.""SolicitadoPor"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreSolicitante"",
                        s.""FechaSolicitud"",
                        s.""FechaVencimiento"",
                        s.""FechaCompletada"",
                        s.""Asunto"",
                        s.""Mensaje"",
                        s.""CodigoVerificacion"",
                        (SELECT COUNT(*) FROM documentos.""Firmantes_Solicitud"" WHERE ""Solicitud"" = s.""Cod"") AS ""TotalFirmantes"",
                        (SELECT COUNT(*) FROM documentos.""Firmantes_Solicitud"" WHERE ""Solicitud"" = s.""Cod"" AND ""Estado"" = 'FIRMADO') AS ""FirmantesCompletados""
                    FROM documentos.""Solicitudes_Firma"" s
                    INNER JOIN documentos.""Archivos"" a ON s.""Archivo"" = a.""Cod""
                    INNER JOIN documentos.""Usuarios"" u ON s.""SolicitadoPor"" = u.""Cod"" AND s.""Entidad"" = u.""Entidad""
                    WHERE s.""Cod"" = @SolicitudId AND s.""Entidad"" = @EntidadId",
                    new { SolicitudId = solicitudId, EntidadId = entidadId });

                if (solicitud == null)
                    return null;

                // Obtener firmantes
                var firmantes = await connection.QueryAsync<FirmanteResponse>(@"
                    SELECT 
                        fs.""Cod"",
                        fs.""Usuario"" AS ""UsuarioId"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreUsuario"",
                        u.""Correo"" AS ""Email"",
                        fs.""Orden"",
                        fs.""RolFirmante"",
                        fs.""Estado"",
                        fs.""FechaNotificacion"",
                        fs.""FechaFirma"",
                        fs.""TipoFirmaUsada"",
                        fs.""FechaRechazo"",
                        fs.""MotivoRechazo""
                    FROM documentos.""Firmantes_Solicitud"" fs
                    INNER JOIN documentos.""Usuarios"" u ON fs.""Usuario"" = u.""Cod"" AND fs.""Entidad"" = u.""Entidad""
                    WHERE fs.""Solicitud"" = @SolicitudId
                    ORDER BY fs.""Orden"", fs.""Cod""",
                    new { SolicitudId = solicitudId });

                solicitud.Firmantes = firmantes.ToList();

                // Marcar si es el turno del usuario actual
                if (usuarioActual.HasValue && solicitud.OrdenSecuencial)
                {
                    var ordenActual = solicitud.Firmantes
                        .Where(f => f.Estado == "FIRMADO")
                        .Select(f => f.Orden)
                        .DefaultIfEmpty(0)
                        .Max() + 1;

                    foreach (var f in solicitud.Firmantes)
                    {
                        f.EsMiTurno = f.UsuarioId == usuarioActual.Value
                            && f.Estado == "NOTIFICADO"
                            && f.Orden == ordenActual;
                    }
                }
                else if (usuarioActual.HasValue)
                {
                    foreach (var f in solicitud.Firmantes)
                    {
                        f.EsMiTurno = f.UsuarioId == usuarioActual.Value
                            && f.Estado == "NOTIFICADO";
                    }
                }

                return solicitud;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo solicitud {SolicitudId}", solicitudId);
                return null;
            }
        }

        public async Task<IEnumerable<SolicitudPendienteResponse>> ObtenerPendientesUsuarioAsync(
            long entidadId,
            long usuarioId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryAsync<SolicitudPendienteResponse>(@"
                    SELECT 
                        s.""Cod"" AS ""SolicitudId"",
                        fs.""Cod"" AS ""FirmanteId"",
                        s.""Asunto"",
                        a.""Nombre"" AS ""NombreArchivo"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreSolicitante"",
                        s.""FechaSolicitud"",
                        s.""FechaVencimiento"",
                        s.""TipoFirma"",
                        fs.""Orden"",
                        fs.""RolFirmante"",
                        CASE 
                            WHEN s.""OrdenSecuencial"" = false THEN true
                            WHEN fs.""Orden"" = (
                                SELECT COALESCE(MAX(f2.""Orden""), 0) + 1
                                FROM documentos.""Firmantes_Solicitud"" f2
                                WHERE f2.""Solicitud"" = s.""Cod"" AND f2.""Estado"" = 'FIRMADO'
                            ) THEN true
                            ELSE false
                        END AS ""EsMiTurno"",
                        GREATEST(0, EXTRACT(DAY FROM s.""FechaVencimiento"" - CURRENT_TIMESTAMP)::INTEGER) AS ""DiasRestantes""
                    FROM documentos.""Firmantes_Solicitud"" fs
                    INNER JOIN documentos.""Solicitudes_Firma"" s ON fs.""Solicitud"" = s.""Cod""
                    INNER JOIN documentos.""Archivos"" a ON s.""Archivo"" = a.""Cod""
                    INNER JOIN documentos.""Usuarios"" u ON s.""SolicitadoPor"" = u.""Cod"" AND s.""Entidad"" = u.""Entidad""
                    WHERE fs.""Usuario"" = @UsuarioId 
                      AND fs.""Entidad"" = @EntidadId
                      AND fs.""Estado"" IN ('PENDIENTE', 'NOTIFICADO')
                      AND s.""Estado"" IN ('PENDIENTE', 'EN_PROCESO')
                    ORDER BY s.""FechaVencimiento"" ASC NULLS LAST, s.""FechaSolicitud"" DESC",
                    new { UsuarioId = usuarioId, EntidadId = entidadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo solicitudes pendientes del usuario {UsuarioId}", usuarioId);
                return Enumerable.Empty<SolicitudPendienteResponse>();
            }
        }

        public async Task<IEnumerable<SolicitudFirmaResponse>> ObtenerMisSolicitudesAsync(
            long entidadId,
            long usuarioId,
            string? estado = null)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        s.""Cod"",
                        s.""Entidad"",
                        s.""Archivo"" AS ""ArchivoId"",
                        a.""Nombre"" AS ""NombreArchivo"",
                        s.""TipoFirma"",
                        s.""OrdenSecuencial"",
                        s.""Estado"",
                        s.""SolicitadoPor"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreSolicitante"",
                        s.""FechaSolicitud"",
                        s.""FechaVencimiento"",
                        s.""FechaCompletada"",
                        s.""Asunto"",
                        s.""CodigoVerificacion"",
                        (SELECT COUNT(*) FROM documentos.""Firmantes_Solicitud"" WHERE ""Solicitud"" = s.""Cod"") AS ""TotalFirmantes"",
                        (SELECT COUNT(*) FROM documentos.""Firmantes_Solicitud"" WHERE ""Solicitud"" = s.""Cod"" AND ""Estado"" = 'FIRMADO') AS ""FirmantesCompletados""
                    FROM documentos.""Solicitudes_Firma"" s
                    INNER JOIN documentos.""Archivos"" a ON s.""Archivo"" = a.""Cod""
                    INNER JOIN documentos.""Usuarios"" u ON s.""SolicitadoPor"" = u.""Cod"" AND s.""Entidad"" = u.""Entidad""
                    WHERE s.""SolicitadoPor"" = @UsuarioId AND s.""Entidad"" = @EntidadId";

                if (!string.IsNullOrEmpty(estado))
                {
                    sql += @" AND s.""Estado"" = @Estado";
                }

                sql += @" ORDER BY s.""FechaSolicitud"" DESC";

                return await connection.QueryAsync<SolicitudFirmaResponse>(
                    sql,
                    new { UsuarioId = usuarioId, EntidadId = entidadId, Estado = estado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo solicitudes del usuario {UsuarioId}", usuarioId);
                return Enumerable.Empty<SolicitudFirmaResponse>();
            }
        }

        public async Task<OperacionFirmaResult> CancelarSolicitudAsync(
            long entidadId,
            long usuarioId,
            long solicitudId,
            string motivo)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que la solicitud existe y pertenece al usuario
                var solicitud = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT ""Cod"", ""Estado"", ""SolicitadoPor""
                    FROM documentos.""Solicitudes_Firma""
                    WHERE ""Cod"" = @SolicitudId AND ""Entidad"" = @EntidadId",
                    new { SolicitudId = solicitudId, EntidadId = entidadId });

                if (solicitud == null)
                {
                    return new OperacionFirmaResult
                    {
                        Exito = false,
                        Mensaje = "Solicitud no encontrada"
                    };
                }

                if (solicitud.SolicitadoPor != usuarioId)
                {
                    return new OperacionFirmaResult
                    {
                        Exito = false,
                        Mensaje = "Solo el solicitante puede cancelar la solicitud"
                    };
                }

                if (solicitud.Estado != "PENDIENTE" && solicitud.Estado != "EN_PROCESO")
                {
                    return new OperacionFirmaResult
                    {
                        Exito = false,
                        Mensaje = $"No se puede cancelar una solicitud en estado {solicitud.Estado}"
                    };
                }

                // Cancelar la solicitud
                await connection.ExecuteAsync(@"
                    UPDATE documentos.""Solicitudes_Firma""
                    SET ""Estado"" = 'CANCELADA',
                        ""FechaCancelada"" = CURRENT_TIMESTAMP,
                        ""CanceladaPor"" = @UsuarioId,
                        ""MotivoCancelacion"" = @Motivo
                    WHERE ""Cod"" = @SolicitudId",
                    new { SolicitudId = solicitudId, UsuarioId = usuarioId, Motivo = motivo });

                _logger.LogInformation(
                    "Solicitud {SolicitudId} cancelada por usuario {UsuarioId}",
                    solicitudId, usuarioId);

                return new OperacionFirmaResult
                {
                    Exito = true,
                    Mensaje = "Solicitud cancelada exitosamente",
                    SolicitudId = solicitudId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando solicitud {SolicitudId}", solicitudId);
                return new OperacionFirmaResult
                {
                    Exito = false,
                    Mensaje = "Error al cancelar la solicitud: " + ex.Message
                };
            }
        }

        public async Task<ResumenSolicitudesResponse> ObtenerResumenAsync(
            long entidadId,
            long usuarioId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resumen = await connection.QueryFirstOrDefaultAsync<ResumenSolicitudesResponse>(@"
                    SELECT 
                        (SELECT COUNT(*) FROM documentos.""Solicitudes_Firma"" 
                         WHERE ""SolicitadoPor"" = @UsuarioId AND ""Entidad"" = @EntidadId AND ""Estado"" = 'PENDIENTE') AS ""Pendientes"",
                        (SELECT COUNT(*) FROM documentos.""Solicitudes_Firma"" 
                         WHERE ""SolicitadoPor"" = @UsuarioId AND ""Entidad"" = @EntidadId AND ""Estado"" = 'EN_PROCESO') AS ""EnProceso"",
                        (SELECT COUNT(*) FROM documentos.""Solicitudes_Firma"" 
                         WHERE ""SolicitadoPor"" = @UsuarioId AND ""Entidad"" = @EntidadId AND ""Estado"" = 'COMPLETADA') AS ""Completadas"",
                        (SELECT COUNT(*) FROM documentos.""Solicitudes_Firma"" 
                         WHERE ""SolicitadoPor"" = @UsuarioId AND ""Entidad"" = @EntidadId AND ""Estado"" = 'RECHAZADA') AS ""Rechazadas"",
                        (SELECT COUNT(*) FROM documentos.""Solicitudes_Firma"" 
                         WHERE ""SolicitadoPor"" = @UsuarioId AND ""Entidad"" = @EntidadId AND ""Estado"" = 'VENCIDA') AS ""Vencidas"",
                        (SELECT COUNT(*) FROM documentos.""Firmantes_Solicitud"" fs
                         INNER JOIN documentos.""Solicitudes_Firma"" s ON fs.""Solicitud"" = s.""Cod""
                         WHERE fs.""Usuario"" = @UsuarioId AND fs.""Entidad"" = @EntidadId 
                           AND fs.""Estado"" IN ('PENDIENTE', 'NOTIFICADO')
                           AND s.""Estado"" IN ('PENDIENTE', 'EN_PROCESO')) AS ""MisFirmasPendientes""",
                    new { UsuarioId = usuarioId, EntidadId = entidadId });

                return resumen ?? new ResumenSolicitudesResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen de solicitudes");
                return new ResumenSolicitudesResponse();
            }
        }

        public async Task<(bool PuedeFirmar, long? FirmanteId, string? Mensaje)> VerificarPuedeFirmarAsync(
            long entidadId,
            long usuarioId,
            long solicitudId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar solicitud
                var solicitud = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT ""Cod"", ""Estado"", ""OrdenSecuencial""
                    FROM documentos.""Solicitudes_Firma""
                    WHERE ""Cod"" = @SolicitudId AND ""Entidad"" = @EntidadId",
                    new { SolicitudId = solicitudId, EntidadId = entidadId });

                if (solicitud == null)
                    return (false, null, "Solicitud no encontrada");

                if (solicitud.Estado != "PENDIENTE" && solicitud.Estado != "EN_PROCESO")
                    return (false, null, $"La solicitud está en estado {solicitud.Estado}");

                // Verificar firmante
                var firmante = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT ""Cod"", ""Estado"", ""Orden""
                    FROM documentos.""Firmantes_Solicitud""
                    WHERE ""Solicitud"" = @SolicitudId AND ""Usuario"" = @UsuarioId AND ""Entidad"" = @EntidadId",
                    new { SolicitudId = solicitudId, UsuarioId = usuarioId, EntidadId = entidadId });

                if (firmante == null)
                    return (false, null, "No eres firmante de esta solicitud");

                if (firmante.Estado == "FIRMADO")
                    return (false, null, "Ya has firmado este documento");

                if (firmante.Estado == "RECHAZADO")
                    return (false, null, "Has rechazado esta solicitud");

                // Verificar orden secuencial
                if (solicitud.OrdenSecuencial)
                {
                    var ordenActual = await connection.QueryFirstAsync<int>(@"
                        SELECT COALESCE(MAX(""Orden""), 0) + 1
                        FROM documentos.""Firmantes_Solicitud""
                        WHERE ""Solicitud"" = @SolicitudId AND ""Estado"" = 'FIRMADO'",
                        new { SolicitudId = solicitudId });

                    if (firmante.Orden != ordenActual)
                        return (false, null, "Aún no es tu turno para firmar");
                }

                return (true, firmante.Cod, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando si puede firmar");
                return (false, null, "Error al verificar permisos");
            }
        }
    }
}