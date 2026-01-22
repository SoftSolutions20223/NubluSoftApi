using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface ICarpetasService
    {
        // Consultas
        Task<IEnumerable<Carpeta>> ObtenerPorEntidadAsync(long entidadId, FiltrosCarpetasRequest? filtros = null);
        Task<IEnumerable<Carpeta>> ObtenerHijosAsync(long carpetaPadreId);
        Task<Carpeta?> ObtenerPorIdAsync(long carpetaId);
        Task<IEnumerable<CarpetaArbol>> ObtenerArbolAsync(long entidadId, long? oficina = null);
        Task<IEnumerable<Carpeta>> ObtenerRutaAsync(long carpetaId);

        // Operaciones CRUD (usan funciones PostgreSQL)
        Task<ResultadoCarpeta> CrearAsync(long entidadId, long usuarioId, CrearCarpetaRequest request);
        Task<(bool Success, string? Error)> ActualizarAsync(long carpetaId, long usuarioId, ActualizarCarpetaRequest request);
        Task<ResultadoCarpeta> EliminarAsync(long carpetaId, long usuarioId);
        Task<ResultadoCarpeta> MoverAsync(long carpetaId, long usuarioId, MoverCarpetaRequest request);
        Task<ResultadoCarpeta> CopiarAsync(long carpetaId, long usuarioId, CopiarCarpetaRequest request);

        // Operaciones de expediente
        Task<(bool Success, string? Error)> CerrarExpedienteAsync(long carpetaId, long usuarioId, CerrarExpedienteRequest request);
        Task<(bool Success, string? Error)> ReabrirExpedienteAsync(long carpetaId, long usuarioId);

        // Estadísticas
        Task<CarpetaEstadisticasDto?> ObtenerEstadisticasAsync(long carpetaId, long entidadId);
    }
}