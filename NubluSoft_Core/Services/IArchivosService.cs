using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface IArchivosService
    {
        // Consultas
        Task<IEnumerable<Archivo>> ObtenerPorCarpetaAsync(long carpetaId);
        Task<IEnumerable<Archivo>> ObtenerConFiltrosAsync(FiltrosArchivosRequest filtros);
        Task<Archivo?> ObtenerPorIdAsync(long archivoId);
        Task<IEnumerable<VersionArchivo>> ObtenerVersionesAsync(long archivoId);

        // Operaciones CRUD
        Task<ResultadoArchivo> CrearAsync(long usuarioId, CrearArchivoRequest request);
        Task<(bool Success, string? Error)> ActualizarAsync(long archivoId, long usuarioId, ActualizarArchivoRequest request);
        Task<ResultadoArchivo> EliminarAsync(long archivoId, long usuarioId);

        // Versionamiento
        Task<ResultadoArchivo> CrearVersionAsync(long archivoId, long usuarioId, CrearVersionRequest request);
        Task<ResultadoArchivo> RestaurarVersionAsync(long archivoId, long usuarioId, RestaurarVersionRequest request);
    }
}