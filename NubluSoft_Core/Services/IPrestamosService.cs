using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Servicio para gestión de préstamos de expedientes físicos
    /// </summary>
    public interface IPrestamosService
    {
        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene préstamos con filtros y paginación
        /// </summary>
        Task<ListaPrestamosResponse> ObtenerPrestamosAsync(
            long entidadId,
            FiltrosPrestamosRequest? filtros = null);

        /// <summary>
        /// Obtiene un préstamo por su ID
        /// </summary>
        Task<PrestamoDto?> ObtenerPorIdAsync(long prestamoCod);

        /// <summary>
        /// Obtiene préstamos de un expediente específico
        /// </summary>
        Task<IEnumerable<PrestamoDto>> ObtenerPorExpedienteAsync(long carpetaId);

        /// <summary>
        /// Obtiene préstamos solicitados por un usuario
        /// </summary>
        Task<IEnumerable<PrestamoDto>> ObtenerMisSolicitudesAsync(
            long entidadId,
            long usuarioId);

        /// <summary>
        /// Obtiene préstamos pendientes de autorización
        /// </summary>
        Task<IEnumerable<PrestamoDto>> ObtenerPendientesAutorizacionAsync(long entidadId);

        /// <summary>
        /// Obtiene préstamos vencidos
        /// </summary>
        Task<IEnumerable<PrestamoDto>> ObtenerVencidosAsync(long entidadId);

        /// <summary>
        /// Obtiene resumen de préstamos para dashboard
        /// </summary>
        Task<ResumenPrestamosDto> ObtenerResumenAsync(long entidadId);

        /// <summary>
        /// Verifica si un expediente está actualmente prestado
        /// </summary>
        Task<(bool EstaPrestado, PrestamoDto? PrestamoActivo)> VerificarDisponibilidadAsync(long carpetaId);

        // ==================== OPERACIONES ====================

        /// <summary>
        /// Solicita préstamo de un expediente
        /// </summary>
        Task<ResultadoPrestamoDto> SolicitarPrestamoAsync(
            long entidadId,
            long usuarioId,
            SolicitarPrestamoRequest request);

        /// <summary>
        /// Autoriza o rechaza un préstamo
        /// </summary>
        Task<ResultadoPrestamoDto> AutorizarPrestamoAsync(
            long prestamoCod,
            long usuarioId,
            AutorizarPrestamoRequest request);

        /// <summary>
        /// Registra la entrega física del expediente
        /// </summary>
        Task<ResultadoPrestamoDto> RegistrarEntregaAsync(
            long prestamoCod,
            long usuarioId,
            RegistrarEntregaRequest request);

        /// <summary>
        /// Registra la devolución del expediente
        /// </summary>
        Task<ResultadoPrestamoDto> RegistrarDevolucionAsync(
            long prestamoCod,
            long usuarioId,
            RegistrarDevolucionRequest request);

        /// <summary>
        /// Cancela una solicitud de préstamo (solo el solicitante)
        /// </summary>
        Task<ResultadoPrestamoDto> CancelarSolicitudAsync(
            long prestamoCod,
            long usuarioId,
            CancelarPrestamoRequest request);
    }
}