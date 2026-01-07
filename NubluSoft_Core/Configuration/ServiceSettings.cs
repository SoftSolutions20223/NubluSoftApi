namespace NubluSoft_Core.Configuration
{
    /// <summary>
    /// URLs de otros microservicios para comunicación interna
    /// </summary>
    public class ServiceSettings
    {
        public const string SectionName = "Services";

        public string Storage { get; set; } = "http://localhost:5002";
    }
}