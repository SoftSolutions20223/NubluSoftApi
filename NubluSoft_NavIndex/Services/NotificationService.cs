using NubluSoft_NavIndex.Models.DTOs;

namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Implementación del servicio de notificaciones
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationService> _logger;
        private readonly List<Func<MensajeActualizacion, Task>> _handlers = new();

        public NotificationService(
            IServiceProvider serviceProvider,
            ILogger<NotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ProcesarCambioAsync(CambioNavIndex cambio)
        {
            try
            {
                _logger.LogInformation(
                    "Procesando cambio: {TipoCambio} en carpeta {CarpetaId} (Tipo: {TipoCarpeta}, Entidad: {EntidadId})",
                    cambio.TipoCambio, cambio.CarpetaId, cambio.TipoCarpeta, cambio.EntidadId);

                // EntidadId viene del trigger de PostgreSQL
                long entidadId = cambio.EntidadId;

                // Crear scope para usar servicios Scoped
                using var scope = _serviceProvider.CreateScope();
                var navIndexService = scope.ServiceProvider.GetRequiredService<INavIndexService>();

                // 1. Regenerar estructura proactivamente con lock (no invalidar)
                // Si otro proceso ya está regenerando, esto retorna inmediatamente
                // El caché viejo permanece disponible durante la regeneración
                var regenerado = await navIndexService.RegenerarConLockAsync(entidadId);

                if (regenerado)
                {
                    _logger.LogDebug("Estructura regenerada exitosamente para entidad {EntidadId}", entidadId);
                }

                // 2. Si el cambio es en un expediente/genérica hijo directo,
                //    también invalidar el índice del padre (se regenerará bajo demanda)
                if (cambio.TipoCarpeta >= 3 && cambio.CarpetaPadre.HasValue)
                {
                    await navIndexService.InvalidarIndiceCacheAsync(entidadId, cambio.CarpetaPadre.Value);
                }

                // 3. Obtener el nodo actualizado para enviar por WebSocket
                NodoNavegacion? nodoActualizado = null;
                if (cambio.TipoCambio != TiposCambio.Eliminado)
                {
                    nodoActualizado = await navIndexService.ObtenerNodoAsync(cambio.CarpetaId);
                }

                // 4. Crear mensaje de actualización
                var mensaje = new MensajeActualizacion
                {
                    Tipo = TiposMensaje.NodoActualizado,
                    EntidadId = entidadId,
                    Version = DateTime.UtcNow.Ticks.ToString(),
                    TipoCambio = cambio.TipoCambio,
                    CarpetaId = cambio.CarpetaId,
                    TipoCarpeta = cambio.TipoCarpeta,
                    NodoActualizado = nodoActualizado,
                    Timestamp = DateTime.UtcNow
                };

                // 5. Notificar a todos los handlers (WebSocket hub)
                foreach (var handler in _handlers)
                {
                    try
                    {
                        await handler(mensaje);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error ejecutando handler de notificación");
                    }
                }

                _logger.LogInformation(
                    "Cambio procesado exitosamente. Notificados {Count} handlers",
                    _handlers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando cambio de carpeta {CarpetaId}", cambio.CarpetaId);
            }
        }

        public void OnCambio(Func<MensajeActualizacion, Task> handler)
        {
            _handlers.Add(handler);
            _logger.LogDebug("Handler de notificación registrado. Total: {Count}", _handlers.Count);
        }
    }
}