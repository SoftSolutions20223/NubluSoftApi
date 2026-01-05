using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface IUsuariosService
    {
        Task<IEnumerable<Usuario>> ObtenerPorEntidadAsync(long entidadId);
        Task<UsuarioConRoles?> ObtenerPorIdAsync(long entidadId, long usuarioId);
        Task<(Usuario? Usuario, string? Error)> CrearAsync(long entidadId, CrearUsuarioRequest request);
        Task<(bool Success, string? Error)> ActualizarAsync(long entidadId, long usuarioId, ActualizarUsuarioRequest request);
        Task<(bool Success, string? Error)> EliminarAsync(long entidadId, long usuarioId);
        Task<(bool Success, string? Error)> CambiarContrasenaAsync(long entidadId, long usuarioId, CambiarContrasenaRequest request);
        Task<(bool Success, string? Error)> ResetearContrasenaAsync(long entidadId, long usuarioId, ResetearContrasenaRequest request);
        Task<(bool Success, string? Error)> AsignarRolAsync(long entidadId, long usuarioId, AsignarRolRequest request);
        Task<(bool Success, string? Error)> RevocarRolAsync(long entidadId, long usuarioId, long oficina, long rol);
        Task<IEnumerable<RolUsuario>> ObtenerRolesUsuarioAsync(long usuarioId);
    }
}