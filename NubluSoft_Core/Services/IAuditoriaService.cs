using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Servicio para consulta de auditoría y trazabilidad del sistema
    /// </summary>
    public interface IAuditoriaService
    {
        // ==================== HISTORIAL DE ACCIONES ====================

        /// <summary>
        /// Obtiene el historial de acciones con filtros y paginación
        /// </summary>
        Task<HistorialAccionesListResponse> ObtenerHistorialAsync(
            long entidadId,
            FiltrosAuditoriaRequest filtros);

        /// <summary>
        /// Obtiene el historial de un registro específico
        /// </summary>
        Task<IEnumerable<HistorialAccionDto>> ObtenerHistorialRegistroAsync(
            string tabla,
            long registroCod);

        /// <summary>
        /// Obtiene las acciones realizadas por un usuario
        /// </summary>
        Task<HistorialAccionesListResponse> ObtenerAccionesUsuarioAsync(
            long entidadId,
            long usuarioId,
            FiltrosAuditoriaRequest filtros);

        // ==================== ARCHIVOS ELIMINADOS ====================

        /// <summary>
        /// Obtiene los archivos eliminados con filtros y paginación
        /// </summary>
        Task<ArchivosEliminadosListResponse> ObtenerArchivosEliminadosAsync(
            long entidadId,
            FiltrosArchivosEliminadosRequest filtros);

        /// <summary>
        /// Obtiene un archivo eliminado por su código original
        /// </summary>
        Task<ArchivoEliminadoDto?> ObtenerArchivoEliminadoAsync(long archivoCod);

        // ==================== CARPETAS ELIMINADAS ====================

        /// <summary>
        /// Obtiene las carpetas eliminadas con filtros y paginación
        /// </summary>
        Task<CarpetasEliminadasListResponse> ObtenerCarpetasEliminadasAsync(
            long entidadId,
            FiltrosCarpetasEliminadasRequest filtros);

        /// <summary>
        /// Obtiene una carpeta eliminada por su código original
        /// </summary>
        Task<CarpetaEliminadaDto?> ObtenerCarpetaEliminadaAsync(long carpetaCod);

        // ==================== ERROR LOG ====================

        /// <summary>
        /// Obtiene los errores del sistema con filtros y paginación
        /// </summary>
        Task<ErrorLogListResponse> ObtenerErroresAsync(FiltrosErrorLogRequest filtros);

        // ==================== RESUMEN / DASHBOARD ====================

        /// <summary>
        /// Obtiene un resumen de auditoría para el dashboard
        /// </summary>
        Task<ResumenAuditoriaDto> ObtenerResumenAsync(long entidadId);

        // ==================== REGISTRO MANUAL ====================

        /// <summary>
        /// Registra una acción manualmente en el historial
        /// </summary>
        Task<bool> RegistrarAccionAsync(
            string tabla,
            long registroCod,
            string accion,
            long usuarioId,
            string? ip = null,
            string? detalleAnterior = null,
            string? detalleNuevo = null,
            string? observaciones = null);
    }
}