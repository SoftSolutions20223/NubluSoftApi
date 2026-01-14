using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Servicio para gestión de configuración de firma electrónica por entidad
    /// </summary>
    public interface IConfiguracionFirmaService
    {
        /// <summary>
        /// Obtiene la configuración de firma de una entidad
        /// </summary>
        Task<ConfiguracionFirmaDto?> ObtenerConfiguracionAsync(long entidadId);

        /// <summary>
        /// Crea configuración inicial para una entidad (si no existe)
        /// </summary>
        Task<ResultadoConfiguracionDto> CrearConfiguracionAsync(long entidadId);

        /// <summary>
        /// Actualiza la configuración completa
        /// </summary>
        Task<ResultadoConfiguracionDto> ActualizarConfiguracionAsync(
            long entidadId,
            ActualizarConfiguracionCompletaRequest request);

        /// <summary>
        /// Actualiza solo la configuración de OTP
        /// </summary>
        Task<ResultadoConfiguracionDto> ActualizarConfigOtpAsync(
            long entidadId,
            ActualizarConfigOtpRequest request);

        /// <summary>
        /// Actualiza solo la configuración de certificados
        /// </summary>
        Task<ResultadoConfiguracionDto> ActualizarConfigCertificadosAsync(
            long entidadId,
            ActualizarConfigCertificadosRequest request);

        /// <summary>
        /// Actualiza solo la configuración del sello visual
        /// </summary>
        Task<ResultadoConfiguracionDto> ActualizarConfigSelloAsync(
            long entidadId,
            ActualizarConfigSelloRequest request);

        /// <summary>
        /// Actualiza solo la configuración de notificaciones
        /// </summary>
        Task<ResultadoConfiguracionDto> ActualizarConfigNotificacionesAsync(
            long entidadId,
            ActualizarConfigNotificacionesRequest request);

        /// <summary>
        /// Actualiza las plantillas de email
        /// </summary>
        Task<ResultadoConfiguracionDto> ActualizarPlantillasAsync(
            long entidadId,
            ActualizarPlantillasRequest request);

        /// <summary>
        /// Actualiza solo los límites
        /// </summary>
        Task<ResultadoConfiguracionDto> ActualizarConfigLimitesAsync(
            long entidadId,
            ActualizarConfigLimitesRequest request);

        /// <summary>
        /// Restaura la configuración a valores por defecto
        /// </summary>
        Task<ResultadoConfiguracionDto> RestaurarValoresPorDefectoAsync(long entidadId);

        /// <summary>
        /// Obtiene las variables disponibles para plantillas
        /// </summary>
        IEnumerable<VariablesPlantillaDto> ObtenerVariablesPlantilla();
    }
}