namespace NubluSoft_Signature.Configuration
{
    /// <summary>
    /// URLs de otros microservicios para comunicación interna
    /// </summary>
    public class ServiceSettings
    {
        public const string SectionName = "Services";

        public string Storage { get; set; } = "http://localhost:5002";
        public string Core { get; set; } = "http://localhost:5001";
    }
}