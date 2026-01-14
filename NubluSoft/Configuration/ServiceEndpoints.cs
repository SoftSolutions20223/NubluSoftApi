namespace NubluSoft.Configuration
{
    public class ServiceEndpoints
    {
        public const string SectionName = "Services";

        public string Core { get; set; } = "http://localhost:5001";
        public string Storage { get; set; } = "http://localhost:5002";
        public string NavIndex { get; set; } = "http://localhost:5003";
        public string Signature { get; set; } = "http://localhost:5004"; 
    }
}