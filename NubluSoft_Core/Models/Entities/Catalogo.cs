namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Modelo genérico para catálogos con Cod/Nombre
    /// </summary>
    public class Catalogo
    {
        public long Cod { get; set; }
        public string? Nombre { get; set; }
        public bool Estado { get; set; } = true;
    }

    /// <summary>
    /// Catálogo con código string (ej: Tipos_Documento_Identidad)
    /// </summary>
    public class CatalogoString
    {
        public string Cod { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public bool Estado { get; set; } = true;
    }

    /// <summary>
    /// Tipo de Solicitud con información de términos legales
    /// </summary>
    public class TipoSolicitud
    {
        public string Cod { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public int DiasHabilesRespuesta { get; set; }
        public int? DiasHabilesProrroga { get; set; }
        public bool EsCalendario { get; set; }
        public bool RequiereRespuestaEscrita { get; set; }
        public string? Normativa { get; set; }
        public bool Estado { get; set; }
    }

    /// <summary>
    /// Estado de Radicado con información visual
    /// </summary>
    public class EstadoRadicado
    {
        public string Cod { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public bool EsFinal { get; set; }
        public string? Color { get; set; }
        public int? Orden { get; set; }
    }
}