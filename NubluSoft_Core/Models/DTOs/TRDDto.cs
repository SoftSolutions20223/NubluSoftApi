using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// DTO para crear una Serie o Subserie (TRD)
    /// </summary>
    public class CrearTRDRequest
    {
        [Required(ErrorMessage = "El código es requerido")]
        [MaxLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Si es null, es una Serie. Si tiene valor, es una Subserie del TRD padre.
        /// </summary>
        public long? TRDPadre { get; set; }

        /// <summary>
        /// Años en archivo de gestión
        /// </summary>
        [Range(0, 100, ErrorMessage = "El tiempo de gestión debe estar entre 0 y 100 años")]
        public int? TiempoGestion { get; set; }

        /// <summary>
        /// Años en archivo central
        /// </summary>
        [Range(0, 100, ErrorMessage = "El tiempo central debe estar entre 0 y 100 años")]
        public int? TiempoCentral { get; set; }

        /// <summary>
        /// Disposición final: CT (Conservación Total), E (Eliminación), S (Selección), M (Microfilmación), D (Digitalización)
        /// </summary>
        [MaxLength(5)]
        public string? DisposicionFinal { get; set; }

        [MaxLength(500)]
        public string? Procedimiento { get; set; }
    }

    /// <summary>
    /// DTO para actualizar una TRD
    /// </summary>
    public class ActualizarTRDRequest
    {
        [Required(ErrorMessage = "El código es requerido")]
        [MaxLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        [Range(0, 100)]
        public int? TiempoGestion { get; set; }

        [Range(0, 100)]
        public int? TiempoCentral { get; set; }

        [MaxLength(5)]
        public string? DisposicionFinal { get; set; }

        [MaxLength(500)]
        public string? Procedimiento { get; set; }

        public bool Estado { get; set; } = true;
    }

    /// <summary>
    /// DTO para asignar TRD a una oficina
    /// </summary>
    public class AsignarTRDOficinaRequest
    {
        [Required(ErrorMessage = "La oficina es requerida")]
        public long Oficina { get; set; }

        public bool PuedeEditar { get; set; } = true;

        public bool PuedeEliminar { get; set; } = false;
    }

    /// <summary>
    /// DTO para asignar múltiples oficinas a una TRD
    /// </summary>
    public class AsignarMultiplesOficinasRequest
    {
        [Required(ErrorMessage = "Las oficinas son requeridas")]
        public List<AsignarTRDOficinaRequest> Oficinas { get; set; } = new();
    }

    /// <summary>
    /// DTO para actualizar permisos de una asignación
    /// </summary>
    public class ActualizarPermisosTRDRequest
    {
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
    }

    /// <summary>
    /// Filtros para listar TRD
    /// </summary>
    public class FiltrosTRDRequest
    {
        public bool? SoloSeries { get; set; }
        public bool? SoloSubseries { get; set; }
        public long? TRDPadre { get; set; }
        public string? Busqueda { get; set; }
        public bool SoloActivas { get; set; } = true;
    }
}