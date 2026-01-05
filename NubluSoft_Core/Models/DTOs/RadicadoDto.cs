using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// DTO para radicar comunicación de entrada
    /// </summary>
    public class RadicarEntradaRequest
    {
        [Required(ErrorMessage = "El asunto es requerido")]
        [MaxLength(500)]
        public string Asunto { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El tipo de solicitud es requerido")]
        public long TipoSolicitud { get; set; }

        [Required(ErrorMessage = "El tercero remitente es requerido")]
        public long Tercero { get; set; }

        [Required(ErrorMessage = "La oficina destino es requerida")]
        public long OficinaDestino { get; set; }

        public long? UsuarioAsignado { get; set; }

        public DateTime? FechaDocumento { get; set; }

        [MaxLength(20)]
        public string? MedioRecepcion { get; set; }

        public long? Prioridad { get; set; }

        [Range(0, 9999)]
        public int? Folios { get; set; }

        [Range(0, 100)]
        public int? Anexos { get; set; }

        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        public bool RequiereRespuesta { get; set; } = true;
    }

    /// <summary>
    /// DTO para radicar comunicación de salida
    /// </summary>
    public class RadicarSalidaRequest
    {
        [Required(ErrorMessage = "El asunto es requerido")]
        [MaxLength(500)]
        public string Asunto { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El tercero destinatario es requerido")]
        public long Tercero { get; set; }

        [Required(ErrorMessage = "La oficina origen es requerida")]
        public long OficinaOrigen { get; set; }

        /// <summary>
        /// Radicado de entrada al que responde (opcional)
        /// </summary>
        public long? RadicadoPadre { get; set; }

        public DateTime? FechaDocumento { get; set; }

        [MaxLength(20)]
        public string? MedioRecepcion { get; set; }

        [Range(0, 9999)]
        public int? Folios { get; set; }

        [Range(0, 100)]
        public int? Anexos { get; set; }

        [MaxLength(1000)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para radicar comunicación interna
    /// </summary>
    public class RadicarInternaRequest
    {
        [Required(ErrorMessage = "El asunto es requerido")]
        [MaxLength(500)]
        public string Asunto { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La oficina origen es requerida")]
        public long OficinaOrigen { get; set; }

        [Required(ErrorMessage = "La oficina destino es requerida")]
        public long OficinaDestino { get; set; }

        public long? UsuarioAsignado { get; set; }

        public DateTime? FechaDocumento { get; set; }

        public long? Prioridad { get; set; }

        [Range(0, 9999)]
        public int? Folios { get; set; }

        [Range(0, 100)]
        public int? Anexos { get; set; }

        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        public bool RequiereRespuesta { get; set; } = false;
    }

    /// <summary>
    /// DTO para asignar/reasignar un radicado
    /// </summary>
    public class AsignarRadicadoRequest
    {
        [Required(ErrorMessage = "La oficina destino es requerida")]
        public long OficinaDestino { get; set; }

        public long? UsuarioAsignado { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para trasladar por competencia (Ley 1755 Art. 21)
    /// </summary>
    public class TrasladarRadicadoRequest
    {
        [Required(ErrorMessage = "La oficina destino es requerida")]
        public long OficinaDestino { get; set; }

        [Required(ErrorMessage = "El motivo del traslado es requerido")]
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para archivar radicado en expediente
    /// </summary>
    public class ArchivarRadicadoRequest
    {
        [Required(ErrorMessage = "El expediente es requerido")]
        public long Expediente { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para solicitar prórroga (Ley 1755 Art. 14)
    /// </summary>
    public class SolicitarProrrogaRequest
    {
        [Required(ErrorMessage = "El motivo es requerido")]
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// Días adicionales solicitados (máximo según normativa)
        /// </summary>
        [Range(1, 30, ErrorMessage = "Los días deben estar entre 1 y 30")]
        public int DiasAdicionales { get; set; }
    }

    /// <summary>
    /// DTO para anular radicado
    /// </summary>
    public class AnularRadicadoRequest
    {
        [Required(ErrorMessage = "El motivo de anulación es requerido")]
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para agregar anexo a radicado
    /// </summary>
    public class AgregarAnexoRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ruta es requerida")]
        public string Ruta { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public long? Tamano { get; set; }
    }

    /// <summary>
    /// Filtros para listar radicados
    /// </summary>
    public class FiltrosRadicadosRequest
    {
        public string? TipoComunicacion { get; set; }
        public long? EstadoRadicado { get; set; }
        public long? OficinaOrigen { get; set; }
        public long? OficinaDestino { get; set; }
        public long? UsuarioAsignado { get; set; }
        public long? Tercero { get; set; }
        public long? TipoSolicitud { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? Busqueda { get; set; }
        public bool? SoloVencidos { get; set; }
        public bool? SoloProximosAVencer { get; set; }
        public bool SoloActivos { get; set; } = true;
        public int? Limite { get; set; } = 100;
    }
}