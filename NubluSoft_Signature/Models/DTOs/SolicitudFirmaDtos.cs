using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Signature.Models.DTOs
{
    // ==================== REQUEST DTOs ====================

    /// <summary>
    /// Request para crear una nueva solicitud de firma
    /// </summary>
    public class CrearSolicitudRequest
    {
        [Required(ErrorMessage = "El archivo es requerido")]
        public long ArchivoId { get; set; }

        [Required(ErrorMessage = "El tipo de firma es requerido")]
        [RegularExpression("^(SIMPLE|AVANZADA)$", ErrorMessage = "Tipo de firma inválido")]
        public string TipoFirma { get; set; } = "SIMPLE";

        [Required(ErrorMessage = "El asunto es requerido")]
        [MaxLength(200, ErrorMessage = "El asunto no puede exceder 200 caracteres")]
        public string Asunto { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres")]
        public string? Mensaje { get; set; }

        /// <summary>
        /// Si es true, los firmantes deben firmar en orden
        /// </summary>
        public bool OrdenSecuencial { get; set; } = false;

        /// <summary>
        /// Días de vigencia de la solicitud (default: 7)
        /// </summary>
        [Range(1, 30, ErrorMessage = "La vigencia debe estar entre 1 y 30 días")]
        public int DiasVigencia { get; set; } = 7;

        /// <summary>
        /// Lista de usuarios que deben firmar
        /// </summary>
        [Required(ErrorMessage = "Debe especificar al menos un firmante")]
        [MinLength(1, ErrorMessage = "Debe especificar al menos un firmante")]
        public List<FirmanteRequest> Firmantes { get; set; } = new();
    }

    /// <summary>
    /// Datos de un firmante en la solicitud
    /// </summary>
    public class FirmanteRequest
    {
        [Required(ErrorMessage = "El usuario es requerido")]
        public long UsuarioId { get; set; }

        /// <summary>
        /// Orden de firma (solo aplica si OrdenSecuencial = true)
        /// </summary>
        [Range(1, 20, ErrorMessage = "El orden debe estar entre 1 y 20")]
        public int Orden { get; set; } = 1;

        /// <summary>
        /// Rol del firmante (ej: "Revisor", "Aprobador", "Jefe")
        /// </summary>
        [MaxLength(50)]
        public string? RolFirmante { get; set; }
    }

    /// <summary>
    /// Request para cancelar una solicitud
    /// </summary>
    public class CancelarSolicitudRequest
    {
        [Required(ErrorMessage = "El motivo es requerido")]
        [MaxLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string Motivo { get; set; } = string.Empty;
    }

    // ==================== RESPONSE DTOs ====================

    /// <summary>
    /// Respuesta al crear/consultar una solicitud
    /// </summary>
    public class SolicitudFirmaResponse
    {
        public long Cod { get; set; }
        public long Entidad { get; set; }
        public long ArchivoId { get; set; }
        public string? NombreArchivo { get; set; }
        public string TipoFirma { get; set; } = string.Empty;
        public bool OrdenSecuencial { get; set; }
        public string Estado { get; set; } = string.Empty;
        public long SolicitadoPor { get; set; }
        public string? NombreSolicitante { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaCompletada { get; set; }
        public string Asunto { get; set; } = string.Empty;
        public string? Mensaje { get; set; }
        public string? CodigoVerificacion { get; set; }
        public int TotalFirmantes { get; set; }
        public int FirmantesCompletados { get; set; }
        public List<FirmanteResponse> Firmantes { get; set; } = new();
    }

    /// <summary>
    /// Información de un firmante
    /// </summary>
    public class FirmanteResponse
    {
        public long Cod { get; set; }
        public long UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int Orden { get; set; }
        public string? RolFirmante { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaNotificacion { get; set; }
        public DateTime? FechaFirma { get; set; }
        public string? TipoFirmaUsada { get; set; }
        public DateTime? FechaRechazo { get; set; }
        public string? MotivoRechazo { get; set; }
        public bool EsMiTurno { get; set; }
    }

    /// <summary>
    /// Solicitud pendiente para el usuario actual
    /// </summary>
    public class SolicitudPendienteResponse
    {
        public long SolicitudId { get; set; }
        public long FirmanteId { get; set; }
        public string Asunto { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public string NombreSolicitante { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string TipoFirma { get; set; } = string.Empty;
        public int Orden { get; set; }
        public string? RolFirmante { get; set; }
        public bool EsMiTurno { get; set; }
        public int DiasRestantes { get; set; }
    }

    /// <summary>
    /// Resultado de operación genérica
    /// </summary>
    public class OperacionFirmaResult
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long? SolicitudId { get; set; }
        public string? CodigoVerificacion { get; set; }
    }

    /// <summary>
    /// Resumen de solicitudes para dashboard
    /// </summary>
    public class ResumenSolicitudesResponse
    {
        public int Pendientes { get; set; }
        public int EnProceso { get; set; }
        public int Completadas { get; set; }
        public int Rechazadas { get; set; }
        public int Vencidas { get; set; }
        public int MisFirmasPendientes { get; set; }
    }
}