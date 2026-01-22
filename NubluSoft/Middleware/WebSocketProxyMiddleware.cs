using Microsoft.Extensions.Options;
using NubluSoft.Configuration;
using System.Net.WebSockets;

namespace NubluSoft.Middleware
{
    /// <summary>
    /// Middleware para hacer proxy de conexiones WebSocket a microservicios
    /// </summary>
    public class WebSocketProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketProxyMiddleware> _logger;
        private readonly ServiceEndpoints _endpoints;

        private const int BufferSize = 4096;

        // Mapeo de rutas WebSocket a servicios
        private static readonly Dictionary<string, string> WebSocketRoutes = new()
        {
            { "/ws/navegacion", "NavIndexService" },
            { "/ws/notificaciones", "CoreService" }    // NUEVO - Notificaciones en tiempo real
        };

        public WebSocketProxyMiddleware(
            RequestDelegate next,
            ILogger<WebSocketProxyMiddleware> logger,
            IOptions<ServiceEndpoints> endpoints)
        {
            _next = next;
            _logger = logger;
            _endpoints = endpoints.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Solo manejar rutas de WebSocket
            if (!context.Request.Path.StartsWithSegments("/ws"))
            {
                await _next(context);
                return;
            }

            // Si NO es una solicitud de WebSocket, dejar que ProxyMiddleware maneje
            // las peticiones HTTP (como /negotiate de SignalR)
            if (!context.WebSockets.IsWebSocketRequest)
            {
                _logger.LogDebug("Petición HTTP a /ws (probablemente negotiate): {Path}", context.Request.Path);
                await _next(context);
                return;
            }

            // Obtener el servicio destino según la ruta
            var serviceName = GetServiceForRoute(context.Request.Path.Value ?? string.Empty);

            if (serviceName == null)
            {
                _logger.LogWarning("Ruta WebSocket no mapeada: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await ProxyWebSocketAsync(context, serviceName);
        }

        private string? GetServiceForRoute(string path)
        {
            foreach (var route in WebSocketRoutes)
            {
                if (path.StartsWith(route.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return route.Value;
                }
            }
            return null;
        }

        private string GetServiceEndpoint(string serviceName)
        {
            return serviceName switch
            {
                "NavIndexService" => _endpoints.NavIndex,
                "CoreService" => _endpoints.Core,
                _ => throw new InvalidOperationException($"Servicio no configurado: {serviceName}")
            };
        }

        private async Task ProxyWebSocketAsync(HttpContext context, string serviceName)
        {
            var path = context.Request.Path.Value ?? "/ws/navegacion";
            var query = context.Request.QueryString.Value ?? string.Empty;

            // Obtener endpoint del servicio
            var serviceEndpoint = GetServiceEndpoint(serviceName);
            var serviceUri = new Uri(serviceEndpoint);
            var wsScheme = serviceUri.Scheme == "https" ? "wss" : "ws";
            var targetUri = new Uri($"{wsScheme}://{serviceUri.Host}:{serviceUri.Port}{path}{query}");

            _logger.LogInformation("Iniciando proxy WebSocket: {Source} -> {Target} (Servicio: {Service})",
                context.Request.Path, targetUri, serviceName);

            try
            {
                // Aceptar conexión del cliente
                using var clientSocket = await context.WebSockets.AcceptWebSocketAsync();

                // Conectar al backend
                using var backendClient = new ClientWebSocket();

                // Copiar headers relevantes (incluyendo Authorization si viene en headers)
                if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    backendClient.Options.SetRequestHeader("Authorization", authHeader.ToString());
                }

                // Conectar al backend con timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await backendClient.ConnectAsync(targetUri, cts.Token);

                _logger.LogInformation("WebSocket conectado a backend: {Target} (Servicio: {Service})",
                    targetUri, serviceName);

                // Iniciar relay bidireccional
                await Task.WhenAll(
                    RelayMessagesAsync(clientSocket, backendClient, "Client->Backend", context.RequestAborted),
                    RelayMessagesAsync(backendClient, clientSocket, "Backend->Client", context.RequestAborted)
                );
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "Error en WebSocket proxy hacia {Service}", serviceName);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket proxy cancelado para {Service}", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en WebSocket proxy hacia {Service}", serviceName);
            }
        }

        private async Task RelayMessagesAsync(
            WebSocket source,
            WebSocket destination,
            string direction,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];

            try
            {
                while (source.State == WebSocketState.Open &&
                       destination.State == WebSocketState.Open &&
                       !cancellationToken.IsCancellationRequested)
                {
                    var result = await source.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogDebug("WebSocket {Direction}: Recibido Close", direction);

                        if (destination.State == WebSocketState.Open)
                        {
                            await destination.CloseAsync(
                                result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                                result.CloseStatusDescription,
                                cancellationToken);
                        }
                        break;
                    }

                    if (destination.State == WebSocketState.Open)
                    {
                        await destination.SendAsync(
                            new ArraySegment<byte>(buffer, 0, result.Count),
                            result.MessageType,
                            result.EndOfMessage,
                            cancellationToken);
                    }
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                _logger.LogDebug("WebSocket {Direction}: Conexión cerrada", direction);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WebSocket {Direction}: Cancelado", direction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en relay WebSocket {Direction}", direction);
            }
        }
    }

    /// <summary>
    /// Extensión para registrar el middleware
    /// </summary>
    public static class WebSocketProxyMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSocketProxy(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketProxyMiddleware>();
        }
    }
}