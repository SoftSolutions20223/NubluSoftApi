using NubluSoft_Signature.Configuration;
using NubluSoft_Signature.Extensions;
using NubluSoft_Signature.Services;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN ====================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<SignatureSettings>(
    builder.Configuration.GetSection(SignatureSettings.SectionName));
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection(ServiceSettings.SectionName));

// ==================== SERVICIOS ====================

// PostgreSQL
builder.Services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();
// Servicios de firma
builder.Services.AddScoped<ISolicitudFirmaService, SolicitudFirmaService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IFirmaService, FirmaService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerificacionService, VerificacionService>();
builder.Services.AddScoped<ICertificadoService, CertificadoService>();
builder.Services.AddScoped<IPdfSignatureService, PdfSignatureService>();  // ← AGREGAR

// Cliente HTTP para Storage
builder.Services.AddHttpClient<IStorageClientService, StorageClientService>(client =>
{
    var storageUrl = builder.Configuration["Services:Storage"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(storageUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(60);
});


// JWT y CORS
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSignatureCors(builder.Configuration);

// HTTP Client para Storage
builder.Services.AddStorageClient(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "NubluSoft Signature API",
        Version = "v1",
        Description = "Microservicio de Firma Electrónica - NubluSoft"
    });

    // Configurar JWT en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==================== APP ====================

var app = builder.Build();

// Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NubluSoft Signature API v1");
    });
}

// Middleware pipeline
app.UseCors("SignaturePolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Log de inicio
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("NubluSoft_Signature iniciado en puerto 5004");

app.Run();