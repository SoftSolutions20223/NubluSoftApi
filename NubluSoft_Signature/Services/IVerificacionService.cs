using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Servicio para verificación pública de firmas
    /// </summary>
    public interface IVerificacionService
    {
        /// <summary>
        /// Verifica una firma por su código de verificación (público, sin auth)
        /// </summary>
        /// <param name="codigoVerificacion">Código único de verificación</param>
        /// <returns>Información pública de la firma</returns>
        Task<VerificacionFirmaResponse> VerificarPorCodigoAsync(string codigoVerificacion);

        /// <summary>
        /// Obtiene el historial completo de una solicitud (requiere auth)
        /// </summary>
        /// <param name="entidadId">ID de la entidad</param>
        /// <param name="solicitudId">ID de la solicitud</param>
        /// <returns>Historial con evidencias</returns>
        Task<HistorialFirmaResponse?> ObtenerHistorialAsync(long entidadId, long solicitudId);
    }
}