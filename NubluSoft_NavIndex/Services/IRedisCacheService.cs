namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Servicio de caché en Redis para NavIndex
    /// </summary>
    public interface IRedisCacheService
    {
        /// <summary>
        /// Obtiene la estructura documental comprimida de Redis
        /// </summary>
        Task<byte[]?> ObtenerEstructuraComprimidaAsync(long entidadId);

        /// <summary>
        /// Guarda la estructura documental comprimida en Redis
        /// </summary>
        Task GuardarEstructuraComprimidaAsync(long entidadId, byte[] datosComprimidos, string version);

        /// <summary>
        /// Obtiene solo la versión de la estructura
        /// </summary>
        Task<string?> ObtenerVersionAsync(long entidadId);

        /// <summary>
        /// Invalida el caché de una entidad
        /// </summary>
        Task InvalidarCacheAsync(long entidadId);

        /// <summary>
        /// Obtiene el índice de una carpeta específica
        /// </summary>
        Task<byte[]?> ObtenerIndiceComprimidoAsync(long entidadId, long carpetaId);

        /// <summary>
        /// Guarda el índice de una carpeta
        /// </summary>
        Task GuardarIndiceComprimidoAsync(long entidadId, long carpetaId, byte[] datosComprimidos);

        /// <summary>
        /// Invalida el índice de una carpeta
        /// </summary>
        Task InvalidarIndiceAsync(long entidadId, long carpetaId);
    }
}