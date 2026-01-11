using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NubluSoft_Core.Extensions;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Hubs
{
    /// <summary>
    /// Hub de SignalR para notificaciones en tiempo real
    /// </summary>
    [Authorize]
    public class NotificacionesHub : Hub
    {
        private readonly ILogger<NotificacionesHub> _logger;
        private readonly IServiceProvider _serviceProvider;

        public NotificacionesHub(
            ILogger<NotificacionesHub> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Se ejecuta cuando un cliente se conecta
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var usuarioId = Context.User?.GetUserId() ?? 0;
            var entidadId = Context.User?.GetEntidadId() ?? 0;

            if (usuarioId == 0 || entidadId == 0)
            {
                _logger.LogWarning("Conexión rechazada: usuario o entidad no válidos. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                Context.Abort();
                return;
            }

            // Agregar a grupo específico del usuario (entidad_usuario)
            var grupoUsuario = $"usuario_{entidadId}_{usuarioId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, grupoUsuario);

            // También agregar al grupo de la entidad (para notificaciones broadcast)
            var grupoEntidad = $"entidad_{entidadId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, grupoEntidad);

            _logger.LogInformation(
                "Usuario {UsuarioId} conectado a NotificacionesHub. Entidad: {EntidadId}, ConnectionId: {ConnectionId}",
                usuarioId, entidadId, Context.ConnectionId);

            // Enviar confirmación de conexión con contador actual
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificacionService = scope.ServiceProvider.GetRequiredService<INotificacionService>();
                var contador = await notificacionService.ObtenerContadorAsync(entidadId, usuarioId);

                await Clients.Caller.SendAsync("Conectado", new ConexionNotificacionesMessage
                {
                    Tipo = "CONECTADO",
                    NoLeidasCount = contador.NoLeidas,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo contador inicial para usuario {UsuarioId}", usuarioId);

                // Enviar conexión sin contador
                await Clients.Caller.SendAsync("Conectado", new ConexionNotificacionesMessage
                {
                    Tipo = "CONECTADO",
                    NoLeidasCount = 0,
                    Timestamp = DateTime.UtcNow
                });
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Se ejecuta cuando un cliente se desconecta
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var usuarioId = Context.User?.GetUserId() ?? 0;
            var entidadId = Context.User?.GetEntidadId() ?? 0;

            if (exception != null)
            {
                _logger.LogWarning(exception,
                    "Usuario {UsuarioId} desconectado con error. ConnectionId: {ConnectionId}",
                    usuarioId, Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation(
                    "Usuario {UsuarioId} desconectado de NotificacionesHub. ConnectionId: {ConnectionId}",
                    usuarioId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Método para que el cliente solicite el contador actual
        /// </summary>
        public async Task ObtenerContador()
        {
            var usuarioId = Context.User?.GetUserId() ?? 0;
            var entidadId = Context.User?.GetEntidadId() ?? 0;

            if (usuarioId == 0 || entidadId == 0)
            {
                await Clients.Caller.SendAsync("Error", new { Message = "Usuario no autenticado" });
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificacionService = scope.ServiceProvider.GetRequiredService<INotificacionService>();
                var contador = await notificacionService.ObtenerContadorAsync(entidadId, usuarioId);

                await Clients.Caller.SendAsync("ContadorActualizado", new ActualizacionContadorMessage
                {
                    Tipo = "CONTADOR_ACTUALIZADO",
                    NoLeidas = contador.NoLeidas,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo contador para usuario {UsuarioId}", usuarioId);
                await Clients.Caller.SendAsync("Error", new { Message = "Error obteniendo contador" });
            }
        }

        /// <summary>
        /// Método para mantener la conexión activa (heartbeat)
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", new { Timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Marca una notificación como leída desde el WebSocket
        /// </summary>
        public async Task MarcarLeida(long notificacionId)
        {
            var usuarioId = Context.User?.GetUserId() ?? 0;
            var entidadId = Context.User?.GetEntidadId() ?? 0;

            if (usuarioId == 0 || entidadId == 0)
            {
                await Clients.Caller.SendAsync("Error", new { Message = "Usuario no autenticado" });
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificacionService = scope.ServiceProvider.GetRequiredService<INotificacionService>();

                var resultado = await notificacionService.MarcarComoLeidaAsync(entidadId, usuarioId, notificacionId);

                if (resultado)
                {
                    // Enviar contador actualizado
                    var contador = await notificacionService.ObtenerContadorAsync(entidadId, usuarioId);

                    await Clients.Caller.SendAsync("NotificacionLeida", new
                    {
                        NotificacionId = notificacionId,
                        NoLeidas = contador.NoLeidas,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", new { Message = "No se pudo marcar como leída" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando notificación {NotificacionId} como leída vía WebSocket", notificacionId);
                await Clients.Caller.SendAsync("Error", new { Message = "Error procesando solicitud" });
            }
        }
    }

    /// <summary>
    /// Servicio para enviar notificaciones a través del Hub desde otros servicios
    /// </summary>
    public interface INotificacionesHubService
    {
        /// <summary>
        /// Envía una notificación a un usuario específico
        /// </summary>
        Task EnviarAUsuarioAsync(long entidadId, long usuarioId, NotificacionWebSocketMessage mensaje);

        /// <summary>
        /// Envía una notificación a todos los usuarios de una entidad
        /// </summary>
        Task EnviarAEntidadAsync(long entidadId, NotificacionWebSocketMessage mensaje);

        /// <summary>
        /// Envía actualización de contador a un usuario
        /// </summary>
        Task ActualizarContadorAsync(long entidadId, long usuarioId, int noLeidas);
    }

    /// <summary>
    /// Implementación del servicio para enviar notificaciones vía Hub
    /// </summary>
    public class NotificacionesHubService : INotificacionesHubService
    {
        private readonly IHubContext<NotificacionesHub> _hubContext;
        private readonly ILogger<NotificacionesHubService> _logger;

        public NotificacionesHubService(
            IHubContext<NotificacionesHub> hubContext,
            ILogger<NotificacionesHubService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task EnviarAUsuarioAsync(long entidadId, long usuarioId, NotificacionWebSocketMessage mensaje)
        {
            var grupo = $"usuario_{entidadId}_{usuarioId}";

            try
            {
                await _hubContext.Clients.Group(grupo).SendAsync("NuevaNotificacion", mensaje);

                _logger.LogDebug(
                    "Notificación enviada a usuario {UsuarioId} en entidad {EntidadId}",
                    usuarioId, entidadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error enviando notificación a usuario {UsuarioId} en entidad {EntidadId}",
                    usuarioId, entidadId);
            }
        }

        public async Task EnviarAEntidadAsync(long entidadId, NotificacionWebSocketMessage mensaje)
        {
            var grupo = $"entidad_{entidadId}";

            try
            {
                await _hubContext.Clients.Group(grupo).SendAsync("NuevaNotificacion", mensaje);

                _logger.LogDebug("Notificación broadcast enviada a entidad {EntidadId}", entidadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando notificación broadcast a entidad {EntidadId}", entidadId);
            }
        }

        public async Task ActualizarContadorAsync(long entidadId, long usuarioId, int noLeidas)
        {
            var grupo = $"usuario_{entidadId}_{usuarioId}";

            try
            {
                await _hubContext.Clients.Group(grupo).SendAsync("ContadorActualizado", new ActualizacionContadorMessage
                {
                    Tipo = "CONTADOR_ACTUALIZADO",
                    NoLeidas = noLeidas,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error actualizando contador para usuario {UsuarioId} en entidad {EntidadId}",
                    usuarioId, entidadId);
            }
        }
    }
}