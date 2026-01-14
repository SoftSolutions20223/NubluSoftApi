using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Signature.Models.DTOs
{
    // ==================== RESPONSES ====================

    /// <summary>
    /// Configuración completa de firma electrónica de una entidad
    /// </summary>
    public class ConfiguracionFirmaDto
    {
        public long Cod { get; set; }
        public long Entidad { get; set; }

        // === Firma Simple (OTP) ===
        public bool FirmaSimpleHabilitada { get; set; } = true;
        public bool OtpPorEmail { get; set; } = true;
        public bool OtpPorSms { get; set; } = false;
        public int OtpLongitud { get; set; } = 6;
        public int OtpVigenciaMinutos { get; set; } = 10;
        public int OtpMaxIntentos { get; set; } = 3;

        // === Firma Avanzada (Certificados) ===
        public bool FirmaAvanzadaHabilitada { get; set; } = true;
        public bool GenerarCertificadoAuto { get; set; } = true;
        public int VigenciaCertificadoDias { get; set; } = 365;
        public bool RequiereContrasenaFuerte { get; set; } = true;

        // === Sello Visual PDF ===
        public bool AgregarPaginaConstancia { get; set; } = true;
        public bool AgregarSelloVisual { get; set; } = true;
        public int PosicionSelloX { get; set; } = 400;
        public int PosicionSelloY { get; set; } = 50;
        public int TamanoSelloAncho { get; set; } = 200;
        public int TamanoSelloAlto { get; set; } = 70;
        public string TextoSello { get; set; } = "Firmado electrónicamente";
        public bool IncluirFechaEnSello { get; set; } = true;
        public bool IncluirNombreEnSello { get; set; } = true;

        // === Notificaciones Email ===
        public bool EnviarEmailSolicitud { get; set; } = true;
        public bool EnviarEmailRecordatorio { get; set; } = true;
        public bool EnviarEmailFirmaCompletada { get; set; } = true;
        public int DiasRecordatorioVencimiento { get; set; } = 2;

        // === Plantillas ===
        public string? PlantillaEmailSolicitud { get; set; }
        public string? PlantillaEmailOtp { get; set; }
        public string? PlantillaEmailCompletada { get; set; }
        public string? PlantillaEmailRecordatorio { get; set; }

        // === Límites ===
        public int MaxFirmantesPorSolicitud { get; set; } = 10;
        public int DiasMaxVigenciaSolicitud { get; set; } = 30;
    }

    // ==================== REQUESTS ====================

    /// <summary>
    /// Request para actualizar configuración de firma simple (OTP)
    /// </summary>
    public class ActualizarConfigOtpRequest
    {
        public bool FirmaSimpleHabilitada { get; set; } = true;
        public bool OtpPorEmail { get; set; } = true;
        public bool OtpPorSms { get; set; } = false;

        [Range(4, 8, ErrorMessage = "La longitud del OTP debe estar entre 4 y 8 dígitos")]
        public int OtpLongitud { get; set; } = 6;

        [Range(1, 60, ErrorMessage = "La vigencia debe estar entre 1 y 60 minutos")]
        public int OtpVigenciaMinutos { get; set; } = 10;

        [Range(1, 10, ErrorMessage = "Los intentos deben estar entre 1 y 10")]
        public int OtpMaxIntentos { get; set; } = 3;
    }

    /// <summary>
    /// Request para actualizar configuración de firma avanzada (certificados)
    /// </summary>
    public class ActualizarConfigCertificadosRequest
    {
        public bool FirmaAvanzadaHabilitada { get; set; } = true;
        public bool GenerarCertificadoAuto { get; set; } = true;

        [Range(30, 730, ErrorMessage = "La vigencia debe estar entre 30 y 730 días")]
        public int VigenciaCertificadoDias { get; set; } = 365;

        public bool RequiereContrasenaFuerte { get; set; } = true;
    }

    /// <summary>
    /// Request para actualizar configuración del sello visual
    /// </summary>
    public class ActualizarConfigSelloRequest
    {
        public bool AgregarPaginaConstancia { get; set; } = true;
        public bool AgregarSelloVisual { get; set; } = true;

        [Range(0, 600, ErrorMessage = "La posición X debe estar entre 0 y 600")]
        public int PosicionSelloX { get; set; } = 400;

        [Range(0, 800, ErrorMessage = "La posición Y debe estar entre 0 y 800")]
        public int PosicionSelloY { get; set; } = 50;

        [Range(50, 400, ErrorMessage = "El ancho debe estar entre 50 y 400")]
        public int TamanoSelloAncho { get; set; } = 200;

        [Range(30, 200, ErrorMessage = "El alto debe estar entre 30 y 200")]
        public int TamanoSelloAlto { get; set; } = 70;

        [MaxLength(200)]
        public string TextoSello { get; set; } = "Firmado electrónicamente";

        public bool IncluirFechaEnSello { get; set; } = true;
        public bool IncluirNombreEnSello { get; set; } = true;
    }

    /// <summary>
    /// Request para actualizar configuración de notificaciones
    /// </summary>
    public class ActualizarConfigNotificacionesRequest
    {
        public bool EnviarEmailSolicitud { get; set; } = true;
        public bool EnviarEmailRecordatorio { get; set; } = true;
        public bool EnviarEmailFirmaCompletada { get; set; } = true;

        [Range(1, 15, ErrorMessage = "Los días de recordatorio deben estar entre 1 y 15")]
        public int DiasRecordatorioVencimiento { get; set; } = 2;
    }

    /// <summary>
    /// Request para actualizar plantillas de email
    /// </summary>
    public class ActualizarPlantillasRequest
    {
        public string? PlantillaEmailSolicitud { get; set; }
        public string? PlantillaEmailOtp { get; set; }
        public string? PlantillaEmailCompletada { get; set; }
        public string? PlantillaEmailRecordatorio { get; set; }
    }

    /// <summary>
    /// Request para actualizar límites
    /// </summary>
    public class ActualizarConfigLimitesRequest
    {
        [Range(1, 50, ErrorMessage = "El máximo de firmantes debe estar entre 1 y 50")]
        public int MaxFirmantesPorSolicitud { get; set; } = 10;

        [Range(1, 90, ErrorMessage = "Los días de vigencia deben estar entre 1 y 90")]
        public int DiasMaxVigenciaSolicitud { get; set; } = 30;
    }

    /// <summary>
    /// Request para actualizar toda la configuración
    /// </summary>
    public class ActualizarConfiguracionCompletaRequest
    {
        // OTP
        public bool FirmaSimpleHabilitada { get; set; } = true;
        public bool OtpPorEmail { get; set; } = true;
        public bool OtpPorSms { get; set; } = false;
        public int OtpLongitud { get; set; } = 6;
        public int OtpVigenciaMinutos { get; set; } = 10;
        public int OtpMaxIntentos { get; set; } = 3;

        // Certificados
        public bool FirmaAvanzadaHabilitada { get; set; } = true;
        public bool GenerarCertificadoAuto { get; set; } = true;
        public int VigenciaCertificadoDias { get; set; } = 365;
        public bool RequiereContrasenaFuerte { get; set; } = true;

        // Sello
        public bool AgregarPaginaConstancia { get; set; } = true;
        public bool AgregarSelloVisual { get; set; } = true;
        public int PosicionSelloX { get; set; } = 400;
        public int PosicionSelloY { get; set; } = 50;
        public int TamanoSelloAncho { get; set; } = 200;
        public int TamanoSelloAlto { get; set; } = 70;
        public string TextoSello { get; set; } = "Firmado electrónicamente";
        public bool IncluirFechaEnSello { get; set; } = true;
        public bool IncluirNombreEnSello { get; set; } = true;

        // Notificaciones
        public bool EnviarEmailSolicitud { get; set; } = true;
        public bool EnviarEmailRecordatorio { get; set; } = true;
        public bool EnviarEmailFirmaCompletada { get; set; } = true;
        public int DiasRecordatorioVencimiento { get; set; } = 2;

        // Límites
        public int MaxFirmantesPorSolicitud { get; set; } = 10;
        public int DiasMaxVigenciaSolicitud { get; set; } = 30;
    }

    /// <summary>
    /// Resultado de operación de configuración
    /// </summary>
    public class ResultadoConfiguracionDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public ConfiguracionFirmaDto? Configuracion { get; set; }
    }

    /// <summary>
    /// Variables disponibles para plantillas de email
    /// </summary>
    public class VariablesPlantillaDto
    {
        public string Variable { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Ejemplo { get; set; } = string.Empty;
    }
}