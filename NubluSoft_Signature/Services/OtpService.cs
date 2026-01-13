using Dapper;
using Microsoft.Extensions.Options;
using NubluSoft_Signature.Configuration;
using NubluSoft_Signature.Helpers;
using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del servicio OTP
    /// </summary>
    public class OtpService : IOtpService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly IEmailService _emailService;
        private readonly SignatureSettings _settings;
        private readonly ILogger<OtpService> _logger;

        public OtpService(
            IPostgresConnectionFactory connectionFactory,
            IEmailService emailService,
            IOptions<SignatureSettings> settings,
            ILogger<OtpService> logger)
        {
            _connectionFactory = connectionFactory;
            _emailService = emailService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<GenerarOtpResponse> GenerarYEnviarAsync(
            long firmanteId,
            string medio,
            string ip,
            string userAgent)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 1. Obtener información del firmante y solicitud
                var info = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT 
                        fs.""Cod"" AS FirmanteId,
                        fs.""Estado"" AS EstadoFirmante,
                        u.""Correo"" AS Email,
                        u.""Telefono"" AS Telefono,
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS NombreCompleto,
                        a.""Nombre"" AS NombreArchivo,
                        s.""Estado"" AS EstadoSolicitud,
                        s.""TipoFirma""
                    FROM documentos.""Firmantes_Solicitud"" fs
                    INNER JOIN documentos.""Usuarios"" u ON fs.""Usuario"" = u.""Cod"" AND fs.""Entidad"" = u.""Entidad""
                    INNER JOIN documentos.""Solicitudes_Firma"" s ON fs.""Solicitud"" = s.""Cod""
                    INNER JOIN documentos.""Archivos"" a ON s.""Archivo"" = a.""Cod""
                    WHERE fs.""Cod"" = @FirmanteId",
                    new { FirmanteId = firmanteId });

                if (info == null)
                {
                    return new GenerarOtpResponse
                    {
                        Enviado = false,
                        Mensaje = "Firmante no encontrado"
                    };
                }

                // 2. Validar estado
                if (info.EstadoSolicitud != "PENDIENTE" && info.EstadoSolicitud != "EN_PROCESO")
                {
                    return new GenerarOtpResponse
                    {
                        Enviado = false,
                        Mensaje = $"La solicitud está en estado {info.EstadoSolicitud}"
                    };
                }

                if (info.EstadoFirmante == "FIRMADO")
                {
                    return new GenerarOtpResponse
                    {
                        Enviado = false,
                        Mensaje = "Ya has firmado este documento"
                    };
                }

                if (info.TipoFirma != "SIMPLE")
                {
                    return new GenerarOtpResponse
                    {
                        Enviado = false,
                        Mensaje = "Esta solicitud requiere firma avanzada con certificado"
                    };
                }

                // 3. Verificar destino según medio
                string destino;
                string destinoCompleto;
                string destinoEnmascarado;

                if (medio == "EMAIL")
                {
                    if (string.IsNullOrEmpty(info.Email))
                    {
                        return new GenerarOtpResponse
                        {
                            Enviado = false,
                            Mensaje = "El usuario no tiene correo electrónico registrado"
                        };
                    }
                    destino = info.Email;
                    destinoCompleto = info.Email;
                    destinoEnmascarado = OtpHelper.EnmascararEmail(info.Email);
                }
                else // SMS
                {
                    if (string.IsNullOrEmpty(info.Telefono))
                    {
                        return new GenerarOtpResponse
                        {
                            Enviado = false,
                            Mensaje = "El usuario no tiene teléfono registrado"
                        };
                    }
                    destino = info.Telefono;
                    destinoCompleto = info.Telefono;
                    destinoEnmascarado = OtpHelper.EnmascararTelefono(info.Telefono);
                }

                // 4. Invalidar códigos anteriores
                await InvalidarCodigosActivosAsync(firmanteId);

                // 5. Generar nuevo código
                var codigo = OtpHelper.GenerarCodigo(_settings.Otp.Longitud);
                var (hash, salt) = OtpHelper.HashearCodigo(codigo);
                var expiracion = DateTime.Now.AddMinutes(_settings.Otp.VigenciaMinutos);

                // 6. Guardar en BD
                await connection.ExecuteAsync(@"
                    INSERT INTO documentos.""Codigos_OTP"" (
                        ""Firmante"", ""CodigoHash"", ""Salt"", ""MedioEnvio"",
                        ""DestinoEnvio"", ""DestinoCompleto"", ""FechaExpiracion"", ""MaxIntentos""
                    ) VALUES (
                        @FirmanteId, @Hash, @Salt, @Medio,
                        @Destino, @DestinoCompleto, @Expiracion, @MaxIntentos
                    )",
                    new
                    {
                        FirmanteId = firmanteId,
                        Hash = hash,
                        Salt = salt,
                        Medio = medio,
                        Destino = destinoEnmascarado,
                        DestinoCompleto = destinoCompleto,
                        Expiracion = expiracion,
                        MaxIntentos = _settings.Otp.MaxIntentos
                    });

                // 7. Registrar evidencia
                await RegistrarEvidenciaAsync(connection, firmanteId, "OTP_GENERADO",
                    $"OTP generado para envío por {medio}", ip, userAgent);

                // 8. Enviar código
                bool enviado;
                if (medio == "EMAIL")
                {
                    enviado = await _emailService.EnviarOtpAsync(
                        destinoCompleto,
                        info.NombreCompleto,
                        codigo,
                        info.NombreArchivo,
                        _settings.Otp.VigenciaMinutos);
                }
                else
                {
                    // TODO: Implementar envío SMS cuando se tenga proveedor
                    _logger.LogWarning("Envío SMS no implementado, código: {Codigo}", codigo);
                    enviado = false;
                }

                if (enviado)
                {
                    await RegistrarEvidenciaAsync(connection, firmanteId, "OTP_ENVIADO",
                        $"OTP enviado a {destinoEnmascarado} por {medio}", ip, userAgent);
                }

                _logger.LogInformation(
                    "OTP generado para firmante {FirmanteId}, enviado: {Enviado}",
                    firmanteId, enviado);

                return new GenerarOtpResponse
                {
                    Enviado = enviado,
                    Mensaje = enviado
                        ? $"Código enviado a {destinoEnmascarado}"
                        : "Error al enviar el código",
                    DestinoEnmascarado = destinoEnmascarado,
                    ExpiraEnSegundos = _settings.Otp.VigenciaMinutos * 60,
                    IntentosRestantes = _settings.Otp.MaxIntentos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando OTP para firmante {FirmanteId}", firmanteId);
                return new GenerarOtpResponse
                {
                    Enviado = false,
                    Mensaje = "Error al generar el código"
                };
            }
        }

        public async Task<ValidarOtpResponse> ValidarAsync(
            long firmanteId,
            string codigo,
            string ip)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 1. Buscar OTP activo
                var otp = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT ""Cod"", ""CodigoHash"", ""Salt"", ""Intentos"", ""MaxIntentos"", ""FechaExpiracion""
                    FROM documentos.""Codigos_OTP""
                    WHERE ""Firmante"" = @FirmanteId 
                      AND ""Usado"" = false
                      AND ""FechaExpiracion"" > CURRENT_TIMESTAMP
                    ORDER BY ""FechaEnvio"" DESC
                    LIMIT 1",
                    new { FirmanteId = firmanteId });

                if (otp == null)
                {
                    return new ValidarOtpResponse
                    {
                        Valido = false,
                        Mensaje = "No hay código activo. Por favor solicite uno nuevo."
                    };
                }

                // 2. Verificar intentos
                if (otp.Intentos >= otp.MaxIntentos)
                {
                    return new ValidarOtpResponse
                    {
                        Valido = false,
                        Mensaje = "Máximo de intentos alcanzado. Solicite un nuevo código.",
                        IntentosRestantes = 0
                    };
                }

                // 3. Incrementar intentos
                await connection.ExecuteAsync(@"
                    UPDATE documentos.""Codigos_OTP""
                    SET ""Intentos"" = ""Intentos"" + 1
                    WHERE ""Cod"" = @Cod",
                    new { Cod = otp.Cod });

                // 4. Validar código
                bool esValido = OtpHelper.ValidarCodigo(codigo, otp.CodigoHash, otp.Salt);

                if (!esValido)
                {
                    var intentosRestantes = otp.MaxIntentos - otp.Intentos - 1;

                    await RegistrarEvidenciaAsync(connection, firmanteId, "OTP_FALLIDO",
                        $"Intento fallido. Restantes: {intentosRestantes}", ip, null);

                    return new ValidarOtpResponse
                    {
                        Valido = false,
                        Mensaje = intentosRestantes > 0
                            ? $"Código incorrecto. {intentosRestantes} intentos restantes."
                            : "Código incorrecto. Máximo de intentos alcanzado.",
                        IntentosRestantes = intentosRestantes
                    };
                }

                // 5. Marcar como usado
                await connection.ExecuteAsync(@"
                    UPDATE documentos.""Codigos_OTP""
                    SET ""Usado"" = true, ""FechaUso"" = CURRENT_TIMESTAMP, ""IPUso"" = @IP
                    WHERE ""Cod"" = @Cod",
                    new { Cod = otp.Cod, IP = ip });

                await RegistrarEvidenciaAsync(connection, firmanteId, "OTP_VALIDADO",
                    "Código OTP validado correctamente", ip, null);

                _logger.LogInformation("OTP validado correctamente para firmante {FirmanteId}", firmanteId);

                return new ValidarOtpResponse
                {
                    Valido = true,
                    Mensaje = "Código verificado correctamente"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando OTP para firmante {FirmanteId}", firmanteId);
                return new ValidarOtpResponse
                {
                    Valido = false,
                    Mensaje = "Error al validar el código"
                };
            }
        }

        public async Task InvalidarCodigosActivosAsync(long firmanteId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                await connection.ExecuteAsync(@"
                    UPDATE documentos.""Codigos_OTP""
                    SET ""Usado"" = true
                    WHERE ""Firmante"" = @FirmanteId AND ""Usado"" = false",
                    new { FirmanteId = firmanteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidando códigos OTP del firmante {FirmanteId}", firmanteId);
            }
        }

        private async Task RegistrarEvidenciaAsync(
            Npgsql.NpgsqlConnection connection,
            long firmanteId,
            string tipo,
            string descripcion,
            string? ip,
            string? userAgent)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO documentos.""Evidencias_Firma"" (
                    ""Firmante"", ""TipoEvidencia"", ""Descripcion"", ""IP"", ""UserAgent""
                ) VALUES (
                    @FirmanteId, @Tipo, @Descripcion, @IP, @UserAgent
                )",
                new
                {
                    FirmanteId = firmanteId,
                    Tipo = tipo,
                    Descripcion = descripcion,
                    IP = ip,
                    UserAgent = userAgent
                });
        }
    }
}