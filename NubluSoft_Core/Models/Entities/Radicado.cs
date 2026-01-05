namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Radicado de correspondencia (Entrada, Salida, Interna)
    /// </summary>
    public class Radicado
    {
        public long Cod { get; set; }
        public string? NumeroRadicado { get; set; }
        public long Entidad { get; set; }
        public string? TipoComunicacion { get; set; }
        public string? Asunto { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaRadicacion { get; set; }
        public DateTime? FechaDocumento { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public long? TipoSolicitud { get; set; }
        public long? EstadoRadicado { get; set; }
        public long? Prioridad { get; set; }
        public string? MedioRecepcion { get; set; }
        public long? UsuarioRadica { get; set; }
        public long? OficinaOrigen { get; set; }
        public long? OficinaDestino { get; set; }
        public long? UsuarioAsignado { get; set; }
        public long? Tercero { get; set; }
        public long? RadicadoPadre { get; set; }
        public long? ExpedienteVinculado { get; set; }
        public int? Folios { get; set; }
        public int? Anexos { get; set; }
        public string? Observaciones { get; set; }
        public bool Estado { get; set; } = true;
        public bool RequiereRespuesta { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string? MotivoAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public long? AnuladoPor { get; set; }

        // Propiedades de navegación
        public string? NombreTipoComunicacion { get; set; }
        public string? NombreTipoSolicitud { get; set; }
        public int? DiasRespuesta { get; set; }
        public string? NombreEstadoRadicado { get; set; }
        public string? ColorEstado { get; set; }
        public string? NombrePrioridad { get; set; }
        public string? NombreMedioRecepcion { get; set; }
        public string? NombreUsuarioRadica { get; set; }
        public string? NombreOficinaOrigen { get; set; }
        public string? NombreOficinaDestino { get; set; }
        public string? NombreUsuarioAsignado { get; set; }
        public string? NombreTercero { get; set; }
        public string? DocumentoTercero { get; set; }
        public string? NumeroRadicadoPadre { get; set; }
        public string? NombreExpedienteVinculado { get; set; }

        // Propiedades calculadas
        public int DiasTranscurridos => (DateTime.Now - FechaRadicacion).Days;
        public int? DiasRestantes => FechaVencimiento.HasValue ? (FechaVencimiento.Value - DateTime.Now).Days : null;
        public bool EstaVencido => FechaVencimiento.HasValue && DateTime.Now > FechaVencimiento.Value && EstadoRadicado != 5; // 5 = Respondido
        public bool ProximoAVencer => DiasRestantes.HasValue && DiasRestantes.Value <= 3 && DiasRestantes.Value > 0;
    }

    /// <summary>
    /// Trazabilidad de movimientos del radicado
    /// </summary>
    public class RadicadoTrazabilidad
    {
        public long Cod { get; set; }
        public long Radicado { get; set; }
        public string? Accion { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaAccion { get; set; }
        public long? Usuario { get; set; }
        public long? OficinaOrigen { get; set; }
        public long? OficinaDestino { get; set; }
        public long? UsuarioOrigen { get; set; }
        public long? UsuarioDestino { get; set; }
        public string? Observaciones { get; set; }

        // Navegación
        public string? NombreUsuario { get; set; }
        public string? NombreOficinaOrigen { get; set; }
        public string? NombreOficinaDestino { get; set; }
        public string? NombreUsuarioOrigen { get; set; }
        public string? NombreUsuarioDestino { get; set; }
    }

    /// <summary>
    /// Anexo de un radicado
    /// </summary>
    public class RadicadoAnexo
    {
        public long Cod { get; set; }
        public long Radicado { get; set; }
        public long? Archivo { get; set; }
        public string? Nombre { get; set; }
        public string? Ruta { get; set; }
        public string? Descripcion { get; set; }
        public long? Tamano { get; set; }
        public DateTime FechaAnexo { get; set; }
        public long? AnexadoPor { get; set; }
        public bool Estado { get; set; } = true;

        // Navegación
        public string? NombreAnexadoPor { get; set; }
    }

    /// <summary>
    /// Resultado de operación de radicado
    /// </summary>
    public class ResultadoRadicado
    {
        public bool Exito { get; set; }
        public string? Mensaje { get; set; }
        public long? RadicadoCod { get; set; }
        public string? NumeroRadicado { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }
}