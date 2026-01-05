using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface IOficinasService
    {
        Task<IEnumerable<Oficina>> ObtenerPorEntidadAsync(long entidadId);
        Task<Oficina?> ObtenerPorIdAsync(long entidadId, long oficinaId);
        Task<IEnumerable<OficinaArbol>> ObtenerArbolAsync(long entidadId);
        Task<(Oficina? Oficina, string? Error)> CrearAsync(long entidadId, CrearOficinaRequest request);
        Task<(bool Success, string? Error)> ActualizarAsync(long entidadId, long oficinaId, ActualizarOficinaRequest request);
        Task<(bool Success, string? Error)> EliminarAsync(long entidadId, long oficinaId);
    }
}