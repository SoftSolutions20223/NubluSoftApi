using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NubluSoft_Core.Configuration;
using NubluSoft_Core.Services;

var builder = WebApplication.CreateBuilder(args);

// === CONFIGURACIÓN ===
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// === SERVICIOS ===
builder.Services.AddSingleton<IPostgresConnectionFactory, PostgresConnectionFactory>();
builder.Services.AddScoped<IDatosEstaticosService, DatosEstaticosService>();
builder.Services.AddScoped<IOficinasService, OficinasService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<ICarpetasService, CarpetasService>();
builder.Services.AddScoped<IArchivosService, ArchivosService>();
builder.Services.AddScoped<ITRDService, TRDService>();
builder.Services.AddScoped<IRadicadosService, RadicadosService>();
builder.Services.AddScoped<ITercerosService, TercerosService>();
builder.Services.AddScoped<ITransferenciasService, TransferenciasService>();



// === CONTROLLERS ===
builder.Services.AddControllers();

// === SWAGGER ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NubluSoft Core API",
        Version = "v1",
        Description = "Microservicio de lógica de negocio - NubluSoft"
    });

    // Configurar autenticación JWT en Swagger
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

// === AUTENTICACIÓN JWT ===
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization();

// === CORS ===
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();

// === PIPELINE ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// === PUERTO ===
app.Urls.Add("http://localhost:5001");

Console.WriteLine("===========================================");
Console.WriteLine("  NubluSoft_Core corriendo en puerto 5001  ");
Console.WriteLine("  Swagger: http://localhost:5001/swagger   ");
Console.WriteLine("===========================================");

app.Run();