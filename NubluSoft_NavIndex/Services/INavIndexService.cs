using NubluSoft_NavIndex.Models.DTOs;

namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Servicio principal de NavIndex
    /// </summary>
    public interface INavIndexService
    {
        /// <summary>
        /// Obtiene la estructura documental comprimida con gzip
        /// </summary>
        Task<byte[]> ObtenerEstructuraComprimidaAsync(long entidadId);

        /// <summary>
        /// Obtiene solo la versión de la estructura
        /// </summary>
        Task<VersionEstructura> ObtenerVersionAsync(long entidadId);

        /// <summary>
        /// Obtiene el índice (contenido) de una carpeta específica comprimido
        /// </summary>
        Task<byte[]?> ObtenerIndiceComprimidoAsync(long entidadId, long carpetaId);

        /// <summary>
        /// Obtiene un nodo específico por su ID
        /// </summary>
        Task<NodoNavegacion?> ObtenerNodoAsync(long carpetaId);

        /// <summary>
        /// Regenera la estructura documental (invalida caché y regenera)
        /// </summary>
        Task<ResultadoNavIndex> RegenerarEstructuraAsync(long entidadId);

        /// <summary>
        /// Invalida el caché de una entidad
        /// </summary>
        Task InvalidarCacheAsync(long entidadId);

        /// <summary>
        /// Invalida el caché de un índice específico
        /// </summary>
        Task InvalidarIndiceCacheAsync(long entidadId, long carpetaId);
    }
}