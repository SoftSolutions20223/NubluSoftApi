using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface ITRDService
    {
        // Consultas TRD
        Task<IEnumerable<TRD>> ObtenerPorEntidadAsync(long entidadId, FiltrosTRDRequest? filtros = null);
        Task<TRD?> ObtenerPorIdAsync(long trdId);
        Task<IEnumerable<TRDArbol>> ObtenerArbolAsync(long entidadId);
        Task<IEnumerable<TRD>> ObtenerSubseriesAsync(long trdPadreId);

        // CRUD TRD
        Task<ResultadoTRD> CrearAsync(long entidadId, long usuarioId, CrearTRDRequest request);
        Task<(bool Success, string? Error)> ActualizarAsync(long trdId, long usuarioId, ActualizarTRDRequest request);
        Task<(bool Success, string? Error)> EliminarAsync(long trdId);

        // Asignaciones Oficina-TRD
        Task<IEnumerable<OficinaTRD>> ObtenerOficinasAsignadasAsync(long trdId);
        Task<IEnumerable<OficinaTRD>> ObtenerTRDsPorOficinaAsync(long entidadId, long oficinaId);
        Task<ResultadoTRD> AsignarOficinaAsync(long trdId, long entidadId, long usuarioId, AsignarTRDOficinaRequest request);
        Task<(bool Success, string? Error)> RevocarOficinaAsync(long trdId, long entidadId, long oficinaId);
        Task<(bool Success, string? Error)> ActualizarPermisosAsync(long trdId, long entidadId, long oficinaId, ActualizarPermisosTRDRequest request);
    }
}