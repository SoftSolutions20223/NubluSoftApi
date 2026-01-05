using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class TercerosService : ITercerosService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<TercerosService> _logger;

        public TercerosService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<TercerosService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<IEnumerable<Tercero>> ObtenerConFiltrosAsync(long entidadId, FiltrosTercerosRequest filtros)
        {
            var sql = @"
                SELECT 
                    t.""Cod"", t.""Entidad"", t.""TipoTercero"", t.""TipoDocumento"", t.""Documento"",
                    t.""DigitoVerificacion"", t.""Nombre"", t.""RazonSocial"", t.""Direccion"",
                    t.""Ciudad"", t.""Departamento"", t.""Pais"", t.""Telefono"", t.""Celular"",
                    t.""Correo"", t.""SitioWeb"", t.""RepresentanteLegal"", t.""DocumentoRepresentante"",
                    t.""CargoContacto"", t.""NombreContacto"", t.""TelefonoContacto"", t.""CorreoContacto"",
                    t.""Observaciones"", t.""Estado"", t.""FechaCreacion"", t.""CreadoPor"",
                    t.""NotificarCorreo"", t.""NotificarSMS"",
                    CASE t.""TipoTercero"" 
                        WHEN 'N' THEN 'Persona Natural'
                        WHEN 'J' THEN 'Persona Jurídica'
                        WHEN 'E' THEN 'Entidad Pública'
                    END AS ""NombreTipoTercero"",
                    td.""Nombre"" AS ""NombreTipoDocumento"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreCreadoPor""
                FROM documentos.""Terceros"" t
                LEFT JOIN documentos.""Tipos_Documento_Identidad"" td ON t.""TipoDocumento"" = td.""Cod""
                LEFT JOIN documentos.""Usuarios"" u ON t.""CreadoPor"" = u.""Cod""
                WHERE t.""Entidad"" = @EntidadId";

            var parameters = new DynamicParameters();
            parameters.Add("EntidadId", entidadId);

            if (filtros.SoloActivos)
                sql += @" AND t.""Estado"" = true";

            if (!string.IsNullOrWhiteSpace(filtros.TipoTercero))
            {
                sql += @" AND t.""TipoTercero"" = @TipoTercero";
                parameters.Add("TipoTercero", filtros.TipoTercero);
            }

            if (!string.IsNullOrWhiteSpace(filtros.TipoDocumento))
            {
                sql += @" AND t.""TipoDocumento"" = @TipoDocumento";
                parameters.Add("TipoDocumento", filtros.TipoDocumento);
            }

            if (!string.IsNullOrWhiteSpace(filtros.Ciudad))
            {
                sql += @" AND t.""Ciudad"" ILIKE @Ciudad";
                parameters.Add("Ciudad", $"%{filtros.Ciudad}%");
            }

            if (!string.IsNullOrWhiteSpace(filtros.Departamento))
            {
                sql += @" AND t.""Departamento"" ILIKE @Departamento";
                parameters.Add("Departamento", $"%{filtros.Departamento}%");
            }

            if (!string.IsNullOrWhiteSpace(filtros.Busqueda))
            {
                sql += @" AND (
                    t.""Nombre"" ILIKE @Busqueda 
                    OR t.""RazonSocial"" ILIKE @Busqueda 
                    OR t.""Documento"" ILIKE @Busqueda 
                    OR t.""Correo"" ILIKE @Busqueda
                    OR t.""NombreContacto"" ILIKE @Busqueda
                )";
                parameters.Add("Busqueda", $"%{filtros.Busqueda}%");
            }

            sql += @" ORDER BY COALESCE(t.""RazonSocial"", t.""Nombre"")";

            if (filtros.Limite.HasValue)
            {
                sql += @" LIMIT @Limite";
                parameters.Add("Limite", filtros.Limite.Value);
            }

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Tercero>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo terceros con filtros");
                return Enumerable.Empty<Tercero>();
            }
        }

        public async Task<Tercero?> ObtenerPorIdAsync(long terceroId)
        {
            const string sql = @"
                SELECT 
                    t.""Cod"", t.""Entidad"", t.""TipoTercero"", t.""TipoDocumento"", t.""Documento"",
                    t.""DigitoVerificacion"", t.""Nombre"", t.""RazonSocial"", t.""Direccion"",
                    t.""Ciudad"", t.""Departamento"", t.""Pais"", t.""Telefono"", t.""Celular"",
                    t.""Correo"", t.""SitioWeb"", t.""RepresentanteLegal"", t.""DocumentoRepresentante"",
                    t.""CargoContacto"", t.""NombreContacto"", t.""TelefonoContacto"", t.""CorreoContacto"",
                    t.""Observaciones"", t.""Estado"", t.""FechaCreacion"", t.""CreadoPor"",
                    t.""FechaModificacion"", t.""ModificadoPor"", t.""NotificarCorreo"", t.""NotificarSMS"",
                    CASE t.""TipoTercero"" 
                        WHEN 'N' THEN 'Persona Natural'
                        WHEN 'J' THEN 'Persona Jurídica'
                        WHEN 'E' THEN 'Entidad Pública'
                    END AS ""NombreTipoTercero"",
                    td.""Nombre"" AS ""NombreTipoDocumento"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreCreadoPor""
                FROM documentos.""Terceros"" t
                LEFT JOIN documentos.""Tipos_Documento_Identidad"" td ON t.""TipoDocumento"" = td.""Cod""
                LEFT JOIN documentos.""Usuarios"" u ON t.""CreadoPor"" = u.""Cod""
                WHERE t.""Cod"" = @TerceroId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<Tercero>(sql, new { TerceroId = terceroId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tercero {TerceroId}", terceroId);
                return null;
            }
        }

        public async Task<Tercero?> ObtenerPorDocumentoAsync(long entidadId, string tipoDocumento, string documento)
        {
            const string sql = @"
                SELECT ""Cod"" FROM documentos.""Terceros"" 
                WHERE ""Entidad"" = @EntidadId 
                  AND ""TipoDocumento"" = @TipoDocumento 
                  AND ""Documento"" = @Documento
                  AND ""Estado"" = true";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var cod = await connection.QueryFirstOrDefaultAsync<long?>(sql,
                    new { EntidadId = entidadId, TipoDocumento = tipoDocumento, Documento = documento });

                if (cod.HasValue)
                    return await ObtenerPorIdAsync(cod.Value);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tercero por documento {Documento}", documento);
                return null;
            }
        }

        public async Task<TerceroConEstadisticas?> ObtenerConEstadisticasAsync(long terceroId)
        {
            const string sql = @"
                SELECT 
                    t.""Cod"", t.""Entidad"", t.""TipoTercero"", t.""TipoDocumento"", t.""Documento"",
                    t.""DigitoVerificacion"", t.""Nombre"", t.""RazonSocial"", t.""Direccion"",
                    t.""Ciudad"", t.""Departamento"", t.""Pais"", t.""Telefono"", t.""Celular"",
                    t.""Correo"", t.""Estado"", t.""FechaCreacion"", t.""NotificarCorreo"", t.""NotificarSMS"",
                    CASE t.""TipoTercero"" 
                        WHEN 'N' THEN 'Persona Natural'
                        WHEN 'J' THEN 'Persona Jurídica'
                        WHEN 'E' THEN 'Entidad Pública'
                    END AS ""NombreTipoTercero"",
                    td.""Nombre"" AS ""NombreTipoDocumento"",
                    (SELECT COUNT(*) FROM documentos.""Radicados"" r 
                     WHERE r.""Tercero"" = t.""Cod"" AND r.""TipoComunicacion"" = 'E' AND r.""Estado"" = true) AS ""TotalRadicadosEntrada"",
                    (SELECT COUNT(*) FROM documentos.""Radicados"" r 
                     WHERE r.""Tercero"" = t.""Cod"" AND r.""TipoComunicacion"" = 'S' AND r.""Estado"" = true) AS ""TotalRadicadosSalida"",
                    (SELECT MAX(r.""FechaRadicacion"") FROM documentos.""Radicados"" r 
                     WHERE r.""Tercero"" = t.""Cod"" AND r.""Estado"" = true) AS ""UltimoRadicado""
                FROM documentos.""Terceros"" t
                LEFT JOIN documentos.""Tipos_Documento_Identidad"" td ON t.""TipoDocumento"" = td.""Cod""
                WHERE t.""Cod"" = @TerceroId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<TerceroConEstadisticas>(sql, new { TerceroId = terceroId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tercero con estadísticas {TerceroId}", terceroId);
                return null;
            }
        }

        public async Task<IEnumerable<Radicado>> ObtenerRadicadosAsync(long terceroId, int? limite = 20)
        {
            var sql = @"
                SELECT 
                    r.""Cod"", r.""NumeroRadicado"", r.""TipoComunicacion"", r.""Asunto"",
                    r.""FechaRadicacion"", r.""FechaVencimiento"", r.""EstadoRadicado"",
                    tc.""Nombre"" AS ""NombreTipoComunicacion"",
                    er.""Nombre"" AS ""NombreEstadoRadicado"",
                    er.""Color"" AS ""ColorEstado""
                FROM documentos.""Radicados"" r
                LEFT JOIN documentos.""Tipos_Comunicacion"" tc ON r.""TipoComunicacion"" = tc.""Cod""
                LEFT JOIN documentos.""Estados_Radicado"" er ON r.""EstadoRadicado"" = er.""Cod""
                WHERE r.""Tercero"" = @TerceroId AND r.""Estado"" = true
                ORDER BY r.""FechaRadicacion"" DESC";

            if (limite.HasValue)
                sql += @" LIMIT @Limite";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Radicado>(sql, new { TerceroId = terceroId, Limite = limite });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo radicados de tercero {TerceroId}", terceroId);
                return Enumerable.Empty<Radicado>();
            }
        }

        // ==================== CRUD ====================

        public async Task<(Tercero? Tercero, string? Error)> CrearAsync(long entidadId, long usuarioId, CrearTerceroRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Validar documento único
                if (await ExisteDocumentoAsync(entidadId, request.TipoDocumento, request.Documento))
                    return (null, "Ya existe un tercero con ese tipo y número de documento");

                // Validar nombre según tipo
                if (request.TipoTercero == "N" && string.IsNullOrWhiteSpace(request.Nombre))
                    return (null, "El nombre es requerido para personas naturales");

                if ((request.TipoTercero == "J" || request.TipoTercero == "E") && string.IsNullOrWhiteSpace(request.RazonSocial))
                    return (null, "La razón social es requerida para personas jurídicas y entidades");

                // Obtener siguiente código
                var nextCod = await connection.QueryFirstAsync<long>(
                    @"SELECT COALESCE(MAX(""Cod""), 0) + 1 FROM documentos.""Terceros"" WHERE ""Entidad"" = @EntidadId",
                    new { EntidadId = entidadId });

                const string sqlInsert = @"
                    INSERT INTO documentos.""Terceros"" 
                        (""Cod"", ""Entidad"", ""TipoTercero"", ""TipoDocumento"", ""Documento"", ""DigitoVerificacion"",
                         ""Nombre"", ""RazonSocial"", ""Direccion"", ""Ciudad"", ""Departamento"", ""Pais"",
                         ""Telefono"", ""Celular"", ""Correo"", ""SitioWeb"", ""RepresentanteLegal"", ""DocumentoRepresentante"",
                         ""CargoContacto"", ""NombreContacto"", ""TelefonoContacto"", ""CorreoContacto"",
                         ""Observaciones"", ""Estado"", ""FechaCreacion"", ""CreadoPor"", ""NotificarCorreo"", ""NotificarSMS"")
                    VALUES 
                        (@Cod, @Entidad, @TipoTercero, @TipoDocumento, @Documento, @DigitoVerificacion,
                         @Nombre, @RazonSocial, @Direccion, @Ciudad, @Departamento, @Pais,
                         @Telefono, @Celular, @Correo, @SitioWeb, @RepresentanteLegal, @DocumentoRepresentante,
                         @CargoContacto, @NombreContacto, @TelefonoContacto, @CorreoContacto,
                         @Observaciones, true, @FechaCreacion, @CreadoPor, @NotificarCorreo, @NotificarSMS)";

                await connection.ExecuteAsync(sqlInsert, new
                {
                    Cod = nextCod,
                    Entidad = entidadId,
                    request.TipoTercero,
                    request.TipoDocumento,
                    request.Documento,
                    request.DigitoVerificacion,
                    request.Nombre,
                    request.RazonSocial,
                    request.Direccion,
                    request.Ciudad,
                    request.Departamento,
                    request.Pais,
                    request.Telefono,
                    request.Celular,
                    request.Correo,
                    request.SitioWeb,
                    request.RepresentanteLegal,
                    request.DocumentoRepresentante,
                    request.CargoContacto,
                    request.NombreContacto,
                    request.TelefonoContacto,
                    request.CorreoContacto,
                    request.Observaciones,
                    FechaCreacion = DateTime.Now,
                    CreadoPor = usuarioId,
                    request.NotificarCorreo,
                    request.NotificarSMS
                });

                var tercero = await ObtenerPorIdAsync(nextCod);
                return (tercero, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando tercero");
                return (null, "Error al crear el tercero: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ActualizarAsync(long terceroId, long usuarioId, ActualizarTerceroRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Terceros"" SET
                    ""Nombre"" = @Nombre,
                    ""RazonSocial"" = @RazonSocial,
                    ""Direccion"" = @Direccion,
                    ""Ciudad"" = @Ciudad,
                    ""Departamento"" = @Departamento,
                    ""Pais"" = @Pais,
                    ""Telefono"" = @Telefono,
                    ""Celular"" = @Celular,
                    ""Correo"" = @Correo,
                    ""SitioWeb"" = @SitioWeb,
                    ""RepresentanteLegal"" = @RepresentanteLegal,
                    ""DocumentoRepresentante"" = @DocumentoRepresentante,
                    ""CargoContacto"" = @CargoContacto,
                    ""NombreContacto"" = @NombreContacto,
                    ""TelefonoContacto"" = @TelefonoContacto,
                    ""CorreoContacto"" = @CorreoContacto,
                    ""Observaciones"" = @Observaciones,
                    ""NotificarCorreo"" = @NotificarCorreo,
                    ""NotificarSMS"" = @NotificarSMS,
                    ""Estado"" = @Estado,
                    ""FechaModificacion"" = @FechaModificacion,
                    ""ModificadoPor"" = @ModificadoPor
                WHERE ""Cod"" = @TerceroId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(sql, new
                {
                    TerceroId = terceroId,
                    request.Nombre,
                    request.RazonSocial,
                    request.Direccion,
                    request.Ciudad,
                    request.Departamento,
                    request.Pais,
                    request.Telefono,
                    request.Celular,
                    request.Correo,
                    request.SitioWeb,
                    request.RepresentanteLegal,
                    request.DocumentoRepresentante,
                    request.CargoContacto,
                    request.NombreContacto,
                    request.TelefonoContacto,
                    request.CorreoContacto,
                    request.Observaciones,
                    request.NotificarCorreo,
                    request.NotificarSMS,
                    request.Estado,
                    FechaModificacion = DateTime.Now,
                    ModificadoPor = usuarioId
                });

                if (affected == 0)
                    return (false, "Tercero no encontrado");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando tercero {TerceroId}", terceroId);
                return (false, "Error al actualizar el tercero: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> EliminarAsync(long terceroId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar si tiene radicados asociados
                var tieneRadicados = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""Cod"" FROM documentos.""Radicados"" WHERE ""Tercero"" = @TerceroId AND ""Estado"" = true LIMIT 1",
                    new { TerceroId = terceroId });

                if (tieneRadicados.HasValue)
                    return (false, "No se puede eliminar un tercero que tiene radicados asociados");

                // Soft delete
                var affected = await connection.ExecuteAsync(
                    @"UPDATE documentos.""Terceros"" SET ""Estado"" = false WHERE ""Cod"" = @TerceroId",
                    new { TerceroId = terceroId });

                if (affected == 0)
                    return (false, "Tercero no encontrado");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando tercero {TerceroId}", terceroId);
                return (false, "Error al eliminar el tercero: " + ex.Message);
            }
        }

        // ==================== UTILIDADES ====================

        public async Task<bool> ExisteDocumentoAsync(long entidadId, string tipoDocumento, string documento, long? excluirId = null)
        {
            var sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM documentos.""Terceros"" 
                    WHERE ""Entidad"" = @EntidadId 
                      AND ""TipoDocumento"" = @TipoDocumento 
                      AND ""Documento"" = @Documento
                      AND ""Estado"" = true";

            if (excluirId.HasValue)
                sql += @" AND ""Cod"" != @ExcluirId";

            sql += ")";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstAsync<bool>(sql,
                    new { EntidadId = entidadId, TipoDocumento = tipoDocumento, Documento = documento, ExcluirId = excluirId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando existencia de documento {Documento}", documento);
                return false;
            }
        }
    }
}