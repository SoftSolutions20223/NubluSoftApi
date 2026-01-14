namespace NubluSoft_Core.Models.DTOs
{
    // ==================== REQUESTS ====================

    /// <summary>
    /// Filtros para consultar historial de acciones
    /// </summary>
    public class FiltrosAuditoriaRequest
    {
        public string? Tabla { get; set; }
        public long? RegistroCod { get; set; }
        public long? UsuarioId { get; set; }
        public string? Accion { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int PorPagina { get; set; } = 50;
    }

    /// <summary>
    /// Filtros para consultar archivos eliminados
    /// </summary>
    public class FiltrosArchivosEliminadosRequest
    {
        public string? Nombre { get; set; }
        public long? EliminadoPor { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int PorPagina { get; set; } = 50;
    }

    /// <summary>
    /// Filtros para consultar carpetas eliminadas
    /// </summary>
    public class FiltrosCarpetasEliminadasRequest
    {
        public string? Nombre { get; set; }
        public long? TipoCarpeta { get; set; }
        public long? EliminadoPor { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int PorPagina { get; set; } = 50;
    }

    /// <summary>
    /// Filtros para consultar errores del sistema
    /// </summary>
    public class FiltrosErrorLogRequest
    {
        public string? ProcedureName { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int PorPagina { get; set; } = 50;
    }

    // ==================== RESPONSES ====================

    /// <summary>
    /// Registro del historial de acciones
    /// </summary>
    public class HistorialAccionDto
    {
        public long Cod { get; set; }
        public string Tabla { get; set; } = string.Empty;
        public long RegistroCod { get; set; }
        public string Accion { get; set; } = string.Empty;
        public long? Usuario { get; set; }
        public string? NombreUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string? IP { get; set; }
        public string? DetalleAnterior { get; set; }
        public string? DetalleNuevo { get; set; }
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// Lista paginada de historial
    /// </summary>
    public class HistorialAccionesListResponse
    {
        public IEnumerable<HistorialAccionDto> Items { get; set; } = new List<HistorialAccionDto>();
        public int TotalItems { get; set; }
        public int Pagina { get; set; }
        public int PorPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalItems / PorPagina);
    }

    /// <summary>
    /// Archivo eliminado
    /// </summary>
    public class ArchivoEliminadoDto
    {
        public long? Cod { get; set; }
        public string? Nombre { get; set; }
        public string? Ruta { get; set; }
        public DateTime? FechaEliminacion { get; set; }
        public string? Carpeta { get; set; }
        public long? CarpetaCod { get; set; }
        public string? Formato { get; set; }
        public long? EliminadoPor { get; set; }
        public string? NombreEliminadoPor { get; set; }
        public string? MotivoEliminacion { get; set; }
        public string? Hash { get; set; }
        public string? Tamaño { get; set; }
    }

    /// <summary>
    /// Lista paginada de archivos eliminados
    /// </summary>
    public class ArchivosEliminadosListResponse
    {
        public IEnumerable<ArchivoEliminadoDto> Items { get; set; } = new List<ArchivoEliminadoDto>();
        public int TotalItems { get; set; }
        public int Pagina { get; set; }
        public int PorPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalItems / PorPagina);
    }

    /// <summary>
    /// Carpeta eliminada
    /// </summary>
    public class CarpetaEliminadaDto
    {
        public long? Cod { get; set; }
        public string? Nombre { get; set; }
        public long? TipoCarpeta { get; set; }
        public string? NombreTipoCarpeta { get; set; }
        public long? CarpetaPadre { get; set; }
        public long? SerieRaiz { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaEliminacion { get; set; }
        public long? EliminadoPor { get; set; }
        public string? NombreEliminadoPor { get; set; }
        public string? MotivoEliminacion { get; set; }
        public int? NumeroFolios { get; set; }
        public int? NumeroArchivos { get; set; }
    }

    /// <summary>
    /// Lista paginada de carpetas eliminadas
    /// </summary>
    public class CarpetasEliminadasListResponse
    {
        public IEnumerable<CarpetaEliminadaDto> Items { get; set; } = new List<CarpetaEliminadaDto>();
        public int TotalItems { get; set; }
        public int Pagina { get; set; }
        public int PorPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalItems / PorPagina);
    }

    /// <summary>
    /// Registro de error del sistema
    /// </summary>
    public class ErrorLogDto
    {
        public long Id { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string? Parameters { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ErrorDate { get; set; }
        public int? ErrorNumber { get; set; }
        public int? ErrorLine { get; set; }
        public int? ErrorSeverity { get; set; }
    }

    /// <summary>
    /// Lista paginada de errores
    /// </summary>
    public class ErrorLogListResponse
    {
        public IEnumerable<ErrorLogDto> Items { get; set; } = new List<ErrorLogDto>();
        public int TotalItems { get; set; }
        public int Pagina { get; set; }
        public int PorPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalItems / PorPagina);
    }

    /// <summary>
    /// Resumen de auditoría para dashboard
    /// </summary>
    public class ResumenAuditoriaDto
    {
        public int TotalAccionesHoy { get; set; }
        public int TotalAccionesSemana { get; set; }
        public int TotalArchivosEliminados { get; set; }
        public int TotalCarpetasEliminadas { get; set; }
        public int TotalErrores { get; set; }
        public IEnumerable<AccionesPorTablaDto> AccionesPorTabla { get; set; } = new List<AccionesPorTablaDto>();
        public IEnumerable<AccionesPorUsuarioDto> AccionesPorUsuario { get; set; } = new List<AccionesPorUsuarioDto>();
    }

    public class AccionesPorTablaDto
    {
        public string Tabla { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class AccionesPorUsuarioDto
    {
        public long UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public int Total { get; set; }
    }
}