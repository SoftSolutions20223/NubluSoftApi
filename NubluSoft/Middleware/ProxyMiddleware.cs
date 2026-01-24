using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NubluSoft.Configuration;
using System.Net.Http.Headers;

namespace NubluSoft.Middleware
{
    /// <summary>
    /// Middleware para enrutar requests a los microservicios internos
    /// </summary>
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProxyMiddleware> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ServiceEndpoints _endpoints;

        // Rutas que maneja el Gateway directamente (no se proxean)
        private static readonly string[] LocalRoutes = new[]
        {
            "/api/auth",
            "/swagger",
            "/health"
        };

        // Mapeo de rutas a microservicios
        private static readonly Dictionary<string, string> RouteMapping = new()
{
    // NubluSoft_Storage (Puerto 5002)
    { "/api/storage", "StorageService" },

    // NubluSoft_Core (Puerto 5001)
    { "/api/usuarios", "CoreService" },
    { "/api/carpetas", "CoreService" },
    { "/api/archivos", "CoreService" },
    { "/api/radicados", "CoreService" },
    { "/api/oficinas", "CoreService" },
    { "/api/trd", "CoreService" },
    { "/api/terceros", "CoreService" },
    { "/api/transferencias", "CoreService" },
    { "/api/datosestaticos", "CoreService" },
    { "/api/notificaciones", "CoreService" },
    { "/api/auditoria", "CoreService" },
    { "/api/diasfestivos", "CoreService" },
    { "/api/oficinas-trd", "CoreService" },
    { "/api/prestamos", "CoreService" },
    { "/api/entidades", "CoreService" },

    // NubluSoft_NavIndex (Puerto 5003)
    { "/api/navegacion", "NavIndexService" },

    // NubluSoft_Signature (Puerto 5004)
    { "/api/solicitudes", "SignatureService" },
    { "/api/firma", "SignatureService" },
    { "/api/certificados", "SignatureService" },
    { "/api/verificar", "SignatureService" },
    { "/api/configuracion", "SignatureService" },

    // WebSocket/SignalR - Rutas HTTP para negociación (negotiate, etc.)
    { "/ws/navegacion", "NavIndexService" },
    { "/ws/notificaciones", "CoreService" }
};

        public ProxyMiddleware(
            RequestDelegate next,
            ILogger<ProxyMiddleware> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<ServiceEndpoints> endpoints)
        {
            _next = next;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _endpoints = endpoints.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            // Bloquear acceso directo a rutas internas de microservicios
            if (path.StartsWith("/internal"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            // Si es una conexión WebSocket, dejar que WebSocketProxyMiddleware la maneje
            if (context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            // Si es una ruta local, continuar con el pipeline normal
            if (IsLocalRoute(path))
            {
                await _next(context);
                return;
            }

            // Buscar el microservicio correspondiente
            var serviceName = GetServiceForRoute(path);

            if (serviceName == null)
            {
                // Ruta no mapeada, continuar con el pipeline normal
                await _next(context);
                return;
            }

            // Proxear la request al microservicio
            await ProxyRequestAsync(context, serviceName);
        }

        private bool IsLocalRoute(string path)
        {
            return LocalRoutes.Any(route => path.StartsWith(route, StringComparison.OrdinalIgnoreCase));
        }

        private string? GetServiceForRoute(string path)
        {
            foreach (var mapping in RouteMapping)
            {
                if (path.StartsWith(mapping.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return mapping.Value;
                }
            }
            return null;
        }

        private async Task ProxyRequestAsync(HttpContext context, string serviceName)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(serviceName);

                // Construir la URL de destino
                var targetUri = BuildTargetUri(context, client.BaseAddress!);

                _logger.LogDebug("Proxeando request a {Service}: {Method} {Uri}",
                    serviceName, context.Request.Method, targetUri);

                // Crear la request
                var requestMessage = CreateProxyRequest(context, targetUri);

                // Enviar la request
                var responseMessage = await client.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseHeadersRead,
                    context.RequestAborted);

                // Copiar la respuesta
                await CopyProxyResponseAsync(context, responseMessage);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error conectando con {Service}", serviceName);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = false,
                    Message = $"Servicio {serviceName} no disponible"
                });
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout conectando con {Service}", serviceName);
                context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = false,
                    Message = "Tiempo de espera agotado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proxy a {Service}", serviceName);
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = false,
                    Message = "Error en el gateway"
                });
            }
        }

        private Uri BuildTargetUri(HttpContext context, Uri baseAddress)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var query = context.Request.QueryString.Value ?? string.Empty;

            return new Uri(baseAddress, path + query);
        }

        private HttpRequestMessage CreateProxyRequest(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = targetUri
            };

            // Copiar headers (excepto Host)
            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Copiar body si existe
            if (context.Request.ContentLength > 0 ||
                context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                var streamContent = new StreamContent(context.Request.Body);

                if (context.Request.ContentType != null)
                {
                    streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
                }

                if (context.Request.ContentLength.HasValue)
                {
                    streamContent.Headers.ContentLength = context.Request.ContentLength;
                }

                requestMessage.Content = streamContent;
            }

            return requestMessage;
        }

        private async Task CopyProxyResponseAsync(HttpContext context, HttpResponseMessage responseMessage)
        {
            context.Response.StatusCode = (int)responseMessage.StatusCode;

            // Copiar headers de respuesta
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // Remover headers que no deben copiarse
            context.Response.Headers.Remove("transfer-encoding");

            // Copiar body
            await responseMessage.Content.CopyToAsync(context.Response.Body);
        }
    }

    /// <summary>
    /// Extensión para registrar el middleware
    /// </summary>
    public static class ProxyMiddlewareExtensions
    {
        public static IApplicationBuilder UseProxyToMicroservices(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyMiddleware>();
        }
    }
}
