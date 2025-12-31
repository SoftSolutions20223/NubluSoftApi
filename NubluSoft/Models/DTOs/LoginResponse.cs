namespace NubluSoft.Models.DTOs
{
    /// <summary>
    /// Response del login exitoso
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiration { get; set; }
        public UserInfo? User { get; set; }
    }

    /// <summary>
    /// Información del usuario para el frontend
    /// </summary>
    public class UserInfo
    {
        public long Cod { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Usuario { get; set; }
        public long Entidad { get; set; }
        public string? NombreEntidad { get; set; }
        public string? DescripcionEstado { get; set; }
        public List<RolUsuarioInfo>? Roles { get; set; }
    }

    /// <summary>
    /// Información de rol del usuario
    /// </summary>
    public class RolUsuarioInfo
    {
        public long Oficina { get; set; }
        public long Rol { get; set; }
        public string? NombreOficina { get; set; }
        public string? NombreRol { get; set; }
    }
}