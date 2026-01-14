namespace NubluSoft_Core.Models.DTOs
{
    // ==================== REQUESTS ====================

    /// <summary>
    /// Request para asignar una TRD a una oficina (desde el módulo Oficinas-TRD)
    /// </summary>
    public class AsignarTRDAOficinaRequest
    {
        public long OficinaId { get; set; }
        public long TRDId { get; set; }
        public bool PuedeEditar { get; set; } = true;
        public bool PuedeEliminar { get; set; } = false;
    }

    /// <summary>
    /// Request para asignación masiva de TRDs a una oficina
    /// </summary>
    public class AsignarTRDsMasivoRequest
    {
        public long OficinaId { get; set; }
        public List<TRDPermisoItem> TRDs { get; set; } = new List<TRDPermisoItem>();
    }

    public class TRDPermisoItem
    {
        public long TRDId { get; set; }
        public bool PuedeEditar { get; set; } = true;
        public bool PuedeEliminar { get; set; } = false;
    }

    /// <summary>
    /// Request para actualizar permisos de una asignación (desde el módulo Oficinas-TRD)
    /// </summary>
    public class ActualizarPermisosOficinaTRDRequest
    {
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
    }

    /// <summary>
    /// Filtros para consultar asignaciones
    /// </summary>
    public class FiltrosOficinaTRDRequest
    {
        public long? OficinaId { get; set; }
        public long? TRDId { get; set; }
        public bool? SoloActivas { get; set; } = true;
    }

    // ==================== RESPONSES ====================

    /// <summary>
    /// Asignación de TRD a oficina
    /// </summary>
    public class OficinaTRDDto
    {
        public long Cod { get; set; }
        public long Entidad { get; set; }
        public long Oficina { get; set; }
        public string? NombreOficina { get; set; }
        public long TRD { get; set; }
        public string? NombreTRD { get; set; }
        public long? CodigoTRD { get; set; }
        public string? TipoTRD { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public DateTime? FechaAsignacion { get; set; }
        public bool Estado { get; set; }
    }

    /// <summary>
    /// TRD con información de permisos para una oficina
    /// </summary>
    public class TRDConPermisosDto
    {
        public long Cod { get; set; }
        public long Codigo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public long? TRDPadre { get; set; }
        public string? NombrePadre { get; set; }
        public bool TieneAcceso { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
    }

    /// <summary>
    /// Resultado de verificación de acceso a carpeta
    /// </summary>
    public class VerificacionAccesoCarpetaDto
    {
        public bool TieneAcceso { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public long? CarpetaTRDCod { get; set; }
        public long? TRDCod { get; set; }
        public string? TRDNombre { get; set; }
        public long? TRDCodigo { get; set; }
        public string? Mensaje { get; set; }
    }

    /// <summary>
    /// Resultado de operación CRUD
    /// </summary>
    public class ResultadoOficinaTRDDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public OficinaTRDDto? Asignacion { get; set; }
    }

    /// <summary>
    /// Resultado de asignación masiva
    /// </summary>
    public class ResultadoAsignacionMasivaDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int Asignadas { get; set; }
        public int Omitidas { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
    }

    /// <summary>
    /// Resumen de permisos de una oficina
    /// </summary>
    public class ResumenPermisosOficinaDto
    {
        public long OficinaId { get; set; }
        public string NombreOficina { get; set; } = string.Empty;
        public int TotalTRDsAsignadas { get; set; }
        public int SeriesAsignadas { get; set; }
        public int SubseriesAsignadas { get; set; }
        public int ConPermisoEditar { get; set; }
        public int ConPermisoEliminar { get; set; }
        public IEnumerable<TRDConPermisosDto> TRDs { get; set; } = new List<TRDConPermisosDto>();
    }
}