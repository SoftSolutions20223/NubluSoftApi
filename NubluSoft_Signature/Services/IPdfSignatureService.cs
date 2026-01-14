using System.Security.Cryptography.X509Certificates;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Servicio para firmar documentos PDF
    /// </summary>
    public interface IPdfSignatureService
    {
        /// <summary>
        /// Firma un PDF con certificado digital
        /// </summary>
        Task<PdfSignatureResult> FirmarPdfAsync(
            byte[] pdfOriginal,
            X509Certificate2 certificado,
            string nombreFirmante,
            string? razon = null,
            string? ubicacion = null);

        /// <summary>
        /// Agrega una página de constancia de firma al PDF
        /// </summary>
        Task<PdfConstanciaResult> AgregarConstanciaAsync(
            byte[] pdfOriginal,
            List<InfoFirmanteConstancia> firmantes,
            string codigoVerificacion,
            string urlVerificacion);

        /// <summary>
        /// Verifica si un PDF tiene firma digital válida
        /// </summary>
        Task<PdfVerificationResult> VerificarFirmaAsync(byte[] pdfFirmado);
    }

    // ==================== DTOs del servicio ====================

    public class PdfSignatureResult
    {
        public bool Exito { get; set; }
        public byte[]? PdfFirmado { get; set; }
        public string? HashFinal { get; set; }
        public string? Error { get; set; }
    }

    public class PdfConstanciaResult
    {
        public bool Exito { get; set; }
        public byte[]? PdfConConstancia { get; set; }
        public string? Error { get; set; }
    }

    public class PdfVerificationResult
    {
        public bool TieneFirma { get; set; }
        public bool FirmaValida { get; set; }
        public string? NombreFirmante { get; set; }
        public DateTime? FechaFirma { get; set; }
        public string? Error { get; set; }
    }

    public class InfoFirmanteConstancia
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Cargo { get; set; }
        public string RolFirmante { get; set; } = string.Empty;
        public string TipoFirma { get; set; } = string.Empty;
        public DateTime FechaFirma { get; set; }
    }
}