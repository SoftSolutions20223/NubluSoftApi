using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Core.Models.DTOs
{
    /// <summary>
    /// DTO para crear un tercero
    /// </summary>
    public class CrearTerceroRequest
    {
        /// <summary>
        /// Tipo: N=Natural, J=Jurídica, E=Entidad Pública
        /// </summary>
        [Required(ErrorMessage = "El tipo de tercero es requerido")]
        [MaxLength(1)]
        [RegularExpression("^[NJE]$", ErrorMessage = "Tipo de tercero inválido (N, J, E)")]
        public string TipoTercero { get; set; } = "N";

        [Required(ErrorMessage = "El tipo de documento es requerido")]
        [MaxLength(10)]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El documento es requerido")]
        [MaxLength(20)]
        public string Documento { get; set; } = string.Empty;

        [MaxLength(1)]
        public string? DigitoVerificacion { get; set; }

        /// <summary>
        /// Nombre completo para personas naturales
        /// </summary>
        [MaxLength(200)]
        public string? Nombre { get; set; }

        /// <summary>
        /// Razón social para personas jurídicas
        /// </summary>
        [MaxLength(300)]
        public string? RazonSocial { get; set; }

        [MaxLength(300)]
        public string? Direccion { get; set; }

        [MaxLength(100)]
        public string? Ciudad { get; set; }

        [MaxLength(100)]
        public string? Departamento { get; set; }

        [MaxLength(100)]
        public string? Pais { get; set; } = "Colombia";

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(20)]
        public string? Celular { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(200)]
        public string? SitioWeb { get; set; }

        [MaxLength(200)]
        public string? RepresentanteLegal { get; set; }

        [MaxLength(20)]
        public string? DocumentoRepresentante { get; set; }

        [MaxLength(100)]
        public string? CargoContacto { get; set; }

        [MaxLength(200)]
        public string? NombreContacto { get; set; }

        [MaxLength(20)]
        public string? TelefonoContacto { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo de contacto no es válido")]
        public string? CorreoContacto { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        public bool NotificarCorreo { get; set; } = true;

        public bool NotificarSMS { get; set; } = false;
    }

    /// <summary>
    /// DTO para actualizar un tercero
    /// </summary>
    public class ActualizarTerceroRequest
    {
        [MaxLength(200)]
        public string? Nombre { get; set; }

        [MaxLength(300)]
        public string? RazonSocial { get; set; }

        [MaxLength(300)]
        public string? Direccion { get; set; }

        [MaxLength(100)]
        public string? Ciudad { get; set; }

        [MaxLength(100)]
        public string? Departamento { get; set; }

        [MaxLength(100)]
        public string? Pais { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(20)]
        public string? Celular { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(200)]
        public string? SitioWeb { get; set; }

        [MaxLength(200)]
        public string? RepresentanteLegal { get; set; }

        [MaxLength(20)]
        public string? DocumentoRepresentante { get; set; }

        [MaxLength(100)]
        public string? CargoContacto { get; set; }

        [MaxLength(200)]
        public string? NombreContacto { get; set; }

        [MaxLength(20)]
        public string? TelefonoContacto { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "El correo de contacto no es válido")]
        public string? CorreoContacto { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        public bool NotificarCorreo { get; set; } = true;

        public bool NotificarSMS { get; set; } = false;

        public bool Estado { get; set; } = true;
    }

    /// <summary>
    /// Filtros para listar terceros
    /// </summary>
    public class FiltrosTercerosRequest
    {
        public string? TipoTercero { get; set; }
        public string? TipoDocumento { get; set; }
        public string? Busqueda { get; set; }
        public string? Ciudad { get; set; }
        public string? Departamento { get; set; }
        public bool SoloActivos { get; set; } = true;
        public bool IncluirEstadisticas { get; set; } = false;
        public int? Limite { get; set; } = 100;
    }
}