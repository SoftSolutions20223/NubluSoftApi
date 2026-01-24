using Dapper;
using Newtonsoft.Json;
using Npgsql;

namespace NubluSoft_Core.Listeners
{
    /// <summary>
    /// Servicio en segundo plano que escucha notificaciones de PostgreSQL
    /// para generar el índice electrónico de carpetas cuando hay cambios
    /// en archivos (insert, update, delete)
    /// </summary>
    public class IndiceElectronicoListener : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IndiceElectronicoListener> _logger;

        private NpgsqlConnection? _connection;
        private const string ChannelName = "indice_electronico_cambios";

        public IndiceElectronicoListener(
            IConfiguration configuration,
            ILogger<IndiceElectronicoListener> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IndiceElectronicoListener iniciando...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EscucharNotificacionesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("IndiceElectronicoListener detenido por cancelación");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en IndiceElectronicoListener. Reintentando en 5 segundos...");

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

            _logger.LogInformation("Escuchando notificaciones de índice electrónico en canal: {Channel}", ChannelName);

            // Configurar el handler de notificaciones
            connection.Notification += async (sender, args) =>
            {
                await ProcesarNotificacionAsync(args.Payload, connectionString);
            };

            // Mantener la conexión activa y procesar notificaciones
            while (!stoppingToken.IsCancellationRequested)
            {
                await connection.WaitAsync(stoppingToken);
            }
        }

        private async Task ProcesarNotificacionAsync(string payload, string connectionString)
        {
            try
            {
                _logger.LogDebug("Notificación de índice electrónico recibida: {Payload}", payload);

                var cambio = JsonConvert.DeserializeObject<IndiceElectronicoCambio>(payload);

                if (cambio == null || cambio.CarpetaId <= 0)
                {
                    _logger.LogWarning("Payload inválido recibido: {Payload}", payload);
                    return;
                }

                _logger.LogInformation(
                    "Generando índice electrónico para carpeta {CarpetaId} (operación: {Operacion})",
                    cambio.CarpetaId, cambio.Operacion);

                // Ejecutar la función PostgreSQL en segundo plano
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await conn.ExecuteAsync(
                    "SELECT documentos.\"F_GenerarIndiceElectronico\"(@CarpetaId)",
                    new { CarpetaId = cambio.CarpetaId });

                _logger.LogInformation(
                    "Índice electrónico generado exitosamente para carpeta {CarpetaId}",
                    cambio.CarpetaId);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error deserializando payload de índice electrónico: {Payload}", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando índice electrónico");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("IndiceElectronicoListener deteniendo...");

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

        /// <summary>
        /// Modelo para el payload de notificación
        /// </summary>
        private class IndiceElectronicoCambio
        {
            [JsonProperty("carpeta_id")]
            public long CarpetaId { get; set; }

            [JsonProperty("operacion")]
            public string Operacion { get; set; } = string.Empty;

            [JsonProperty("timestamp")]
            public DateTime Timestamp { get; set; }
        }
    }
}
