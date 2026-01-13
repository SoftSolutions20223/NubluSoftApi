using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Servicio para gestión de solicitudes de firma
    /// </summary>
    public interface ISolicitudFirmaService
    {
        /// <summary>
        /// Crea una nueva solicitud de firma
        /// </summary>
        Task<OperacionFirmaResult> CrearSolicitudAsync(
            long entidadId,
            long usuarioId,
            CrearSolicitudRequest request);

        /// <summary>
        /// Obtiene una solicitud por su ID
        /// </summary>
        Task<SolicitudFirmaResponse?> ObtenerPorIdAsync(
            long entidadId,
            long solicitudId,
            long? usuarioActual = null);

        /// <summary>
        /// Obtiene las solicitudes pendientes de firma para un usuario
        /// </summary>
        Task<IEnumerable<SolicitudPendienteResponse>> ObtenerPendientesUsuarioAsync(
            long entidadId,
            long usuarioId);

        /// <summary>
        /// Obtiene las solicitudes creadas por un usuario
        /// </summary>
        Task<IEnumerable<SolicitudFirmaResponse>> ObtenerMisSolicitudesAsync(
            long entidadId,
            long usuarioId,
            string? estado = null);

        /// <summary>
        /// Cancela una solicitud de firma
        /// </summary>
        Task<OperacionFirmaResult> CancelarSolicitudAsync(
            long entidadId,
            long usuarioId,
            long solicitudId,
            string motivo);

        /// <summary>
        /// Obtiene el resumen de solicitudes para dashboard
        /// </summary>
        Task<ResumenSolicitudesResponse> ObtenerResumenAsync(
            long entidadId,
            long usuarioId);

        /// <summary>
        /// Verifica si un usuario puede firmar una solicitud
        /// </summary>
        Task<(bool PuedeFirmar, long? FirmanteId, string? Mensaje)> VerificarPuedeFirmarAsync(
            long entidadId,
            long usuarioId,
            long solicitudId);
    }
}