namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Archivo del sistema documental
    /// </summary>
    public class Archivo
    {
        public long Cod { get; set; }
        public string? Nombre { get; set; }
        public string? Ruta { get; set; }
        public long? Carpeta { get; set; }
        public bool Estado { get; set; } = true;
        public long? Indice { get; set; }
        public DateTime? FechaSubida { get; set; }
        public DateTime? FechaDocumento { get; set; }
        public string? CodigoDocumento { get; set; }
        public string? Descripcion { get; set; }
        public long? SubidoPor { get; set; }
        public long? TipoArchivo { get; set; }
        public long? TipoDocumental { get; set; }
        public string? OrigenDocumento { get; set; }
        public int? PaginaInicio { get; set; }
        public int? PaginaFin { get; set; }
        public string? Hash { get; set; }
        public long? Tamano { get; set; }
        public int Version { get; set; } = 1;
        public string? TipoFirma { get; set; }
        public bool Firmado { get; set; }
        public string? MetadatosAdicionales { get; set; }

        // Propiedades de navegación
        public string? NombreCarpeta { get; set; }
        public string? NombreSubidoPor { get; set; }
        public string? NombreTipoArchivo { get; set; }
        public string? NombreTipoDocumental { get; set; }
        public string? NombreOrigenDocumento { get; set; }
        public string? Extension { get; set; }
    }

    /// <summary>
    /// Versión de un archivo
    /// </summary>
    public class VersionArchivo
    {
        public long Cod { get; set; }
        public long Archivo { get; set; }
        public int Version { get; set; }
        public string? Ruta { get; set; }
        public string? Hash { get; set; }
        public long? Tamano { get; set; }
        public DateTime FechaCreacion { get; set; }
        public long? CreadoPor { get; set; }
        public string? Comentario { get; set; }
        public bool EsVersionActual { get; set; }

        // Navegación
        public string? NombreCreadoPor { get; set; }
    }

    /// <summary>
    /// Resultado de operación de archivo
    /// </summary>
    public class ResultadoArchivo
    {
        public bool Exito { get; set; }
        public string? Mensaje { get; set; }
        public long? ArchivoCod { get; set; }
        public int? Version { get; set; }
    }
}