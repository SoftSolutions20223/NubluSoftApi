using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// DTO para crear una carpeta
    /// </summary>
    public class CrearCarpetaRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Tipo: 1=Serie, 2=Subserie, 3=Expediente, 4=Genérica
        /// </summary>
        [Required(ErrorMessage = "El tipo de carpeta es requerido")]
        [Range(1, 4, ErrorMessage = "Tipo de carpeta inválido")]
        public long TipoCarpeta { get; set; }

        /// <summary>
        /// Carpeta padre (null para Series raíz)
        /// </summary>
        public long? CarpetaPadre { get; set; }

        /// <summary>
        /// TRD asociado (requerido para Series y Subseries)
        /// </summary>
        public long? TRD { get; set; }

        /// <summary>
        /// Oficina que crea la carpeta (opcional, informativo)
        /// </summary>
        public long? Oficina { get; set; }

        public long? NivelVisualizacion { get; set; }

        [MaxLength(20)]
        public string? Soporte { get; set; }

        [MaxLength(10)]
        public string? FrecuenciaConsulta { get; set; }

        [MaxLength(200)]
        public string? UbicacionFisica { get; set; }

        public string? Observaciones { get; set; }

        [MaxLength(50)]
        public string? CodigoExpediente { get; set; }

        [MaxLength(500)]
        public string? PalabrasClave { get; set; }
    }

    /// <summary>
    /// DTO para actualizar una carpeta
    /// </summary>
    public class ActualizarCarpetaRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public long? NivelVisualizacion { get; set; }

        [MaxLength(20)]
        public string? Soporte { get; set; }

        [MaxLength(10)]
        public string? FrecuenciaConsulta { get; set; }

        [MaxLength(200)]
        public string? UbicacionFisica { get; set; }

        public string? Observaciones { get; set; }

        [MaxLength(500)]
        public string? PalabrasClave { get; set; }

        public int? NumeroFolios { get; set; }

        public int? Tomo { get; set; }

        public int? TotalTomos { get; set; }
    }

    /// <summary>
    /// DTO para mover una carpeta
    /// </summary>
    public class MoverCarpetaRequest
    {
        [Required(ErrorMessage = "La carpeta destino es requerida")]
        public long CarpetaDestino { get; set; }
    }

    /// <summary>
    /// DTO para copiar una carpeta
    /// </summary>
    public class CopiarCarpetaRequest
    {
        [Required(ErrorMessage = "La carpeta destino es requerida")]
        public long CarpetaDestino { get; set; }

        /// <summary>
        /// Nuevo nombre (opcional, si no se especifica usa el original)
        /// </summary>
        [MaxLength(200)]
        public string? NuevoNombre { get; set; }

        /// <summary>
        /// Si true, copia también los archivos
        /// </summary>
        public bool IncluirArchivos { get; set; } = true;
    }

    /// <summary>
    /// DTO para cerrar un expediente
    /// </summary>
    public class CerrarExpedienteRequest
    {
        public int? NumeroFolios { get; set; }

        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// Filtros para listar carpetas
    /// </summary>
    public class FiltrosCarpetasRequest
    {
        public long? CarpetaPadre { get; set; }
        public long? TipoCarpeta { get; set; }
        public long? SerieRaiz { get; set; }
        public long? Oficina { get; set; }
        public string? Busqueda { get; set; }
        public bool SoloActivas { get; set; } = true;
    }

    /// <summary>
    /// Estadísticas de una carpeta raíz (Serie/Subserie)
    /// </summary>
    public class CarpetaEstadisticasDto
    {
        /// <summary>
        /// ID de la carpeta
        /// </summary>
        public long CarpetaCod { get; set; }

        /// <summary>
        /// Cantidad de expedientes activos (TipoCarpeta = 3, EstadoCarpeta = 1)
        /// </summary>
        public int ExpedientesActivos { get; set; }

        /// <summary>
        /// Total de documentos/archivos dentro de la carpeta y subcarpetas
        /// </summary>
        public int DocumentosTotales { get; set; }

        /// <summary>
        /// Cantidad de usuarios con acceso a través de oficinas asignadas a la TRD
        /// </summary>
        public int UsuariosConAcceso { get; set; }

        /// <summary>
        /// Fecha de última modificación de la carpeta o cualquier subcarpeta/archivo
        /// </summary>
        public DateTime? UltimaModificacion { get; set; }
    }
}