using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NubluSoft_Signature.Configuration;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del servicio de correo con MailKit
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<SmtpSettings> smtpSettings,
            ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task<bool> EnviarOtpAsync(
            string destinatario,
            string nombreDestinatario,
            string codigo,
            string nombreDocumento,
            int minutosVigencia)
        {
            var asunto = "Código de verificación para firma electrónica";

            var cuerpo = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c5282; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f7fafc; }}
        .code {{ font-size: 32px; font-weight: bold; color: #2c5282; text-align: center; 
                 padding: 20px; background-color: #edf2f7; border-radius: 8px; margin: 20px 0;
                 letter-spacing: 8px; }}
        .warning {{ color: #c53030; font-size: 14px; margin-top: 20px; }}
        .footer {{ text-align: center; padding: 20px; color: #718096; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Firma Electrónica</h1>
        </div>
        <div class='content'>
            <p>Estimado(a) <strong>{nombreDestinatario}</strong>,</p>
            <p>Ha solicitado firmar electrónicamente el documento:</p>
            <p><strong>{nombreDocumento}</strong></p>
            <p>Su código de verificación es:</p>
            <div class='code'>{codigo}</div>
            <p class='warning'>
                ⚠️ Este código expira en <strong>{minutosVigencia} minutos</strong>.<br>
                No comparta este código con nadie.
            </p>
        </div>
        <div class='footer'>
            <p>Este es un mensaje automático del sistema de gestión documental.</p>
            <p>Si no solicitó este código, ignore este mensaje.</p>
        </div>
    </div>
</body>
</html>";

            return await EnviarCorreoAsync(destinatario, nombreDestinatario, asunto, cuerpo);
        }

        public async Task<bool> EnviarNotificacionSolicitudAsync(
            string destinatario,
            string nombreDestinatario,
            string nombreSolicitante,
            string nombreDocumento,
            string asunto,
            string? mensaje,
            DateTime? fechaVencimiento)
        {
            var asuntoEmail = $"Solicitud de firma: {asunto}";
            var vencimiento = fechaVencimiento?.ToString("dd/MM/yyyy HH:mm") ?? "Sin fecha límite";

            var cuerpo = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c5282; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f7fafc; }}
        .info {{ background-color: #edf2f7; padding: 15px; border-radius: 8px; margin: 15px 0; }}
        .button {{ display: inline-block; background-color: #2c5282; color: white; 
                   padding: 12px 24px; text-decoration: none; border-radius: 4px; margin-top: 15px; }}
        .footer {{ text-align: center; padding: 20px; color: #718096; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Solicitud de Firma</h1>
        </div>
        <div class='content'>
            <p>Estimado(a) <strong>{nombreDestinatario}</strong>,</p>
            <p><strong>{nombreSolicitante}</strong> le ha solicitado firmar el siguiente documento:</p>
            <div class='info'>
                <p><strong>Documento:</strong> {nombreDocumento}</p>
                <p><strong>Asunto:</strong> {asunto}</p>
                <p><strong>Fecha límite:</strong> {vencimiento}</p>
                {(string.IsNullOrEmpty(mensaje) ? "" : $"<p><strong>Mensaje:</strong> {mensaje}</p>")}
            </div>
            <p>Por favor ingrese al sistema para revisar y firmar el documento.</p>
        </div>
        <div class='footer'>
            <p>Este es un mensaje automático del sistema de gestión documental.</p>
        </div>
    </div>
</body>
</html>";

            return await EnviarCorreoAsync(destinatario, nombreDestinatario, asuntoEmail, cuerpo);
        }

        public async Task<bool> EnviarNotificacionFirmaCompletadaAsync(
            string destinatario,
            string nombreDestinatario,
            string nombreDocumento,
            string codigoVerificacion)
        {
            var asunto = $"Documento firmado: {nombreDocumento}";

            var cuerpo = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #38a169; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f7fafc; }}
        .success {{ background-color: #c6f6d5; padding: 15px; border-radius: 8px; 
                    margin: 15px 0; text-align: center; }}
        .code {{ font-family: monospace; background-color: #edf2f7; padding: 10px; 
                 border-radius: 4px; }}
        .footer {{ text-align: center; padding: 20px; color: #718096; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Documento Firmado</h1>
        </div>
        <div class='content'>
            <p>Estimado(a) <strong>{nombreDestinatario}</strong>,</p>
            <div class='success'>
                <p>El documento <strong>{nombreDocumento}</strong> ha sido firmado exitosamente por todos los firmantes.</p>
            </div>
            <p>Código de verificación:</p>
            <p class='code'>{codigoVerificacion}</p>
            <p>Puede usar este código para verificar la autenticidad del documento en cualquier momento.</p>
        </div>
        <div class='footer'>
            <p>Este es un mensaje automático del sistema de gestión documental.</p>
        </div>
    </div>
</body>
</html>";

            return await EnviarCorreoAsync(destinatario, nombreDestinatario, asunto, cuerpo);
        }

        private async Task<bool> EnviarCorreoAsync(
            string destinatario,
            string nombreDestinatario,
            string asunto,
            string cuerpoHtml)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
                message.To.Add(new MailboxAddress(nombreDestinatario, destinatario));
                message.Subject = asunto;

                var builder = new BodyBuilder
                {
                    HtmlBody = cuerpoHtml
                };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();

                var secureOption = _smtpSettings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, secureOption);

                if (!string.IsNullOrEmpty(_smtpSettings.Username))
                {
                    await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Correo enviado exitosamente a {Destinatario}", destinatario);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando correo a {Destinatario}", destinatario);
                return false;
            }
        }
    }
}