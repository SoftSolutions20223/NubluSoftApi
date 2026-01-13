using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NubluSoft_Signature.Configuration;
using NubluSoft_Signature.Services;
using System.Text;

namespace NubluSoft_Signature.Extensions
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

                options.Events = new JwtBearerEvents
                {
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
        public static IServiceCollection AddSignatureCors(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200", "http://localhost:5008" };

            services.AddCors(options =>
            {
                options.AddPolicy("SignaturePolicy", builder =>
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
        /// Configura HttpClient para comunicación con Storage
        /// </summary>
        public static IServiceCollection AddStorageClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var serviceSettings = configuration.GetSection(ServiceSettings.SectionName).Get<ServiceSettings>()
                ?? new ServiceSettings();

            services.AddHttpClient("StorageClient", client =>
            {
                client.BaseAddress = new Uri(serviceSettings.Storage);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }
}