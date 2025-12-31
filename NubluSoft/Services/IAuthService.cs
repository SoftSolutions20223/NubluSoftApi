using NubluSoft.Models.DTOs;
using NubluSoft.Models.Entities;

namespace NubluSoft.Services
{
    /// <summary>
    /// Interfaz para el servicio de autenticación
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Valida las credenciales del usuario contra PostgreSQL
        /// </summary>
        Task<(Usuario? Usuario, string? Error)> ValidarCredencialesAsync(string usuario, string contraseña);

        /// <summary>
        /// Obtiene la información de la entidad del usuario
        /// </summary>
        Task<Entidad?> ObtenerEntidadAsync(long entidadId);

        /// <summary>
        /// Obtiene los roles del usuario
        /// </summary>
        Task<List<RolUsuarioInfo>> ObtenerRolesUsuarioAsync(long usuarioId);

        /// <summary>
        /// Actualiza el token en la base de datos
        /// </summary>
        Task ActualizarTokenUsuarioAsync(long usuarioId, string token);

        /// <summary>
        /// Limpia el token del usuario (logout)
        /// </summary>
        Task LimpiarTokenUsuarioAsync(long usuarioId);
    }
}