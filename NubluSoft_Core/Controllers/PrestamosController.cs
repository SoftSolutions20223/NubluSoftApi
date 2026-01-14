using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Extensions;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    /// <summary>
    /// Controller para gestión de préstamos de expedientes físicos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrestamosController : ControllerBase
    {
        private readonly IPrestamosService _service;
        private readonly ILogger<PrestamosController> _logger;

        public PrestamosController(
            IPrestamosService service,
            ILogger<PrestamosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene préstamos con filtros y paginación
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ListaPrestamosResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosPrestamosRequest? filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resultado = await _service.ObtenerPrestamosAsync(entidadId, filtros);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene un préstamo por su ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PrestamoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id)
        {
            var prestamo = await _service.ObtenerPorIdAsync(id);

            if (prestamo == null)
                return NotFound(new { Message = "Préstamo no encontrado" });

            return Ok(prestamo);
        }

        /// <summary>
        /// Obtiene préstamos de un expediente específico
        /// </summary>
        [HttpGet("expediente/{carpetaId}")]
        [ProducesResponseType(typeof(IEnumerable<PrestamoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByExpediente(long carpetaId)
        {
            var prestamos = await _service.ObtenerPorExpedienteAsync(carpetaId);
            return Ok(prestamos);
        }

        /// <summary>
        /// Obtiene mis solicitudes de préstamo
        /// </summary>
        [HttpGet("mis-solicitudes")]
        [ProducesResponseType(typeof(IEnumerable<PrestamoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMisSolicitudes()
        {
            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "Usuario no autenticado" });

            var prestamos = await _service.ObtenerMisSolicitudesAsync(entidadId, usuarioId);
            return Ok(prestamos);
        }

        /// <summary>
        /// Obtiene préstamos pendientes de autorización
        /// </summary>
        [HttpGet("pendientes")]
        [ProducesResponseType(typeof(IEnumerable<PrestamoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendientes()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var prestamos = await _service.ObtenerPendientesAutorizacionAsync(entidadId);
            return Ok(prestamos);
        }

        /// <summary>
        /// Obtiene préstamos vencidos
        /// </summary>
        [HttpGet("vencidos")]
        [ProducesResponseType(typeof(IEnumerable<PrestamoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVencidos()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var prestamos = await _service.ObtenerVencidosAsync(entidadId);
            return Ok(prestamos);
        }

        /// <summary>
        /// Obtiene resumen de préstamos para dashboard
        /// </summary>
        [HttpGet("resumen")]
        [ProducesResponseType(typeof(ResumenPrestamosDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResumen()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var resumen = await _service.ObtenerResumenAsync(entidadId);
            return Ok(resumen);
        }

        /// <summary>
        /// Verifica disponibilidad de un expediente para préstamo
        /// </summary>
        [HttpGet("disponibilidad/{carpetaId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> VerificarDisponibilidad(long carpetaId)
        {
            var (estaPrestado, prestamoActivo) = await _service.VerificarDisponibilidadAsync(carpetaId);

            return Ok(new
            {
                Disponible = !estaPrestado,
                PrestamoActivo = prestamoActivo
            });
        }

        // ==================== OPERACIONES ====================

        /// <summary>
        /// Solicita préstamo de un expediente
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResultadoPrestamoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SolicitarPrestamo([FromBody] SolicitarPrestamoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "Usuario no autenticado" });

            var resultado = await _service.SolicitarPrestamoAsync(entidadId, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return CreatedAtAction(nameof(GetById), new { id = resultado.PrestamoCod }, resultado);
        }

        /// <summary>
        /// Autoriza o rechaza un préstamo
        /// </summary>
        [HttpPost("{id}/autorizar")]
        [ProducesResponseType(typeof(ResultadoPrestamoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AutorizarPrestamo(long id, [FromBody] AutorizarPrestamoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "Usuario no autenticado" });

            var resultado = await _service.AutorizarPrestamoAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Registra la entrega física del expediente
        /// </summary>
        [HttpPost("{id}/entregar")]
        [ProducesResponseType(typeof(ResultadoPrestamoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrarEntrega(long id, [FromBody] RegistrarEntregaRequest? request)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "Usuario no autenticado" });

            var resultado = await _service.RegistrarEntregaAsync(id, usuarioId, request ?? new RegistrarEntregaRequest());

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Registra la devolución del expediente
        /// </summary>
        [HttpPost("{id}/devolver")]
        [ProducesResponseType(typeof(ResultadoPrestamoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrarDevolucion(long id, [FromBody] RegistrarDevolucionRequest? request)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "Usuario no autenticado" });

            var resultado = await _service.RegistrarDevolucionAsync(id, usuarioId, request ?? new RegistrarDevolucionRequest());

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Cancela una solicitud de préstamo (solo el solicitante)
        /// </summary>
        [HttpPost("{id}/cancelar")]
        [ProducesResponseType(typeof(ResultadoPrestamoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelarSolicitud(long id, [FromBody] CancelarPrestamoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "Usuario no autenticado" });

            var resultado = await _service.CancelarSolicitudAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }
    }
}