using Dapper;
using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    public class EntidadesService : IEntidadesService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<EntidadesService> _logger;

        public EntidadesService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<EntidadesService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<ListaEntidadesResponse> ObtenerEntidadesAsync(FiltrosEntidadesRequest? filtros = null)
        {
            var response = new ListaEntidadesResponse
            {
                PaginaActual = filtros?.Pagina ?? 1,
                PorPagina = filtros?.PorPagina ?? 20
            };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var whereClause = "WHERE 1=1";
                var parameters = new DynamicParameters();

                if (filtros != null)
                {
                    if (!string.IsNullOrEmpty(filtros.Busqueda))
                    {
                        whereClause += @" AND (e.""Nombre"" ILIKE @Busqueda OR e.""Nit"" ILIKE @Busqueda OR e.""Correo"" ILIKE @Busqueda)";
                        parameters.Add("Busqueda", $"%{filtros.Busqueda}%");
                    }

                    if (filtros.Sector.HasValue)
                    {
                        whereClause += @" AND e.""Sector"" = @Sector";
                        parameters.Add("Sector", filtros.Sector.Value);
                    }

                    if (filtros.TipoEntidad.HasValue)
                    {
                        whereClause += @" AND e.""TipoEntidad"" = @TipoEntidad";
                        parameters.Add("TipoEntidad", filtros.TipoEntidad.Value);
                    }

                    if (filtros.SoloActivas == true)
                    {
                        whereClause += @" AND e.""FechaLimite"" >= CURRENT_DATE";
                    }

                    if (filtros.PlanVencido == true)
                    {
                        whereClause += @" AND e.""FechaLimite"" < CURRENT_DATE";
                    }

                    if (filtros.PlanPorVencer == true)
                    {
                        whereClause += @" AND e.""FechaLimite"" BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '15 days'";
                    }
                }

                // Contar total
                var countSql = $@"SELECT COUNT(*) FROM usuarios.""Entidades"" e {whereClause}";
                response.TotalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
                response.TotalPaginas = (int)Math.Ceiling((double)response.TotalItems / response.PorPagina);

                // Obtener página
                var offset = (response.PaginaActual - 1) * response.PorPagina;
                parameters.Add("Offset", offset);
                parameters.Add("Limit", response.PorPagina);

                var sql = $@"
                    SELECT
                        e.""Cod"",
                        e.""Nombre"",
                        e.""Nit"",
                        e.""Correo"",
                        e.""FechaLimite""::timestamp AS ""FechaLimite"",
                        CASE WHEN e.""FechaLimite"" >= CURRENT_DATE THEN true ELSE false END AS ""PlanActivo"",
                        (SELECT COUNT(*) FROM usuarios.""Usuarios"" u WHERE u.""Entidad"" = e.""Cod"") AS ""TotalUsuarios""
                    FROM usuarios.""Entidades"" e
                    {whereClause}
                    ORDER BY e.""Nombre""
                    OFFSET @Offset LIMIT @Limit";

                response.Entidades = await connection.QueryAsync<EntidadResumenDto>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo entidades");
            }

            return response;
        }

        public async Task<EntidadDto?> ObtenerPorIdAsync(long entidadId)
        {
            const string sql = @"
                SELECT
                    e.""Cod"",
                    e.""Nombre"",
                    e.""Nit"",
                    e.""Telefono"",
                    e.""Direccion"",
                    e.""Correo"",
                    e.""FechaLimite""::timestamp AS ""FechaLimite"",
                    e.""Bd"",
                    b.""Nombre"" AS ""NombreBd"",
                    e.""Url"",
                    e.""Coleccion"",
                    e.""Sector"",
                    s.""Nombre"" AS ""NombreSector"",
                    e.""TipoEntidad"",
                    te.""Nombre"" AS ""NombreTipoEntidad"",
                    (SELECT COUNT(*) FROM usuarios.""Usuarios"" u WHERE u.""Entidad"" = e.""Cod"") AS ""TotalUsuarios"",
                    (SELECT COUNT(*) FROM usuarios.""Usuarios"" u WHERE u.""Entidad"" = e.""Cod"" AND u.""Estado"" = true) AS ""UsuariosActivos"",
                    (SELECT COUNT(*) FROM documentos.""Oficinas"" o WHERE o.""Entidad"" = e.""Cod"" AND o.""Estado"" = true) AS ""TotalOficinas"",
                    (SELECT COUNT(*) FROM documentos.""Carpetas"" c WHERE c.""Entidad"" = e.""Cod"" AND c.""Estado"" = true) AS ""TotalCarpetas""
                FROM usuarios.""Entidades"" e
                LEFT JOIN usuarios.""BasesDatos"" b ON e.""Bd"" = b.""Cod""
                LEFT JOIN documentos.""Sectores"" s ON e.""Sector"" = s.""Cod""
                LEFT JOIN documentos.""Tipos_Entidad"" te ON e.""TipoEntidad"" = te.""Cod""
                WHERE e.""Cod"" = @EntidadId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<EntidadDto>(sql, new { EntidadId = entidadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo entidad {EntidadId}", entidadId);
                return null;
            }
        }

        public async Task<EntidadDto?> ObtenerPorNitAsync(string nit)
        {
            const string sql = @"
                SELECT ""Cod"" FROM usuarios.""Entidades"" WHERE ""Nit"" = @Nit LIMIT 1";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var cod = await connection.ExecuteScalarAsync<long?>(sql, new { Nit = nit });

                if (!cod.HasValue)
                    return null;

                return await ObtenerPorIdAsync(cod.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo entidad por NIT {Nit}", nit);
                return null;
            }
        }

        public async Task<IEnumerable<PlanEntidadDto>> ObtenerPlanesEntidadAsync(long entidadId)
        {
            const string sql = @"
                SELECT 
                    pe.""Cod"",
                    pe.""Plan"",
                    p.""Nombre"" AS ""NombrePlan"",
                    pe.""Valor"",
                    p.""NumeroUsuarios"",
                    pe.""FechaInicio"",
                    pe.""FechaFin"",
                    pe.""Estado""
                FROM usuarios.""Planes_Entidades"" pe
                LEFT JOIN usuarios.""Planes"" p ON pe.""Plan"" = p.""Cod""
                WHERE pe.""Entidad"" = @EntidadId
                ORDER BY pe.""FechaInicio"" DESC";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<PlanEntidadDto>(sql, new { EntidadId = entidadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo planes de entidad {EntidadId}", entidadId);
                return Enumerable.Empty<PlanEntidadDto>();
            }
        }

        public async Task<ResumenEntidadesDto> ObtenerResumenAsync()
        {
            var resumen = new ResumenEntidadesDto();

            const string sql = @"
                SELECT 
                    COUNT(*) AS ""TotalEntidades"",
                    COUNT(*) FILTER (WHERE ""FechaLimite"" >= CURRENT_DATE) AS ""EntidadesActivas"",
                    COUNT(*) FILTER (WHERE ""FechaLimite"" < CURRENT_DATE) AS ""PlanesVencidos"",
                    COUNT(*) FILTER (WHERE ""FechaLimite"" BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '15 days') AS ""PlanesPorVencer""
                FROM usuarios.""Entidades""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(sql);

                if (stats != null)
                {
                    resumen.TotalEntidades = (int)(stats.TotalEntidades ?? 0);
                    resumen.EntidadesActivas = (int)(stats.EntidadesActivas ?? 0);
                    resumen.PlanesVencidos = (int)(stats.PlanesVencidos ?? 0);
                    resumen.PlanesPorVencer = (int)(stats.PlanesPorVencer ?? 0);
                }

                // Total usuarios
                resumen.TotalUsuarios = await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM usuarios.""Usuarios""");

                // Entidades por vencer
                var porVencer = await ObtenerEntidadesAsync(new FiltrosEntidadesRequest { PlanPorVencer = true, PorPagina = 10 });
                resumen.EntidadesPorVencer = porVencer.Entidades;

                // Entidades vencidas
                var vencidas = await ObtenerEntidadesAsync(new FiltrosEntidadesRequest { PlanVencido = true, PorPagina = 10 });
                resumen.EntidadesVencidas = vencidas.Entidades;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen de entidades");
            }

            return resumen;
        }

        public async Task<bool> ExisteNitAsync(string nit, long? excluirEntidadId = null)
        {
            var sql = @"SELECT EXISTS(SELECT 1 FROM usuarios.""Entidades"" WHERE ""Nit"" = @Nit";
            var parameters = new DynamicParameters();
            parameters.Add("Nit", nit);

            if (excluirEntidadId.HasValue)
            {
                sql += @" AND ""Cod"" != @ExcluirId";
                parameters.Add("ExcluirId", excluirEntidadId.Value);
            }

            sql += ")";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<bool>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando NIT {Nit}", nit);
                return false;
            }
        }

        // ==================== OPERACIONES ====================

        public async Task<ResultadoEntidadDto> CrearAsync(CrearEntidadRequest request)
        {
            try
            {
                // Verificar NIT único
                if (await ExisteNitAsync(request.Nit))
                {
                    return new ResultadoEntidadDto
                    {
                        Exito = false,
                        Mensaje = $"Ya existe una entidad con el NIT {request.Nit}"
                    };
                }

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener siguiente código
                var nuevoCod = await connection.ExecuteScalarAsync<long>(
                    @"SELECT COALESCE(MAX(""Cod""), 0) + 1 FROM usuarios.""Entidades""");

                // Insertar entidad
                const string insertSql = @"
                    INSERT INTO usuarios.""Entidades"" (
                        ""Cod"", ""Nombre"", ""Nit"", ""Telefono"", ""Direccion"", 
                        ""Correo"", ""Url"", ""Sector"", ""TipoEntidad"", ""FechaLimite""
                    ) VALUES (
                        @Cod, @Nombre, @Nit, @Telefono, @Direccion,
                        @Correo, @Url, @Sector, @TipoEntidad, CURRENT_DATE + INTERVAL '1 day' * @DiasVigencia
                    )";

                await connection.ExecuteAsync(insertSql, new
                {
                    Cod = nuevoCod,
                    request.Nombre,
                    request.Nit,
                    request.Telefono,
                    request.Direccion,
                    request.Correo,
                    request.Url,
                    request.Sector,
                    request.TipoEntidad,
                    request.DiasVigencia
                });

                // Crear configuración de firma por defecto
                await connection.ExecuteAsync(@"
                    INSERT INTO documentos.""Configuracion_Firma"" (""Cod"", ""Entidad"")
                    VALUES ((SELECT COALESCE(MAX(""Cod""), 0) + 1 FROM documentos.""Configuracion_Firma""), @EntidadId)",
                    new { EntidadId = nuevoCod });

                var entidad = await ObtenerPorIdAsync(nuevoCod);

                return new ResultadoEntidadDto
                {
                    Exito = true,
                    Mensaje = "Entidad creada exitosamente",
                    EntidadCod = nuevoCod,
                    Entidad = entidad
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando entidad");
                return new ResultadoEntidadDto
                {
                    Exito = false,
                    Mensaje = "Error al crear entidad: " + ex.Message
                };
            }
        }

        public async Task<ResultadoEntidadDto> ActualizarAsync(long entidadId, ActualizarEntidadRequest request)
        {
            try
            {
                // Verificar NIT único si se cambió
                if (!string.IsNullOrEmpty(request.Nit) && await ExisteNitAsync(request.Nit, entidadId))
                {
                    return new ResultadoEntidadDto
                    {
                        Exito = false,
                        Mensaje = $"Ya existe otra entidad con el NIT {request.Nit}"
                    };
                }

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE usuarios.""Entidades""
                    SET 
                        ""Nombre"" = @Nombre,
                        ""Nit"" = COALESCE(@Nit, ""Nit""),
                        ""Telefono"" = @Telefono,
                        ""Direccion"" = @Direccion,
                        ""Correo"" = @Correo,
                        ""Url"" = @Url,
                        ""Sector"" = @Sector,
                        ""TipoEntidad"" = @TipoEntidad
                    WHERE ""Cod"" = @EntidadId";

                var affected = await connection.ExecuteAsync(sql, new
                {
                    EntidadId = entidadId,
                    request.Nombre,
                    request.Nit,
                    request.Telefono,
                    request.Direccion,
                    request.Correo,
                    request.Url,
                    request.Sector,
                    request.TipoEntidad
                });

                if (affected == 0)
                {
                    return new ResultadoEntidadDto
                    {
                        Exito = false,
                        Mensaje = "Entidad no encontrada"
                    };
                }

                var entidad = await ObtenerPorIdAsync(entidadId);

                return new ResultadoEntidadDto
                {
                    Exito = true,
                    Mensaje = "Entidad actualizada correctamente",
                    EntidadCod = entidadId,
                    Entidad = entidad
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando entidad {EntidadId}", entidadId);
                return new ResultadoEntidadDto
                {
                    Exito = false,
                    Mensaje = "Error al actualizar entidad: " + ex.Message
                };
            }
        }

        public async Task<ResultadoEntidadDto> ExtenderPlanAsync(long entidadId, ExtenderPlanRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Extender desde la fecha actual o desde FechaLimite si es futura
                const string sql = @"
                    UPDATE usuarios.""Entidades""
                    SET ""FechaLimite"" = GREATEST(CURRENT_DATE, COALESCE(""FechaLimite"", CURRENT_DATE)) + INTERVAL '1 day' * @Dias
                    WHERE ""Cod"" = @EntidadId";

                var affected = await connection.ExecuteAsync(sql, new { EntidadId = entidadId, request.Dias });

                if (affected == 0)
                {
                    return new ResultadoEntidadDto
                    {
                        Exito = false,
                        Mensaje = "Entidad no encontrada"
                    };
                }

                var entidad = await ObtenerPorIdAsync(entidadId);

                return new ResultadoEntidadDto
                {
                    Exito = true,
                    Mensaje = $"Plan extendido por {request.Dias} días. Nueva fecha límite: {entidad?.FechaLimite:dd/MM/yyyy}",
                    EntidadCod = entidadId,
                    Entidad = entidad
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extendiendo plan de entidad {EntidadId}", entidadId);
                return new ResultadoEntidadDto
                {
                    Exito = false,
                    Mensaje = "Error al extender plan: " + ex.Message
                };
            }
        }

        public async Task<ResultadoEntidadDto> AsignarPlanAsync(long entidadId, AsignarPlanRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que existe el plan
                var plan = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT ""Cod"", ""Nombre"", ""Valor"" FROM usuarios.""Planes"" WHERE ""Cod"" = @PlanId AND ""Estado"" = true",
                    new { request.PlanId });

                if (plan == null)
                {
                    return new ResultadoEntidadDto
                    {
                        Exito = false,
                        Mensaje = "Plan no encontrado o inactivo"
                    };
                }

                var fechaInicio = request.FechaInicio ?? DateTime.Today;
                var fechaFin = fechaInicio.AddDays(request.DiasVigencia);
                var valor = request.ValorPersonalizado ?? (decimal)plan.Valor;

                // Desactivar planes anteriores
                await connection.ExecuteAsync(@"
                    UPDATE usuarios.""Planes_Entidades""
                    SET ""Estado"" = false
                    WHERE ""Entidad"" = @EntidadId AND ""Estado"" = true",
                    new { EntidadId = entidadId });

                // Obtener siguiente código
                var nuevoCod = await connection.ExecuteScalarAsync<long>(
                    @"SELECT COALESCE(MAX(""Cod""), 0) + 1 FROM usuarios.""Planes_Entidades""");

                // Insertar nuevo plan
                await connection.ExecuteAsync(@"
                    INSERT INTO usuarios.""Planes_Entidades"" (
                        ""Cod"", ""Entidad"", ""Plan"", ""Valor"", ""Estado"", ""FechaInicio"", ""FechaFin""
                    ) VALUES (
                        @Cod, @EntidadId, @PlanId, @Valor, true, @FechaInicio, @FechaFin
                    )",
                    new
                    {
                        Cod = nuevoCod,
                        EntidadId = entidadId,
                        request.PlanId,
                        Valor = valor,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    });

                // Actualizar fecha límite en la entidad
                await connection.ExecuteAsync(@"
                    UPDATE usuarios.""Entidades""
                    SET ""FechaLimite"" = @FechaFin
                    WHERE ""Cod"" = @EntidadId",
                    new { EntidadId = entidadId, FechaFin = fechaFin });

                var entidad = await ObtenerPorIdAsync(entidadId);

                return new ResultadoEntidadDto
                {
                    Exito = true,
                    Mensaje = $"Plan '{plan.Nombre}' asignado hasta {fechaFin:dd/MM/yyyy}",
                    EntidadCod = entidadId,
                    Entidad = entidad
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando plan a entidad {EntidadId}", entidadId);
                return new ResultadoEntidadDto
                {
                    Exito = false,
                    Mensaje = "Error al asignar plan: " + ex.Message
                };
            }
        }

        public async Task<ResultadoEntidadDto> DesactivarAsync(long entidadId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que no tiene usuarios activos
                var usuariosActivos = await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM usuarios.""Usuarios"" WHERE ""Entidad"" = @EntidadId AND ""Estado"" = true",
                    new { EntidadId = entidadId });

                if (usuariosActivos > 0)
                {
                    return new ResultadoEntidadDto
                    {
                        Exito = false,
                        Mensaje = $"No se puede desactivar. La entidad tiene {usuariosActivos} usuarios activos"
                    };
                }

                // Establecer fecha límite en el pasado
                await connection.ExecuteAsync(@"
                    UPDATE usuarios.""Entidades""
                    SET ""FechaLimite"" = CURRENT_DATE - INTERVAL '1 day'
                    WHERE ""Cod"" = @EntidadId",
                    new { EntidadId = entidadId });

                var entidad = await ObtenerPorIdAsync(entidadId);

                return new ResultadoEntidadDto
                {
                    Exito = true,
                    Mensaje = "Entidad desactivada",
                    EntidadCod = entidadId,
                    Entidad = entidad
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando entidad {EntidadId}", entidadId);
                return new ResultadoEntidadDto
                {
                    Exito = false,
                    Mensaje = "Error al desactivar entidad: " + ex.Message
                };
            }
        }
    }
}