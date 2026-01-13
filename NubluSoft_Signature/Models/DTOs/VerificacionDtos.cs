namespace NubluSoft_Signature.Models.DTOs
{
    /// <summary>
    /// Respuesta de verificación pública de firma
    /// </summary>
    public class VerificacionFirmaResponse
    {
        /// <summary>
        /// Indica si el código es válido y el documento fue firmado
        /// </summary>
        public bool Valido { get; set; }

        /// <summary>
        /// Mensaje descriptivo del resultado
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Estado de la solicitud de firma
        /// </summary>
        public string? Estado { get; set; }

        /// <summary>
        /// Nombre del documento firmado
        /// </summary>
        public string? NombreDocumento { get; set; }

        /// <summary>
        /// Tipo de firma utilizada (SIMPLE o AVANZADA)
        /// </summary>
        public string? TipoFirma { get; set; }

        /// <summary>
        /// Nombre de la entidad que emitió la solicitud
        /// </summary>
        public string? Entidad { get; set; }

        /// <summary>
        /// Fecha de creación de la solicitud
        /// </summary>
        public DateTime? FechaSolicitud { get; set; }

        /// <summary>
        /// Fecha en que se completaron todas las firmas
        /// </summary>
        public DateTime? FechaCompletada { get; set; }

        /// <summary>
        /// Hash original del documento al crear la solicitud
        /// </summary>
        public string? HashOriginal { get; set; }

        /// <summary>
        /// Hash final del documento después de firmar
        /// </summary>
        public string? HashFinal { get; set; }

        /// <summary>
        /// Lista de firmantes con sus datos públicos
        /// </summary>
        public List<FirmantePublicoResponse> Firmantes { get; set; } = new();
    }

    /// <summary>
    /// Información pública de un firmante (sin datos sensibles)
    /// </summary>
    public class FirmantePublicoResponse
    {
        /// <summary>
        /// Nombre completo del firmante
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Cargo del firmante (si está disponible)
        /// </summary>
        public string? Cargo { get; set; }

        /// <summary>
        /// Rol asignado en la firma (ej: "Revisor", "Aprobador")
        /// </summary>
        public string? RolFirmante { get; set; }

        /// <summary>
        /// Estado del firmante
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Orden de firma (si era secuencial)
        /// </summary>
        public int Orden { get; set; }

        /// <summary>
        /// Fecha y hora de la firma
        /// </summary>
        public DateTime? FechaFirma { get; set; }

        /// <summary>
        /// Tipo de firma utilizada (SIMPLE_OTP, AVANZADA_CERTIFICADO)
        /// </summary>
        public string? TipoFirmaUsada { get; set; }
    }

    /// <summary>
    /// Respuesta de historial de verificación (para auditoría interna)
    /// </summary>
    public class HistorialFirmaResponse
    {
        public long SolicitudId { get; set; }
        public string CodigoVerificacion { get; set; } = string.Empty;
        public string NombreDocumento { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public List<EvidenciaFirmaResponse> Evidencias { get; set; } = new();
    }

    /// <summary>
    /// Evidencia de una acción de firma (para auditoría)
    /// </summary>
    public class EvidenciaFirmaResponse
    {
        public string TipoEvidencia { get; set; } = string.Empty;
        public DateTime FechaEvidencia { get; set; }
        public string? Descripcion { get; set; }
        public string? NombreFirmante { get; set; }
        public string? HashDocumento { get; set; }
    }
}