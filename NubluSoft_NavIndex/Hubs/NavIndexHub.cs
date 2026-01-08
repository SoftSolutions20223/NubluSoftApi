using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NubluSoft_NavIndex.Extensions;
using NubluSoft_NavIndex.Models.DTOs;
using NubluSoft_NavIndex.Services;

namespace NubluSoft_NavIndex.Hubs
{
    /// <summary>
    /// Hub de SignalR para actualizaciones en tiempo real de NavIndex
    /// </summary>
    [Authorize]
    public class NavIndexHub : Hub
    {
        private readonly ILogger<NavIndexHub> _logger;
        private readonly INotificationService _notificationService;

        // Grupos por entidad: "entidad_1", "entidad_2", etc.
        private const string GroupPrefix = "entidad_";

        public NavIndexHub(
            ILogger<NavIndexHub> logger,
            INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Cuando un cliente se conecta
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var entidadId = Context.User?.GetEntidadId() ?? 0;
            var userId = Context.User?.GetUserId() ?? 0;
            var userName = Context.User?.GetUserName() ?? "Desconocido";

            if (entidadId == 0)
            {
                _logger.LogWarning("Cliente intentó conectarse sin entidad válida. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                Context.Abort();
                return;
            }

            // Agregar al grupo de su entidad
            var groupName = $"{GroupPrefix}{entidadId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation(
                "Cliente conectado. Usuario: {UserName} ({UserId}), Entidad: {EntidadId}, ConnectionId: {ConnectionId}",
                userName, userId, entidadId, Context.ConnectionId);

            // Enviar confirmación al cliente
            await Clients.Caller.SendAsync("Conectado", new
            {
                mensaje = "Conectado exitosamente",
                entidadId,
                connectionId = Context.ConnectionId,
                timestamp = DateTime.UtcNow
            });

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Cuando un cliente se desconecta
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var entidadId = Context.User?.GetEntidadId() ?? 0;
            var userId = Context.User?.GetUserId() ?? 0;

            if (entidadId > 0)
            {
                var groupName = $"{GroupPrefix}{entidadId}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }

            if (exception != null)
            {
                _logger.LogWarning(exception,
                    "Cliente desconectado con error. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation(
                    "Cliente desconectado. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Cliente solicita la versión actual de la estructura
        /// </summary>
        public async Task ObtenerVersion()
        {
            var entidadId = Context.User?.GetEntidadId() ?? 0;

            if (entidadId == 0)
            {
                await Clients.Caller.SendAsync("Error", new { mensaje = "Entidad no válida" });
                return;
            }

            // Aquí podrías obtener la versión del servicio si lo necesitas
            await Clients.Caller.SendAsync("Version", new
            {
                entidadId,
                version = DateTime.UtcNow.Ticks.ToString(),
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Ping para mantener la conexión activa
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", new
            {
                timestamp = DateTime.UtcNow
            });
        }
    }
}