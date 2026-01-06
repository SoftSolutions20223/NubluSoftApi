using Google.Apis.Storage.v1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NubluSoft_Storage.Configuration;
using NubluSoft_Storage.Services;
using System.Runtime;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN ====================

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<GcsSettings>(builder.Configuration.GetSection("GoogleCloudStorage"));

// ==================== SERVICIOS ====================

// PostgreSQL
builder.Services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();

// Google Cloud Storage
builder.Services.AddSingleton<IGcsStorageService, GcsStorageService>();

// Servicio orquestador
builder.Services.AddScoped<IStorageService, NubluSoft_Storage.Services.StorageService>();

// ==================== JWT ====================

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
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
});

// ==================== CORS ====================

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200", "http://localhost:5008" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ==================== CONTROLLERS ====================

builder.Services.AddControllers();

// ==================== SWAGGER ====================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NubluSoft Storage API",
        Version = "v1",
        Description = "API de almacenamiento de archivos - Google Cloud Storage"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT: Bearer {token}"
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

// ==================== REQUEST SIZE LIMITS ====================

var maxFileSizeMB = builder.Configuration.GetValue<int>("GoogleCloudStorage:MaxFileSizeMB", 500);
var maxFileSize = maxFileSizeMB * 1024L * 1024L;

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxFileSize;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxFileSize;
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

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ==================== HEALTH CHECK ====================

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "NubluSoft_Storage",
    Timestamp = DateTime.UtcNow
})).AllowAnonymous();

// ==================== STARTUP LOG ====================

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var port = builder.Configuration.GetValue<int>("ServiceInfo:Port", 5002);
logger.LogInformation("NubluSoft_Storage iniciado en puerto {Port}", port);

app.Run();