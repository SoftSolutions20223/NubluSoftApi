using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NubluSoft_Storage.Configuration;
using NubluSoft_Storage.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN ====================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<GcsSettings>(
    builder.Configuration.GetSection(GcsSettings.SectionName));

// ==================== SERVICIOS ====================

// PostgreSQL
builder.Services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();

// Google Cloud Storage
builder.Services.AddSingleton<IGcsStorageService, GcsStorageService>();
builder.Services.AddScoped<IStorageService, StorageService>();

// JWT Authentication (idéntica al Gateway)
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings no configurado");

var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

builder.Services.AddAuthentication(options =>
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

builder.Services.AddAuthorization();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200", "http://localhost:5008" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("StoragePolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NubluSoft Storage API",
        Version = "v1",
        Description = "Microservicio de almacenamiento con Google Cloud Storage"
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NubluSoft Storage API v1");
    });
}

app.UseCors("StoragePolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ==================== INICIO ====================

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var port = builder.Configuration["ServiceInfo:Port"] ?? "5002";

logger.LogInformation("==============================================");
logger.LogInformation("  NubluSoft_Storage iniciando en puerto {Port}", port);
logger.LogInformation("==============================================");

try
{
    // Verificar PostgreSQL
    var pgFactory = app.Services.GetRequiredService<IPostgresConnectionFactory>();
    using var conn = pgFactory.CreateConnection();
    await conn.OpenAsync();
    logger.LogInformation("[OK] PostgreSQL conectado: {Info}", pgFactory.GetConnectionInfo());

    // Verificar GCS
    var gcsService = app.Services.GetRequiredService<IGcsStorageService>();
    logger.LogInformation("[OK] GCS configurado para bucket: {Bucket}", gcsService.BucketName);
}
catch (Exception ex)
{
    logger.LogError(ex, "Error verificando conexiones al inicio");
}

app.Run();