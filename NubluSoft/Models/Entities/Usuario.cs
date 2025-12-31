namespace NubluSoft.Models.Entities
{
    /// <summary>
    /// Entidad Usuario - Mapea a usuarios."Usuarios" en PostgreSQL
    /// </summary>
    public class Usuario
    {
        public long Cod { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Telefono { get; set; }
        public string? Documento { get; set; }
        public string Usuario_ { get; set; } = string.Empty; // "Usuario" en BD
        public string? Contraseña { get; set; }
        public bool Estado { get; set; } = true;
        public long Entidad { get; set; }
        public bool SesionActiva { get; set; } = false;
        public string? Token { get; set; }

        // Propiedades de navegación/auxiliares (no están en BD usuarios)
        public string? NombreEntidad { get; set; }
        public DateTime? FechaLimite { get; set; }
        public string? RolesUsuario { get; set; } // JSON de roles

        // Propiedad calculada
        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
        public string DescripcionEstado => Estado ? "Activo" : "Inactivo";
    }
}