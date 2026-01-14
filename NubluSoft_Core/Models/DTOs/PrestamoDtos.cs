using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    // ==================== REQUESTS ====================

    /// <summary>
    /// Request para solicitar préstamo de expediente
    /// </summary>
    public class SolicitarPrestamoRequest
    {
        [Required(ErrorMessage = "El expediente es requerido")]
        public long CarpetaId { get; set; }

        [Required(ErrorMessage = "El motivo es requerido")]
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// Fecha esperada de devolución (opcional, si no se indica se usa default)
        /// </summary>
        public DateTime? FechaDevolucionEsperada { get; set; }

        [MaxLength(2000)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// Request para autorizar/rechazar préstamo
    /// </summary>
    public class AutorizarPrestamoRequest
    {
        public bool Autorizar { get; set; } = true;

        /// <summary>
        /// Días de préstamo autorizados (por defecto 15)
        /// </summary>
        [Range(1, 90, ErrorMessage = "Los días deben estar entre 1 y 90")]
        public int DiasPrestamo { get; set; } = 15;

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Motivo de rechazo (requerido si Autorizar = false)
        /// </summary>
        [MaxLength(500)]
        public string? MotivoRechazo { get; set; }
    }

    /// <summary>
    /// Request para registrar entrega física del expediente
    /// </summary>
    public class RegistrarEntregaRequest
    {
        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// Request para registrar devolución del expediente
    /// </summary>
    public class RegistrarDevolucionRequest
    {
        [MaxLength(500)]
        public string? ObservacionesDevolucion { get; set; }

        /// <summary>
        /// Indica si el expediente se devuelve en buen estado
        /// </summary>
        public bool EnBuenEstado { get; set; } = true;
    }

    /// <summary>
    /// Request para cancelar solicitud de préstamo
    /// </summary>
    public class CancelarPrestamoRequest
    {
        [Required(ErrorMessage = "El motivo de cancelación es requerido")]
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Filtros para consultar préstamos
    /// </summary>
    public class FiltrosPrestamosRequest
    {
        public long? CarpetaId { get; set; }
        public long? SolicitadoPor { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool? SoloVencidos { get; set; }
        public bool? SoloPendientes { get; set; }

        // Paginación
        public int Pagina { get; set; } = 1;
        public int PorPagina { get; set; } = 20;
    }

    // ==================== RESPONSES ====================

    /// <summary>
    /// Préstamo de expediente con información completa
    /// </summary>
    public class PrestamoDto
    {
        public long Cod { get; set; }
        public long Carpeta { get; set; }
        public string? NombreCarpeta { get; set; }
        public string? CodigoExpediente { get; set; }
        public long SolicitadoPor { get; set; }
        public string? NombreSolicitante { get; set; }
        public long? AutorizadoPor { get; set; }
        public string? NombreAutorizador { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
        public DateTime? FechaPrestamo { get; set; }
        public DateTime? FechaDevolucionEsperada { get; set; }
        public DateTime? FechaDevolucionReal { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public string? Observaciones { get; set; }
        public string? ObservacionesDevolucion { get; set; }

        // Campos calculados
        public int? DiasEnPrestamo { get; set; }
        public int? DiasVencido { get; set; }
        public bool EstaVencido { get; set; }
    }

    /// <summary>
    /// Lista paginada de préstamos
    /// </summary>
    public class ListaPrestamosResponse
    {
        public IEnumerable<PrestamoDto> Prestamos { get; set; } = new List<PrestamoDto>();
        public int TotalItems { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int PorPagina { get; set; }
    }

    /// <summary>
    /// Resultado de operación de préstamo
    /// </summary>
    public class ResultadoPrestamoDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long? PrestamoCod { get; set; }
        public PrestamoDto? Prestamo { get; set; }
    }

    /// <summary>
    /// Resumen de préstamos para dashboard
    /// </summary>
    public class ResumenPrestamosDto
    {
        public int TotalSolicitados { get; set; }
        public int TotalAutorizados { get; set; }
        public int TotalPrestados { get; set; }
        public int TotalDevueltos { get; set; }
        public int TotalRechazados { get; set; }
        public int TotalVencidos { get; set; }
        public int PendientesAutorizacion { get; set; }
        public int PendientesDevolucion { get; set; }
        public IEnumerable<PrestamoDto> PrestamosVencidos { get; set; } = new List<PrestamoDto>();
    }

    /// <summary>
    /// Estados posibles de un préstamo
    /// </summary>
    public static class EstadosPrestamo
    {
        public const string Solicitado = "SOLICITADO";
        public const string Autorizado = "AUTORIZADO";
        public const string Rechazado = "RECHAZADO";
        public const string Prestado = "PRESTADO";
        public const string Devuelto = "DEVUELTO";
        public const string Cancelado = "CANCELADO";
        public const string Vencido = "VENCIDO";
    }
}