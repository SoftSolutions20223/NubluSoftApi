using NubluSoft_Signature.Models.DTOs;
using System.Security.Cryptography.X509Certificates;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Servicio para gestión de certificados digitales
    /// </summary>
    public interface ICertificadoService
    {
        /// <summary>
        /// Obtiene el certificado activo del usuario
        /// </summary>
        Task<CertificadoResponse?> ObtenerCertificadoActivoAsync(long entidadId, long usuarioId);

        /// <summary>
        /// Genera un nuevo certificado para el usuario
        /// </summary>
        Task<GenerarCertificadoResponse> GenerarCertificadoAsync(
            long entidadId,
            long usuarioId,
            string contrasena);

        /// <summary>
        /// Revoca el certificado activo del usuario
        /// </summary>
        Task<RevocarCertificadoResponse> RevocarCertificadoAsync(
            long entidadId,
            long usuarioId,
            string motivo,
            string contrasena);

        /// <summary>
        /// Obtiene el certificado desencriptado para firmar
        /// </summary>
        Task<X509Certificate2?> ObtenerCertificadoParaFirmarAsync(
            long entidadId,
            long usuarioId,
            string contrasena);

        /// <summary>
        /// Valida la contraseña del certificado
        /// </summary>
        Task<bool> ValidarContrasenaAsync(long entidadId, long usuarioId, string contrasena);

        /// <summary>
        /// Registra el uso del certificado
        /// </summary>
        Task RegistrarUsoAsync(long certificadoId);

        /// <summary>
        /// Descarga el certificado público en formato .cer
        /// </summary>
        Task<byte[]?> DescargarCertificadoPublicoAsync(long entidadId, long usuarioId);
    }
}