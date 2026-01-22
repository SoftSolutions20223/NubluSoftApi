using Dapper;
using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    public class DiasFestivosService : IDiasFestivosService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<DiasFestivosService> _logger;

        // Días de la semana en español
        private static readonly string[] DiaSemanaEspanol =
        {
            "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"
        };

        public DiasFestivosService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<DiasFestivosService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        public async Task<IEnumerable<DiaFestivoDto>> ObtenerTodosAsync(FiltrosDiasFestivosRequest? filtros = null)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var whereClause = "WHERE 1=1";
                var parameters = new DynamicParameters();

                if (filtros != null)
                {
                    if (filtros.Anio.HasValue)
                    {
                        whereClause += @" AND EXTRACT(YEAR FROM ""Fecha"") = @Anio";
                        parameters.Add("Anio", filtros.Anio.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filtros.Pais))
                    {
                        whereClause += @" AND ""Pais"" = @Pais";
                        parameters.Add("Pais", filtros.Pais);
                    }

                    if (filtros.SoloFijos.HasValue)
                    {
                        whereClause += @" AND ""EsFijo"" = @EsFijo";
                        parameters.Add("EsFijo", filtros.SoloFijos.Value);
                    }

                    if (filtros.FechaDesde.HasValue)
                    {
                        whereClause += @" AND ""Fecha"" >= @FechaDesde";
                        parameters.Add("FechaDesde", filtros.FechaDesde.Value.Date);
                    }

                    if (filtros.FechaHasta.HasValue)
                    {
                        whereClause += @" AND ""Fecha"" <= @FechaHasta";
                        parameters.Add("FechaHasta", filtros.FechaHasta.Value.Date);
                    }
                }

                var sql = $@"
                    SELECT ""Fecha""::timestamp AS ""Fecha"", ""Nombre"", ""EsFijo"", ""Pais""
                    FROM documentos.""Dias_Festivos""
                    {whereClause}
                    ORDER BY ""Fecha""";

                var festivos = await connection.QueryAsync<DiaFestivoDto>(sql, parameters);

                // Agregar día de la semana
                foreach (var festivo in festivos)
                {
                    festivo.DiaSemana = DiaSemanaEspanol[(int)festivo.Fecha.DayOfWeek];
                }

                return festivos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo días festivos");
                return Enumerable.Empty<DiaFestivoDto>();
            }
        }

        public async Task<DiaFestivoDto?> ObtenerPorFechaAsync(DateTime fecha)
        {
            const string sql = @"
                SELECT ""Fecha""::timestamp AS ""Fecha"", ""Nombre"", ""EsFijo"", ""Pais""
                FROM documentos.""Dias_Festivos""
                WHERE ""Fecha"" = @Fecha";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var festivo = await connection.QueryFirstOrDefaultAsync<DiaFestivoDto>(sql, new { Fecha = fecha.Date });

                if (festivo != null)
                {
                    festivo.DiaSemana = DiaSemanaEspanol[(int)festivo.Fecha.DayOfWeek];
                }

                return festivo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo día festivo {Fecha}", fecha);
                return null;
            }
        }

        public async Task<IEnumerable<DiaFestivoDto>> ObtenerPorAnioAsync(int anio)
        {
            return await ObtenerTodosAsync(new FiltrosDiasFestivosRequest { Anio = anio });
        }

        public async Task<IEnumerable<DiaFestivoDto>> ObtenerProximosAsync(int cantidad = 5)
        {
            const string sql = @"
                SELECT ""Fecha""::timestamp AS ""Fecha"", ""Nombre"", ""EsFijo"", ""Pais""
                FROM documentos.""Dias_Festivos""
                WHERE ""Fecha"" >= CURRENT_DATE
                ORDER BY ""Fecha""
                LIMIT @Cantidad";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var festivos = await connection.QueryAsync<DiaFestivoDto>(sql, new { Cantidad = cantidad });

                foreach (var festivo in festivos)
                {
                    festivo.DiaSemana = DiaSemanaEspanol[(int)festivo.Fecha.DayOfWeek];
                }

                return festivos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo próximos festivos");
                return Enumerable.Empty<DiaFestivoDto>();
            }
        }

        public async Task<ResumenDiasFestivosDto> ObtenerResumenAsync(int? anio = null)
        {
            var anioConsulta = anio ?? DateTime.Now.Year;
            var resumen = new ResumenDiasFestivosDto { Anio = anioConsulta };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Total festivos del año
                resumen.TotalFestivos = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM documentos.""Dias_Festivos""
                    WHERE EXTRACT(YEAR FROM ""Fecha"") = @Anio",
                    new { Anio = anioConsulta });

                // Festivos fijos
                resumen.FestivosFijos = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM documentos.""Dias_Festivos""
                    WHERE EXTRACT(YEAR FROM ""Fecha"") = @Anio AND ""EsFijo"" = true",
                    new { Anio = anioConsulta });

                // Festivos móviles
                resumen.FestivosMoviles = resumen.TotalFestivos - resumen.FestivosFijos;

                // Próximo festivo
                var proximoFestivo = await connection.QueryFirstOrDefaultAsync<DiaFestivoDto>(@"
                    SELECT ""Fecha""::timestamp AS ""Fecha"", ""Nombre"", ""EsFijo"", ""Pais""
                    FROM documentos.""Dias_Festivos""
                    WHERE ""Fecha"" >= CURRENT_DATE
                    ORDER BY ""Fecha""
                    LIMIT 1");

                if (proximoFestivo != null)
                {
                    proximoFestivo.DiaSemana = DiaSemanaEspanol[(int)proximoFestivo.Fecha.DayOfWeek];
                    resumen.ProximoFestivo = proximoFestivo;
                }

                // Festivos del mes actual
                var festivosDelMes = await connection.QueryAsync<DiaFestivoDto>(@"
                    SELECT ""Fecha""::timestamp AS ""Fecha"", ""Nombre"", ""EsFijo"", ""Pais""
                    FROM documentos.""Dias_Festivos""
                    WHERE EXTRACT(YEAR FROM ""Fecha"") = @Anio
                      AND EXTRACT(MONTH FROM ""Fecha"") = @Mes
                    ORDER BY ""Fecha""",
                    new { Anio = DateTime.Now.Year, Mes = DateTime.Now.Month });

                foreach (var festivo in festivosDelMes)
                {
                    festivo.DiaSemana = DiaSemanaEspanol[(int)festivo.Fecha.DayOfWeek];
                }

                resumen.FestivosDelMes = festivosDelMes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen de días festivos");
            }

            return resumen;
        }

        // ==================== VERIFICACIONES ====================

        public async Task<VerificacionDiaHabilDto> VerificarDiaHabilAsync(DateTime fecha)
        {
            var resultado = new VerificacionDiaHabilDto
            {
                Fecha = fecha.Date,
                DiaSemana = DiaSemanaEspanol[(int)fecha.DayOfWeek]
            };

            // Verificar si es fin de semana
            if (fecha.DayOfWeek == DayOfWeek.Saturday)
            {
                resultado.EsDiaHabil = false;
                resultado.Motivo = "Sábado";
                return resultado;
            }

            if (fecha.DayOfWeek == DayOfWeek.Sunday)
            {
                resultado.EsDiaHabil = false;
                resultado.Motivo = "Domingo";
                return resultado;
            }

            // Verificar si es festivo
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var festivo = await connection.QueryFirstOrDefaultAsync<string>(@"
                    SELECT ""Nombre"" FROM documentos.""Dias_Festivos""
                    WHERE ""Fecha"" = @Fecha",
                    new { Fecha = fecha.Date });

                if (!string.IsNullOrEmpty(festivo))
                {
                    resultado.EsDiaHabil = false;
                    resultado.Motivo = $"Festivo: {festivo}";
                    return resultado;
                }

                resultado.EsDiaHabil = true;
                resultado.Motivo = "Día hábil";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando día hábil {Fecha}", fecha);
                resultado.EsDiaHabil = true; // Por defecto asumimos hábil si hay error
                resultado.Motivo = "No se pudo verificar";
            }

            return resultado;
        }

        public async Task<bool> EsFestivoAsync(DateTime fecha)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.ExecuteScalarAsync<bool>(@"
                    SELECT EXISTS(
                        SELECT 1 FROM documentos.""Dias_Festivos"" 
                        WHERE ""Fecha"" = @Fecha
                    )",
                    new { Fecha = fecha.Date });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando si es festivo {Fecha}", fecha);
                return false;
            }
        }

        // ==================== CÁLCULOS ====================

        public async Task<CalculoDiasHabilesDto> CalcularFechaConDiasHabilesAsync(DateTime fechaInicio, int diasHabiles)
        {
            var resultado = new CalculoDiasHabilesDto
            {
                FechaInicio = fechaInicio.Date,
                DiasHabilesSolicitados = diasHabiles
            };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener todos los festivos desde la fecha de inicio hacia adelante
                var festivos = await connection.QueryAsync<DateTime>(@"
                    SELECT ""Fecha""::timestamp AS ""Fecha"" FROM documentos.""Dias_Festivos""
                    WHERE ""Fecha"" >= @FechaInicio
                    ORDER BY ""Fecha""",
                    new { FechaInicio = fechaInicio.Date });

                var festivosSet = new HashSet<DateTime>(festivos);

                var fechaActual = fechaInicio.Date;
                var diasHabilesContados = 0;
                var diasCalendario = 0;
                var finesSemana = 0;
                var festivosEnRango = 0;

                while (diasHabilesContados < diasHabiles)
                {
                    fechaActual = fechaActual.AddDays(1);
                    diasCalendario++;

                    if (fechaActual.DayOfWeek == DayOfWeek.Saturday || fechaActual.DayOfWeek == DayOfWeek.Sunday)
                    {
                        finesSemana++;
                        continue;
                    }

                    if (festivosSet.Contains(fechaActual))
                    {
                        festivosEnRango++;
                        continue;
                    }

                    diasHabilesContados++;
                }

                resultado.FechaFin = fechaActual;
                resultado.DiasCalendarioTotal = diasCalendario;
                resultado.DiasFestivosEnRango = festivosEnRango;
                resultado.FinesSemanasEnRango = finesSemana;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculando fecha con días hábiles");
                // Fallback simple sin festivos
                resultado.FechaFin = fechaInicio.AddDays(diasHabiles * 1.5);
            }

            return resultado;
        }

        public async Task<int> ContarDiasHabilesEntreAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener festivos en el rango
                var festivos = await connection.QueryAsync<DateTime>(@"
                    SELECT ""Fecha""::timestamp AS ""Fecha"" FROM documentos.""Dias_Festivos""
                    WHERE ""Fecha"" >= @FechaInicio AND ""Fecha"" <= @FechaFin",
                    new { FechaInicio = fechaInicio.Date, FechaFin = fechaFin.Date });

                var festivosSet = new HashSet<DateTime>(festivos);
                var diasHabiles = 0;

                for (var fecha = fechaInicio.Date; fecha <= fechaFin.Date; fecha = fecha.AddDays(1))
                {
                    if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                        continue;

                    if (festivosSet.Contains(fecha))
                        continue;

                    diasHabiles++;
                }

                return diasHabiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error contando días hábiles entre fechas");
                return 0;
            }
        }

        // ==================== CRUD ====================

        public async Task<ResultadoDiaFestivoDto> CrearAsync(CrearDiaFestivoRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar si ya existe
                var existe = await connection.ExecuteScalarAsync<bool>(@"
                    SELECT EXISTS(SELECT 1 FROM documentos.""Dias_Festivos"" WHERE ""Fecha"" = @Fecha)",
                    new { Fecha = request.Fecha.Date });

                if (existe)
                {
                    return new ResultadoDiaFestivoDto
                    {
                        Exito = false,
                        Mensaje = $"Ya existe un día festivo para la fecha {request.Fecha:dd/MM/yyyy}"
                    };
                }

                await connection.ExecuteAsync(@"
                    INSERT INTO documentos.""Dias_Festivos"" (""Fecha"", ""Nombre"", ""EsFijo"", ""Pais"")
                    VALUES (@Fecha, @Nombre, @EsFijo, @Pais)",
                    new
                    {
                        Fecha = request.Fecha.Date,
                        request.Nombre,
                        request.EsFijo,
                        request.Pais
                    });

                return new ResultadoDiaFestivoDto
                {
                    Exito = true,
                    Mensaje = "Día festivo creado correctamente",
                    DiaFestivo = new DiaFestivoDto
                    {
                        Fecha = request.Fecha.Date,
                        Nombre = request.Nombre,
                        EsFijo = request.EsFijo,
                        Pais = request.Pais,
                        DiaSemana = DiaSemanaEspanol[(int)request.Fecha.DayOfWeek]
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando día festivo");
                return new ResultadoDiaFestivoDto
                {
                    Exito = false,
                    Mensaje = "Error al crear el día festivo: " + ex.Message
                };
            }
        }

        public async Task<ResultadoDiaFestivoDto> ActualizarAsync(DateTime fecha, ActualizarDiaFestivoRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(@"
                    UPDATE documentos.""Dias_Festivos""
                    SET ""Nombre"" = @Nombre, ""EsFijo"" = @EsFijo, ""Pais"" = @Pais
                    WHERE ""Fecha"" = @Fecha",
                    new
                    {
                        Fecha = fecha.Date,
                        request.Nombre,
                        request.EsFijo,
                        request.Pais
                    });

                if (affected == 0)
                {
                    return new ResultadoDiaFestivoDto
                    {
                        Exito = false,
                        Mensaje = "Día festivo no encontrado"
                    };
                }

                return new ResultadoDiaFestivoDto
                {
                    Exito = true,
                    Mensaje = "Día festivo actualizado correctamente",
                    DiaFestivo = new DiaFestivoDto
                    {
                        Fecha = fecha.Date,
                        Nombre = request.Nombre,
                        EsFijo = request.EsFijo,
                        Pais = request.Pais,
                        DiaSemana = DiaSemanaEspanol[(int)fecha.DayOfWeek]
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando día festivo");
                return new ResultadoDiaFestivoDto
                {
                    Exito = false,
                    Mensaje = "Error al actualizar el día festivo: " + ex.Message
                };
            }
        }

        public async Task<ResultadoDiaFestivoDto> EliminarAsync(DateTime fecha)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener info antes de eliminar
                var festivo = await ObtenerPorFechaAsync(fecha);

                var affected = await connection.ExecuteAsync(@"
                    DELETE FROM documentos.""Dias_Festivos"" WHERE ""Fecha"" = @Fecha",
                    new { Fecha = fecha.Date });

                if (affected == 0)
                {
                    return new ResultadoDiaFestivoDto
                    {
                        Exito = false,
                        Mensaje = "Día festivo no encontrado"
                    };
                }

                return new ResultadoDiaFestivoDto
                {
                    Exito = true,
                    Mensaje = "Día festivo eliminado correctamente",
                    DiaFestivo = festivo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando día festivo");
                return new ResultadoDiaFestivoDto
                {
                    Exito = false,
                    Mensaje = "Error al eliminar el día festivo: " + ex.Message
                };
            }
        }

        // ==================== GENERACIÓN MASIVA ====================

        public async Task<ResultadoGeneracionFestivosDto> GenerarFestivosColombiaAsync(int anio, bool sobreescribir = false)
        {
            var resultado = new ResultadoGeneracionFestivosDto();
            var errores = new List<string>();

            try
            {
                var festivos = GenerarFestivosColombia(anio);

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                foreach (var festivo in festivos)
                {
                    try
                    {
                        var existe = await connection.ExecuteScalarAsync<bool>(@"
                            SELECT EXISTS(SELECT 1 FROM documentos.""Dias_Festivos"" WHERE ""Fecha"" = @Fecha)",
                            new { festivo.Fecha });

                        if (existe)
                        {
                            if (sobreescribir)
                            {
                                await connection.ExecuteAsync(@"
                                    UPDATE documentos.""Dias_Festivos""
                                    SET ""Nombre"" = @Nombre, ""EsFijo"" = @EsFijo
                                    WHERE ""Fecha"" = @Fecha",
                                    festivo);
                                resultado.FestivosCreados++;
                            }
                            else
                            {
                                resultado.FestivosOmitidos++;
                            }
                        }
                        else
                        {
                            await connection.ExecuteAsync(@"
                                INSERT INTO documentos.""Dias_Festivos"" (""Fecha"", ""Nombre"", ""EsFijo"", ""Pais"")
                                VALUES (@Fecha, @Nombre, @EsFijo, @Pais)",
                                festivo);
                            resultado.FestivosCreados++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errores.Add($"{festivo.Fecha:dd/MM/yyyy} ({festivo.Nombre}): {ex.Message}");
                    }
                }

                resultado.Exito = true;
                resultado.Mensaje = $"Se crearon {resultado.FestivosCreados} festivos para el año {anio}";
                resultado.Errores = errores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando festivos de Colombia para {Anio}", anio);
                resultado.Exito = false;
                resultado.Mensaje = "Error al generar festivos: " + ex.Message;
            }

            return resultado;
        }

        /// <summary>
        /// Genera la lista de festivos de Colombia para un año dado
        /// Implementa la Ley Emiliani (Ley 51 de 1983)
        /// </summary>
        private static List<DiaFestivoDto> GenerarFestivosColombia(int anio)
        {
            var festivos = new List<DiaFestivoDto>();

            // Festivos fijos
            festivos.Add(new DiaFestivoDto { Fecha = new DateTime(anio, 1, 1), Nombre = "Año Nuevo", EsFijo = true, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = new DateTime(anio, 5, 1), Nombre = "Día del Trabajo", EsFijo = true, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = new DateTime(anio, 7, 20), Nombre = "Día de la Independencia", EsFijo = true, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = new DateTime(anio, 8, 7), Nombre = "Batalla de Boyacá", EsFijo = true, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = new DateTime(anio, 12, 8), Nombre = "Día de la Inmaculada Concepción", EsFijo = true, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = new DateTime(anio, 12, 25), Nombre = "Día de Navidad", EsFijo = true, Pais = "Colombia" });

            // Festivos móviles (Ley Emiliani - se trasladan al lunes siguiente)
            festivos.Add(new DiaFestivoDto { Fecha = SiguienteLunes(new DateTime(anio, 1, 6)), Nombre = "Día de los Reyes Magos", EsFijo = false, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = SiguienteLunes(new DateTime(anio, 3, 19)), Nombre = "Día de San José", EsFijo = false, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = SiguienteLunes(new DateTime(anio, 6, 29)), Nombre = "San Pedro y San Pablo", EsFijo = false, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = SiguienteLunes(new DateTime(anio, 8, 15)), Nombre = "La Asunción de la Virgen", EsFijo = false, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = SiguienteLunes(new DateTime(anio, 10, 12)), Nombre = "Día de la Raza", EsFijo = false, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = SiguienteLunes(new DateTime(anio, 11, 1)), Nombre = "Todos los Santos", EsFijo = false, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = SiguienteLunes(new DateTime(anio, 11, 11)), Nombre = "Independencia de Cartagena", EsFijo = false, Pais = "Colombia" });

            // Festivos basados en Semana Santa (dependen del Domingo de Pascua)
            var domingoPascua = CalcularDomingoPascua(anio);
            festivos.Add(new DiaFestivoDto { Fecha = domingoPascua.AddDays(-3), Nombre = "Jueves Santo", EsFijo = false, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = domingoPascua.AddDays(-2), Nombre = "Viernes Santo", EsFijo = false, Pais = "Colombia" });
            festivos.Add(new DiaFestivoDto { Fecha = domingoPascua.AddDays(43), Nombre = "Día de la Ascensión", EsFijo = false, Pais = "Colombia" }); // 39 días + lunes
            festivos.Add(new DiaFestivoDto { Fecha = domingoPascua.AddDays(64), Nombre = "Corpus Christi", EsFijo = false, Pais = "Colombia" }); // 60 días + lunes
            festivos.Add(new DiaFestivoDto { Fecha = domingoPascua.AddDays(71), Nombre = "Sagrado Corazón", EsFijo = false, Pais = "Colombia" }); // 68 días + lunes

            // Aplicar Ley Emiliani a Ascensión, Corpus y Sagrado Corazón
            for (int i = festivos.Count - 3; i < festivos.Count; i++)
            {
                festivos[i].Fecha = SiguienteLunes(festivos[i].Fecha);
            }

            return festivos.OrderBy(f => f.Fecha).ToList();
        }

        /// <summary>
        /// Calcula el Domingo de Pascua usando el algoritmo de Butcher
        /// </summary>
        private static DateTime CalcularDomingoPascua(int anio)
        {
            int a = anio % 19;
            int b = anio / 100;
            int c = anio % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int mes = (h + l - 7 * m + 114) / 31;
            int dia = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(anio, mes, dia);
        }

        /// <summary>
        /// Retorna la fecha del siguiente lunes (Ley Emiliani)
        /// Si la fecha ya es lunes, retorna la misma fecha
        /// </summary>
        private static DateTime SiguienteLunes(DateTime fecha)
        {
            if (fecha.DayOfWeek == DayOfWeek.Monday)
                return fecha;

            int diasHastaLunes = ((int)DayOfWeek.Monday - (int)fecha.DayOfWeek + 7) % 7;
            if (diasHastaLunes == 0) diasHastaLunes = 7;

            return fecha.AddDays(diasHastaLunes);
        }
    }
}