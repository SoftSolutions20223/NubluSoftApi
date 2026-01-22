# PATRONES DE CÓDIGO - NubluSoft

## 1. Estructura de Controller

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Extensions;  // Ajustar namespace según servicio
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EjemploController : ControllerBase
    {
        private readonly IEjemploService _service;
        private readonly ILogger<EjemploController> _logger;

        public EjemploController(IEjemploService service, ILogger<EjemploController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Descripción del endpoint
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerTodosAsync(entidadId);
            return Ok(resultado);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var resultado = await _service.ObtenerPorIdAsync(id);
            if (resultado == null)
                return NotFound(new { Message = "Elemento no encontrado" });

            return Ok(resultado);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearEjemploRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (success, id, error) = await _service.CrearAsync(entidadId, usuarioId, request);

            if (!success)
                return BadRequest(new { Message = error });

            return CreatedAtAction(nameof(GetById), new { id }, new { Message = "Creado correctamente", Id = id });
        }
    }
}
```

## 2. Estructura de Service

```csharp
using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class EjemploService : IEjemploService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<EjemploService> _logger;

        public EjemploService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<EjemploService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Ejemplo>> ObtenerTodosAsync(long entidadId)
        {
            const string sql = @"
                SELECT ""Cod"", ""Nombre"", ""Descripcion"", ""Estado""
                FROM documentos.""Ejemplos""
                WHERE ""Entidad"" = @EntidadId AND ""Estado"" = true
                ORDER BY ""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Ejemplo>(sql, new { EntidadId = entidadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ejemplos");
                return Enumerable.Empty<Ejemplo>();
            }
        }

        public async Task<(bool Success, long? Id, string? Error)> CrearAsync(
            long entidadId, long usuarioId, CrearEjemploRequest request)
        {
            const string sql = @"
                SELECT * FROM documentos.""F_CrearEjemplo""(
                    p_Entidad := @Entidad,
                    p_Nombre := @Nombre,
                    p_Usuario := @Usuario
                )";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoEjemplo>(sql, new
                {
                    Entidad = entidadId,
                    request.Nombre,
                    Usuario = usuarioId
                });

                if (resultado == null || !resultado.Exito)
                    return (false, null, resultado?.Mensaje ?? "Error desconocido");

                return (true, resultado.Cod, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando ejemplo");
                return (false, null, "Error al crear: " + ex.Message);
            }
        }
    }
}
```

## 3. Estructura de Interface

```csharp
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface IEjemploService
    {
        Task<IEnumerable<Ejemplo>> ObtenerTodosAsync(long entidadId);
        Task<Ejemplo?> ObtenerPorIdAsync(long id);
        Task<(bool Success, long? Id, string? Error)> CrearAsync(
            long entidadId, long usuarioId, CrearEjemploRequest request);
        Task<(bool Success, string? Error)> ActualizarAsync(
            long id, long usuarioId, ActualizarEjemploRequest request);
        Task<(bool Success, string? Error)> EliminarAsync(long id, long usuarioId);
    }
}
```

## 4. Estructura de DTOs

```csharp
using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    // Request para crear
    public class CrearEjemploRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }
    }

    // Request para actualizar
    public class ActualizarEjemploRequest
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    // Response con información adicional
    public class EjemploDto
    {
        public long Cod { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? NombreUsuarioCreador { get; set; }  // JOIN con usuarios
    }

    // Resultado de operación
    public class ResultadoEjemplo
    {
        public bool Exito { get; set; }
        public long? Cod { get; set; }
        public string? Mensaje { get; set; }
    }
}
```

## 5. Estructura de Entity

```csharp
namespace NubluSoft_Core.Models.Entities
{
    public class Ejemplo
    {
        public long Cod { get; set; }
        public long Entidad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public long CreadoPor { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public long? ModificadoPor { get; set; }

        // Propiedades de navegación (JOIN)
        public string? NombreEntidad { get; set; }
        public string? NombreUsuarioCreador { get; set; }
    }
}
```

## 6. Registro en Program.cs

```csharp
// En la sección de servicios
builder.Services.AddScoped<IEjemploService, EjemploService>();
```

## 7. Patrones de Consultas SQL

### Consulta con JOINs
```csharp
const string sql = @"
    SELECT
        e.""Cod"", e.""Nombre"", e.""Descripcion"", e.""Estado"",
        e.""FechaCreacion"", e.""CreadoPor"",
        u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreUsuarioCreador""
    FROM documentos.""Ejemplos"" e
    LEFT JOIN documentos.""Usuarios"" u ON e.""CreadoPor"" = u.""Cod""
    WHERE e.""Entidad"" = @EntidadId AND e.""Estado"" = true
    ORDER BY e.""Nombre""";
```

### Llamada a Función PostgreSQL
```csharp
const string sql = @"
    SELECT * FROM documentos.""F_CrearEjemplo""(
        p_Entidad := @Entidad,
        p_Nombre := @Nombre,
        p_Descripcion := @Descripcion,
        p_Usuario := @Usuario
    )";

var resultado = await connection.QueryFirstOrDefaultAsync<ResultadoEjemplo>(sql, new
{
    Entidad = entidadId,
    request.Nombre,
    request.Descripcion,
    Usuario = usuarioId
});
```

### Consulta con Filtros Dinámicos
```csharp
var sql = @"SELECT * FROM documentos.""Ejemplos"" WHERE ""Entidad"" = @EntidadId";
var parameters = new DynamicParameters();
parameters.Add("EntidadId", entidadId);

if (!string.IsNullOrWhiteSpace(filtros.Busqueda))
{
    sql += @" AND ""Nombre"" ILIKE @Busqueda";
    parameters.Add("Busqueda", $"%{filtros.Busqueda}%");
}

if (filtros.SoloActivos)
{
    sql += @" AND ""Estado"" = true";
}

sql += @" ORDER BY ""Nombre""";

if (filtros.Limite.HasValue)
{
    sql += @" LIMIT @Limite";
    parameters.Add("Limite", filtros.Limite.Value);
}
```

## 8. Manejo de Errores

```csharp
try
{
    // Operación
}
catch (PostgresException ex) when (ex.SqlState == "23505")
{
    // Violación de unicidad
    return (false, null, "Ya existe un registro con ese nombre");
}
catch (PostgresException ex) when (ex.SqlState == "23503")
{
    // Violación de FK
    return (false, null, "Referencia a registro inexistente");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error en operación {Operacion}", nameof(CrearAsync));
    return (false, null, "Error interno del servidor");
}
```

## 9. Convenciones de Nombres

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| Controller | `{Nombre}Controller` | `RadicadosController` |
| Service | `{Nombre}Service` | `RadicadosService` |
| Interface | `I{Nombre}Service` | `IRadicadosService` |
| DTO Request | `{Accion}{Nombre}Request` | `CrearRadicadoRequest` |
| DTO Response | `{Nombre}Dto` | `RadicadoDto` |
| Entity | `{Nombre}` (singular) | `Radicado` |
| Método GET | `Obtener{X}Async` | `ObtenerPorIdAsync` |
| Método POST | `{Accion}Async` | `CrearAsync`, `RadicarAsync` |
| Método PUT | `Actualizar{X}Async` | `ActualizarAsync` |
| Método DELETE | `Eliminar{X}Async` | `EliminarAsync` |

## 10. ClaimsPrincipalExtensions

### Para Gateway, Core, NavIndex, Storage:
```csharp
public static long GetUserId(this ClaimsPrincipal principal)
{
    var claim = principal.FindFirst("Cod") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
    return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
}

public static long GetEntidadId(this ClaimsPrincipal principal)
{
    var claim = principal.FindFirst("Entidad");
    return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
}
```

### Para Signature (DIFERENTE - nullable):
```csharp
public static long? GetUserId(this ClaimsPrincipal principal)
{
    var claim = principal.FindFirst("Cod") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
    return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
}

public static long? GetEntidadId(this ClaimsPrincipal principal)
{
    var claim = principal.FindFirst("Entidad");
    return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
}
```
