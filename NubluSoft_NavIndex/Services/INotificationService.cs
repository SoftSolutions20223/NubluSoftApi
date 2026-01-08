using NubluSoft_NavIndex.Models.DTOs;

namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Servicio para manejar notificaciones de cambios
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Procesa un cambio recibido de PostgreSQL NOTIFY
        /// </summary>
        Task ProcesarCambioAsync(CambioNavIndex cambio);

        /// <summary>
        /// Registra un handler para recibir notificaciones de cambios
        /// </summary>
        void OnCambio(Func<MensajeActualizacion, Task> handler);
    }

    /// <summary>
    /// Representa un cambio recibido de PostgreSQL
    /// </summary>
    public class CambioNavIndex
    {
        public string TipoCambio { get; set; } = string.Empty;
        public long CarpetaId { get; set; }
        public long TipoCarpeta { get; set; }
        public long? CarpetaPadre { get; set; }
        public long? SerieRaiz { get; set; }
        public string? Nombre { get; set; }
        public bool EsEstructura { get; set; }
        public long Timestamp { get; set; }
    }
}