namespace NubluSoft.Models.DTOs
{
    /// <summary>
    /// Datos de sesión almacenados en Redis
    /// </summary>
    public class UserSessionDto
    {
        public long Cod { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Usuario { get; set; }
        public long Entidad { get; set; }
        public string? NombreEntidad { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime TokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public List<RolUsuarioInfo>? Roles { get; set; }

        // Para identificar la sesión única
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
    }
}