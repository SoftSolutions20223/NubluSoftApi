using Microsoft.OpenApi.Models;
using NubluSoft_Core.Configuration;
using NubluSoft_Core.Extensions;
using NubluSoft_Core.Hubs;
using NubluSoft_Core.Listeners;
using NubluSoft_Core.Services;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN ====================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection(ServiceSettings.SectionName));

// ==================== SERVICIOS ====================

// Conexión a PostgreSQL
builder.Services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();

// Cliente HTTP para NubluSoft_Storage
builder.Services.AddStorageClient(builder.Configuration);

// Servicios de negocio
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<ICarpetasService, CarpetasService>();
builder.Services.AddScoped<IArchivosService, ArchivosService>();
builder.Services.AddScoped<IRadicadosService, RadicadosService>();
builder.Services.AddScoped<IOficinasService, OficinasService>();
builder.Services.AddScoped<ITRDService, TRDService>();
builder.Services.AddScoped<ITercerosService, TercerosService>();
builder.Services.AddScoped<ITransferenciasService, TransferenciasService>();
builder.Services.AddScoped<IDatosEstaticosService, DatosEstaticosService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IDiasFestivosService, DiasFestivosService>();
builder.Services.AddScoped<IOficinaTRDService, OficinaTRDService>();
builder.Services.AddScoped<IPrestamosService, PrestamosService>();
builder.Services.AddScoped<IEntidadesService, EntidadesService>();

// ==================== NOTIFICACIONES ====================

// Servicio de notificaciones
builder.Services.AddScoped<INotificacionService, NotificacionService>();

// Servicio para enviar notificaciones vía Hub (Singleton porque IHubContext es thread-safe)
builder.Services.AddSingleton<INotificacionesHubService, NotificacionesHubService>();

// Background service para escuchar PostgreSQL NOTIFY
builder.Services.AddHostedService<PostgresNotificacionListener>();

// SignalR para WebSockets
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// ==================== AUTENTICACIÓN ====================

// Autenticación JWT
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCoreCors(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NubluSoft Core API",
        Version = "v1",
        Description = "Microservicio de lógica de negocio"
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NubluSoft Core API v1");
    });
}

app.UseCors("CorePolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Mapear el Hub de SignalR para notificaciones en tiempo real
app.MapHub<NotificacionesHub>("/ws/notificaciones");

// ==================== INICIO ====================

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var port = builder.Configuration["ServiceInfo:Port"] ?? "5001";

logger.LogInformation("==============================================");
logger.LogInformation("  NubluSoft_Core iniciando en puerto {Port}", port);
logger.LogInformation("==============================================");

try
{
    using var scope = app.Services.CreateScope();

    // Verificar PostgreSQL
    var pgFactory = scope.ServiceProvider.GetRequiredService<IPostgresConnectionFactory>();
    using var conn = pgFactory.CreateConnection();
    await conn.OpenAsync();
    logger.LogInformation("[OK] PostgreSQL conectado");

    // Verificar configuración de Storage
    var storageUrl = builder.Configuration["Services:Storage"];
    logger.LogInformation("[OK] Storage URL configurada: {Url}", storageUrl);

    // Log de notificaciones
    logger.LogInformation("[OK] PostgreSQL NOTIFY listener activo (canal: notificaciones_cambios)");
    logger.LogInformation("[OK] WebSocket Hub disponible en /ws/notificaciones");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error verificando conexiones al inicio");
}

app.Run();