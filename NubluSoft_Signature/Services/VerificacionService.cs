using Dapper;
using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del servicio de verificación pública
    /// </summary>
    public class VerificacionService : IVerificacionService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<VerificacionService> _logger;

        public VerificacionService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<VerificacionService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<VerificacionFirmaResponse> VerificarPorCodigoAsync(string codigoVerificacion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoVerificacion))
                {
                    return new VerificacionFirmaResponse
                    {
                        Valido = false,
                        Mensaje = "Código de verificación no proporcionado"
                    };
                }

                // Limpiar el código (quitar espacios, guiones extra, etc.)
                codigoVerificacion = codigoVerificacion.Trim().ToUpper();

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 1. Buscar la solicitud por código de verificación
                var solicitud = await connection.QueryFirstOrDefaultAsync<SolicitudVerificacionDto>(@"
                    SELECT 
                        s.""Cod"",
                        s.""Estado"",
                        s.""TipoFirma"",
                        a.""Nombre"" AS NombreDocumento,
                        e.""Nombre"" AS NombreEntidad,
                        s.""FechaSolicitud"",
                        s.""FechaCompletada"",
                        s.""HashOriginal"",
                        s.""HashFinal"",
                        s.""CodigoVerificacion""
                    FROM documentos.""Solicitudes_Firma"" s
                    INNER JOIN documentos.""Archivos"" a ON s.""Archivo"" = a.""Cod""
                    INNER JOIN usuarios.""Entidades"" e ON s.""Entidad"" = e.""Cod""
                    WHERE UPPER(s.""CodigoVerificacion"") = @Codigo",
                    new { Codigo = codigoVerificacion });

                if (solicitud == null)
                {
                    _logger.LogWarning(
                        "Intento de verificación con código inválido: {Codigo}",
                        codigoVerificacion);

                    return new VerificacionFirmaResponse
                    {
                        Valido = false,
                        Mensaje = "Código de verificación no encontrado"
                    };
                }

                // 2. Obtener lista de firmantes (solo información pública)
                var firmantes = await connection.QueryAsync<FirmantePublicoResponse>(@"
                    SELECT 
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS Nombre,
                        u.""Cargo"",
                        fs.""RolFirmante"",
                        fs.""Estado"",
                        fs.""Orden"",
                        fs.""FechaFirma"",
                        fs.""TipoFirmaUsada""
                    FROM documentos.""Firmantes_Solicitud"" fs
                    INNER JOIN documentos.""Usuarios"" u ON fs.""Usuario"" = u.""Cod"" AND fs.""Entidad"" = u.""Entidad""
                    WHERE fs.""Solicitud"" = @SolicitudId
                    ORDER BY fs.""Orden"", fs.""FechaFirma""",
                    new { SolicitudId = solicitud.Cod });

                // 3. Determinar si es válido (estado COMPLETADA)
                bool esValido = solicitud.Estado == "COMPLETADA";
                string mensaje = solicitud.Estado switch
                {
                    "COMPLETADA" => "Documento firmado correctamente por todos los firmantes",
                    "PENDIENTE" => "El documento aún tiene firmas pendientes",
                    "EN_PROCESO" => "El documento está en proceso de firma",
                    "CANCELADA" => "La solicitud de firma fue cancelada",
                    "RECHAZADA" => "La solicitud de firma fue rechazada",
                    "VENCIDA" => "La solicitud de firma venció sin completarse",
                    _ => "Estado desconocido"
                };

                _logger.LogInformation(
                    "Verificación de código {Codigo}: Estado={Estado}, Válido={Valido}",
                    codigoVerificacion, solicitud.Estado, esValido);

                return new VerificacionFirmaResponse
                {
                    Valido = esValido,
                    Mensaje = mensaje,
                    Estado = solicitud.Estado,
                    NombreDocumento = solicitud.NombreDocumento,
                    TipoFirma = solicitud.TipoFirma,
                    Entidad = solicitud.NombreEntidad,
                    FechaSolicitud = solicitud.FechaSolicitud,
                    FechaCompletada = solicitud.FechaCompletada,
                    HashOriginal = OcultarHash(solicitud.HashOriginal),
                    HashFinal = OcultarHash(solicitud.HashFinal),
                    Firmantes = firmantes.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando código {Codigo}", codigoVerificacion);
                return new VerificacionFirmaResponse
                {
                    Valido = false,
                    Mensaje = "Error al procesar la verificación"
                };
            }
        }

        public async Task<HistorialFirmaResponse?> ObtenerHistorialAsync(long entidadId, long solicitudId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 1. Obtener información básica de la solicitud
                var solicitud = await connection.QueryFirstOrDefaultAsync<SolicitudHistorialDto>(@"
                    SELECT 
                        s.""Cod"",
                        s.""CodigoVerificacion"",
                        a.""Nombre"" AS NombreDocumento,
                        s.""Estado""
                    FROM documentos.""Solicitudes_Firma"" s
                    INNER JOIN documentos.""Archivos"" a ON s.""Archivo"" = a.""Cod""
                    WHERE s.""Cod"" = @SolicitudId AND s.""Entidad"" = @EntidadId",
                    new { SolicitudId = solicitudId, EntidadId = entidadId });

                if (solicitud == null)
                    return null;

                // 2. Obtener todas las evidencias
                var evidencias = await connection.QueryAsync<EvidenciaFirmaResponse>(@"
                    SELECT 
                        e.""TipoEvidencia"",
                        e.""FechaEvidencia"",
                        e.""Descripcion"",
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS NombreFirmante,
                        e.""HashDocumento""
                    FROM documentos.""Evidencias_Firma"" e
                    INNER JOIN documentos.""Firmantes_Solicitud"" fs ON e.""Firmante"" = fs.""Cod""
                    INNER JOIN documentos.""Usuarios"" u ON fs.""Usuario"" = u.""Cod"" AND fs.""Entidad"" = u.""Entidad""
                    WHERE fs.""Solicitud"" = @SolicitudId
                    ORDER BY e.""FechaEvidencia"" ASC",
                    new { SolicitudId = solicitudId });

                return new HistorialFirmaResponse
                {
                    SolicitudId = solicitud.Cod,
                    CodigoVerificacion = solicitud.CodigoVerificacion ?? "",
                    NombreDocumento = solicitud.NombreDocumento ?? "",
                    Estado = solicitud.Estado ?? "",
                    Evidencias = evidencias.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de solicitud {SolicitudId}", solicitudId);
                return null;
            }
        }

        /// <summary>
        /// Oculta parcialmente el hash para la respuesta pública
        /// Muestra solo los primeros y últimos caracteres
        /// </summary>
        private static string? OcultarHash(string? hash)
        {
            if (string.IsNullOrEmpty(hash) || hash.Length < 16)
                return hash;

            // Mostrar formato: SHA256:abc...xyz
            return $"SHA256:{hash[..6]}...{hash[^6..]}";
        }

        // ==================== DTOs internos para mapeo ====================

        private class SolicitudVerificacionDto
        {
            public long Cod { get; set; }
            public string Estado { get; set; } = string.Empty;
            public string? TipoFirma { get; set; }
            public string? NombreDocumento { get; set; }
            public string? NombreEntidad { get; set; }
            public DateTime? FechaSolicitud { get; set; }
            public DateTime? FechaCompletada { get; set; }
            public string? HashOriginal { get; set; }
            public string? HashFinal { get; set; }
            public string? CodigoVerificacion { get; set; }
        }

        private class SolicitudHistorialDto
        {
            public long Cod { get; set; }
            public string? CodigoVerificacion { get; set; }
            public string? NombreDocumento { get; set; }
            public string? Estado { get; set; }
        }
    }
}