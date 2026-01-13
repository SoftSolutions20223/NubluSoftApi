namespace NubluSoft_Signature.Models.Enums
{
    /// <summary>
    /// Estados posibles de una solicitud de firma
    /// </summary>
    public enum EstadoSolicitud
    {
        PENDIENTE,      // Creada, esperando firmas
        EN_PROCESO,     // Al menos uno ha firmado
        COMPLETADA,     // Todos firmaron
        CANCELADA,      // Cancelada por el solicitante
        RECHAZADA,      // Rechazada por algún firmante
        VENCIDA         // Pasó la fecha de vencimiento
    }

    /// <summary>
    /// Estados posibles de un firmante
    /// </summary>
    public enum EstadoFirmante
    {
        PENDIENTE,      // Aún no le toca o no ha iniciado
        NOTIFICADO,     // Ya fue notificado (le toca firmar)
        FIRMADO,        // Ya firmó
        RECHAZADO       // Rechazó la firma
    }

    /// <summary>
    /// Tipos de firma electrónica
    /// </summary>
    public enum TipoFirma
    {
        SIMPLE,         // Verificación por OTP
        AVANZADA        // Con certificado X.509
    }

    /// <summary>
    /// Medio de envío del OTP
    /// </summary>
    public enum MedioOtp
    {
        EMAIL,
        SMS
    }
}