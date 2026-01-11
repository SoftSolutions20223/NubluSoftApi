using Microsoft.OpenApi.Models;
using NubluSoft.Extensions;
using NubluSoft.Middleware;
using NubluSoft.Services;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN ====================

builder.Services.AddNubluSoftConfiguration(builder.Configuration);

// ==================== SERVICIOS ====================

// Servicios de autenticación
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IRedisSessionService, RedisSessionService>();

// JWT Authentication (con soporte WebSocket)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// Redis
builder.Services.AddRedisCache(builder.Configuration);

// CORS
builder.Services.AddNubluSoftCors(builder.Configuration);

// HttpClients para microservicios
builder.Services.AddMicroserviceClients(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NubluSoft Gateway API",
        Version = "v1",
        Description = "API Gateway para el sistema de gestión documental NubluSoft"
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NubluSoft Gateway API v1");
    });
}

// CORS - DEBE ir antes de Authentication
app.UseCors("NubluSoftPolicy");

// WebSockets - DEBE ir antes de Authentication para el handshake
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Authentication y Authorization
app.UseAuthentication();
app.UseAuthorization();

// Validación de sesión en Redis (después de auth)
app.UseJwtSessionValidation();

// WebSocket Proxy (después de auth, para /ws/*)
app.UseWebSocketProxy();

// Proxy HTTP a microservicios (después de WebSocket)
app.UseProxyToMicroservices();

// Controllers locales (auth, health)
app.MapControllers();

// ==================== INICIO ====================

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("==============================================");
logger.LogInformation("  NubluSoft Gateway iniciando en puerto 5008");
logger.LogInformation("==============================================");

try
{
    // Verificar Redis
    var redis = app.Services.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
    var db = redis.GetDatabase();
    db.Ping();
    logger.LogInformation("[OK] Redis conectado: {Endpoint}", builder.Configuration["Redis:ConnectionString"]);

    // Mostrar URLs de microservicios
    logger.LogInformation("[OK] Core URL: {Url}", builder.Configuration["Services:Core"]);
    logger.LogInformation("[OK] Storage URL: {Url}", builder.Configuration["Services:Storage"]);
    logger.LogInformation("[OK] NavIndex URL: {Url}", builder.Configuration["Services:NavIndex"]);
    logger.LogInformation("[OK] WebSocket Proxy habilitado en /ws/*");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error verificando conexiones al inicio");
}

app.Run();