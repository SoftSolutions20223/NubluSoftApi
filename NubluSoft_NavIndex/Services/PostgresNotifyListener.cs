using Newtonsoft.Json;
using Npgsql;

namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Servicio en background que escucha NOTIFY de PostgreSQL
    /// </summary>
    public class PostgresNotifyListener : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PostgresNotifyListener> _logger;
        private NpgsqlConnection? _connection;
        private const string ChannelName = "navindex_cambios";

        public PostgresNotifyListener(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<PostgresNotifyListener> logger)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PostgresNotifyListener iniciando...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EscucharNotificacionesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("PostgresNotifyListener detenido por cancelación");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en PostgresNotifyListener. Reintentando en 5 segundos...");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task EscucharNotificacionesAsync(CancellationToken stoppingToken)
        {
            var connectionString = _configuration.GetConnectionString("PostgreSQL");

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(stoppingToken);

            _connection = connection;
            _logger.LogInformation("Conexión a PostgreSQL establecida para NOTIFY");

            // Suscribirse al canal
            await using (var cmd = new NpgsqlCommand($"LISTEN {ChannelName}", connection))
            {
                await cmd.ExecuteNonQueryAsync(stoppingToken);
            }

            _logger.LogInformation("Escuchando canal '{Channel}'...", ChannelName);

            // Registrar handler para notificaciones
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

                var cambio = JsonConvert.DeserializeObject<CambioNavIndex>(payload);

                if (cambio == null)
                {
                    _logger.LogWarning("Payload inválido recibido: {Payload}", payload);
                    return;
                }

                // Usar scope para obtener servicios scoped
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await notificationService.ProcesarCambioAsync(cambio);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializando payload: {Payload}", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando notificación");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PostgresNotifyListener deteniendo...");

            if (_connection != null)
            {
                try
                {
                    await using var cmd = new NpgsqlCommand($"UNLISTEN {ChannelName}", _connection);
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
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