namespace NubluSoft_Core.Models.Entities
{
    /// <summary>
    /// Tercero (ciudadano, empresa, otra entidad) que interactúa con la entidad
    /// </summary>
    public class Tercero
    {
        public long Cod { get; set; }
        public long Entidad { get; set; }
        public string? TipoTercero { get; set; }
        public string? TipoDocumento { get; set; }
        public string? Documento { get; set; }
        public string? DigitoVerificacion { get; set; }
        public string? Nombre { get; set; }
        public string? RazonSocial { get; set; }
        public string? Direccion { get; set; }
        public string? Ciudad { get; set; }
        public string? Departamento { get; set; }
        public string? Pais { get; set; }
        public string? Telefono { get; set; }
        public string? Celular { get; set; }
        public string? Correo { get; set; }
        public string? SitioWeb { get; set; }
        public string? RepresentanteLegal { get; set; }
        public string? DocumentoRepresentante { get; set; }
        public string? CargoContacto { get; set; }
        public string? NombreContacto { get; set; }
        public string? TelefonoContacto { get; set; }
        public string? CorreoContacto { get; set; }
        public string? Observaciones { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime? FechaCreacion { get; set; }
        public long? CreadoPor { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public long? ModificadoPor { get; set; }
        public bool NotificarCorreo { get; set; } = true;
        public bool NotificarSMS { get; set; } = false;

        // Propiedades de navegación
        public string? NombreTipoTercero { get; set; }
        public string? NombreTipoDocumento { get; set; }
        public string? NombreCreadoPor { get; set; }

        // Propiedades calculadas
        public string NombreCompleto => TipoTercero == "J" ? RazonSocial ?? Nombre : Nombre ?? string.Empty;
        public string DocumentoCompleto => !string.IsNullOrEmpty(DigitoVerificacion)
            ? $"{Documento}-{DigitoVerificacion}"
            : Documento ?? string.Empty;
    }

    /// <summary>
    /// Tercero con estadísticas de radicados
    /// </summary>
    public class TerceroConEstadisticas : Tercero
    {
        public int TotalRadicadosEntrada { get; set; }
        public int TotalRadicadosSalida { get; set; }
        public DateTime? UltimoRadicado { get; set; }
    }
}