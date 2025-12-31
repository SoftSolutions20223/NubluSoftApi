namespace NubluSoft.Models.Entities
{
    /// <summary>
    /// Entidad Entidad - Mapea a usuarios."Entidades" en PostgreSQL
    /// </summary>
    public class Entidad
    {
        public long Cod { get; set; }
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public string? Nit { get; set; }
        public string? Direccion { get; set; }
        public string? Correo { get; set; }
        public DateTime? FechaLimite { get; set; }
        public long? Bd { get; set; }
        public string? Url { get; set; }
        public string? Coleccion { get; set; }

        // Propiedad calculada
        public bool PlanActivo => FechaLimite.HasValue && FechaLimite.Value >= DateTime.Today;
    }
}