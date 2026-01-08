using Microsoft.AspNetCore.SignalR;
using NubluSoft_NavIndex.Hubs;
using NubluSoft_NavIndex.Models.DTOs;

namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Servicio para enviar mensajes a través del Hub de SignalR
    /// </summary>
    public class NavIndexHubService : IHostedService
    {
        private readonly IHubContext<NavIndexHub> _hubContext;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NavIndexHubService> _logger;

        private const string GroupPrefix = "entidad_";

        public NavIndexHubService(
            IHubContext<NavIndexHub> hubContext,
            INotificationService notificationService,
            ILogger<NavIndexHubService> logger)
        {
            _hubContext = hubContext;
            _notificationService = notificationService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Registrar el handler para recibir notificaciones de cambios
            _notificationService.OnCambio(EnviarActualizacionAsync);

            _logger.LogInformation("NavIndexHubService iniciado. Handler de notificaciones registrado.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("NavIndexHubService detenido.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Envía una actualización a todos los clientes de una entidad
        /// </summary>
        private async Task EnviarActualizacionAsync(MensajeActualizacion mensaje)
        {
            try
            {
                var groupName = $"{GroupPrefix}{mensaje.EntidadId}";

                _logger.LogInformation(
                    "Enviando actualización por WebSocket. Entidad: {EntidadId}, Tipo: {TipoCambio}, CarpetaId: {CarpetaId}",
                    mensaje.EntidadId, mensaje.TipoCambio, mensaje.CarpetaId);

                // Enviar al grupo de la entidad
                await _hubContext.Clients.Group(groupName).SendAsync("ActualizacionEstructura", mensaje);

                _logger.LogDebug("Actualización enviada al grupo {GroupName}", groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando actualización por WebSocket");
            }
        }
    }
}