namespace NubluSoft_Signature.Configuration
{
    /// <summary>
    /// Configuración del sistema de firma electrónica
    /// </summary>
    public class SignatureSettings
    {
        public const string SectionName = "Signature";

        public OtpSettings Otp { get; set; } = new();
        public CertificadoSettings Certificado { get; set; } = new();
        public PdfSettings Pdf { get; set; } = new();
    }

    public class OtpSettings
    {
        /// <summary>
        /// Longitud del código OTP (default: 6 dígitos)
        /// </summary>
        public int Longitud { get; set; } = 6;

        /// <summary>
        /// Minutos de vigencia del OTP (default: 10)
        /// </summary>
        public int VigenciaMinutos { get; set; } = 10;

        /// <summary>
        /// Máximo de intentos fallidos (default: 3)
        /// </summary>
        public int MaxIntentos { get; set; } = 3;
    }

    public class CertificadoSettings
    {
        /// <summary>
        /// Días de vigencia del certificado (default: 365)
        /// </summary>
        public int VigenciaDias { get; set; } = 365;

        /// <summary>
        /// Tamaño de la clave RSA (default: 2048)
        /// </summary>
        public int TamanoClave { get; set; } = 2048;

        /// <summary>
        /// Nombre del emisor del certificado
        /// </summary>
        public string IssuerName { get; set; } = "CN=NubluSoft CA, O=NubluSoft, C=CO";
    }

    public class PdfSettings
    {
        /// <summary>
        /// Agregar sello visual en el PDF
        /// </summary>
        public bool AgregarSelloVisual { get; set; } = true;

        /// <summary>
        /// Posición X del sello (desde la izquierda)
        /// </summary>
        public float PosicionSelloX { get; set; } = 400;

        /// <summary>
        /// Posición Y del sello (desde abajo)
        /// </summary>
        public float PosicionSelloY { get; set; } = 50;

        /// <summary>
        /// Ancho del sello
        /// </summary>
        public float TamanoSelloAncho { get; set; } = 200;

        /// <summary>
        /// Alto del sello
        /// </summary>
        public float TamanoSelloAlto { get; set; } = 70;

        /// <summary>
        /// Agregar página de constancia al final
        /// </summary>
        public bool AgregarPaginaConstancia { get; set; } = true;

        /// <summary>
        /// URL base para verificación pública
        /// </summary>
        public string UrlVerificacion { get; set; } = "https://nublusoft.ejemplo.gov.co/verificar";
    }
}