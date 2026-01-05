using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface ITercerosService
    {
        // Consultas
        Task<IEnumerable<Tercero>> ObtenerConFiltrosAsync(long entidadId, FiltrosTercerosRequest filtros);
        Task<Tercero?> ObtenerPorIdAsync(long terceroId);
        Task<Tercero?> ObtenerPorDocumentoAsync(long entidadId, string tipoDocumento, string documento);
        Task<TerceroConEstadisticas?> ObtenerConEstadisticasAsync(long terceroId);
        Task<IEnumerable<Radicado>> ObtenerRadicadosAsync(long terceroId, int? limite = 20);

        // CRUD
        Task<(Tercero? Tercero, string? Error)> CrearAsync(long entidadId, long usuarioId, CrearTerceroRequest request);
        Task<(bool Success, string? Error)> ActualizarAsync(long terceroId, long usuarioId, ActualizarTerceroRequest request);
        Task<(bool Success, string? Error)> EliminarAsync(long terceroId);

        // Utilidades
        Task<bool> ExisteDocumentoAsync(long entidadId, string tipoDocumento, string documento, long? excluirId = null);
    }
}