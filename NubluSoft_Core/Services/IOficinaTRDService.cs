using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Servicio para gestión de permisos de TRD por oficina
    /// </summary>
    public interface IOficinaTRDService
    {
        // ==================== CONSULTAS ====================

        Task<IEnumerable<OficinaTRDDto>> ObtenerAsignacionesAsync(
            long entidadId,
            FiltrosOficinaTRDRequest? filtros = null);

        Task<IEnumerable<TRDConPermisosDto>> ObtenerTRDsDeOficinaAsync(
            long entidadId,
            long oficinaId);

        Task<IEnumerable<OficinaTRDDto>> ObtenerOficinasConTRDAsync(
            long entidadId,
            long trdId);

        Task<ResumenPermisosOficinaDto> ObtenerResumenOficinaAsync(
            long entidadId,
            long oficinaId);

        Task<IEnumerable<TRDConPermisosDto>> ObtenerTRDsConEstadoAccesoAsync(
            long entidadId,
            long oficinaId);

        // ==================== VERIFICACIÓN DE ACCESO ====================

        Task<VerificacionAccesoCarpetaDto> VerificarAccesoCarpetaAsync(
            long entidadId,
            long oficinaId,
            long carpetaId);

        Task<VerificacionAccesoCarpetaDto> VerificarAccesoTRDAsync(
            long entidadId,
            long oficinaId,
            long trdId);

        // ==================== ASIGNACIÓN ====================

        Task<ResultadoOficinaTRDDto> AsignarTRDAsync(
            long entidadId,
            AsignarTRDAOficinaRequest request);

        Task<ResultadoAsignacionMasivaDto> AsignarTRDsMasivoAsync(
            long entidadId,
            AsignarTRDsMasivoRequest request);

        Task<ResultadoOficinaTRDDto> ActualizarPermisosAsync(
            long entidadId,
            long oficinaId,
            long trdId,
            ActualizarPermisosOficinaTRDRequest request);

        Task<ResultadoOficinaTRDDto> RevocarTRDAsync(
            long entidadId,
            long oficinaId,
            long trdId);

        Task<ResultadoAsignacionMasivaDto> RevocarTodosAsync(
            long entidadId,
            long oficinaId);
    }
}