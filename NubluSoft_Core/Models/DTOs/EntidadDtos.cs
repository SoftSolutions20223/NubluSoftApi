using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    // ==================== RESPONSES ====================

    /// <summary>
    /// Entidad con información completa
    /// </summary>
    public class EntidadDto
    {
        public long Cod { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Nit { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Correo { get; set; }
        public DateTime? FechaLimite { get; set; }
        public long? Bd { get; set; }
        public string? NombreBd { get; set; }
        public string? Url { get; set; }
        public string? Coleccion { get; set; }
        public long? Sector { get; set; }
        public string? NombreSector { get; set; }
        public long? TipoEntidad { get; set; }
        public string? NombreTipoEntidad { get; set; }

        // Campos calculados
        public bool PlanActivo => FechaLimite.HasValue && FechaLimite.Value >= DateTime.Today;
        public int? DiasRestantesPlan => FechaLimite.HasValue
            ? (int)(FechaLimite.Value - DateTime.Today).TotalDays
            : null;

        // Estadísticas
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int TotalOficinas { get; set; }
        public int TotalCarpetas { get; set; }
    }

    /// <summary>
    /// Entidad resumida para listados
    /// </summary>
    public class EntidadResumenDto
    {
        public long Cod { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Nit { get; set; }
        public string? Correo { get; set; }
        public DateTime? FechaLimite { get; set; }
        public bool PlanActivo { get; set; }
        public int TotalUsuarios { get; set; }
    }

    /// <summary>
    /// Plan asignado a entidad
    /// </summary>
    public class PlanEntidadDto
    {
        public long Cod { get; set; }
        public long? Plan { get; set; }
        public string? NombrePlan { get; set; }
        public decimal? Valor { get; set; }
        public int? NumeroUsuarios { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Estado { get; set; }
    }

    /// <summary>
    /// Lista paginada de entidades
    /// </summary>
    public class ListaEntidadesResponse
    {
        public IEnumerable<EntidadResumenDto> Entidades { get; set; } = new List<EntidadResumenDto>();
        public int TotalItems { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int PorPagina { get; set; }
    }

    // ==================== REQUESTS ====================

    /// <summary>
    /// Request para crear una entidad
    /// </summary>
    public class CrearEntidadRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El NIT es requerido")]
        [MaxLength(20)]
        public string Nit { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        public string? Direccion { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        public long? Sector { get; set; }
        public long? TipoEntidad { get; set; }

        /// <summary>
        /// Días de vigencia inicial del plan (default 30)
        /// </summary>
        [Range(1, 365)]
        public int DiasVigencia { get; set; } = 30;
    }

    /// <summary>
    /// Request para actualizar una entidad
    /// </summary>
    public class ActualizarEntidadRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Nit { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        public string? Direccion { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        public long? Sector { get; set; }
        public long? TipoEntidad { get; set; }
    }

    /// <summary>
    /// Request para extender el plan de una entidad
    /// </summary>
    public class ExtenderPlanRequest
    {
        [Required(ErrorMessage = "Los días son requeridos")]
        [Range(1, 365, ErrorMessage = "Los días deben estar entre 1 y 365")]
        public int Dias { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// Request para asignar plan a entidad
    /// </summary>
    public class AsignarPlanRequest
    {
        [Required(ErrorMessage = "El plan es requerido")]
        public long PlanId { get; set; }

        public DateTime? FechaInicio { get; set; }

        [Range(1, 365)]
        public int DiasVigencia { get; set; } = 30;

        public decimal? ValorPersonalizado { get; set; }
    }

    /// <summary>
    /// Filtros para consultar entidades
    /// </summary>
    public class FiltrosEntidadesRequest
    {
        public string? Busqueda { get; set; }
        public long? Sector { get; set; }
        public long? TipoEntidad { get; set; }
        public bool? SoloActivas { get; set; }
        public bool? PlanVencido { get; set; }
        public bool? PlanPorVencer { get; set; }

        // Paginación
        public int Pagina { get; set; } = 1;
        public int PorPagina { get; set; } = 20;
    }

    /// <summary>
    /// Resultado de operación de entidad
    /// </summary>
    public class ResultadoEntidadDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long? EntidadCod { get; set; }
        public EntidadDto? Entidad { get; set; }
    }

    /// <summary>
    /// Resumen de entidades para dashboard administrativo
    /// </summary>
    public class ResumenEntidadesDto
    {
        public int TotalEntidades { get; set; }
        public int EntidadesActivas { get; set; }
        public int PlanesVencidos { get; set; }
        public int PlanesPorVencer { get; set; }
        public int TotalUsuarios { get; set; }
        public IEnumerable<EntidadResumenDto> EntidadesPorVencer { get; set; } = new List<EntidadResumenDto>();
        public IEnumerable<EntidadResumenDto> EntidadesVencidas { get; set; } = new List<EntidadResumenDto>();
    }
}