namespace NubluSoft_NavIndex.Models.DTOs
{
    /// <summary>
    /// Nodo de carpeta para navegación (campos mínimos para el frontend)
    /// </summary>
    public class NodoNavegacion
    {
        public long Cod { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public long? CodSerie { get; set; }
        public long? CodSubSerie { get; set; }
        public bool Estado { get; set; }
        public long? CarpetaPadre { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public long? SerieRaiz { get; set; }
        public long? NivelVisualizacion { get; set; }
        public long TipoCarpeta { get; set; }
        public long? Delegado { get; set; }
        public long? TRD { get; set; }
        public string? CodigoTRD { get; set; }
        public string? NombreTipoCarpeta { get; set; }
    }

    /// <summary>
    /// Estructura documental completa para una entidad
    /// </summary>
    public class EstructuraDocumental
    {
        public long EntidadId { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime GeneradoEn { get; set; }
        public int TotalNodos { get; set; }
        public List<NodoNavegacion> Nodos { get; set; } = new();
    }

    /// <summary>
    /// Contenido de un expediente o carpeta genérica (índice)
    /// </summary>
    public class IndiceContenido
    {
        public long CarpetaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public long TipoCarpeta { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public List<NodoNavegacion> Subcarpetas { get; set; } = new();
        public List<ArchivoNavegacion> Archivos { get; set; } = new();
    }

    /// <summary>
    /// Archivo para navegación (campos mínimos)
    /// </summary>
    public class ArchivoNavegacion
    {
        public long Cod { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public long Carpeta { get; set; }
        public long? TipoArchivo { get; set; }
        public string? Formato { get; set; }
        public short? NumeroHojas { get; set; }
        public string? Duracion { get; set; }
        public long? Tamano { get; set; }
        public bool Estado { get; set; }
        public short Indice { get; set; }
        public string? NombreTipoArchivo { get; set; }
    }

    /// <summary>
    /// Respuesta con versión de la estructura (para verificar si cambió)
    /// </summary>
    public class VersionEstructura
    {
        public long EntidadId { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime UltimaActualizacion { get; set; }
        public int TotalNodos { get; set; }
    }

    /// <summary>
    /// Resultado genérico de operación
    /// </summary>
    public class ResultadoNavIndex
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}