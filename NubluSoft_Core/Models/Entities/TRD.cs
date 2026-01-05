namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Tabla de Retención Documental (Serie o Subserie)
    /// </summary>
    public class TRD
    {
        public long Cod { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public long? TRDPadre { get; set; }
        public long Entidad { get; set; }
        public int? TiempoGestion { get; set; }
        public int? TiempoCentral { get; set; }
        public string? DisposicionFinal { get; set; }
        public string? Procedimiento { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime? FechaCreacion { get; set; }
        public long? CreadoPor { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public long? ModificadoPor { get; set; }

        // Propiedades de navegación
        public string? NombreTRDPadre { get; set; }
        public string? CodigoTRDPadre { get; set; }
        public string? NombreDisposicionFinal { get; set; }
        public string? NombreCreadoPor { get; set; }
        public bool EsSerie => TRDPadre == null;
        public bool EsSubserie => TRDPadre != null;
    }

    /// <summary>
    /// TRD con estructura de árbol
    /// </summary>
    public class TRDArbol : TRD
    {
        public List<TRDArbol> Subseries { get; set; } = new();
        public int CantidadSubseries { get; set; }
        public int CantidadOficinasAsignadas { get; set; }
    }

    /// <summary>
    /// Relación Oficina-TRD con permisos
    /// </summary>
    public class OficinaTRD
    {
        public long Entidad { get; set; }
        public long Cod { get; set; }
        public long Oficina { get; set; }
        public long TRD { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime? FechaAsignacion { get; set; }
        public long? AsignadoPor { get; set; }

        // Navegación
        public string? NombreOficina { get; set; }
        public string? CodigoOficina { get; set; }
        public string? NombreTRD { get; set; }
        public string? CodigoTRD { get; set; }
        public string? NombreAsignadoPor { get; set; }
    }

    /// <summary>
    /// Resultado de operación TRD
    /// </summary>
    public class ResultadoTRD
    {
        public bool Exito { get; set; }
        public string? Mensaje { get; set; }
        public long? TRDCod { get; set; }
    }
}