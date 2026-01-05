namespace NubluSoft_Core.Configuration
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "NubluSoft";
        public string Audience { get; set; } = "NubluSoftClients";
    }
}