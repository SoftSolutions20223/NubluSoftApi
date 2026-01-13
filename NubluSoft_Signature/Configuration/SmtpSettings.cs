namespace NubluSoft_Signature.Configuration
{
    /// <summary>
    /// Configuración SMTP para envío de correos (OTP)
    /// </summary>
    public class SmtpSettings
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "NubluSoft - Gestión Documental";
    }
}