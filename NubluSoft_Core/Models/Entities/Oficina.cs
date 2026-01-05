namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Oficina/Dependencia de la entidad
    /// </summary>
    public class Oficina
    {
        public long Cod { get; set; }
        public string? Nombre { get; set; }
        public bool Estado { get; set; } = true;
        public long Entidad { get; set; }
        public string? Codigo { get; set; }
        public long? OficinaPadre { get; set; }
        public long? Responsable { get; set; }
        public int? NivelJerarquico { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string? Sigla { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Ubicacion { get; set; }

        // Propiedades de navegación (para respuestas enriquecidas)
        public string? NombreResponsable { get; set; }
        public string? NombreOficinaPadre { get; set; }
    }

    /// <summary>
    /// Oficina con sus hijos (para árbol jerárquico)
    /// </summary>
    public class OficinaArbol : Oficina
    {
        public List<OficinaArbol> Hijos { get; set; } = new();
    }
}