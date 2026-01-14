using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Servicio para gestión de entidades del sistema
    /// </summary>
    public interface IEntidadesService
    {
        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene entidades con filtros y paginación
        /// </summary>
        Task<ListaEntidadesResponse> ObtenerEntidadesAsync(FiltrosEntidadesRequest? filtros = null);

        /// <summary>
        /// Obtiene una entidad por su ID
        /// </summary>
        Task<EntidadDto?> ObtenerPorIdAsync(long entidadId);

        /// <summary>
        /// Obtiene una entidad por NIT
        /// </summary>
        Task<EntidadDto?> ObtenerPorNitAsync(string nit);

        /// <summary>
        /// Obtiene los planes asignados a una entidad
        /// </summary>
        Task<IEnumerable<PlanEntidadDto>> ObtenerPlanesEntidadAsync(long entidadId);

        /// <summary>
        /// Obtiene resumen de entidades para dashboard
        /// </summary>
        Task<ResumenEntidadesDto> ObtenerResumenAsync();

        /// <summary>
        /// Verifica si un NIT ya está registrado
        /// </summary>
        Task<bool> ExisteNitAsync(string nit, long? excluirEntidadId = null);

        // ==================== OPERACIONES ====================

        /// <summary>
        /// Crea una nueva entidad
        /// </summary>
        Task<ResultadoEntidadDto> CrearAsync(CrearEntidadRequest request);

        /// <summary>
        /// Actualiza una entidad existente
        /// </summary>
        Task<ResultadoEntidadDto> ActualizarAsync(long entidadId, ActualizarEntidadRequest request);

        /// <summary>
        /// Extiende el plan de una entidad
        /// </summary>
        Task<ResultadoEntidadDto> ExtenderPlanAsync(long entidadId, ExtenderPlanRequest request);

        /// <summary>
        /// Asigna un plan a una entidad
        /// </summary>
        Task<ResultadoEntidadDto> AsignarPlanAsync(long entidadId, AsignarPlanRequest request);

        /// <summary>
        /// Desactiva una entidad (soft delete)
        /// </summary>
        Task<ResultadoEntidadDto> DesactivarAsync(long entidadId);
    }
}