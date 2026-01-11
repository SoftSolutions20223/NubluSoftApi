using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Servicio para gestión de notificaciones de usuario
    /// </summary>
    public interface INotificacionService
    {
        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene las notificaciones del usuario con filtros y paginación
        /// </summary>
        Task<NotificacionesListResponse> ObtenerNotificacionesAsync(
            long entidadId,
            long usuarioId,
            FiltrosNotificacionesRequest? filtros = null);

        /// <summary>
        /// Obtiene una notificación específica por su ID
        /// </summary>
        Task<NotificacionResponse?> ObtenerPorIdAsync(
            long entidadId,
            long usuarioId,
            long notificacionId);

        /// <summary>
        /// Obtiene el conteo de notificaciones no leídas
        /// </summary>
        Task<ContadorNotificacionesResponse> ObtenerContadorAsync(
            long entidadId,
            long usuarioId);

        /// <summary>
        /// Obtiene los tipos de notificación disponibles
        /// </summary>
        Task<IEnumerable<TipoNotificacionResponse>> ObtenerTiposNotificacionAsync();

        // ==================== ACCIONES ====================

        /// <summary>
        /// Marca una notificación como leída
        /// </summary>
        Task<bool> MarcarComoLeidaAsync(
            long entidadId,
            long usuarioId,
            long notificacionId);

        /// <summary>
        /// Marca todas las notificaciones del usuario como leídas
        /// </summary>
        Task<int> MarcarTodasComoLeidasAsync(
            long entidadId,
            long usuarioId);

        /// <summary>
        /// Marca múltiples notificaciones como leídas
        /// </summary>
        Task<int> MarcarVariasComoLeidasAsync(
            long entidadId,
            long usuarioId,
            List<long> notificacionIds);

        /// <summary>
        /// Elimina (marca como eliminada) una notificación
        /// </summary>
        Task<bool> EliminarAsync(
            long entidadId,
            long usuarioId,
            long notificacionId);

        // ==================== CREACIÓN (USO INTERNO) ====================

        /// <summary>
        /// Crea una nueva notificación (llamado internamente por triggers o servicios)
        /// </summary>
        Task<NotificacionResponse?> CrearNotificacionAsync(
            long entidadId,
            CrearNotificacionRequest request);

        // ==================== WEBSOCKET ====================

        /// <summary>
        /// Obtiene una notificación formateada para enviar por WebSocket
        /// </summary>
        Task<NotificacionWebSocketMessage?> ObtenerParaWebSocketAsync(
            long entidadId,
            long usuarioId,
            long notificacionId);
    }
}