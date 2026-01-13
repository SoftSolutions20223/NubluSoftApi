using NubluSoft_Signature.Models.DTOs;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Servicio para gestión de códigos OTP
    /// </summary>
    public interface IOtpService
    {
        /// <summary>
        /// Genera y envía un código OTP al firmante
        /// </summary>
        /// <param name="firmanteId">ID del firmante</param>
        /// <param name="medio">Medio de envío (EMAIL o SMS)</param>
        /// <param name="ip">IP del cliente</param>
        /// <param name="userAgent">User-Agent del cliente</param>
        /// <returns>Resultado de la operación</returns>
        Task<GenerarOtpResponse> GenerarYEnviarAsync(
            long firmanteId,
            string medio,
            string ip,
            string userAgent);

        /// <summary>
        /// Valida un código OTP
        /// </summary>
        /// <param name="firmanteId">ID del firmante</param>
        /// <param name="codigo">Código ingresado</param>
        /// <param name="ip">IP del cliente</param>
        /// <returns>Resultado de la validación</returns>
        Task<ValidarOtpResponse> ValidarAsync(
            long firmanteId,
            string codigo,
            string ip);

        /// <summary>
        /// Invalida todos los códigos OTP activos de un firmante
        /// </summary>
        Task InvalidarCodigosActivosAsync(long firmanteId);
    }
}