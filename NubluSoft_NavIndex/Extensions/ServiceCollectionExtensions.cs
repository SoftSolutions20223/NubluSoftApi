using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NubluSoft_NavIndex.Configuration;
using StackExchange.Redis;
using System.Text;

namespace NubluSoft_NavIndex.Extensions
{
    /// <summary>
    /// Extensiones para configurar servicios en Program.cs
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configura la autenticación JWT (idéntica al Gateway)
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

                // Soporte para WebSockets con JWT
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Permitir token en query string para WebSockets
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

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
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Configura la conexión a Redis
        /// </summary>
        public static IServiceCollection AddRedisConnection(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>()
                ?? new RedisSettings();

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                try
                {
                    var options = ConfigurationOptions.Parse(redisSettings.ConnectionString);
                    options.AbortOnConnectFail = false;
                    options.ConnectRetry = 3;
                    options.ConnectTimeout = 5000;

                    var connection = ConnectionMultiplexer.Connect(options);
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
        /// Configura CORS para el frontend y WebSockets
        /// </summary>
        public static IServiceCollection AddNavIndexCors(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200", "http://localhost:5008" };

            services.AddCors(options =>
            {
                options.AddPolicy("NavIndexPolicy", builder =>
                {
                    builder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // Requerido para WebSockets
                });
            });

            return services;
        }
    }
}