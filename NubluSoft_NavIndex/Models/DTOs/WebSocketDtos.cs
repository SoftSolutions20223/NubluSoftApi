namespace NubluSoft_NavIndex.Models.DTOs
{
    /// <summary>
    /// Mensaje enviado por WebSocket cuando hay actualizaciones
    /// </summary>
    public class MensajeActualizacion
    {
        public string Tipo { get; set; } = string.Empty; // "estructura_actualizada", "nodo_actualizado"
        public long EntidadId { get; set; }
        public string Version { get; set; } = string.Empty;
        public string TipoCambio { get; set; } = string.Empty; // "creado", "modificado", "eliminado"
        public long? CarpetaId { get; set; }
        public long? TipoCarpeta { get; set; }
        public NodoNavegacion? NodoActualizado { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Mensaje de suscripción del cliente
    /// </summary>
    public class MensajeSuscripcion
    {
        public string Tipo { get; set; } = string.Empty; // "suscribir", "desuscribir"
        public long EntidadId { get; set; }
    }

    /// <summary>
    /// Tipos de cambio en carpetas
    /// </summary>
    public static class TiposCambio
    {
        public const string Creado = "creado";
        public const string Modificado = "modificado";
        public const string Eliminado = "eliminado";
    }

    /// <summary>
    /// Tipos de mensaje WebSocket
    /// </summary>
    public static class TiposMensaje
    {
        public const string EstructuraActualizada = "estructura_actualizada";
        public const string NodoActualizado = "nodo_actualizado";
        public const string Suscribir = "suscribir";
        public const string Desuscribir = "desuscribir";
        public const string Error = "error";
        public const string Confirmacion = "confirmacion";
    }
}