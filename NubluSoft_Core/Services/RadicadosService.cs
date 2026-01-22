using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class RadicadosService : IRadicadosService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<RadicadosService> _logger;

        public RadicadosService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<RadicadosService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<IEnumerable<Radicado>> ObtenerConFiltrosAsync(long entidadId, FiltrosRadicadosRequest filtros)
        {
            var sql = @"
                SELECT
                    r.""Cod"", r.""NumeroRadicado"", r.""Entidad"", r.""TipoComunicacion"",
                    r.""Asunto"", r.""Descripcion"", r.""FechaRadicacion"", r.""FechaDocumento""::timestamp AS ""FechaDocumento"",
                    r.""FechaVencimiento"", r.""TipoSolicitud"", r.""EstadoRadicado"", r.""Prioridad"",
                    r.""MedioRecepcion"", r.""UsuarioRadica"", r.""OficinaOrigen"", r.""OficinaDestino"",
                    r.""UsuarioAsignado"", r.""Tercero"", r.""RadicadoPadre"", r.""ExpedienteVinculado"",
                    r.""Folios"", r.""Anexos"", r.""Observaciones"", r.""Estado"", r.""RequiereRespuesta"",
                    tc.""Nombre"" AS ""NombreTipoComunicacion"",
                    ts.""Nombre"" AS ""NombreTipoSolicitud"",
                    ts.""DiasHabilesRespuesta"" AS ""DiasRespuesta"",
                    er.""Nombre"" AS ""NombreEstadoRadicado"",
                    er.""Color"" AS ""ColorEstado"",
                    pr.""Nombre"" AS ""NombrePrioridad"",
                    mr.""Nombre"" AS ""NombreMedioRecepcion"",
                    ur.""Nombres"" || ' ' || ur.""Apellidos"" AS ""NombreUsuarioRadica"",
                    oo.""Nombre"" AS ""NombreOficinaOrigen"",
                    od.""Nombre"" AS ""NombreOficinaDestino"",
                    ua.""Nombres"" || ' ' || ua.""Apellidos"" AS ""NombreUsuarioAsignado"",
                    t.""Nombre"" AS ""NombreTercero"",
                    t.""Documento"" AS ""DocumentoTercero"",
                    rp.""NumeroRadicado"" AS ""NumeroRadicadoPadre""
                FROM documentos.""Radicados"" r
                LEFT JOIN documentos.""Tipos_Comunicacion"" tc ON r.""TipoComunicacion"" = tc.""Cod""
                LEFT JOIN documentos.""Tipos_Solicitud"" ts ON r.""TipoSolicitud"" = ts.""Cod""
                LEFT JOIN documentos.""Estados_Radicado"" er ON r.""EstadoRadicado"" = er.""Cod""
                LEFT JOIN documentos.""Prioridades_Radicado"" pr ON r.""Prioridad"" = pr.""Cod""
                LEFT JOIN documentos.""Medios_Recepcion"" mr ON r.""MedioRecepcion"" = mr.""Cod""
                LEFT JOIN documentos.""Usuarios"" ur ON r.""UsuarioRadica"" = ur.""Cod""
                LEFT JOIN documentos.""Oficinas"" oo ON r.""OficinaOrigen"" = oo.""Cod"" AND r.""Entidad"" = oo.""Entidad""
                LEFT JOIN documentos.""Oficinas"" od ON r.""OficinaDestino"" = od.""Cod"" AND r.""Entidad"" = od.""Entidad""
                LEFT JOIN documentos.""Usuarios"" ua ON r.""UsuarioAsignado"" = ua.""Cod""
                LEFT JOIN documentos.""Terceros"" t ON r.""Tercero"" = t.""Cod""
                LEFT JOIN documentos.""Radicados"" rp ON r.""RadicadoPadre"" = rp.""Cod""
                WHERE r.""Entidad"" = @EntidadId";

            var parameters = new DynamicParameters();
            parameters.Add("EntidadId", entidadId);

            if (filtros.SoloActivos)
                sql += @" AND r.""Estado"" = true";

            if (!string.IsNullOrWhiteSpace(filtros.TipoComunicacion))
            {
                sql += @" AND r.""TipoComunicacion"" = @TipoComunicacion";
                parameters.Add("TipoComunicacion", filtros.TipoComunicacion);
            }

            if (filtros.EstadoRadicado.HasValue)
            {
                sql += @" AND r.""EstadoRadicado"" = @EstadoRadicado";
                parameters.Add("EstadoRadicado", filtros.EstadoRadicado.Value);
            }

            if (filtros.OficinaOrigen.HasValue)
            {
                sql += @" AND r.""OficinaOrigen"" = @OficinaOrigen";
                parameters.Add("OficinaOrigen", filtros.OficinaOrigen.Value);
            }

            if (filtros.OficinaDestino.HasValue)
            {
                sql += @" AND r.""OficinaDestino"" = @OficinaDestino";
                parameters.Add("OficinaDestino", filtros.OficinaDestino.Value);
            }

            if (filtros.UsuarioAsignado.HasValue)
            {
                sql += @" AND r.""UsuarioAsignado"" = @UsuarioAsignado";
                parameters.Add("UsuarioAsignado", filtros.UsuarioAsignado.Value);
            }

            if (filtros.Tercero.HasValue)
            {
                sql += @" AND r.""Tercero"" = @Tercero";
                parameters.Add("Tercero", filtros.Tercero.Value);
            }

            if (filtros.TipoSolicitud.HasValue)
            {
                sql += @" AND r.""TipoSolicitud"" = @TipoSolicitud";
                parameters.Add("TipoSolicitud", filtros.TipoSolicitud.Value);
            }

            if (filtros.FechaDesde.HasValue)
            {
                sql += @" AND r.""FechaRadicacion"" >= @FechaDesde";
                parameters.Add("FechaDesde", filtros.FechaDesde.Value);
            }

            if (filtros.FechaHasta.HasValue)
            {
                sql += @" AND r.""FechaRadicacion"" <= @FechaHasta";
                parameters.Add("FechaHasta", filtros.FechaHasta.Value.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(filtros.Busqueda))
            {
                sql += @" AND (r.""NumeroRadicado"" ILIKE @Busqueda OR r.""Asunto"" ILIKE @Busqueda OR t.""Nombre"" ILIKE @Busqueda)";
                parameters.Add("Busqueda", $"%{filtros.Busqueda}%");
            }

            if (filtros.SoloVencidos == true)
            {
                sql += @" AND r.""FechaVencimiento"" < CURRENT_DATE AND r.""EstadoRadicado"" NOT IN (5, 6, 9)"; // No respondido, archivado, anulado
            }

            if (filtros.SoloProximosAVencer == true)
            {
                sql += @" AND r.""FechaVencimiento"" BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '3 days' AND r.""EstadoRadicado"" NOT IN (5, 6, 9)";
            }

            sql += @" ORDER BY r.""FechaRadicacion"" DESC";

            if (filtros.Limite.HasValue)
            {
                sql += @" LIMIT @Limite";
                parameters.Add("Limite", filtros.Limite.Value);
            }

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Radicado>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo radicados con filtros");
                return Enumerable.Empty<Radicado>();
            }
        }

        public async Task<Radicado?> ObtenerPorIdAsync(long radicadoId)
        {
            const string sql = @"
                SELECT
                    r.""Cod"", r.""NumeroRadicado"", r.""Entidad"", r.""TipoComunicacion"",
                    r.""Asunto"", r.""Descripcion"", r.""FechaRadicacion"", r.""FechaDocumento""::timestamp AS ""FechaDocumento"",
                    r.""FechaVencimiento"", r.""TipoSolicitud"", r.""EstadoRadicado"", r.""Prioridad"",
                    r.""MedioRecepcion"", r.""UsuarioRadica"", r.""OficinaOrigen"", r.""OficinaDestino"",
                    r.""UsuarioAsignado"", r.""Tercero"", r.""RadicadoPadre"", r.""ExpedienteVinculado"",
                    r.""Folios"", r.""Anexos"", r.""Observaciones"", r.""Estado"", r.""RequiereRespuesta"",
                    r.""FechaRespuesta"", r.""FechaCierre"", r.""MotivoAnulacion"", r.""FechaAnulacion"", r.""AnuladoPor"",
                    tc.""Nombre"" AS ""NombreTipoComunicacion"",
                    ts.""Nombre"" AS ""NombreTipoSolicitud"",
                    ts.""DiasHabilesRespuesta"" AS ""DiasRespuesta"",
                    er.""Nombre"" AS ""NombreEstadoRadicado"",
                    er.""Color"" AS ""ColorEstado"",
                    pr.""Nombre"" AS ""NombrePrioridad"",
                    mr.""Nombre"" AS ""NombreMedioRecepcion"",
                    ur.""Nombres"" || ' ' || ur.""Apellidos"" AS ""NombreUsuarioRadica"",
                    oo.""Nombre"" AS ""NombreOficinaOrigen"",
                    od.""Nombre"" AS ""NombreOficinaDestino"",
                    ua.""Nombres"" || ' ' || ua.""Apellidos"" AS ""NombreUsuarioAsignado"",
                    t.""Nombre"" AS ""NombreTercero"",
                    t.""Documento"" AS ""DocumentoTercero"",
                    rp.""NumeroRadicado"" AS ""NumeroRadicadoPadre"",
                    c.""Nombre"" AS ""NombreExpedienteVinculado""
                FROM documentos.""Radicados"" r
                LEFT JOIN documentos.""Tipos_Comunicacion"" tc ON r.""TipoComunicacion"" = tc.""Cod""
                LEFT JOIN documentos.""Tipos_Solicitud"" ts ON r.""TipoSolicitud"" = ts.""Cod""
                LEFT JOIN documentos.""Estados_Radicado"" er ON r.""EstadoRadicado"" = er.""Cod""
                LEFT JOIN documentos.""Prioridades_Radicado"" pr ON r.""Prioridad"" = pr.""Cod""
                LEFT JOIN documentos.""Medios_Recepcion"" mr ON r.""MedioRecepcion"" = mr.""Cod""
                LEFT JOIN documentos.""Usuarios"" ur ON r.""UsuarioRadica"" = ur.""Cod""
                LEFT JOIN documentos.""Oficinas"" oo ON r.""OficinaOrigen"" = oo.""Cod"" AND r.""Entidad"" = oo.""Entidad""
                LEFT JOIN documentos.""Oficinas"" od ON r.""OficinaDestino"" = od.""Cod"" AND r.""Entidad"" = od.""Entidad""
                LEFT JOIN documentos.""Usuarios"" ua ON r.""UsuarioAsignado"" = ua.""Cod""
                LEFT JOIN documentos.""Terceros"" t ON r.""Tercero"" = t.""Cod""
                LEFT JOIN documentos.""Radicados"" rp ON r.""RadicadoPadre"" = rp.""Cod""
                LEFT JOIN documentos.""Carpetas"" c ON r.""ExpedienteVinculado"" = c.""Cod""
                WHERE r.""Cod"" = @RadicadoId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<Radicado>(sql, new { RadicadoId = radicadoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo radicado {RadicadoId}", radicadoId);
                return null;
            }
        }

        public async Task<Radicado?> ObtenerPorNumeroAsync(long entidadId, string numeroRadicado)
        {
            const string sql = @"
                SELECT ""Cod"" FROM documentos.""Radicados"" 
                WHERE ""Entidad"" = @EntidadId AND ""NumeroRadicado"" = @NumeroRadicado";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var cod = await connection.QueryFirstOrDefaultAsync<long?>(sql, new { EntidadId = entidadId, NumeroRadicado = numeroRadicado });

                if (cod.HasValue)
                    return await ObtenerPorIdAsync(cod.Value);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo radicado por número {NumeroRadicado}", numeroRadicado);
                return null;
            }
        }

        public async Task<IEnumerable<RadicadoTrazabilidad>> ObtenerTrazabilidadAsync(long radicadoId)
        {
            const string sql = @"
                SELECT 
                    rt.""Cod"", rt.""Radicado"", rt.""Accion"", rt.""Descripcion"",
                    rt.""FechaAccion"", rt.""Usuario"", rt.""OficinaOrigen"", rt.""OficinaDestino"",
                    rt.""UsuarioOrigen"", rt.""UsuarioDestino"", rt.""Observaciones"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreUsuario"",
                    oo.""Nombre"" AS ""NombreOficinaOrigen"",
                    od.""Nombre"" AS ""NombreOficinaDestino"",
                    uo.""Nombres"" || ' ' || uo.""Apellidos"" AS ""NombreUsuarioOrigen"",
                    ud.""Nombres"" || ' ' || ud.""Apellidos"" AS ""NombreUsuarioDestino""
                FROM documentos.""Radicados_Trazabilidad"" rt
                LEFT JOIN documentos.""Usuarios"" u ON rt.""Usuario"" = u.""Cod""
                LEFT JOIN documentos.""Oficinas"" oo ON rt.""OficinaOrigen"" = oo.""Cod""
                LEFT JOIN documentos.""Oficinas"" od ON rt.""OficinaDestino"" = od.""Cod""
                LEFT JOIN documentos.""Usuarios"" uo ON rt.""UsuarioOrigen"" = uo.""Cod""
                LEFT JOIN documentos.""Usuarios"" ud ON rt.""UsuarioDestino"" = ud.""Cod""
                WHERE rt.""Radicado"" = @RadicadoId
                ORDER BY rt.""FechaAccion"" DESC";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<RadicadoTrazabilidad>(sql, new { RadicadoId = radicadoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo trazabilidad de radicado {RadicadoId}", radicadoId);
                return Enumerable.Empty<RadicadoTrazabilidad>();
            }
        }

        public async Task<IEnumerable<RadicadoAnexo>> ObtenerAnexosAsync(long radicadoId)
        {
            const string sql = @"
                SELECT 
                    ra.""Cod"", ra.""Radicado"", ra.""Archivo"", ra.""Nombre"", ra.""Ruta"",
                    ra.""Descripcion"", ra.""Tamano"", ra.""FechaAnexo"", ra.""AnexadoPor"", ra.""Estado"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreAnexadoPor""
                FROM documentos.""Radicados_Anexos"" ra
                LEFT JOIN documentos.""Usuarios"" u ON ra.""AnexadoPor"" = u.""Cod""
                WHERE ra.""Radicado"" = @RadicadoId AND ra.""Estado"" = true
                ORDER BY ra.""FechaAnexo""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<RadicadoAnexo>(sql, new { RadicadoId = radicadoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo anexos de radicado {RadicadoId}", radicadoId);
                return Enumerable.Empty<RadicadoAnexo>();
            }
        }

        public async Task<IEnumerable<Radicado>> ObtenerPorVencerAsync(long entidadId, int diasAnticipacion = 3)
        {
            var filtros = new FiltrosRadicadosRequest { SoloProximosAVencer = true };
            return await ObtenerConFiltrosAsync(entidadId, filtros);
        }

        public async Task<IEnumerable<Radicado>> ObtenerVencidosAsync(long entidadId)
        {
            var filtros = new FiltrosRadicadosRequest { SoloVencidos = true };
            return await ObtenerConFiltrosAsync(entidadId, filtros);
        }

        // ==================== RADICACIÓN ====================

        public async Task<ResultadoRadicado> RadicarEntradaAsync(long entidadId, long usuarioId, RadicarEntradaRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_RadicarEntrada""(
                    p_Entidad := @Entidad,
                    p_Asunto := @Asunto,
                    p_Descripcion := @Descripcion,
                    p_TipoSolicitud := @TipoSolicitud,
                    p_Tercero := @Tercero,
                    p_OficinaDestino := @OficinaDestino,
                    p_UsuarioAsignado := @UsuarioAsignado,
                    p_UsuarioRadica := @UsuarioRadica,
                    p_FechaDocumento := @FechaDocumento,
                    p_MedioRecepcion := @MedioRecepcion,
                    p_Prioridad := @Prioridad,
                    p_Folios := @Folios,
                    p_Anexos := @Anexos,
                    p_Observaciones := @Observaciones,
                    p_RequiereRespuesta := @RequiereRespuesta
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRadicado>(sql, new
                {
                    Entidad = entidadId,
                    request.Asunto,
                    request.Descripcion,
                    request.TipoSolicitud,
                    request.Tercero,
                    request.OficinaDestino,
                    request.UsuarioAsignado,
                    UsuarioRadica = usuarioId,
                    request.FechaDocumento,
                    request.MedioRecepcion,
                    request.Prioridad,
                    request.Folios,
                    request.Anexos,
                    request.Observaciones,
                    request.RequiereRespuesta
                });

                return resultado ?? new ResultadoRadicado { Exito = false, Mensaje = "Error al radicar entrada" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error radicando entrada");
                return new ResultadoRadicado { Exito = false, Mensaje = "Error al radicar entrada: " + ex.Message };
            }
        }

        public async Task<ResultadoRadicado> RadicarSalidaAsync(long entidadId, long usuarioId, RadicarSalidaRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_RadicarSalida""(
                    p_Entidad := @Entidad,
                    p_Asunto := @Asunto,
                    p_Descripcion := @Descripcion,
                    p_Tercero := @Tercero,
                    p_OficinaOrigen := @OficinaOrigen,
                    p_RadicadoPadre := @RadicadoPadre,
                    p_UsuarioRadica := @UsuarioRadica,
                    p_FechaDocumento := @FechaDocumento,
                    p_MedioRecepcion := @MedioRecepcion,
                    p_Folios := @Folios,
                    p_Anexos := @Anexos,
                    p_Observaciones := @Observaciones
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRadicado>(sql, new
                {
                    Entidad = entidadId,
                    request.Asunto,
                    request.Descripcion,
                    request.Tercero,
                    request.OficinaOrigen,
                    request.RadicadoPadre,
                    UsuarioRadica = usuarioId,
                    request.FechaDocumento,
                    request.MedioRecepcion,
                    request.Folios,
                    request.Anexos,
                    request.Observaciones
                });

                return resultado ?? new ResultadoRadicado { Exito = false, Mensaje = "Error al radicar salida" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error radicando salida");
                return new ResultadoRadicado { Exito = false, Mensaje = "Error al radicar salida: " + ex.Message };
            }
        }

        public async Task<ResultadoRadicado> RadicarInternaAsync(long entidadId, long usuarioId, RadicarInternaRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_RadicarInterna""(
                    p_Entidad := @Entidad,
                    p_Asunto := @Asunto,
                    p_Descripcion := @Descripcion,
                    p_OficinaOrigen := @OficinaOrigen,
                    p_OficinaDestino := @OficinaDestino,
                    p_UsuarioAsignado := @UsuarioAsignado,
                    p_UsuarioRadica := @UsuarioRadica,
                    p_FechaDocumento := @FechaDocumento,
                    p_Prioridad := @Prioridad,
                    p_Folios := @Folios,
                    p_Anexos := @Anexos,
                    p_Observaciones := @Observaciones,
                    p_RequiereRespuesta := @RequiereRespuesta
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRadicado>(sql, new
                {
                    Entidad = entidadId,
                    request.Asunto,
                    request.Descripcion,
                    request.OficinaOrigen,
                    request.OficinaDestino,
                    request.UsuarioAsignado,
                    UsuarioRadica = usuarioId,
                    request.FechaDocumento,
                    request.Prioridad,
                    request.Folios,
                    request.Anexos,
                    request.Observaciones,
                    request.RequiereRespuesta
                });

                return resultado ?? new ResultadoRadicado { Exito = false, Mensaje = "Error al radicar comunicación interna" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error radicando comunicación interna");
                return new ResultadoRadicado { Exito = false, Mensaje = "Error al radicar comunicación interna: " + ex.Message };
            }
        }

        // ==================== GESTIÓN ====================

        public async Task<ResultadoRadicado> AsignarAsync(long radicadoId, long usuarioId, AsignarRadicadoRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_AsignarRadicado""(
                    p_Radicado := @Radicado,
                    p_OficinaDestino := @OficinaDestino,
                    p_UsuarioAsignado := @UsuarioAsignado,
                    p_Usuario := @Usuario,
                    p_Observaciones := @Observaciones
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRadicado>(sql, new
                {
                    Radicado = radicadoId,
                    request.OficinaDestino,
                    request.UsuarioAsignado,
                    Usuario = usuarioId,
                    request.Observaciones
                });

                return resultado ?? new ResultadoRadicado { Exito = false, Mensaje = "Error al asignar radicado" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando radicado {RadicadoId}", radicadoId);
                return new ResultadoRadicado { Exito = false, Mensaje = "Error al asignar radicado: " + ex.Message };
            }
        }

        public async Task<ResultadoRadicado> TrasladarAsync(long radicadoId, long usuarioId, TrasladarRadicadoRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_TrasladarRadicado""(
                    p_Radicado := @Radicado,
                    p_OficinaDestino := @OficinaDestino,
                    p_Usuario := @Usuario,
                    p_Motivo := @Motivo
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRadicado>(sql, new
                {
                    Radicado = radicadoId,
                    request.OficinaDestino,
                    Usuario = usuarioId,
                    request.Motivo
                });

                return resultado ?? new ResultadoRadicado { Exito = false, Mensaje = "Error al trasladar radicado" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error trasladando radicado {RadicadoId}", radicadoId);
                return new ResultadoRadicado { Exito = false, Mensaje = "Error al trasladar radicado: " + ex.Message };
            }
        }

        public async Task<ResultadoRadicado> ArchivarAsync(long radicadoId, long usuarioId, ArchivarRadicadoRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_ArchivarRadicado""(
                    p_Radicado := @Radicado,
                    p_Expediente := @Expediente,
                    p_Usuario := @Usuario,
                    p_Observaciones := @Observaciones
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRadicado>(sql, new
                {
                    Radicado = radicadoId,
                    request.Expediente,
                    Usuario = usuarioId,
                    request.Observaciones
                });

                return resultado ?? new ResultadoRadicado { Exito = false, Mensaje = "Error al archivar radicado" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archivando radicado {RadicadoId}", radicadoId);
                return new ResultadoRadicado { Exito = false, Mensaje = "Error al archivar radicado: " + ex.Message };
            }
        }

        public async Task<ResultadoRadicado> SolicitarProrrogaAsync(long radicadoId, long usuarioId, SolicitarProrrogaRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_SolicitarProrroga""(
                    p_Radicado := @Radicado,
                    p_DiasAdicionales := @DiasAdicionales,
                    p_Usuario := @Usuario,
                    p_Motivo := @Motivo
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRadicado>(sql, new
                {
                    Radicado = radicadoId,
                    request.DiasAdicionales,
                    Usuario = usuarioId,
                    request.Motivo
                });

                return resultado ?? new ResultadoRadicado { Exito = false, Mensaje = "Error al solicitar prórroga" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error solicitando prórroga para radicado {RadicadoId}", radicadoId);
                return new ResultadoRadicado { Exito = false, Mensaje = "Error al solicitar prórroga: " + ex.Message };
            }
        }

        public async Task<ResultadoRadicado> AnularAsync(long radicadoId, long usuarioId, AnularRadicadoRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_AnularRadicado""(
                    p_Radicado := @Radicado,
                    p_Usuario := @Usuario,
                    p_Motivo := @Motivo
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRadicado>(sql, new
                {
                    Radicado = radicadoId,
                    Usuario = usuarioId,
                    request.Motivo
                });

                return resultado ?? new ResultadoRadicado { Exito = false, Mensaje = "Error al anular radicado" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error anulando radicado {RadicadoId}", radicadoId);
                return new ResultadoRadicado { Exito = false, Mensaje = "Error al anular radicado: " + ex.Message };
            }
        }

        // ==================== ANEXOS ====================

        public async Task<(bool Success, string? Error)> AgregarAnexoAsync(long radicadoId, long usuarioId, AgregarAnexoRequest request)
        {
            const string sql = @"
                INSERT INTO documentos.""Radicados_Anexos"" 
                    (""Radicado"", ""Nombre"", ""Ruta"", ""Descripcion"", ""Tamano"", ""FechaAnexo"", ""AnexadoPor"", ""Estado"")
                VALUES 
                    (@Radicado, @Nombre, @Ruta, @Descripcion, @Tamano, @FechaAnexo, @AnexadoPor, true)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                await connection.ExecuteAsync(sql, new
                {
                    Radicado = radicadoId,
                    request.Nombre,
                    request.Ruta,
                    request.Descripcion,
                    request.Tamano,
                    FechaAnexo = DateTime.Now,
                    AnexadoPor = usuarioId
                });

                // Actualizar contador de anexos
                await connection.ExecuteAsync(
                    @"UPDATE documentos.""Radicados"" SET ""Anexos"" = COALESCE(""Anexos"", 0) + 1 WHERE ""Cod"" = @RadicadoId",
                    new { RadicadoId = radicadoId });

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error agregando anexo a radicado {RadicadoId}", radicadoId);
                return (false, "Error al agregar anexo: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> EliminarAnexoAsync(long anexoId, long usuarioId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener radicado antes de eliminar
                var radicadoId = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""Radicado"" FROM documentos.""Radicados_Anexos"" WHERE ""Cod"" = @AnexoId AND ""Estado"" = true",
                    new { AnexoId = anexoId });

                if (!radicadoId.HasValue)
                    return (false, "Anexo no encontrado");

                // Soft delete
                await connection.ExecuteAsync(
                    @"UPDATE documentos.""Radicados_Anexos"" SET ""Estado"" = false WHERE ""Cod"" = @AnexoId",
                    new { AnexoId = anexoId });

                // Actualizar contador
                await connection.ExecuteAsync(
                    @"UPDATE documentos.""Radicados"" SET ""Anexos"" = GREATEST(COALESCE(""Anexos"", 1) - 1, 0) WHERE ""Cod"" = @RadicadoId",
                    new { RadicadoId = radicadoId.Value });

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando anexo {AnexoId}", anexoId);
                return (false, "Error al eliminar anexo: " + ex.Message);
            }
        }
    }
}