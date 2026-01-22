namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Carpeta del sistema documental (Serie, Subserie, Expediente, Genérica)
    /// </summary>
    public class Carpeta
    {
        public long Cod { get; set; }
        public long? CodSerie { get; set; }
        public long? CodSubSerie { get; set; }
        public bool Estado { get; set; } = true;
        public long? EstadoCarpeta { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public bool Copia { get; set; }
        public long? CarpetaOriginal { get; set; }
        public long? CarpetaPadre { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string? IndiceElectronico { get; set; }
        public long? Delegado { get; set; }
        public long TipoCarpeta { get; set; }
        public long? NivelVisualizacion { get; set; }
        public long? SerieRaiz { get; set; }
        public long? TRD { get; set; }
        public DateTime? FechaCierre { get; set; }
        public int? NumeroFolios { get; set; }
        public string? Soporte { get; set; }
        public string? FrecuenciaConsulta { get; set; }
        public string? UbicacionFisica { get; set; }
        public string? Observaciones { get; set; }
        public long? CreadoPor { get; set; }
        public long? ModificadoPor { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? Tomo { get; set; }
        public int? TotalTomos { get; set; }
        public string? CodigoExpediente { get; set; }
        public string? PalabrasClave { get; set; }
        public DateTime? FechaDocumentoInicial { get; set; }
        public DateTime? FechaDocumentoFinal { get; set; }
        public long? ExpedienteRelacionado { get; set; }

        // Propiedades de navegación
        public string? NombreTipoCarpeta { get; set; }
        public string? NombreEstadoCarpeta { get; set; }
        public string? NombreCarpetaPadre { get; set; }
        public string? NombreCreadoPor { get; set; }
        public string? NombreTRD { get; set; }
        public string? CodigoTRD { get; set; }
    }

    /// <summary>
    /// Carpeta con hijos para estructura de árbol
    /// </summary>
    public class CarpetaArbol : Carpeta
    {
        public List<CarpetaArbol> Hijos { get; set; } = new();
        public int CantidadArchivos { get; set; }
        public int CantidadSubcarpetas { get; set; }

        // Estadísticas (solo para carpetas raíz/Series)
        public int? ExpedientesActivos { get; set; }
        public int? DocumentosTotales { get; set; }
        public int? UsuariosConAcceso { get; set; }
        public DateTime? UltimaModificacionEstadistica { get; set; }
    }

    /// <summary>
    /// Resultado de operación de carpeta
    /// </summary>
    public class ResultadoCarpeta
    {
        public bool Exito { get; set; }
        public string? Mensaje { get; set; }
        public long? CarpetaCod { get; set; }
    }
}