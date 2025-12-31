using NubluSoft.Models.DTOs;

namespace NubluSoft.Services
{
    /// <summary>
    /// Interfaz para el servicio de sesiones en Redis
    /// </summary>
    public interface IRedisSessionService
    {
        /// <summary>
        /// Guarda una sesión de usuario en Redis
        /// </summary>
        Task GuardarSesionAsync(UserSessionDto session);

        /// <summary>
        /// Obtiene una sesión por su ID
        /// </summary>
        Task<UserSessionDto?> ObtenerSesionAsync(string sessionId);

        /// <summary>
        /// Obtiene una sesión por el refresh token
        /// </summary>
        Task<UserSessionDto?> ObtenerSesionPorRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Actualiza la última actividad de la sesión
        /// </summary>
        Task ActualizarActividadAsync(string sessionId);

        /// <summary>
        /// Elimina una sesión (logout)
        /// </summary>
        Task EliminarSesionAsync(string sessionId);

        /// <summary>
        /// Elimina todas las sesiones de un usuario
        /// </summary>
        Task EliminarTodasSesionesUsuarioAsync(long usuarioId);

        /// <summary>
        /// Verifica si una sesión es válida
        /// </summary>
        Task<bool> SesionValidaAsync(string sessionId);

        /// <summary>
        /// Obtiene todas las sesiones activas de un usuario
        /// </summary>
        Task<List<UserSessionDto>> ObtenerSesionesUsuarioAsync(long usuarioId);
    }
}