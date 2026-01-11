using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    // ==================== RESPONSES ====================

    /// <summary>
    /// Notificación individual para mostrar al usuario
    /// </summary>
    public class NotificacionResponse
    {
        public long Cod { get; set; }
        public string TipoNotificacion { get; set; } = string.Empty;
        public string TipoNotificacionNombre { get; set; } = string.Empty;
        public string? Icono { get; set; }
        public string? Color { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Mensaje { get; set; }
        public string Prioridad { get; set; } = "MEDIA";
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaLeido { get; set; }
        public bool NoLeido { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? UsuarioOrigenNombre { get; set; }
        public long? RadicadoRef { get; set; }
        public string? NumeroRadicado { get; set; }
        public long? ArchivoRef { get; set; }
        public string? ArchivoNombre { get; set; }
        public long? SolicitudFirmaRef { get; set; }
        public string? UrlAccion { get; set; }
        public bool RequiereAccion { get; set; }
    }

    /// <summary>
    /// Lista paginada de notificaciones
    /// </summary>
    public class NotificacionesListResponse
    {
        public List<NotificacionResponse> Notificaciones { get; set; } = new();
        public int TotalRegistros { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int NoLeidasCount { get; set; }
    }

    /// <summary>
    /// Contador de notificaciones no leídas
    /// </summary>
    public class ContadorNotificacionesResponse
    {
        public int NoLeidas { get; set; }
        public int Alta { get; set; }
        public int Media { get; set; }
        public int Baja { get; set; }
    }

    /// <summary>
    /// Tipo de notificación (catálogo)
    /// </summary>
    public class TipoNotificacionResponse
    {
        public string Cod { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Icono { get; set; }
        public string? Color { get; set; }
        public bool RequiereAccion { get; set; }
    }

    // ==================== REQUESTS ====================

    /// <summary>
    /// Filtros para listar notificaciones
    /// </summary>
    public class FiltrosNotificacionesRequest
    {
        public bool? SoloNoLeidas { get; set; }
        public string? TipoNotificacion { get; set; }
        public string? Prioridad { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamañoPagina { get; set; } = 20;
    }

    /// <summary>
    /// Request para crear una notificación (uso interno)
    /// </summary>
    public class CrearNotificacionRequest
    {
        [Required]
        public string TipoNotificacion { get; set; } = string.Empty;

        [Required]
        public long UsuarioDestino { get; set; }

        public long? UsuarioOrigen { get; set; }

        [Required]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Mensaje { get; set; }

        public string Prioridad { get; set; } = "MEDIA";

        public long? RadicadoRef { get; set; }
        public long? ArchivoRef { get; set; }
        public long? SolicitudFirmaRef { get; set; }
        public string? UrlAccion { get; set; }
        public Dictionary<string, object>? DatosAdicionales { get; set; }
    }

    /// <summary>
    /// Request para marcar múltiples notificaciones como leídas
    /// </summary>
    public class MarcarLeidasRequest
    {
        public List<long>? NotificacionIds { get; set; }
        public bool MarcarTodas { get; set; } = false;
    }

    // ==================== WEBSOCKET MESSAGES ====================

    /// <summary>
    /// Mensaje enviado por WebSocket cuando hay nueva notificación
    /// </summary>
    public class NotificacionWebSocketMessage
    {
        public string Tipo { get; set; } = "NUEVA_NOTIFICACION";
        public NotificacionResponse Notificacion { get; set; } = new();
        public int TotalNoLeidas { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Mensaje de confirmación de conexión WebSocket
    /// </summary>
    public class ConexionNotificacionesMessage
    {
        public string Tipo { get; set; } = "CONECTADO";
        public int NoLeidasCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Mensaje de actualización de contador
    /// </summary>
    public class ActualizacionContadorMessage
    {
        public string Tipo { get; set; } = "CONTADOR_ACTUALIZADO";
        public int NoLeidas { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // ==================== INTERNAL MODELS ====================

    /// <summary>
    /// Payload recibido de PostgreSQL NOTIFY
    /// </summary>
    public class NotificacionCambioPayload
    {
        public string Tipo { get; set; } = string.Empty;
        public long NotificacionId { get; set; }
        public long UsuarioDestino { get; set; }
        public long Entidad { get; set; }
        public string TipoNotificacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Prioridad { get; set; } = "MEDIA";
        public bool RequiereAccion { get; set; }
        public long Timestamp { get; set; }
    }
}