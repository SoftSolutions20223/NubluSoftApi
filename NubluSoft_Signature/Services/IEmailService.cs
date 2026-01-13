namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Servicio para envío de correos electrónicos
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envía un código OTP por correo
        /// </summary>
        /// <param name="destinatario">Email del destinatario</param>
        /// <param name="nombreDestinatario">Nombre del destinatario</param>
        /// <param name="codigo">Código OTP</param>
        /// <param name="nombreDocumento">Nombre del documento a firmar</param>
        /// <param name="minutosVigencia">Minutos de vigencia del código</param>
        /// <returns>True si se envió correctamente</returns>
        Task<bool> EnviarOtpAsync(
            string destinatario,
            string nombreDestinatario,
            string codigo,
            string nombreDocumento,
            int minutosVigencia);

        /// <summary>
        /// Envía notificación de solicitud de firma
        /// </summary>
        Task<bool> EnviarNotificacionSolicitudAsync(
            string destinatario,
            string nombreDestinatario,
            string nombreSolicitante,
            string nombreDocumento,
            string asunto,
            string? mensaje,
            DateTime? fechaVencimiento);

        /// <summary>
        /// Envía notificación de firma completada
        /// </summary>
        Task<bool> EnviarNotificacionFirmaCompletadaAsync(
            string destinatario,
            string nombreDestinatario,
            string nombreDocumento,
            string codigoVerificacion);
    }
}