namespace NubluSoft.Configuration
{
    /// <summary>
    /// Configuración de Redis desde appsettings.json
    /// </summary>
    public class RedisSettings
    {
        public const string SectionName = "Redis";

        public string ConnectionString { get; set; } = "localhost:6379";
        public string InstanceName { get; set; } = "NubluSoft_";
        public int SessionExpirationMinutes { get; set; } = 60;
    }
}