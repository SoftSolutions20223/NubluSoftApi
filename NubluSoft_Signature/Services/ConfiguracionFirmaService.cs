using Dapper;
using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    public class ConfiguracionFirmaService : IConfiguracionFirmaService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<ConfiguracionFirmaService> _logger;

        public ConfiguracionFirmaService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<ConfiguracionFirmaService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<ConfiguracionFirmaDto?> ObtenerConfiguracionAsync(long entidadId)
        {
            const string sql = @"
                SELECT 
                    ""Cod"", ""Entidad"",
                    ""FirmaSimpleHabilitada"", ""OtpPorEmail"", ""OtpPorSms"",
                    ""OtpLongitud"", ""OtpVigenciaMinutos"", ""OtpMaxIntentos"",
                    ""FirmaAvanzadaHabilitada"", ""GenerarCertificadoAuto"",
                    ""VigenciaCertificadoDias"", ""RequiereContrasenaFuerte"",
                    ""AgregarPaginaConstancia"", ""AgregarSelloVisual"",
                    ""PosicionSelloX"", ""PosicionSelloY"",
                    ""TamanoSelloAncho"", ""TamanoSelloAlto"",
                    ""TextoSello"", ""IncluirFechaEnSello"", ""IncluirNombreEnSello"",
                    ""EnviarEmailSolicitud"", ""EnviarEmailRecordatorio"",
                    ""EnviarEmailFirmaCompletada"", ""DiasRecordatorioVencimiento"",
                    ""PlantillaEmailSolicitud"", ""PlantillaEmailOtp"",
                    ""PlantillaEmailCompletada"", ""PlantillaEmailRecordatorio"",
                    ""MaxFirmantesPorSolicitud"", ""DiasMaxVigenciaSolicitud""
                FROM documentos.""Configuracion_Firma""
                WHERE ""Entidad"" = @EntidadId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<ConfiguracionFirmaDto>(sql, new { EntidadId = entidadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo configuración de firma para entidad {EntidadId}", entidadId);
                return null;
            }
        }

        public async Task<ResultadoConfiguracionDto> CrearConfiguracionAsync(long entidadId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar si ya existe
                var existe = await connection.ExecuteScalarAsync<bool>(
                    @"SELECT EXISTS(SELECT 1 FROM documentos.""Configuracion_Firma"" WHERE ""Entidad"" = @EntidadId)",
                    new { EntidadId = entidadId });

                if (existe)
                {
                    var configExistente = await ObtenerConfiguracionAsync(entidadId);
                    return new ResultadoConfiguracionDto
                    {
                        Exito = true,
                        Mensaje = "La entidad ya tiene configuración de firma",
                        Configuracion = configExistente
                    };
                }

                // Obtener siguiente código
                var nuevoCod = await connection.ExecuteScalarAsync<long>(
                    @"SELECT COALESCE(MAX(""Cod""), 0) + 1 FROM documentos.""Configuracion_Firma""");

                // Crear con valores por defecto
                const string insertSql = @"
                    INSERT INTO documentos.""Configuracion_Firma"" (""Cod"", ""Entidad"")
                    VALUES (@Cod, @EntidadId)";

                await connection.ExecuteAsync(insertSql, new { Cod = nuevoCod, EntidadId = entidadId });

                var config = await ObtenerConfiguracionAsync(entidadId);

                return new ResultadoConfiguracionDto
                {
                    Exito = true,
                    Mensaje = "Configuración de firma creada con valores por defecto",
                    Configuracion = config
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando configuración de firma para entidad {EntidadId}", entidadId);
                return new ResultadoConfiguracionDto
                {
                    Exito = false,
                    Mensaje = "Error al crear configuración: " + ex.Message
                };
            }
        }

        public async Task<ResultadoConfiguracionDto> ActualizarConfiguracionAsync(
            long entidadId,
            ActualizarConfiguracionCompletaRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Configuracion_Firma""
                SET 
                    ""FirmaSimpleHabilitada"" = @FirmaSimpleHabilitada,
                    ""OtpPorEmail"" = @OtpPorEmail,
                    ""OtpPorSms"" = @OtpPorSms,
                    ""OtpLongitud"" = @OtpLongitud,
                    ""OtpVigenciaMinutos"" = @OtpVigenciaMinutos,
                    ""OtpMaxIntentos"" = @OtpMaxIntentos,
                    ""FirmaAvanzadaHabilitada"" = @FirmaAvanzadaHabilitada,
                    ""GenerarCertificadoAuto"" = @GenerarCertificadoAuto,
                    ""VigenciaCertificadoDias"" = @VigenciaCertificadoDias,
                    ""RequiereContrasenaFuerte"" = @RequiereContrasenaFuerte,
                    ""AgregarPaginaConstancia"" = @AgregarPaginaConstancia,
                    ""AgregarSelloVisual"" = @AgregarSelloVisual,
                    ""PosicionSelloX"" = @PosicionSelloX,
                    ""PosicionSelloY"" = @PosicionSelloY,
                    ""TamanoSelloAncho"" = @TamanoSelloAncho,
                    ""TamanoSelloAlto"" = @TamanoSelloAlto,
                    ""TextoSello"" = @TextoSello,
                    ""IncluirFechaEnSello"" = @IncluirFechaEnSello,
                    ""IncluirNombreEnSello"" = @IncluirNombreEnSello,
                    ""EnviarEmailSolicitud"" = @EnviarEmailSolicitud,
                    ""EnviarEmailRecordatorio"" = @EnviarEmailRecordatorio,
                    ""EnviarEmailFirmaCompletada"" = @EnviarEmailFirmaCompletada,
                    ""DiasRecordatorioVencimiento"" = @DiasRecordatorioVencimiento,
                    ""MaxFirmantesPorSolicitud"" = @MaxFirmantesPorSolicitud,
                    ""DiasMaxVigenciaSolicitud"" = @DiasMaxVigenciaSolicitud
                WHERE ""Entidad"" = @EntidadId";

            return await EjecutarActualizacionAsync(entidadId, sql, new
            {
                EntidadId = entidadId,
                request.FirmaSimpleHabilitada,
                request.OtpPorEmail,
                request.OtpPorSms,
                request.OtpLongitud,
                request.OtpVigenciaMinutos,
                request.OtpMaxIntentos,
                request.FirmaAvanzadaHabilitada,
                request.GenerarCertificadoAuto,
                request.VigenciaCertificadoDias,
                request.RequiereContrasenaFuerte,
                request.AgregarPaginaConstancia,
                request.AgregarSelloVisual,
                request.PosicionSelloX,
                request.PosicionSelloY,
                request.TamanoSelloAncho,
                request.TamanoSelloAlto,
                request.TextoSello,
                request.IncluirFechaEnSello,
                request.IncluirNombreEnSello,
                request.EnviarEmailSolicitud,
                request.EnviarEmailRecordatorio,
                request.EnviarEmailFirmaCompletada,
                request.DiasRecordatorioVencimiento,
                request.MaxFirmantesPorSolicitud,
                request.DiasMaxVigenciaSolicitud
            });
        }

        public async Task<ResultadoConfiguracionDto> ActualizarConfigOtpAsync(
            long entidadId,
            ActualizarConfigOtpRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Configuracion_Firma""
                SET 
                    ""FirmaSimpleHabilitada"" = @FirmaSimpleHabilitada,
                    ""OtpPorEmail"" = @OtpPorEmail,
                    ""OtpPorSms"" = @OtpPorSms,
                    ""OtpLongitud"" = @OtpLongitud,
                    ""OtpVigenciaMinutos"" = @OtpVigenciaMinutos,
                    ""OtpMaxIntentos"" = @OtpMaxIntentos
                WHERE ""Entidad"" = @EntidadId";

            return await EjecutarActualizacionAsync(entidadId, sql, new
            {
                EntidadId = entidadId,
                request.FirmaSimpleHabilitada,
                request.OtpPorEmail,
                request.OtpPorSms,
                request.OtpLongitud,
                request.OtpVigenciaMinutos,
                request.OtpMaxIntentos
            });
        }

        public async Task<ResultadoConfiguracionDto> ActualizarConfigCertificadosAsync(
            long entidadId,
            ActualizarConfigCertificadosRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Configuracion_Firma""
                SET 
                    ""FirmaAvanzadaHabilitada"" = @FirmaAvanzadaHabilitada,
                    ""GenerarCertificadoAuto"" = @GenerarCertificadoAuto,
                    ""VigenciaCertificadoDias"" = @VigenciaCertificadoDias,
                    ""RequiereContrasenaFuerte"" = @RequiereContrasenaFuerte
                WHERE ""Entidad"" = @EntidadId";

            return await EjecutarActualizacionAsync(entidadId, sql, new
            {
                EntidadId = entidadId,
                request.FirmaAvanzadaHabilitada,
                request.GenerarCertificadoAuto,
                request.VigenciaCertificadoDias,
                request.RequiereContrasenaFuerte
            });
        }

        public async Task<ResultadoConfiguracionDto> ActualizarConfigSelloAsync(
            long entidadId,
            ActualizarConfigSelloRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Configuracion_Firma""
                SET 
                    ""AgregarPaginaConstancia"" = @AgregarPaginaConstancia,
                    ""AgregarSelloVisual"" = @AgregarSelloVisual,
                    ""PosicionSelloX"" = @PosicionSelloX,
                    ""PosicionSelloY"" = @PosicionSelloY,
                    ""TamanoSelloAncho"" = @TamanoSelloAncho,
                    ""TamanoSelloAlto"" = @TamanoSelloAlto,
                    ""TextoSello"" = @TextoSello,
                    ""IncluirFechaEnSello"" = @IncluirFechaEnSello,
                    ""IncluirNombreEnSello"" = @IncluirNombreEnSello
                WHERE ""Entidad"" = @EntidadId";

            return await EjecutarActualizacionAsync(entidadId, sql, new
            {
                EntidadId = entidadId,
                request.AgregarPaginaConstancia,
                request.AgregarSelloVisual,
                request.PosicionSelloX,
                request.PosicionSelloY,
                request.TamanoSelloAncho,
                request.TamanoSelloAlto,
                request.TextoSello,
                request.IncluirFechaEnSello,
                request.IncluirNombreEnSello
            });
        }

        public async Task<ResultadoConfiguracionDto> ActualizarConfigNotificacionesAsync(
            long entidadId,
            ActualizarConfigNotificacionesRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Configuracion_Firma""
                SET 
                    ""EnviarEmailSolicitud"" = @EnviarEmailSolicitud,
                    ""EnviarEmailRecordatorio"" = @EnviarEmailRecordatorio,
                    ""EnviarEmailFirmaCompletada"" = @EnviarEmailFirmaCompletada,
                    ""DiasRecordatorioVencimiento"" = @DiasRecordatorioVencimiento
                WHERE ""Entidad"" = @EntidadId";

            return await EjecutarActualizacionAsync(entidadId, sql, new
            {
                EntidadId = entidadId,
                request.EnviarEmailSolicitud,
                request.EnviarEmailRecordatorio,
                request.EnviarEmailFirmaCompletada,
                request.DiasRecordatorioVencimiento
            });
        }

        public async Task<ResultadoConfiguracionDto> ActualizarPlantillasAsync(
            long entidadId,
            ActualizarPlantillasRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Configuracion_Firma""
                SET 
                    ""PlantillaEmailSolicitud"" = @PlantillaEmailSolicitud,
                    ""PlantillaEmailOtp"" = @PlantillaEmailOtp,
                    ""PlantillaEmailCompletada"" = @PlantillaEmailCompletada,
                    ""PlantillaEmailRecordatorio"" = @PlantillaEmailRecordatorio
                WHERE ""Entidad"" = @EntidadId";

            return await EjecutarActualizacionAsync(entidadId, sql, new
            {
                EntidadId = entidadId,
                request.PlantillaEmailSolicitud,
                request.PlantillaEmailOtp,
                request.PlantillaEmailCompletada,
                request.PlantillaEmailRecordatorio
            });
        }

        public async Task<ResultadoConfiguracionDto> ActualizarConfigLimitesAsync(
            long entidadId,
            ActualizarConfigLimitesRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Configuracion_Firma""
                SET 
                    ""MaxFirmantesPorSolicitud"" = @MaxFirmantesPorSolicitud,
                    ""DiasMaxVigenciaSolicitud"" = @DiasMaxVigenciaSolicitud
                WHERE ""Entidad"" = @EntidadId";

            return await EjecutarActualizacionAsync(entidadId, sql, new
            {
                EntidadId = entidadId,
                request.MaxFirmantesPorSolicitud,
                request.DiasMaxVigenciaSolicitud
            });
        }

        public async Task<ResultadoConfiguracionDto> RestaurarValoresPorDefectoAsync(long entidadId)
        {
            const string sql = @"
                UPDATE documentos.""Configuracion_Firma""
                SET 
                    ""FirmaSimpleHabilitada"" = true,
                    ""OtpPorEmail"" = true,
                    ""OtpPorSms"" = false,
                    ""OtpLongitud"" = 6,
                    ""OtpVigenciaMinutos"" = 10,
                    ""OtpMaxIntentos"" = 3,
                    ""FirmaAvanzadaHabilitada"" = true,
                    ""GenerarCertificadoAuto"" = true,
                    ""VigenciaCertificadoDias"" = 365,
                    ""RequiereContrasenaFuerte"" = true,
                    ""AgregarPaginaConstancia"" = true,
                    ""AgregarSelloVisual"" = true,
                    ""PosicionSelloX"" = 400,
                    ""PosicionSelloY"" = 50,
                    ""TamanoSelloAncho"" = 200,
                    ""TamanoSelloAlto"" = 70,
                    ""TextoSello"" = 'Firmado electrónicamente',
                    ""IncluirFechaEnSello"" = true,
                    ""IncluirNombreEnSello"" = true,
                    ""EnviarEmailSolicitud"" = true,
                    ""EnviarEmailRecordatorio"" = true,
                    ""EnviarEmailFirmaCompletada"" = true,
                    ""DiasRecordatorioVencimiento"" = 2,
                    ""PlantillaEmailSolicitud"" = NULL,
                    ""PlantillaEmailOtp"" = NULL,
                    ""PlantillaEmailCompletada"" = NULL,
                    ""PlantillaEmailRecordatorio"" = NULL,
                    ""MaxFirmantesPorSolicitud"" = 10,
                    ""DiasMaxVigenciaSolicitud"" = 30
                WHERE ""Entidad"" = @EntidadId";

            return await EjecutarActualizacionAsync(entidadId, sql, new { EntidadId = entidadId }, "restaurada a valores por defecto");
        }

        public IEnumerable<VariablesPlantillaDto> ObtenerVariablesPlantilla()
        {
            return new List<VariablesPlantillaDto>
            {
                new() { Variable = "{{NombreFirmante}}", Descripcion = "Nombre completo del firmante", Ejemplo = "Juan Pérez" },
                new() { Variable = "{{EmailFirmante}}", Descripcion = "Email del firmante", Ejemplo = "juan@ejemplo.com" },
                new() { Variable = "{{NombreDocumento}}", Descripcion = "Nombre del archivo a firmar", Ejemplo = "Contrato.pdf" },
                new() { Variable = "{{NombreSolicitante}}", Descripcion = "Quien solicita la firma", Ejemplo = "María García" },
                new() { Variable = "{{FechaVencimiento}}", Descripcion = "Fecha límite para firmar", Ejemplo = "15/01/2026" },
                new() { Variable = "{{CodigoOTP}}", Descripcion = "Código OTP generado", Ejemplo = "123456" },
                new() { Variable = "{{MinutosVigencia}}", Descripcion = "Minutos de vigencia del OTP", Ejemplo = "10" },
                new() { Variable = "{{UrlFirma}}", Descripcion = "Enlace para firmar", Ejemplo = "https://..." },
                new() { Variable = "{{NombreEntidad}}", Descripcion = "Nombre de la entidad", Ejemplo = "Alcaldía Municipal" },
                new() { Variable = "{{FechaHoy}}", Descripcion = "Fecha actual", Ejemplo = "14/01/2026" },
                new() { Variable = "{{CodigoVerificacion}}", Descripcion = "Código para verificar firma", Ejemplo = "ABC123XYZ" }
            };
        }

        // ==================== HELPER ====================

        private async Task<ResultadoConfiguracionDto> EjecutarActualizacionAsync(
            long entidadId,
            string sql,
            object parametros,
            string? mensajeExito = null)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que existe, si no crear
                var existe = await connection.ExecuteScalarAsync<bool>(
                    @"SELECT EXISTS(SELECT 1 FROM documentos.""Configuracion_Firma"" WHERE ""Entidad"" = @EntidadId)",
                    new { EntidadId = entidadId });

                if (!existe)
                {
                    var crear = await CrearConfiguracionAsync(entidadId);
                    if (!crear.Exito)
                        return crear;
                }

                var affected = await connection.ExecuteAsync(sql, parametros);

                if (affected == 0)
                {
                    return new ResultadoConfiguracionDto
                    {
                        Exito = false,
                        Mensaje = "No se encontró la configuración para actualizar"
                    };
                }

                var config = await ObtenerConfiguracionAsync(entidadId);

                return new ResultadoConfiguracionDto
                {
                    Exito = true,
                    Mensaje = mensajeExito ?? "Configuración actualizada correctamente",
                    Configuracion = config
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando configuración de firma");
                return new ResultadoConfiguracionDto
                {
                    Exito = false,
                    Mensaje = "Error al actualizar: " + ex.Message
                };
            }
        }
    }
}