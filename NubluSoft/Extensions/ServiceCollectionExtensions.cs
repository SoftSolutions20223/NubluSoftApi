using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NubluSoft.Configuration;
using StackExchange.Redis;
using System.Text;

namespace NubluSoft.Extensions
{
    /// <summary>
    /// Extensiones para configurar servicios en Program.cs
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configura las opciones desde appsettings.json
        /// </summary>
        public static IServiceCollection AddNubluSoftConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
            services.Configure<ServiceEndpoints>(configuration.GetSection(ServiceEndpoints.SectionName));

            return services;
        }

        /// <summary>
        /// Configura la autenticación JWT
        /// </summary>
        /// <summary>
        /// Configura la autenticación JWT con soporte para WebSockets
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings no configurado en appsettings.json");

            var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Eventos para logging y soporte WebSocket
                options.Events = new JwtBearerEvents
                {
                    // IMPORTANTE: Permitir token en query string para WebSockets
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // Si es una ruta de WebSocket y tiene token en query string
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Autenticación fallida: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        var userName = context.Principal?.Identity?.Name;
                        logger.LogDebug("Token validado para usuario: {User}", userName);
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Configura la conexión a Redis
        /// </summary>
        public static IServiceCollection AddRedisCache(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>()
                ?? new RedisSettings();

            // Registrar ConnectionMultiplexer como Singleton
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                try
                {
                    var connection = ConnectionMultiplexer.Connect(redisSettings.ConnectionString);
                    logger.LogInformation("Conexión a Redis establecida: {Endpoint}", redisSettings.ConnectionString);
                    return connection;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error conectando a Redis: {Endpoint}", redisSettings.ConnectionString);
                    throw;
                }
            });

            return services;
        }

        /// <summary>
        /// Configura CORS para el frontend
        /// </summary>
        public static IServiceCollection AddNubluSoftCors(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200" };

            services.AddCors(options =>
            {
                options.AddPolicy("NubluSoftPolicy", builder =>
                {
                    builder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            return services;
        }

        /// <summary>
        /// Configura HttpClient para comunicación con microservicios
        /// </summary>
        public static IServiceCollection AddMicroserviceClients(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var endpoints = configuration.GetSection(ServiceEndpoints.SectionName).Get<ServiceEndpoints>()
                ?? new ServiceEndpoints();

            // HttpClient para NubluSoft_Core
            services.AddHttpClient("CoreService", client =>
            {
                client.BaseAddress = new Uri(endpoints.Core);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // HttpClient para NubluSoft_Storage
            services.AddHttpClient("StorageService", client =>
            {
                client.BaseAddress = new Uri(endpoints.Storage);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(60); // Mayor timeout para archivos
            });

            // HttpClient para NubluSoft_NavIndex
            services.AddHttpClient("NavIndexService", client =>
            {
                client.BaseAddress = new Uri(endpoints.NavIndex);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}