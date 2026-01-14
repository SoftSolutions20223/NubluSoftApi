using System.ComponentModel.DataAnnotations;

namespace NubluSoft_Signature.Models.DTOs
{
    // ==================== REQUEST DTOs ====================

    /// <summary>
    /// Request para generar un nuevo certificado
    /// </summary>
    public class GenerarCertificadoRequest
    {
        /// <summary>
        /// Contraseña para proteger la clave privada
        /// </summary>
        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [MaxLength(50, ErrorMessage = "La contraseña no puede exceder 50 caracteres")]
        public string Contrasena { get; set; } = string.Empty;

        /// <summary>
        /// Confirmación de la contraseña
        /// </summary>
        [Required(ErrorMessage = "La confirmación es requerida")]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para firmar con certificado
    /// </summary>
    public class FirmarConCertificadoRequest
    {
        /// <summary>
        /// Contraseña del certificado
        /// </summary>
        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Contrasena { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para revocar un certificado
    /// </summary>
    public class RevocarCertificadoRequest
    {
        /// <summary>
        /// Motivo de la revocación
        /// </summary>
        [Required(ErrorMessage = "El motivo es requerido")]
        [MaxLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña para confirmar la revocación
        /// </summary>
        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Contrasena { get; set; } = string.Empty;
    }

    // ==================== RESPONSE DTOs ====================

    /// <summary>
    /// Información del certificado del usuario
    /// </summary>
    public class CertificadoResponse
    {
        public long Cod { get; set; }
        public string NumeroSerie { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Huella { get; set; } = string.Empty;
        public string AlgoritmoFirma { get; set; } = string.Empty;
        public int TamanoClave { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? UltimoUso { get; set; }
        public int VecesUsado { get; set; }
        public int DiasParaVencer { get; set; }
        public bool EstaVigente { get; set; }
    }

    /// <summary>
    /// Resultado de generación de certificado
    /// </summary>
    public class GenerarCertificadoResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string? NumeroSerie { get; set; }
        public string? Huella { get; set; }
        public DateTime? VigenciaHasta { get; set; }
    }

    /// <summary>
    /// Resultado de revocación
    /// </summary>
    public class RevocarCertificadoResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de validación de contraseña
    /// </summary>
    public class ValidarContrasenaResponse
    {
        public bool Valida { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public List<string> Errores { get; set; } = new();
    }
}