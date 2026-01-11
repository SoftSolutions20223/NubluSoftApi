using Newtonsoft.Json;
using Npgsql;
using NubluSoft_Core.Hubs;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Listeners
{
    /// <summary>
    /// Servicio en segundo plano que escucha notificaciones de PostgreSQL
    /// y las envía a los clientes conectados vía SignalR
    /// </summary>
    public class PostgresNotificacionListener : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PostgresNotificacionListener> _logger;

        private NpgsqlConnection? _connection;
        private const string ChannelName = "notificaciones_cambios";

        public PostgresNotificacionListener(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<PostgresNotificacionListener> logger)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PostgresNotificacionListener iniciando...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EscucharNotificacionesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("PostgresNotificacionListener detenido por cancelación");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en PostgresNotificacionListener. Reintentando en 5 segundos...");

                    try
                    {
                        await Task.Delay(5000, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task EscucharNotificacionesAsync(CancellationToken stoppingToken)
        {
            var connectionString = _configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionString 'PostgreSQL' no configurado");

            await using var connection = new NpgsqlConnection(connectionString);
            _connection = connection;

            await connection.OpenAsync(stoppingToken);
            _logger.LogInformation("Conexión PostgreSQL establecida para LISTEN {Channel}", ChannelName);

            // Suscribirse al canal
            await using (var cmd = new NpgsqlCommand($"LISTEN {ChannelName}", connection))
            {
                await cmd.ExecuteNonQueryAsync(stoppingToken);
            }

            _logger.LogInformation("Escuchando notificaciones en canal: {Channel}", ChannelName);

            // Configurar el handler de notificaciones
            connection.Notification += async (sender, args) =>
            {
                await ProcesarNotificacionAsync(args.Payload);
            };

            // Mantener la conexión activa y procesar notificaciones
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait espera por notificaciones o timeout
                await connection.WaitAsync(stoppingToken);
            }
        }

        private async Task ProcesarNotificacionAsync(string payload)
        {
            try
            {
                _logger.LogDebug("Notificación recibida: {Payload}", payload);

                var cambio = JsonConvert.DeserializeObject<NotificacionCambioPayload>(payload);

                if (cambio == null)
                {
                    _logger.LogWarning("Payload inválido recibido: {Payload}", payload);
                    return;
                }

                // Verificar que es una nueva notificación
                if (cambio.Tipo != "NUEVA_NOTIFICACION")
                {
                    _logger.LogDebug("Tipo de cambio ignorado: {Tipo}", cambio.Tipo);
                    return;
                }

                _logger.LogInformation(
                    "Procesando notificación {NotificacionId} para usuario {UsuarioId} en entidad {EntidadId}",
                    cambio.NotificacionId, cambio.UsuarioDestino, cambio.Entidad);

                // Usar scope para obtener servicios scoped
                using var scope = _serviceProvider.CreateScope();

                var notificacionService = scope.ServiceProvider.GetRequiredService<INotificacionService>();
                var hubService = scope.ServiceProvider.GetRequiredService<INotificacionesHubService>();

                // Obtener la notificación completa para enviar por WebSocket
                var mensaje = await notificacionService.ObtenerParaWebSocketAsync(
                    cambio.Entidad,
                    cambio.UsuarioDestino,
                    cambio.NotificacionId);

                if (mensaje != null)
                {
                    // Enviar al usuario específico
                    await hubService.EnviarAUsuarioAsync(
                        cambio.Entidad,
                        cambio.UsuarioDestino,
                        mensaje);

                    _logger.LogInformation(
                        "Notificación {NotificacionId} enviada a usuario {UsuarioId} vía WebSocket",
                        cambio.NotificacionId, cambio.UsuarioDestino);
                }
                else
                {
                    _logger.LogWarning(
                        "No se pudo obtener la notificación {NotificacionId} para enviar por WebSocket",
                        cambio.NotificacionId);
                }
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error deserializando payload: {Payload}", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando notificación desde PostgreSQL");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PostgresNotificacionListener deteniendo...");

            if (_connection != null)
            {
                try
                {
                    if (_connection.State == System.Data.ConnectionState.Open)
                    {
                        await using var cmd = new NpgsqlCommand($"UNLISTEN {ChannelName}", _connection);
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                        _logger.LogInformation("UNLISTEN ejecutado para canal {Channel}", ChannelName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al hacer UNLISTEN");
                }
            }

            await base.StopAsync(cancellationToken);
        }
    }
}