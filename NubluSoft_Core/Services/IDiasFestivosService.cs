using NubluSoft_Core.Models.DTOs;

namespace NubluSoft_Core.Services
{
    /// <summary>
    /// Servicio para gestión de días festivos y cálculo de términos legales
    /// </summary>
    public interface IDiasFestivosService
    {
        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene todos los días festivos con filtros
        /// </summary>
        Task<IEnumerable<DiaFestivoDto>> ObtenerTodosAsync(FiltrosDiasFestivosRequest? filtros = null);

        /// <summary>
        /// Obtiene un día festivo por fecha
        /// </summary>
        Task<DiaFestivoDto?> ObtenerPorFechaAsync(DateTime fecha);

        /// <summary>
        /// Obtiene los días festivos de un año específico
        /// </summary>
        Task<IEnumerable<DiaFestivoDto>> ObtenerPorAnioAsync(int anio);

        /// <summary>
        /// Obtiene los próximos N días festivos
        /// </summary>
        Task<IEnumerable<DiaFestivoDto>> ObtenerProximosAsync(int cantidad = 5);

        /// <summary>
        /// Obtiene resumen de días festivos
        /// </summary>
        Task<ResumenDiasFestivosDto> ObtenerResumenAsync(int? anio = null);

        // ==================== VERIFICACIONES ====================

        /// <summary>
        /// Verifica si una fecha es día hábil
        /// </summary>
        Task<VerificacionDiaHabilDto> VerificarDiaHabilAsync(DateTime fecha);

        /// <summary>
        /// Verifica si una fecha es festivo
        /// </summary>
        Task<bool> EsFestivoAsync(DateTime fecha);

        // ==================== CÁLCULOS ====================

        /// <summary>
        /// Calcula la fecha sumando días hábiles
        /// </summary>
        Task<CalculoDiasHabilesDto> CalcularFechaConDiasHabilesAsync(DateTime fechaInicio, int diasHabiles);

        /// <summary>
        /// Cuenta los días hábiles entre dos fechas
        /// </summary>
        Task<int> ContarDiasHabilesEntreAsync(DateTime fechaInicio, DateTime fechaFin);

        // ==================== CRUD ====================

        /// <summary>
        /// Crea un nuevo día festivo
        /// </summary>
        Task<ResultadoDiaFestivoDto> CrearAsync(CrearDiaFestivoRequest request);

        /// <summary>
        /// Actualiza un día festivo existente
        /// </summary>
        Task<ResultadoDiaFestivoDto> ActualizarAsync(DateTime fecha, ActualizarDiaFestivoRequest request);

        /// <summary>
        /// Elimina un día festivo
        /// </summary>
        Task<ResultadoDiaFestivoDto> EliminarAsync(DateTime fecha);

        // ==================== GENERACIÓN MASIVA ====================

        /// <summary>
        /// Genera los días festivos de Colombia para un año
        /// </summary>
        Task<ResultadoGeneracionFestivosDto> GenerarFestivosColombiaAsync(int anio, bool sobreescribir = false);
    }
}