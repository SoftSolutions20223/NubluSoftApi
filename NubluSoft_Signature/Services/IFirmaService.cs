using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Servicio para el proceso de firma electrónica
    /// </summary>
    public interface IFirmaService
    {
        /// <summary>
        /// Obtiene información del documento a firmar
        /// </summary>
        Task<InfoDocumentoFirmaResponse?> ObtenerInfoDocumentoAsync(
            long entidadId,
            long usuarioId,
            long solicitudId);

        /// <summary>
        /// Registra una firma simple (después de validar OTP)
        /// </summary>
        Task<ResultadoFirmaResponse> RegistrarFirmaSimpleAsync(
            long firmanteId,
            string ip,
            string userAgent);

        /// <summary>
        /// Rechaza una firma
        /// </summary>
        Task<ResultadoFirmaResponse> RechazarFirmaAsync(
            long firmanteId,
            string motivo,
            string ip,
            string userAgent);

        /// <summary>
        /// Obtiene el hash actual del documento
        /// </summary>
        Task<string?> ObtenerHashDocumentoAsync(long archivoId);

        /// <summary>
        /// Registra una firma avanzada con certificado
        /// </summary>
        Task<ResultadoFirmaResponse> RegistrarFirmaAvanzadaAsync(
            long firmanteId,
            long certificadoId,
            string hashFinal,
            string ip,
            string userAgent);
    }
}