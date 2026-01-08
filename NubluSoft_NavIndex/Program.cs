using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using NubluSoft_NavIndex.Configuration;
using NubluSoft_NavIndex.Extensions;
using NubluSoft_NavIndex.Hubs;
using NubluSoft_NavIndex.Services;
using StackExchange.Redis;
using System;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN ====================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection(RedisSettings.SectionName));

// ==================== SERVICIOS ====================

// Conexión a PostgreSQL
builder.Services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();

// Conexión a Redis
builder.Services.AddRedisConnection(builder.Configuration);

// Servicios de NavIndex
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddScoped<INavIndexService, NavIndexService>();

// Servicio de notificaciones (Singleton para mantener los handlers)
builder.Services.AddSingleton<INotificationService, NotificationService>();

// Background service para escuchar PostgreSQL NOTIFY
builder.Services.AddHostedService<PostgresNotifyListener>();

// Background service para el Hub de SignalR
builder.Services.AddHostedService<NavIndexHubService>();

// Autenticación JWT
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// CORS
builder.Services.AddNavIndexCors(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// SignalR para WebSockets
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NubluSoft NavIndex API",
        Version = "v1",
        Description = "Microservicio de navegación y estructura documental con WebSockets"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ==================== MIDDLEWARE ====================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NubluSoft NavIndex API v1");
    });
}

app.UseCors("NavIndexPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Mapear el Hub de SignalR
app.MapHub<NavIndexHub>("/ws/navegacion");

// ==================== INICIO ====================

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var port = builder.Configuration["ServiceInfo:Port"] ?? "5003";

logger.LogInformation("==============================================");
logger.LogInformation("  NubluSoft_NavIndex iniciando en puerto {Port}", port);
logger.LogInformation("==============================================");

try
{
    using var scope = app.Services.CreateScope();

    var pgFactory = scope.ServiceProvider.GetRequiredService<IPostgresConnectionFactory>();
    using var conn = pgFactory.CreateConnection();
    await conn.OpenAsync();
    logger.LogInformation("[OK] PostgreSQL conectado: {Info}", pgFactory.GetConnectionInfo());

    var redis = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
    var db = redis.GetDatabase();
    db.Ping();
    logger.LogInformation("[OK] Redis conectado: {Endpoint}", builder.Configuration["Redis:ConnectionString"]);

    logger.LogInformation("[OK] PostgreSQL NOTIFY listener activo");
    logger.LogInformation("[OK] WebSocket Hub disponible en /ws/navegacion");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error verificando conexiones al inicio");
}

app.Run();