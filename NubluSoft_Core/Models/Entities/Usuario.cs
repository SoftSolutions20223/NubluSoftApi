namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Usuario del sistema
    /// </summary>
    public class Usuario
    {
        public long Cod { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Telefono { get; set; }
        public string? Documento { get; set; }
        public string? Usuario_ { get; set; }
        public bool Estado { get; set; } = true;
        public long Entidad { get; set; }
        public string? Correo { get; set; }
        public string? TipoDocumento { get; set; }
        public string? Cargo { get; set; }
        public string? Foto { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
        public string? FirmaDigitalRuta { get; set; }
        public DateTime? FirmaDigitalVence { get; set; }
        public int? IntentosFallidos { get; set; }
        public DateTime? FechaBloqueo { get; set; }
        public bool DebeCambiarContrasena { get; set; }
        public DateTime? FechaCambioContrasena { get; set; }
        public bool NotificacionesCorreo { get; set; }
        public long? OficinaPredeterminada { get; set; }

        // Propiedades calculadas/navegación
        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
        public string? NombreOficinaPredeterminada { get; set; }
        public string? NombreTipoDocumento { get; set; }
    }

    /// <summary>
    /// Usuario con sus roles asignados
    /// </summary>
    public class UsuarioConRoles : Usuario
    {
        public List<RolUsuario> Roles { get; set; } = new();
    }

    /// <summary>
    /// Rol asignado a un usuario
    /// </summary>
    public class RolUsuario
    {
        public long Cod { get; set; }
        public long Oficina { get; set; }
        public string? NombreOficina { get; set; }
        public long Rol { get; set; }
        public string? NombreRol { get; set; }
        public bool Estado { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}