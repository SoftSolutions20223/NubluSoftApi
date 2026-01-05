namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Transferencia documental (Primaria: Gestión→Central, Secundaria: Central→Histórico)
    /// </summary>
    public class Transferencia
    {
        public long Cod { get; set; }
        public long Entidad { get; set; }
        public string? NumeroTransferencia { get; set; }
        public string? TipoTransferencia { get; set; }
        public long? OficinaOrigen { get; set; }
        public string? ArchivoDestino { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public long? EstadoTransferencia { get; set; }
        public long? CreadoPor { get; set; }
        public long? EnviadoPor { get; set; }
        public long? RecibidoPor { get; set; }
        public string? Observaciones { get; set; }
        public string? ObservacionesRechazo { get; set; }
        public int TotalExpedientes { get; set; }
        public int TotalFolios { get; set; }
        public bool Estado { get; set; } = true;

        // Propiedades de navegación
        public string? NombreTipoTransferencia { get; set; }
        public string? NombreOficinaOrigen { get; set; }
        public string? NombreArchivoDestino { get; set; }
        public string? NombreEstadoTransferencia { get; set; }
        public string? NombreCreadoPor { get; set; }
        public string? NombreEnviadoPor { get; set; }
        public string? NombreRecibidoPor { get; set; }
    }

    /// <summary>
    /// Detalle de expediente en una transferencia
    /// </summary>
    public class TransferenciaDetalle
    {
        public long Cod { get; set; }
        public long Transferencia { get; set; }
        public long Expediente { get; set; }
        public int? NumeroOrden { get; set; }
        public int? Folios { get; set; }
        public DateTime? FechaInicial { get; set; }
        public DateTime? FechaFinal { get; set; }
        public string? Soporte { get; set; }
        public string? FrecuenciaConsulta { get; set; }
        public string? Observaciones { get; set; }
        public bool Estado { get; set; } = true;

        // Propiedades de navegación
        public string? NombreExpediente { get; set; }
        public string? CodigoExpediente { get; set; }
        public string? NombreSerie { get; set; }
        public string? CodigoSerie { get; set; }
        public string? NombreSubserie { get; set; }
        public string? CodigoSubserie { get; set; }
    }

    /// <summary>
    /// Expediente candidato para transferencia
    /// </summary>
    public class ExpedienteCandidato
    {
        public long Cod { get; set; }
        public string? Nombre { get; set; }
        public string? CodigoExpediente { get; set; }
        public DateTime? FechaCierre { get; set; }
        public int? NumeroFolios { get; set; }
        public string? Soporte { get; set; }
        public DateTime? FechaDocumentoInicial { get; set; }
        public DateTime? FechaDocumentoFinal { get; set; }
        public long? TRD { get; set; }
        public string? NombreTRD { get; set; }
        public string? CodigoTRD { get; set; }
        public int? TiempoGestion { get; set; }
        public int? TiempoCentral { get; set; }
        public string? DisposicionFinal { get; set; }
        public int DiasEnArchivo { get; set; }
        public bool ListoParaTransferir { get; set; }
    }

    /// <summary>
    /// Resultado de operación de transferencia
    /// </summary>
    public class ResultadoTransferencia
    {
        public bool Exito { get; set; }
        public string? Mensaje { get; set; }
        public long? TransferenciaCod { get; set; }
        public string? NumeroTransferencia { get; set; }
    }
}