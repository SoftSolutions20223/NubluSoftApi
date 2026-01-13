using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Signature.Models.DTOs
{
    // ==================== OTP REQUEST DTOs ====================

    /// <summary>
    /// Request para generar código OTP
    /// </summary>
    public class GenerarOtpRequest
    {
        /// <summary>
        /// Medio de envío: EMAIL o SMS
        /// </summary>
        [Required(ErrorMessage = "El medio de envío es requerido")]
        [RegularExpression("^(EMAIL|SMS)$", ErrorMessage = "Medio inválido. Use EMAIL o SMS")]
        public string Medio { get; set; } = "EMAIL";
    }

    /// <summary>
    /// Request para validar código OTP y firmar
    /// </summary>
    public class ValidarOtpRequest
    {
        [Required(ErrorMessage = "El código es requerido")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Código inválido")]
        public string Codigo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para rechazar una firma
    /// </summary>
    public class RechazarFirmaRequest
    {
        [Required(ErrorMessage = "El motivo es requerido")]
        [MaxLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string Motivo { get; set; } = string.Empty;
    }

    // ==================== OTP RESPONSE DTOs ====================

    /// <summary>
    /// Respuesta al generar OTP
    /// </summary>
    public class GenerarOtpResponse
    {
        public bool Enviado { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Destino enmascarado (ej: "u***@***.gov.co")
        /// </summary>
        public string? DestinoEnmascarado { get; set; }

        /// <summary>
        /// Segundos hasta expiración
        /// </summary>
        public int ExpiraEnSegundos { get; set; }

        /// <summary>
        /// Intentos restantes
        /// </summary>
        public int IntentosRestantes { get; set; }
    }

    /// <summary>
    /// Respuesta al validar OTP
    /// </summary>
    public class ValidarOtpResponse
    {
        public bool Valido { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? IntentosRestantes { get; set; }
    }

    // ==================== FIRMA RESPONSE DTOs ====================

    /// <summary>
    /// Resultado del proceso de firma
    /// </summary>
    public class ResultadoFirmaResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public bool SolicitudCompletada { get; set; }
        public long? SiguienteFirmanteId { get; set; }
        public string? CodigoVerificacion { get; set; }
    }

    /// <summary>
    /// Información del documento a firmar
    /// </summary>
    public class InfoDocumentoFirmaResponse
    {
        public long SolicitudId { get; set; }
        public long FirmanteId { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string? Mensaje { get; set; }
        public string NombreSolicitante { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string TipoFirma { get; set; } = string.Empty;
        public string? RolFirmante { get; set; }
        public int Orden { get; set; }
        public bool EsMiTurno { get; set; }
        public string? UrlDescarga { get; set; }
    }
}