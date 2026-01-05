using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class TransferenciasService : ITransferenciasService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<TransferenciasService> _logger;

        public TransferenciasService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<TransferenciasService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<IEnumerable<Transferencia>> ObtenerConFiltrosAsync(long entidadId, FiltrosTransferenciasRequest filtros)
        {
            var sql = @"
                SELECT 
                    t.""Cod"", t.""Entidad"", t.""NumeroTransferencia"", t.""TipoTransferencia"",
                    t.""OficinaOrigen"", t.""ArchivoDestino"", t.""FechaCreacion"", t.""FechaEnvio"",
                    t.""FechaRecepcion"", t.""EstadoTransferencia"", t.""CreadoPor"", t.""EnviadoPor"",
                    t.""RecibidoPor"", t.""Observaciones"", t.""ObservacionesRechazo"",
                    t.""TotalExpedientes"", t.""TotalFolios"", t.""Estado"",
                    CASE t.""TipoTransferencia"" 
                        WHEN 'P' THEN 'Primaria (Gestión → Central)'
                        WHEN 'S' THEN 'Secundaria (Central → Histórico)'
                    END AS ""NombreTipoTransferencia"",
                    o.""Nombre"" AS ""NombreOficinaOrigen"",
                    CASE t.""ArchivoDestino"" 
                        WHEN 'C' THEN 'Archivo Central'
                        WHEN 'H' THEN 'Archivo Histórico'
                    END AS ""NombreArchivoDestino"",
                    et.""Nombre"" AS ""NombreEstadoTransferencia"",
                    uc.""Nombres"" || ' ' || uc.""Apellidos"" AS ""NombreCreadoPor"",
                    ue.""Nombres"" || ' ' || ue.""Apellidos"" AS ""NombreEnviadoPor"",
                    ur.""Nombres"" || ' ' || ur.""Apellidos"" AS ""NombreRecibidoPor""
                FROM documentos.""Transferencias_Documentales"" t
                LEFT JOIN documentos.""Oficinas"" o ON t.""OficinaOrigen"" = o.""Cod"" AND t.""Entidad"" = o.""Entidad""
                LEFT JOIN documentos.""Estados_Transferencia"" et ON t.""EstadoTransferencia"" = et.""Cod""
                LEFT JOIN documentos.""Usuarios"" uc ON t.""CreadoPor"" = uc.""Cod""
                LEFT JOIN documentos.""Usuarios"" ue ON t.""EnviadoPor"" = ue.""Cod""
                LEFT JOIN documentos.""Usuarios"" ur ON t.""RecibidoPor"" = ur.""Cod""
                WHERE t.""Entidad"" = @EntidadId";

            var parameters = new DynamicParameters();
            parameters.Add("EntidadId", entidadId);

            if (filtros.SoloActivas)
                sql += @" AND t.""Estado"" = true";

            if (!string.IsNullOrWhiteSpace(filtros.TipoTransferencia))
            {
                sql += @" AND t.""TipoTransferencia"" = @TipoTransferencia";
                parameters.Add("TipoTransferencia", filtros.TipoTransferencia);
            }

            if (filtros.OficinaOrigen.HasValue)
            {
                sql += @" AND t.""OficinaOrigen"" = @OficinaOrigen";
                parameters.Add("OficinaOrigen", filtros.OficinaOrigen.Value);
            }

            if (filtros.EstadoTransferencia.HasValue)
            {
                sql += @" AND t.""EstadoTransferencia"" = @EstadoTransferencia";
                parameters.Add("EstadoTransferencia", filtros.EstadoTransferencia.Value);
            }

            if (filtros.FechaDesde.HasValue)
            {
                sql += @" AND t.""FechaCreacion"" >= @FechaDesde";
                parameters.Add("FechaDesde", filtros.FechaDesde.Value);
            }

            if (filtros.FechaHasta.HasValue)
            {
                sql += @" AND t.""FechaCreacion"" <= @FechaHasta";
                parameters.Add("FechaHasta", filtros.FechaHasta.Value.AddDays(1));
            }

            sql += @" ORDER BY t.""FechaCreacion"" DESC";

            if (filtros.Limite.HasValue)
            {
                sql += @" LIMIT @Limite";
                parameters.Add("Limite", filtros.Limite.Value);
            }

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Transferencia>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo transferencias con filtros");
                return Enumerable.Empty<Transferencia>();
            }
        }

        public async Task<Transferencia?> ObtenerPorIdAsync(long transferenciaId)
        {
            const string sql = @"
                SELECT 
                    t.""Cod"", t.""Entidad"", t.""NumeroTransferencia"", t.""TipoTransferencia"",
                    t.""OficinaOrigen"", t.""ArchivoDestino"", t.""FechaCreacion"", t.""FechaEnvio"",
                    t.""FechaRecepcion"", t.""EstadoTransferencia"", t.""CreadoPor"", t.""EnviadoPor"",
                    t.""RecibidoPor"", t.""Observaciones"", t.""ObservacionesRechazo"",
                    t.""TotalExpedientes"", t.""TotalFolios"", t.""Estado"",
                    CASE t.""TipoTransferencia"" 
                        WHEN 'P' THEN 'Primaria (Gestión → Central)'
                        WHEN 'S' THEN 'Secundaria (Central → Histórico)'
                    END AS ""NombreTipoTransferencia"",
                    o.""Nombre"" AS ""NombreOficinaOrigen"",
                    CASE t.""ArchivoDestino"" 
                        WHEN 'C' THEN 'Archivo Central'
                        WHEN 'H' THEN 'Archivo Histórico'
                    END AS ""NombreArchivoDestino"",
                    et.""Nombre"" AS ""NombreEstadoTransferencia"",
                    uc.""Nombres"" || ' ' || uc.""Apellidos"" AS ""NombreCreadoPor"",
                    ue.""Nombres"" || ' ' || ue.""Apellidos"" AS ""NombreEnviadoPor"",
                    ur.""Nombres"" || ' ' || ur.""Apellidos"" AS ""NombreRecibidoPor""
                FROM documentos.""Transferencias_Documentales"" t
                LEFT JOIN documentos.""Oficinas"" o ON t.""OficinaOrigen"" = o.""Cod"" AND t.""Entidad"" = o.""Entidad""
                LEFT JOIN documentos.""Estados_Transferencia"" et ON t.""EstadoTransferencia"" = et.""Cod""
                LEFT JOIN documentos.""Usuarios"" uc ON t.""CreadoPor"" = uc.""Cod""
                LEFT JOIN documentos.""Usuarios"" ue ON t.""EnviadoPor"" = ue.""Cod""
                LEFT JOIN documentos.""Usuarios"" ur ON t.""RecibidoPor"" = ur.""Cod""
                WHERE t.""Cod"" = @TransferenciaId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<Transferencia>(sql, new { TransferenciaId = transferenciaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo transferencia {TransferenciaId}", transferenciaId);
                return null;
            }
        }

        public async Task<IEnumerable<TransferenciaDetalle>> ObtenerDetalleAsync(long transferenciaId)
        {
            const string sql = @"
                SELECT 
                    td.""Cod"", td.""Transferencia"", td.""Expediente"", td.""NumeroOrden"",
                    td.""Folios"", td.""FechaInicial"", td.""FechaFinal"", td.""Soporte"",
                    td.""FrecuenciaConsulta"", td.""Observaciones"", td.""Estado"",
                    c.""Nombre"" AS ""NombreExpediente"",
                    c.""CodigoExpediente"",
                    trd.""Nombre"" AS ""NombreSerie"",
                    trd.""Codigo"" AS ""CodigoSerie"",
                    trdp.""Nombre"" AS ""NombreSubserie"",
                    trdp.""Codigo"" AS ""CodigoSubserie""
                FROM documentos.""Transferencias_Detalle"" td
                INNER JOIN documentos.""Carpetas"" c ON td.""Expediente"" = c.""Cod""
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" trd ON c.""SerieRaiz"" = trd.""Cod""
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" trdp ON c.""TRD"" = trdp.""Cod"" AND trdp.""TRDPadre"" IS NOT NULL
                WHERE td.""Transferencia"" = @TransferenciaId AND td.""Estado"" = true
                ORDER BY td.""NumeroOrden"", c.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<TransferenciaDetalle>(sql, new { TransferenciaId = transferenciaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo detalle de transferencia {TransferenciaId}", transferenciaId);
                return Enumerable.Empty<TransferenciaDetalle>();
            }
        }

        public async Task<IEnumerable<ExpedienteCandidato>> ObtenerExpedientesCandidatosAsync(long entidadId, FiltrosExpedientesCandidatosRequest filtros)
        {
            // Expedientes cerrados que cumplen tiempo de retención según TRD
            var sql = @"
                SELECT 
                    c.""Cod"", c.""Nombre"", c.""CodigoExpediente"", c.""FechaCierre"",
                    c.""NumeroFolios"", c.""Soporte"", c.""FechaDocumentoInicial"", c.""FechaDocumentoFinal"",
                    c.""TRD"",
                    trd.""Nombre"" AS ""NombreTRD"",
                    trd.""Codigo"" AS ""CodigoTRD"",
                    trd.""TiempoGestion"",
                    trd.""TiempoCentral"",
                    trd.""DisposicionFinal"",
                    EXTRACT(DAY FROM (CURRENT_DATE - c.""FechaCierre""))::INT AS ""DiasEnArchivo"",
                    CASE 
                        WHEN @TipoTransferencia = 'P' THEN 
                            EXTRACT(DAY FROM (CURRENT_DATE - c.""FechaCierre""))::INT >= COALESCE(trd.""TiempoGestion"", 0) * 365
                        ELSE
                            EXTRACT(DAY FROM (CURRENT_DATE - c.""FechaCierre""))::INT >= (COALESCE(trd.""TiempoGestion"", 0) + COALESCE(trd.""TiempoCentral"", 0)) * 365
                    END AS ""ListoParaTransferir""
                FROM documentos.""Carpetas"" c
                INNER JOIN documentos.""Oficinas_TRD"" ot ON c.""SerieRaiz"" = ot.""TRD"" AND ot.""Entidad"" = @EntidadId
                LEFT JOIN documentos.""Tablas_Retencion_Documental"" trd ON c.""SerieRaiz"" = trd.""Cod""
                WHERE c.""Estado"" = true
                  AND c.""TipoCarpeta"" = 3 -- Solo expedientes
                  AND c.""EstadoCarpeta"" = 2 -- Solo cerrados
                  AND c.""FechaCierre"" IS NOT NULL
                  AND ot.""Oficina"" = @Oficina
                  AND ot.""Estado"" = true
                  -- No incluir expedientes ya en transferencia activa
                  AND NOT EXISTS (
                      SELECT 1 FROM documentos.""Transferencias_Detalle"" td
                      INNER JOIN documentos.""Transferencias_Documentales"" t ON td.""Transferencia"" = t.""Cod""
                      WHERE td.""Expediente"" = c.""Cod"" 
                        AND td.""Estado"" = true 
                        AND t.""Estado"" = true
                        AND t.""EstadoTransferencia"" NOT IN (4, 5) -- No rechazadas ni ejecutadas
                  )";

            var parameters = new DynamicParameters();
            parameters.Add("EntidadId", entidadId);
            parameters.Add("Oficina", filtros.Oficina);
            parameters.Add("TipoTransferencia", filtros.TipoTransferencia);

            if (filtros.Serie.HasValue)
            {
                sql += @" AND c.""SerieRaiz"" = @Serie";
                parameters.Add("Serie", filtros.Serie.Value);
            }

            sql += @" ORDER BY trd.""Codigo"", c.""FechaCierre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var resultados = await connection.QueryAsync<ExpedienteCandidato>(sql, parameters);

                if (filtros.SoloListos)
                    return resultados.Where(e => e.ListoParaTransferir);

                return resultados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo expedientes candidatos");
                return Enumerable.Empty<ExpedienteCandidato>();
            }
        }

        // ==================== CRUD TRANSFERENCIA ====================

        public async Task<ResultadoTransferencia> CrearAsync(long entidadId, long usuarioId, CrearTransferenciaRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_CrearTransferencia""(
                    p_Entidad := @Entidad,
                    p_TipoTransferencia := @TipoTransferencia,
                    p_OficinaOrigen := @OficinaOrigen,
                    p_ArchivoDestino := @ArchivoDestino,
                    p_Usuario := @Usuario,
                    p_Observaciones := @Observaciones
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoTransferencia>(sql, new
                {
                    Entidad = entidadId,
                    request.TipoTransferencia,
                    request.OficinaOrigen,
                    request.ArchivoDestino,
                    Usuario = usuarioId,
                    request.Observaciones
                });

                return resultado ?? new ResultadoTransferencia { Exito = false, Mensaje = "Error al crear la transferencia" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando transferencia");
                return new ResultadoTransferencia { Exito = false, Mensaje = "Error al crear la transferencia: " + ex.Message };
            }
        }

        public async Task<(bool Success, string? Error)> EliminarAsync(long transferenciaId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar estado - solo se pueden eliminar en borrador
                var estado = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""EstadoTransferencia"" FROM documentos.""Transferencias_Documentales"" 
                      WHERE ""Cod"" = @Id AND ""Estado"" = true",
                    new { Id = transferenciaId });

                if (!estado.HasValue)
                    return (false, "Transferencia no encontrada");

                if (estado.Value != 1) // 1 = Borrador
                    return (false, "Solo se pueden eliminar transferencias en estado Borrador");

                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    // Eliminar detalles
                    await connection.ExecuteAsync(
                        @"UPDATE documentos.""Transferencias_Detalle"" SET ""Estado"" = false WHERE ""Transferencia"" = @Id",
                        new { Id = transferenciaId }, transaction);

                    // Eliminar transferencia
                    await connection.ExecuteAsync(
                        @"UPDATE documentos.""Transferencias_Documentales"" SET ""Estado"" = false WHERE ""Cod"" = @Id",
                        new { Id = transferenciaId }, transaction);

                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando transferencia {TransferenciaId}", transferenciaId);
                return (false, "Error al eliminar la transferencia: " + ex.Message);
            }
        }

        // ==================== GESTIÓN DE EXPEDIENTES ====================

        public async Task<ResultadoTransferencia> AgregarExpedienteAsync(long transferenciaId, long usuarioId, AgregarExpedienteTransferenciaRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_AgregarExpedienteTransferencia""(
                    p_Transferencia := @Transferencia,
                    p_Expediente := @Expediente,
                    p_Folios := @Folios,
                    p_FechaInicial := @FechaInicial,
                    p_FechaFinal := @FechaFinal,
                    p_Soporte := @Soporte,
                    p_FrecuenciaConsulta := @FrecuenciaConsulta,
                    p_Observaciones := @Observaciones
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoTransferencia>(sql, new
                {
                    Transferencia = transferenciaId,
                    request.Expediente,
                    request.Folios,
                    request.FechaInicial,
                    request.FechaFinal,
                    request.Soporte,
                    request.FrecuenciaConsulta,
                    request.Observaciones
                });

                return resultado ?? new ResultadoTransferencia { Exito = false, Mensaje = "Error al agregar expediente" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error agregando expediente a transferencia {TransferenciaId}", transferenciaId);
                return new ResultadoTransferencia { Exito = false, Mensaje = "Error al agregar expediente: " + ex.Message };
            }
        }

        public async Task<(bool Success, string? Error)> RemoverExpedienteAsync(long transferenciaId, long expedienteId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar estado de la transferencia
                var estado = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""EstadoTransferencia"" FROM documentos.""Transferencias_Documentales"" 
                      WHERE ""Cod"" = @Id AND ""Estado"" = true",
                    new { Id = transferenciaId });

                if (!estado.HasValue)
                    return (false, "Transferencia no encontrada");

                if (estado.Value != 1) // 1 = Borrador
                    return (false, "Solo se pueden modificar transferencias en estado Borrador");

                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    var affected = await connection.ExecuteAsync(
                        @"UPDATE documentos.""Transferencias_Detalle"" SET ""Estado"" = false 
                          WHERE ""Transferencia"" = @TransferenciaId AND ""Expediente"" = @ExpedienteId AND ""Estado"" = true",
                        new { TransferenciaId = transferenciaId, ExpedienteId = expedienteId }, transaction);

                    if (affected == 0)
                    {
                        await transaction.RollbackAsync();
                        return (false, "El expediente no está en esta transferencia");
                    }

                    // Actualizar totales
                    await ActualizarTotalesTransferenciaAsync(connection, transaction, transferenciaId);

                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removiendo expediente de transferencia {TransferenciaId}", transferenciaId);
                return (false, "Error al remover expediente: " + ex.Message);
            }
        }

        // ==================== FLUJO DE TRANSFERENCIA ====================

        public async Task<ResultadoTransferencia> EnviarAsync(long transferenciaId, long usuarioId, EnviarTransferenciaRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar estado y expedientes
                var transferencia = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT ""EstadoTransferencia"", ""TotalExpedientes"" FROM documentos.""Transferencias_Documentales"" 
                      WHERE ""Cod"" = @Id AND ""Estado"" = true",
                    new { Id = transferenciaId });

                if (transferencia == null)
                    return new ResultadoTransferencia { Exito = false, Mensaje = "Transferencia no encontrada" };

                if (transferencia.EstadoTransferencia != 1)
                    return new ResultadoTransferencia { Exito = false, Mensaje = "Solo se pueden enviar transferencias en estado Borrador" };

                if (transferencia.TotalExpedientes == 0)
                    return new ResultadoTransferencia { Exito = false, Mensaje = "La transferencia no tiene expedientes" };

                await connection.ExecuteAsync(
                    @"UPDATE documentos.""Transferencias_Documentales"" SET
                        ""EstadoTransferencia"" = 2, -- Enviada
                        ""FechaEnvio"" = @Fecha,
                        ""EnviadoPor"" = @Usuario,
                        ""Observaciones"" = COALESCE(@Observaciones, ""Observaciones"")
                      WHERE ""Cod"" = @Id",
                    new { Id = transferenciaId, Fecha = DateTime.Now, Usuario = usuarioId, request.Observaciones });

                return new ResultadoTransferencia { Exito = true, Mensaje = "Transferencia enviada correctamente", TransferenciaCod = transferenciaId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando transferencia {TransferenciaId}", transferenciaId);
                return new ResultadoTransferencia { Exito = false, Mensaje = "Error al enviar la transferencia: " + ex.Message };
            }
        }

        public async Task<ResultadoTransferencia> RecibirAsync(long transferenciaId, long usuarioId, RecibirTransferenciaRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var estado = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""EstadoTransferencia"" FROM documentos.""Transferencias_Documentales"" 
                      WHERE ""Cod"" = @Id AND ""Estado"" = true",
                    new { Id = transferenciaId });

                if (!estado.HasValue)
                    return new ResultadoTransferencia { Exito = false, Mensaje = "Transferencia no encontrada" };

                if (estado.Value != 2) // 2 = Enviada
                    return new ResultadoTransferencia { Exito = false, Mensaje = "Solo se pueden recibir transferencias en estado Enviada" };

                await connection.ExecuteAsync(
                    @"UPDATE documentos.""Transferencias_Documentales"" SET
                        ""EstadoTransferencia"" = 3, -- Recibida
                        ""FechaRecepcion"" = @Fecha,
                        ""RecibidoPor"" = @Usuario,
                        ""Observaciones"" = COALESCE(@Observaciones, ""Observaciones"")
                      WHERE ""Cod"" = @Id",
                    new { Id = transferenciaId, Fecha = DateTime.Now, Usuario = usuarioId, request.Observaciones });

                return new ResultadoTransferencia { Exito = true, Mensaje = "Transferencia recibida correctamente", TransferenciaCod = transferenciaId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recibiendo transferencia {TransferenciaId}", transferenciaId);
                return new ResultadoTransferencia { Exito = false, Mensaje = "Error al recibir la transferencia: " + ex.Message };
            }
        }

        public async Task<ResultadoTransferencia> RechazarAsync(long transferenciaId, long usuarioId, RechazarTransferenciaRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var estado = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""EstadoTransferencia"" FROM documentos.""Transferencias_Documentales"" 
                      WHERE ""Cod"" = @Id AND ""Estado"" = true",
                    new { Id = transferenciaId });

                if (!estado.HasValue)
                    return new ResultadoTransferencia { Exito = false, Mensaje = "Transferencia no encontrada" };

                if (estado.Value != 2) // 2 = Enviada
                    return new ResultadoTransferencia { Exito = false, Mensaje = "Solo se pueden rechazar transferencias en estado Enviada" };

                await connection.ExecuteAsync(
                    @"UPDATE documentos.""Transferencias_Documentales"" SET
                        ""EstadoTransferencia"" = 4, -- Rechazada
                        ""RecibidoPor"" = @Usuario,
                        ""ObservacionesRechazo"" = @Motivo
                      WHERE ""Cod"" = @Id",
                    new { Id = transferenciaId, Usuario = usuarioId, request.Motivo });

                return new ResultadoTransferencia { Exito = true, Mensaje = "Transferencia rechazada", TransferenciaCod = transferenciaId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rechazando transferencia {TransferenciaId}", transferenciaId);
                return new ResultadoTransferencia { Exito = false, Mensaje = "Error al rechazar la transferencia: " + ex.Message };
            }
        }

        public async Task<ResultadoTransferencia> EjecutarAsync(long transferenciaId, long usuarioId)
        {
            const string sql = @"SELECT * FROM documentos.""F_EjecutarTransferencia""(@TransferenciaId, @UsuarioId)";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoTransferencia>(sql,
                    new { TransferenciaId = transferenciaId, UsuarioId = usuarioId });

                return resultado ?? new ResultadoTransferencia { Exito = false, Mensaje = "Error al ejecutar la transferencia" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando transferencia {TransferenciaId}", transferenciaId);
                return new ResultadoTransferencia { Exito = false, Mensaje = "Error al ejecutar la transferencia: " + ex.Message };
            }
        }

        // ==================== HELPERS ====================

        private async Task ActualizarTotalesTransferenciaAsync(Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, long transferenciaId)
        {
            await connection.ExecuteAsync(@"
                UPDATE documentos.""Transferencias_Documentales"" SET
                    ""TotalExpedientes"" = (SELECT COUNT(*) FROM documentos.""Transferencias_Detalle"" WHERE ""Transferencia"" = @Id AND ""Estado"" = true),
                    ""TotalFolios"" = (SELECT COALESCE(SUM(""Folios""), 0) FROM documentos.""Transferencias_Detalle"" WHERE ""Transferencia"" = @Id AND ""Estado"" = true)
                WHERE ""Cod"" = @Id",
                new { Id = transferenciaId }, transaction);
        }
    }
}