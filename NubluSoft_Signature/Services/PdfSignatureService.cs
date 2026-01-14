using Microsoft.Extensions.Options;
using NubluSoft_Signature.Configuration;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del servicio de firma PDF usando Syncfusion
    /// </summary>
    public class PdfSignatureService : IPdfSignatureService
    {
        private readonly SignatureSettings _signatureSettings;
        private readonly ILogger<PdfSignatureService> _pdfLogger;

        public PdfSignatureService(
            IOptions<SignatureSettings> settings,
            ILogger<PdfSignatureService> logger)
        {
            _signatureSettings = settings.Value;
            _pdfLogger = logger;
        }

        public async Task<PdfSignatureResult> FirmarPdfAsync(
            byte[] pdfOriginal,
            X509Certificate2 certificado,
            string nombreFirmante,
            string? razon = null,
            string? ubicacion = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Cargar PDF existente
                    using var inputStream = new MemoryStream(pdfOriginal);
                    using var loadedDocument = new PdfLoadedDocument(inputStream);

                    // Crear campo de firma en la última página
                    var page = loadedDocument.Pages[loadedDocument.Pages.Count - 1];
                    var signatureField = new PdfSignatureField(page, "Firma_" + Guid.NewGuid().ToString("N")[..8]);

                    // Configurar posición del sello visual
                    if (_signatureSettings.Pdf.AgregarSelloVisual)
                    {
                        signatureField.Bounds = new RectangleF(
                            _signatureSettings.Pdf.PosicionSelloX,
                            page.Size.Height - _signatureSettings.Pdf.PosicionSelloY - _signatureSettings.Pdf.TamanoSelloAlto,
                            _signatureSettings.Pdf.TamanoSelloAncho,
                            _signatureSettings.Pdf.TamanoSelloAlto);
                    }

                    // Agregar campo al formulario
                    loadedDocument.Form.Fields.Add(signatureField);

                    // Crear certificado PDF desde X509
                    var pdfCertificate = new PdfCertificate(certificado);
                    signatureField.Signature = new PdfSignature(loadedDocument, page, pdfCertificate, "Firma_Digital");

                    // Configurar sello visual
                    if (_signatureSettings.Pdf.AgregarSelloVisual)
                    {
                        var graphics = signatureField.Signature.Appearance.Normal.Graphics;
                        var font = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
                        var textoBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

                        var textoFirma = $"Firmado por: {nombreFirmante}\n" +
                                        $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}\n" +
                                        $"{razon ?? "Firma electrónica"}";

                        graphics.DrawString(textoFirma, font, textoBrush, 5f, 5f);
                    }

                    // Configurar algoritmo de firma
                    signatureField.Signature.Settings.CryptographicStandard = CryptographicStandard.CADES;
                    signatureField.Signature.Settings.DigestAlgorithm = DigestAlgorithm.SHA256;

                    // Guardar PDF firmado
                    using var outputStream = new MemoryStream();
                    loadedDocument.Save(outputStream);
                    var pdfFirmado = outputStream.ToArray();

                    // Calcular hash del PDF firmado
                    var hashFinal = CalcularHash(pdfFirmado);

                    _pdfLogger.LogInformation(
                        "PDF firmado exitosamente por {Firmante}, hash: {Hash}",
                        nombreFirmante, hashFinal[..16] + "...");

                    return new PdfSignatureResult
                    {
                        Exito = true,
                        PdfFirmado = pdfFirmado,
                        HashFinal = hashFinal
                    };
                }
                catch (Exception ex)
                {
                    _pdfLogger.LogError(ex, "Error firmando PDF");
                    return new PdfSignatureResult
                    {
                        Exito = false,
                        Error = $"Error al firmar el PDF: {ex.Message}"
                    };
                }
            });
        }

        public async Task<PdfConstanciaResult> AgregarConstanciaAsync(
            byte[] pdfOriginal,
            List<InfoFirmanteConstancia> firmantes,
            string codigoVerificacion,
            string urlVerificacion)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var inputStream = new MemoryStream(pdfOriginal);
                    using var loadedDocument = new PdfLoadedDocument(inputStream);

                    // Crear nueva página para constancia
                    var page = loadedDocument.Pages.Add();
                    var graphics = page.Graphics;
                    var font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                    var fontBold = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
                    var fontTitle = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);

                    var brushBlack = new PdfSolidBrush(new PdfColor(0, 0, 0));
                    var brushGray = new PdfSolidBrush(new PdfColor(128, 128, 128));
                    var brushBlue = new PdfSolidBrush(new PdfColor(0, 0, 139));
                    var penBlue = new PdfPen(new PdfColor(0, 0, 139), 1);
                    var penGray = new PdfPen(new PdfColor(211, 211, 211), 1);

                    float y = 50;
                    float margin = 50;

                    // Título
                    graphics.DrawString(
                        "CONSTANCIA DE FIRMA ELECTRÓNICA",
                        fontTitle,
                        brushBlue,
                        new PointF(margin, y));
                    y += 30;

                    // Línea separadora
                    graphics.DrawLine(
                        penBlue,
                        new PointF(margin, y),
                        new PointF(page.Size.Width - margin, y));
                    y += 20;

                    // Información del documento
                    graphics.DrawString(
                        $"Código de verificación: {codigoVerificacion}",
                        fontBold,
                        brushBlack,
                        new PointF(margin, y));
                    y += 20;

                    graphics.DrawString(
                        $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                        font,
                        brushBlack,
                        new PointF(margin, y));
                    y += 30;

                    // Lista de firmantes
                    graphics.DrawString(
                        "FIRMANTES:",
                        fontBold,
                        brushBlack,
                        new PointF(margin, y));
                    y += 20;

                    foreach (var firmante in firmantes)
                    {
                        graphics.DrawString(
                            $"• {firmante.Nombre}",
                            fontBold,
                            brushBlack,
                            new PointF(margin + 10, y));
                        y += 15;

                        if (!string.IsNullOrEmpty(firmante.Cargo))
                        {
                            graphics.DrawString(
                                $"  Cargo: {firmante.Cargo}",
                                font,
                                brushGray,
                                new PointF(margin + 20, y));
                            y += 15;
                        }

                        graphics.DrawString(
                            $"  Rol: {firmante.RolFirmante} | Tipo: {firmante.TipoFirma}",
                            font,
                            brushGray,
                            new PointF(margin + 20, y));
                        y += 15;

                        graphics.DrawString(
                            $"  Fecha de firma: {firmante.FechaFirma:dd/MM/yyyy HH:mm:ss}",
                            font,
                            brushGray,
                            new PointF(margin + 20, y));
                        y += 25;
                    }

                    // Nota de verificación
                    y += 20;
                    graphics.DrawLine(
                        penGray,
                        new PointF(margin, y),
                        new PointF(page.Size.Width - margin, y));
                    y += 15;

                    graphics.DrawString(
                        "Para verificar la autenticidad de este documento, visite:",
                        font,
                        brushGray,
                        new PointF(margin, y));
                    y += 15;

                    graphics.DrawString(
                        urlVerificacion,
                        fontBold,
                        brushBlue,
                        new PointF(margin, y));
                    y += 15;

                    graphics.DrawString(
                        $"e ingrese el código: {codigoVerificacion}",
                        font,
                        brushGray,
                        new PointF(margin, y));

                    // Guardar
                    using var outputStream = new MemoryStream();
                    loadedDocument.Save(outputStream);

                    return new PdfConstanciaResult
                    {
                        Exito = true,
                        PdfConConstancia = outputStream.ToArray()
                    };
                }
                catch (Exception ex)
                {
                    _pdfLogger.LogError(ex, "Error agregando constancia al PDF");
                    return new PdfConstanciaResult
                    {
                        Exito = false,
                        Error = $"Error al agregar constancia: {ex.Message}"
                    };
                }
            });
        }

        public async Task<PdfVerificationResult> VerificarFirmaAsync(byte[] pdfFirmado)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(pdfFirmado);
                    using var loadedDocument = new PdfLoadedDocument(stream);

                    var form = loadedDocument.Form;
                    if (form == null || form.Fields.Count == 0)
                    {
                        return new PdfVerificationResult
                        {
                            TieneFirma = false,
                            FirmaValida = false
                        };
                    }

                    foreach (var field in form.Fields)
                    {
                        if (field is PdfLoadedSignatureField signatureField && signatureField.IsSigned)
                        {
                            var signature = signatureField.Signature;
                            bool isValid = signature.Certificate != null;

                            return new PdfVerificationResult
                            {
                                TieneFirma = true,
                                FirmaValida = isValid,
                                NombreFirmante = signature.ContactInfo,
                                FechaFirma = DateTime.Now
                            };
                        }
                    }

                    return new PdfVerificationResult
                    {
                        TieneFirma = false,
                        FirmaValida = false
                    };
                }
                catch (Exception ex)
                {
                    _pdfLogger.LogError(ex, "Error verificando firma del PDF");
                    return new PdfVerificationResult
                    {
                        TieneFirma = false,
                        FirmaValida = false,
                        Error = ex.Message
                    };
                }
            });
        }

        private static string CalcularHash(byte[] data)
        {
            var hashBytes = SHA256.HashData(data);
            return Convert.ToHexString(hashBytes);
        }
    }
}