using Dapper;
using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del servicio de firma
    /// </summary>
    public class FirmaService : IFirmaService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<FirmaService> _logger;

        public FirmaService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<FirmaService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<InfoDocumentoFirmaResponse?> ObtenerInfoDocumentoAsync(
            long entidadId,
            long usuarioId,
            long solicitudId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var info = await connection.QueryFirstOrDefaultAsync<InfoDocumentoFirmaResponse>(@"
                    SELECT 
                        s.""Cod"" AS SolicitudId,
                        fs.""Cod"" AS FirmanteId,
                        a.""Nombre"" AS NombreArchivo,
                        s.""Asunto"",
                        s.""Mensaje"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS NombreSolicitante,
                        s.""FechaSolicitud"",
                        s.""FechaVencimiento"",
                        s.""TipoFirma"",
                        fs.""RolFirmante"",
                        fs.""Orden"",
                        CASE 
                            WHEN s.""OrdenSecuencial"" = false THEN true
                            WHEN fs.""Orden"" = (
                                SELECT COALESCE(MAX(f2.""Orden""), 0) + 1
                                FROM documentos.""Firmantes_Solicitud"" f2
                                WHERE f2.""Solicitud"" = s.""Cod"" AND f2.""Estado"" = 'FIRMADO'
                            ) THEN true
                            ELSE false
                        END AS EsMiTurno
                    FROM documentos.""Solicitudes_Firma"" s
                    INNER JOIN documentos.""Firmantes_Solicitud"" fs ON fs.""Solicitud"" = s.""Cod""
                    INNER JOIN documentos.""Archivos"" a ON s.""Archivo"" = a.""Cod""
                    INNER JOIN documentos.""Usuarios"" u ON s.""SolicitadoPor"" = u.""Cod"" AND s.""Entidad"" = u.""Entidad""
                    WHERE s.""Cod"" = @SolicitudId 
                      AND s.""Entidad"" = @EntidadId
                      AND fs.""Usuario"" = @UsuarioId
                      AND fs.""Estado"" IN ('PENDIENTE', 'NOTIFICADO')",
                    new { SolicitudId = solicitudId, EntidadId = entidadId, UsuarioId = usuarioId });

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo info del documento para firma");
                return null;
            }
        }

        public async Task<ResultadoFirmaResponse> RegistrarFirmaSimpleAsync(
            long firmanteId,
            string ip,
            string userAgent)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 1. Obtener información del firmante y archivo
                var info = await connection.QueryFirstOrDefaultAsync<InfoFirmaDto>(@"
                    SELECT 
                        fs.""Solicitud"",
                        s.""Archivo"",
                        a.""Hash"" AS HashActual,
                        s.""HashOriginal"",
                        s.""CodigoVerificacion""
                    FROM documentos.""Firmantes_Solicitud"" fs
                    INNER JOIN documentos.""Solicitudes_Firma"" s ON fs.""Solicitud"" = s.""Cod""
                    INNER JOIN documentos.""Archivos"" a ON s.""Archivo"" = a.""Cod""
                    WHERE fs.""Cod"" = @FirmanteId",
                    new { FirmanteId = firmanteId });

                if (info == null)
                {
                    return new ResultadoFirmaResponse
                    {
                        Exito = false,
                        Mensaje = "Firmante no encontrado"
                    };
                }

                // 2. Verificar integridad del documento (opcional, según política)
                string hashAlFirmar = info.HashActual ?? info.HashOriginal ?? "PENDIENTE";

                // 3. Ejecutar función de registro de firma
                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRegistroFirmaDto>(@"
                    SELECT * FROM documentos.""F_RegistrarFirma""(
                        @FirmanteId, 
                        'SIMPLE_OTP',
                        @HashAlFirmar,
                        @IP,
                        @UserAgent,
                        NULL,
                        NULL
                    )",
                    new
                    {
                        FirmanteId = firmanteId,
                        HashAlFirmar = hashAlFirmar,
                        IP = ip,
                        UserAgent = userAgent
                    });

                if (resultado == null || !resultado.Exito)
                {
                    return new ResultadoFirmaResponse
                    {
                        Exito = false,
                        Mensaje = resultado?.Mensaje ?? "Error al registrar la firma"
                    };
                }

                _logger.LogInformation(
                    "Firma simple registrada para firmante {FirmanteId}, solicitud completada: {Completada}",
                    firmanteId, resultado.SolicitudCompletada);

                return new ResultadoFirmaResponse
                {
                    Exito = true,
                    Mensaje = resultado.Mensaje ?? "Firma registrada",
                    SolicitudCompletada = resultado.SolicitudCompletada,
                    SiguienteFirmanteId = resultado.SiguienteFirmante,
                    CodigoVerificacion = resultado.SolicitudCompletada ? info.CodigoVerificacion : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando firma simple para firmante {FirmanteId}", firmanteId);
                return new ResultadoFirmaResponse
                {
                    Exito = false,
                    Mensaje = "Error al procesar la firma"
                };
            }
        }

        public async Task<ResultadoFirmaResponse> RechazarFirmaAsync(
            long firmanteId,
            string motivo,
            string ip,
            string userAgent)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRechazoDto>(@"
                    SELECT * FROM documentos.""F_RechazarFirma""(
                        @FirmanteId,
                        @Motivo,
                        @IP,
                        @UserAgent
                    )",
                    new
                    {
                        FirmanteId = firmanteId,
                        Motivo = motivo,
                        IP = ip,
                        UserAgent = userAgent
                    });

                if (resultado == null || !resultado.Exito)
                {
                    return new ResultadoFirmaResponse
                    {
                        Exito = false,
                        Mensaje = resultado?.Mensaje ?? "Error al rechazar la firma"
                    };
                }

                _logger.LogInformation("Firma rechazada por firmante {FirmanteId}", firmanteId);

                return new ResultadoFirmaResponse
                {
                    Exito = true,
                    Mensaje = resultado.Mensaje ?? "Firma rechazada"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rechazando firma del firmante {FirmanteId}", firmanteId);
                return new ResultadoFirmaResponse
                {
                    Exito = false,
                    Mensaje = "Error al procesar el rechazo"
                };
            }
        }

        public async Task<string?> ObtenerHashDocumentoAsync(long archivoId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<string>(@"
                    SELECT ""Hash"" FROM documentos.""Archivos"" WHERE ""Cod"" = @ArchivoId",
                    new { ArchivoId = archivoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo hash del archivo {ArchivoId}", archivoId);
                return null;
            }
        }



        public async Task<ResultadoFirmaResponse> RegistrarFirmaAvanzadaAsync(
            long firmanteId,
            long certificadoId,
            string hashFinal,
            string ip,
            string userAgent)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Obtener información del firmante y solicitud
                var info = await connection.QueryFirstOrDefaultAsync<InfoFirmaDto>(@"
            SELECT 
                fs.""Solicitud"",
                s.""CodigoVerificacion""
            FROM documentos.""Firmantes_Solicitud"" fs
            INNER JOIN documentos.""Solicitudes_Firma"" s ON fs.""Solicitud"" = s.""Cod""
            WHERE fs.""Cod"" = @FirmanteId",
                    new { FirmanteId = firmanteId });

                if (info == null)
                {
                    return new ResultadoFirmaResponse
                    {
                        Exito = false,
                        Mensaje = "Firmante no encontrado"
                    };
                }

                // Ejecutar función de registro de firma avanzada
                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoRegistroFirmaDto>(@"
            SELECT * FROM documentos.""F_RegistrarFirma""(
                @FirmanteId, 
                'AVANZADA_CERTIFICADO',
                @HashFinal,
                @IP,
                @UserAgent,
                @CertificadoId,
                NULL
            )",
                    new
                    {
                        FirmanteId = firmanteId,
                        HashFinal = hashFinal,
                        IP = ip,
                        UserAgent = userAgent,
                        CertificadoId = certificadoId
                    });

                if (resultado == null || !resultado.Exito)
                {
                    return new ResultadoFirmaResponse
                    {
                        Exito = false,
                        Mensaje = resultado?.Mensaje ?? "Error al registrar la firma"
                    };
                }

                _logger.LogInformation(
                    "Firma avanzada registrada para firmante {FirmanteId}, certificado: {CertificadoId}",
                    firmanteId, certificadoId);

                return new ResultadoFirmaResponse
                {
                    Exito = true,
                    Mensaje = resultado.Mensaje ?? "Firma registrada",
                    SolicitudCompletada = resultado.SolicitudCompletada,
                    SiguienteFirmanteId = resultado.SiguienteFirmante,
                    CodigoVerificacion = resultado.SolicitudCompletada ? info.CodigoVerificacion : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando firma avanzada para firmante {FirmanteId}", firmanteId);
                return new ResultadoFirmaResponse
                {
                    Exito = false,
                    Mensaje = "Error al procesar la firma"
                };
            }
        }

        // ==================== DTOs internos para mapeo ====================

        private class InfoFirmaDto
        {
            public long Solicitud { get; set; }
            public long Archivo { get; set; }
            public string? HashActual { get; set; }
            public string? HashOriginal { get; set; }
            public string? CodigoVerificacion { get; set; }
        }

        private class ResultadoRegistroFirmaDto
        {
            public bool SolicitudCompletada { get; set; }
            public long? SiguienteFirmante { get; set; }
            public bool Exito { get; set; }
            public string? Mensaje { get; set; }
        }

        private class ResultadoRechazoDto
        {
            public bool Exito { get; set; }
            public string? Mensaje { get; set; }
        }
    }
}