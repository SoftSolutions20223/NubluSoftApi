using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    /// <summary>
    /// Controller para gestión de días festivos y cálculo de términos legales
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DiasFestivosController : ControllerBase
    {
        private readonly IDiasFestivosService _service;
        private readonly ILogger<DiasFestivosController> _logger;

        public DiasFestivosController(
            IDiasFestivosService service,
            ILogger<DiasFestivosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene todos los días festivos con filtros opcionales
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DiaFestivoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosDiasFestivosRequest? filtros)
        {
            var resultado = await _service.ObtenerTodosAsync(filtros);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene un día festivo por fecha
        /// </summary>
        [HttpGet("{fecha}")]
        [ProducesResponseType(typeof(DiaFestivoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByFecha(DateTime fecha)
        {
            var resultado = await _service.ObtenerPorFechaAsync(fecha);
            if (resultado == null)
                return NotFound(new { Message = "No existe día festivo para esa fecha" });

            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene los días festivos de un año específico
        /// </summary>
        [HttpGet("anio/{anio}")]
        [ProducesResponseType(typeof(IEnumerable<DiaFestivoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByAnio(int anio)
        {
            var resultado = await _service.ObtenerPorAnioAsync(anio);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene los próximos N días festivos
        /// </summary>
        [HttpGet("proximos")]
        [ProducesResponseType(typeof(IEnumerable<DiaFestivoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProximos([FromQuery] int cantidad = 5)
        {
            var resultado = await _service.ObtenerProximosAsync(cantidad);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene resumen de días festivos
        /// </summary>
        [HttpGet("resumen")]
        [ProducesResponseType(typeof(ResumenDiasFestivosDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResumen([FromQuery] int? anio)
        {
            var resultado = await _service.ObtenerResumenAsync(anio);
            return Ok(resultado);
        }

        // ==================== VERIFICACIONES ====================

        /// <summary>
        /// Verifica si una fecha es día hábil
        /// </summary>
        [HttpGet("verificar-habil/{fecha}")]
        [ProducesResponseType(typeof(VerificacionDiaHabilDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerificarDiaHabil(DateTime fecha)
        {
            var resultado = await _service.VerificarDiaHabilAsync(fecha);
            return Ok(resultado);
        }

        /// <summary>
        /// Verifica si una fecha es festivo
        /// </summary>
        [HttpGet("es-festivo/{fecha}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> EsFestivo(DateTime fecha)
        {
            var esFestivo = await _service.EsFestivoAsync(fecha);
            return Ok(new { Fecha = fecha, EsFestivo = esFestivo });
        }

        // ==================== CÁLCULOS ====================

        /// <summary>
        /// Calcula la fecha sumando días hábiles a una fecha inicial
        /// Útil para calcular fechas de vencimiento según términos legales
        /// </summary>
        [HttpPost("calcular-fecha")]
        [ProducesResponseType(typeof(CalculoDiasHabilesDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CalcularFecha([FromBody] CalcularDiasHabilesRequest request)
        {
            var resultado = await _service.CalcularFechaConDiasHabilesAsync(request.FechaInicio, request.DiasHabiles);
            return Ok(resultado);
        }

        /// <summary>
        /// Cuenta los días hábiles entre dos fechas
        /// </summary>
        [HttpGet("contar-habiles")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> ContarDiasHabiles([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
        {
            var diasHabiles = await _service.ContarDiasHabilesEntreAsync(fechaInicio, fechaFin);
            return Ok(new
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                DiasHabiles = diasHabiles
            });
        }

        // ==================== CRUD ====================

        /// <summary>
        /// Crea un nuevo día festivo
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResultadoDiaFestivoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CrearDiaFestivoRequest request)
        {
            var resultado = await _service.CrearAsync(request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return CreatedAtAction(nameof(GetByFecha), new { fecha = request.Fecha.ToString("yyyy-MM-dd") }, resultado);
        }

        /// <summary>
        /// Actualiza un día festivo existente
        /// </summary>
        [HttpPut("{fecha}")]
        [ProducesResponseType(typeof(ResultadoDiaFestivoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(DateTime fecha, [FromBody] ActualizarDiaFestivoRequest request)
        {
            var resultado = await _service.ActualizarAsync(fecha, request);

            if (!resultado.Exito)
                return NotFound(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Elimina un día festivo
        /// </summary>
        [HttpDelete("{fecha}")]
        [ProducesResponseType(typeof(ResultadoDiaFestivoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(DateTime fecha)
        {
            var resultado = await _service.EliminarAsync(fecha);

            if (!resultado.Exito)
                return NotFound(resultado);

            return Ok(resultado);
        }

        // ==================== GENERACIÓN MASIVA ====================

        /// <summary>
        /// Genera automáticamente los días festivos de Colombia para un año
        /// Implementa la Ley Emiliani (Ley 51 de 1983)
        /// </summary>
        [HttpPost("generar/{anio}")]
        [ProducesResponseType(typeof(ResultadoGeneracionFestivosDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerarFestivos(int anio, [FromQuery] bool sobreescribir = false)
        {
            var resultado = await _service.GenerarFestivosColombiaAsync(anio, sobreescribir);
            return Ok(resultado);
        }
    }
}