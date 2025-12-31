namespace NubluSoft.Configuration
{
    /// <summary>
    /// URLs de los microservicios internos desde appsettings.json
    /// </summary>
    public class ServiceEndpoints
    {
        public const string SectionName = "Services";

        public string Core { get; set; } = "http://localhost:5001";
        public string Storage { get; set; } = "http://localhost:5002";
        public string NavIndex { get; set; } = "http://localhost:5003";
    }
}