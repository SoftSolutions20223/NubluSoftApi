namespace NubluSoft.Configuration
{
    /// <summary>
    /// Configuración de JWT desde appsettings.json
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; } = 60;
        public int RefreshExpirationDays { get; set; } = 7;
    }
}