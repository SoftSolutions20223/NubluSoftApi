namespace NubluSoft_NavIndex.Configuration
{
    /// <summary>
    /// Configuración de Redis desde appsettings.json
    /// </summary>
    public class RedisSettings
    {
        public const string SectionName = "Redis";

        public string ConnectionString { get; set; } = "localhost:6379";
        public string InstanceName { get; set; } = "NubluSoft_NavIndex_";
        public int CacheExpirationMinutes { get; set; } = 1440; // 24 horas
    }
}