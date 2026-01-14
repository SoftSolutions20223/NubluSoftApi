using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Signature.Extensions;
using NubluSoft_Signature.Models.DTOs;
using NubluSoft_Signature.Services;

namespace NubluSoft_Signature.Controllers
{
    /// <summary>
    /// Controller para gestión de configuración de firma electrónica
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConfiguracionController : ControllerBase
    {
        private readonly IConfiguracionFirmaService _service;
        private readonly ILogger<ConfiguracionController> _logger;

        public ConfiguracionController(
            IConfiguracionFirmaService service,
            ILogger<ConfiguracionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la configuración de firma de la entidad actual
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ConfiguracionFirmaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetConfiguracion()
        {
            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var config = await _service.ObtenerConfiguracionAsync(entidadId.Value);

            if (config == null)
            {
                // Crear configuración por defecto si no existe
                var resultado = await _service.CrearConfiguracionAsync(entidadId.Value);
                return Ok(resultado.Configuracion);
            }

            return Ok(config);
        }

        /// <summary>
        /// Actualiza toda la configuración
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(ResultadoConfiguracionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateConfiguracion([FromBody] ActualizarConfiguracionCompletaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ActualizarConfiguracionAsync(entidadId.Value, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Actualiza configuración de OTP (firma simple)
        /// </summary>
        [HttpPut("otp")]
        [ProducesResponseType(typeof(ResultadoConfiguracionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateConfigOtp([FromBody] ActualizarConfigOtpRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ActualizarConfigOtpAsync(entidadId.Value, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Actualiza configuración de certificados (firma avanzada)
        /// </summary>
        [HttpPut("certificados")]
        [ProducesResponseType(typeof(ResultadoConfiguracionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateConfigCertificados([FromBody] ActualizarConfigCertificadosRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ActualizarConfigCertificadosAsync(entidadId.Value, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Actualiza configuración del sello visual en PDF
        /// </summary>
        [HttpPut("sello")]
        [ProducesResponseType(typeof(ResultadoConfiguracionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateConfigSello([FromBody] ActualizarConfigSelloRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ActualizarConfigSelloAsync(entidadId.Value, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Actualiza configuración de notificaciones por email
        /// </summary>
        [HttpPut("notificaciones")]
        [ProducesResponseType(typeof(ResultadoConfiguracionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateConfigNotificaciones([FromBody] ActualizarConfigNotificacionesRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ActualizarConfigNotificacionesAsync(entidadId.Value, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Actualiza plantillas de email
        /// </summary>
        [HttpPut("plantillas")]
        [ProducesResponseType(typeof(ResultadoConfiguracionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePlantillas([FromBody] ActualizarPlantillasRequest request)
        {
            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ActualizarPlantillasAsync(entidadId.Value, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Actualiza límites de solicitudes
        /// </summary>
        [HttpPut("limites")]
        [ProducesResponseType(typeof(ResultadoConfiguracionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateConfigLimites([FromBody] ActualizarConfigLimitesRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ActualizarConfigLimitesAsync(entidadId.Value, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Restaura la configuración a valores por defecto
        /// </summary>
        [HttpPost("restaurar")]
        [ProducesResponseType(typeof(ResultadoConfiguracionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> RestaurarDefaults()
        {
            var entidadId = User.GetEntidadId();
            if (!entidadId.HasValue)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.RestaurarValoresPorDefectoAsync(entidadId.Value);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene las variables disponibles para usar en plantillas de email
        /// </summary>
        [HttpGet("plantillas/variables")]
        [ProducesResponseType(typeof(IEnumerable<VariablesPlantillaDto>), StatusCodes.Status200OK)]
        public IActionResult GetVariablesPlantilla()
        {
            var variables = _service.ObtenerVariablesPlantilla();
            return Ok(variables);
        }
    }
}