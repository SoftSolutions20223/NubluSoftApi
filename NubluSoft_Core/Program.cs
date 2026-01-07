using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NubluSoft_Core.Configuration;
using NubluSoft_Core.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN ====================

// Configurar JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// ==================== SERVICIOS ====================

// PostgreSQL Connection Factory
builder.Services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();

// Servicios de negocio
builder.Services.AddScoped<IDatosEstaticosService, DatosEstaticosService>();
builder.Services.AddScoped<IOficinasService, OficinasService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<ICarpetasService, CarpetasService>();
builder.Services.AddScoped<IArchivosService, ArchivosService>();
builder.Services.AddScoped<ITRDService, TRDService>();
builder.Services.AddScoped<IRadicadosService, RadicadosService>();
builder.Services.AddScoped<ITercerosService, TercerosService>();
builder.Services.AddScoped<ITransferenciasService, TransferenciasService>();

// ==================== AUTENTICACIÓN JWT ====================

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

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Auth failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

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

// ==================== CONFIGURACIÓN DE SERVICIOS EXTERNOS ====================

// Configuración
builder.Services.Configure<NubluSoft_Core.Configuration.ServiceSettings>(
    builder.Configuration.GetSection(NubluSoft_Core.Configuration.ServiceSettings.SectionName));

// HttpClient para comunicación con Storage
var storageUrl = builder.Configuration.GetSection("Services:Storage").Value ?? "http://localhost:5002";
builder.Services.AddHttpClient<IStorageClientService, StorageClientService>(client =>
{
    client.BaseAddress = new Uri(storageUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ==================== CONTROLLERS ====================

builder.Services.AddControllers();

// ==================== SWAGGER ====================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NubluSoft Core API",
        Version = "v1",
        Description = "API de gestión documental - Microservicio Core"
    });

    // Configurar autenticación JWT en Swagger
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

// ==================== BUILD APP ====================

var app = builder.Build();

// ==================== MIDDLEWARE PIPELINE ====================

// Swagger (siempre habilitado para desarrollo)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NubluSoft Core API v1");
        c.RoutePrefix = "swagger";
    });
}

// CORS
app.UseCors("AllowAll");

// Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

// Health Check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "NubluSoft_Core",
    Timestamp = DateTime.UtcNow
})).AllowAnonymous();

// Controllers
app.MapControllers();

// ==================== RUN ====================

app.Run();