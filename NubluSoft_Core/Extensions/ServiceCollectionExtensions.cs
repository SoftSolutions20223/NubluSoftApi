using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NubluSoft_Core.Configuration;
using NubluSoft_Core.Services;
using System.Text;

namespace NubluSoft_Core.Extensions
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
        /// Configura CORS
        /// </summary>
        public static IServiceCollection AddCoreCors(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200", "http://localhost:5008" };

            services.AddCors(options =>
            {
                options.AddPolicy("CorePolicy", builder =>
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
        /// Configura HttpClient tipado para comunicación con NubluSoft_Storage
        /// </summary>
        public static IServiceCollection AddStorageClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var serviceSettings = configuration.GetSection(ServiceSettings.SectionName).Get<ServiceSettings>()
                ?? new ServiceSettings();

            // HttpClient tipado para StorageClientService
            services.AddHttpClient<IStorageClientService, StorageClientService>(client =>
            {
                client.BaseAddress = new Uri(serviceSettings.Storage);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }
}