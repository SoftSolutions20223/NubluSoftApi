using Microsoft.OpenApi.Models;
using NubluSoft.Extensions;
using NubluSoft.Middleware;
using NubluSoft.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ============================================

// Configuraciones desde appsettings.json
builder.Services.AddNubluSoftConfiguration(builder.Configuration);

// Autenticación JWT
builder.Services.AddJwtAuthentication(builder.Configuration);

// Autorización
builder.Services.AddAuthorization();

// Redis para sesiones
builder.Services.AddRedisCache(builder.Configuration);

// CORS
builder.Services.AddNubluSoftCors(builder.Configuration);

// HttpClients para microservicios
builder.Services.AddMicroserviceClients(builder.Configuration);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Mantener PascalCase
        options.JsonSerializerOptions.WriteIndented = true;
    });

// ============================================
// 2. INYECCIÓN DE DEPENDENCIAS - SERVICIOS
// ============================================

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRedisSessionService, RedisSessionService>();

// ============================================
// 3. SWAGGER / OPENAPI
// ============================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NubluSoft API Gateway",
        Version = "v1",
        Description = "API Gateway para el Sistema de Gestión Documental NubluSoft",
        Contact = new OpenApiContact
        {
            Name = "NubluSoft",
            Email = "soporte@nublusoft.com"
        }
    });

    // Configurar autenticación JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en el formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// ============================================
// 4. HEALTH CHECKS (Opcional)
// ============================================

builder.Services.AddHealthChecks();

// ============================================
// BUILD APP
// ============================================

var app = builder.Build();

// ============================================
// 5. PIPELINE DE MIDDLEWARE (ORDEN IMPORTANTE)
// ============================================

// Swagger (solo en desarrollo o si se habilita)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "NubluSoft API v1");
        options.RoutePrefix = "swagger";
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

// HTTPS Redirection (comentado para desarrollo local)
// app.UseHttpsRedirection();

// CORS - Debe ir antes de Authentication
app.UseCors("NubluSoftPolicy");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Validación de sesión en Redis (después de Authentication)
app.UseJwtSessionValidation();

// Health Check endpoint
app.MapHealthChecks("/health");

// Controllers (rutas locales como /api/auth)
app.MapControllers();

// Proxy a microservicios (debe ir al final)
app.UseProxyToMicroservices();

// ============================================
// 6. LOGGING DE INICIO
// ============================================

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var env = app.Environment.EnvironmentName;
var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";

logger.LogInformation("========================================");
logger.LogInformation("NubluSoft API Gateway iniciando...");
logger.LogInformation("Ambiente: {Environment}", env);
logger.LogInformation("URLs: {Urls}", urls);
logger.LogInformation("========================================");

// ============================================
// RUN
// ============================================

app.Run();