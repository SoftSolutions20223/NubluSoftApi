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

        // ==================== UPLOAD DIRECTO (Opción C) ====================

        /// <summary>
        /// Inicia el proceso de upload obteniendo URL firmada
        /// </summary>
        Task<IniciarUploadResponse> IniciarUploadAsync(long usuarioId, long entidadId, IniciarUploadRequest request, string authToken);

        /// <summary>
        /// Confirma que el upload se completó exitosamente
        /// </summary>
        Task<ConfirmarUploadResponse> ConfirmarUploadAsync(long archivoId, long usuarioId, ConfirmarUploadRequest request, string authToken);

        /// <summary>
        /// Cancela un upload pendiente
        /// </summary>
        Task<ResultadoArchivo> CancelarUploadAsync(long archivoId, long usuarioId, string? motivo = null, string? authToken = null);

        /// <summary>
        /// Obtiene URL de descarga para un archivo
        /// </summary>
        Task<DescargarArchivoResponse> ObtenerUrlDescargaAsync(long archivoId, long usuarioId, string authToken);
    }
}