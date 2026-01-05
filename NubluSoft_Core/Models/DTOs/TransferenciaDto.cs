using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// DTO para crear una transferencia
    /// </summary>
    public class CrearTransferenciaRequest
    {
        /// <summary>
        /// Tipo: P=Primaria (Gestión→Central), S=Secundaria (Central→Histórico)
        /// </summary>
        [Required(ErrorMessage = "El tipo de transferencia es requerido")]
        [MaxLength(1)]
        [RegularExpression("^[PS]$", ErrorMessage = "Tipo de transferencia inválido (P, S)")]
        public string TipoTransferencia { get; set; } = "P";

        [Required(ErrorMessage = "La oficina origen es requerida")]
        public long OficinaOrigen { get; set; }

        /// <summary>
        /// Destino: C=Central, H=Histórico
        /// </summary>
        [Required(ErrorMessage = "El archivo destino es requerido")]
        [MaxLength(1)]
        [RegularExpression("^[CH]$", ErrorMessage = "Archivo destino inválido (C, H)")]
        public string ArchivoDestino { get; set; } = "C";

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para agregar expediente a transferencia
    /// </summary>
    public class AgregarExpedienteTransferenciaRequest
    {
        [Required(ErrorMessage = "El expediente es requerido")]
        public long Expediente { get; set; }

        public int? Folios { get; set; }

        public DateTime? FechaInicial { get; set; }

        public DateTime? FechaFinal { get; set; }

        [MaxLength(20)]
        public string? Soporte { get; set; }

        [MaxLength(10)]
        public string? FrecuenciaConsulta { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para agregar múltiples expedientes
    /// </summary>
    public class AgregarMultiplesExpedientesRequest
    {
        [Required(ErrorMessage = "Los expedientes son requeridos")]
        public List<AgregarExpedienteTransferenciaRequest> Expedientes { get; set; } = new();
    }

    /// <summary>
    /// DTO para enviar transferencia a revisión
    /// </summary>
    public class EnviarTransferenciaRequest
    {
        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para recibir/aprobar transferencia
    /// </summary>
    public class RecibirTransferenciaRequest
    {
        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para rechazar transferencia
    /// </summary>
    public class RechazarTransferenciaRequest
    {
        [Required(ErrorMessage = "El motivo del rechazo es requerido")]
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Filtros para listar transferencias
    /// </summary>
    public class FiltrosTransferenciasRequest
    {
        public string? TipoTransferencia { get; set; }
        public long? OficinaOrigen { get; set; }
        public long? EstadoTransferencia { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool SoloActivas { get; set; } = true;
        public int? Limite { get; set; } = 50;
    }

    /// <summary>
    /// Filtros para expedientes candidatos
    /// </summary>
    public class FiltrosExpedientesCandidatosRequest
    {
        [Required(ErrorMessage = "La oficina es requerida")]
        public long Oficina { get; set; }

        /// <summary>
        /// Tipo: P=Primaria, S=Secundaria
        /// </summary>
        public string TipoTransferencia { get; set; } = "P";

        public long? Serie { get; set; }

        public bool SoloListos { get; set; } = false;
    }
}