using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Extensions;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RadicadosController : ControllerBase
    {
        private readonly IRadicadosService _service;
        private readonly ILogger<RadicadosController> _logger;

        public RadicadosController(IRadicadosService service, ILogger<RadicadosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ==================== CONSULTAS ====================

        /// <summary>
        /// Obtiene radicados con filtros
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosRadicadosRequest filtros)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var radicados = await _service.ObtenerConFiltrosAsync(entidadId, filtros);
            return Ok(radicados);
        }

        /// <summary>
        /// Obtiene un radicado por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var radicado = await _service.ObtenerPorIdAsync(id);
            if (radicado == null)
                return NotFound(new { Message = "Radicado no encontrado" });

            return Ok(radicado);
        }

        /// <summary>
        /// Busca un radicado por su número
        /// </summary>
        [HttpGet("buscar/{numeroRadicado}")]
        public async Task<IActionResult> GetByNumero(string numeroRadicado)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var radicado = await _service.ObtenerPorNumeroAsync(entidadId, numeroRadicado);
            if (radicado == null)
                return NotFound(new { Message = "Radicado no encontrado" });

            return Ok(radicado);
        }

        /// <summary>
        /// Obtiene la trazabilidad de un radicado
        /// </summary>
        [HttpGet("{id}/trazabilidad")]
        public async Task<IActionResult> GetTrazabilidad(long id)
        {
            var trazabilidad = await _service.ObtenerTrazabilidadAsync(id);
            return Ok(trazabilidad);
        }

        /// <summary>
        /// Obtiene los anexos de un radicado
        /// </summary>
        [HttpGet("{id}/anexos")]
        public async Task<IActionResult> GetAnexos(long id)
        {
            var anexos = await _service.ObtenerAnexosAsync(id);
            return Ok(anexos);
        }

        /// <summary>
        /// Obtiene radicados próximos a vencer
        /// </summary>
        [HttpGet("por-vencer")]
        public async Task<IActionResult> GetPorVencer([FromQuery] int dias = 3)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var radicados = await _service.ObtenerPorVencerAsync(entidadId, dias);
            return Ok(radicados);
        }

        /// <summary>
        /// Obtiene radicados vencidos
        /// </summary>
        [HttpGet("vencidos")]
        public async Task<IActionResult> GetVencidos()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var radicados = await _service.ObtenerVencidosAsync(entidadId);
            return Ok(radicados);
        }

        // ==================== RADICACIÓN ====================

        /// <summary>
        /// Radica una comunicación de entrada (usa función PostgreSQL F_RadicarEntrada)
        /// </summary>
        [HttpPost("entrada")]
        public async Task<IActionResult> RadicarEntrada([FromBody] RadicarEntradaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.RadicarEntradaAsync(entidadId, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            var radicado = await _service.ObtenerPorIdAsync(resultado.RadicadoCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.RadicadoCod }, new
            {
                radicado,
                resultado.NumeroRadicado,
                resultado.FechaVencimiento,
                resultado.Mensaje
            });
        }

        /// <summary>
        /// Radica una comunicación de salida (usa función PostgreSQL F_RadicarSalida)
        /// </summary>
        [HttpPost("salida")]
        public async Task<IActionResult> RadicarSalida([FromBody] RadicarSalidaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.RadicarSalidaAsync(entidadId, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            var radicado = await _service.ObtenerPorIdAsync(resultado.RadicadoCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.RadicadoCod }, new
            {
                radicado,
                resultado.NumeroRadicado,
                resultado.Mensaje
            });
        }

        /// <summary>
        /// Radica una comunicación interna (usa función PostgreSQL F_RadicarInterna)
        /// </summary>
        [HttpPost("interna")]
        public async Task<IActionResult> RadicarInterna([FromBody] RadicarInternaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.RadicarInternaAsync(entidadId, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            var radicado = await _service.ObtenerPorIdAsync(resultado.RadicadoCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.RadicadoCod }, new
            {
                radicado,
                resultado.NumeroRadicado,
                resultado.Mensaje
            });
        }

        // ==================== GESTIÓN ====================

        /// <summary>
        /// Asigna/reasigna un radicado (usa función PostgreSQL F_AsignarRadicado)
        /// </summary>
        [HttpPost("{id}/asignar")]
        public async Task<IActionResult> Asignar(long id, [FromBody] AsignarRadicadoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.AsignarAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }

        /// <summary>
        /// Traslada por competencia según Ley 1755 Art. 21 (usa función PostgreSQL F_TrasladarRadicado)
        /// </summary>
        [HttpPost("{id}/trasladar")]
        public async Task<IActionResult> Trasladar(long id, [FromBody] TrasladarRadicadoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.TrasladarAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje, resultado.FechaVencimiento });
        }

        /// <summary>
        /// Archiva un radicado en un expediente (usa función PostgreSQL F_ArchivarRadicado)
        /// </summary>
        [HttpPost("{id}/archivar")]
        public async Task<IActionResult> Archivar(long id, [FromBody] ArchivarRadicadoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.ArchivarAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }

        /// <summary>
        /// Solicita prórroga según Ley 1755 Art. 14 (usa función PostgreSQL F_SolicitarProrroga)
        /// </summary>
        [HttpPost("{id}/prorroga")]
        public async Task<IActionResult> SolicitarProrroga(long id, [FromBody] SolicitarProrrogaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.SolicitarProrrogaAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje, resultado.FechaVencimiento });
        }

        /// <summary>
        /// Anula un radicado (usa función PostgreSQL F_AnularRadicado)
        /// </summary>
        [HttpPost("{id}/anular")]
        public async Task<IActionResult> Anular(long id, [FromBody] AnularRadicadoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.AnularAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje });
        }

        // ==================== ANEXOS ====================

        /// <summary>
        /// Agrega un anexo a un radicado
        /// </summary>
        [HttpPost("{id}/anexos")]
        public async Task<IActionResult> AgregarAnexo(long id, [FromBody] AgregarAnexoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (success, error) = await _service.AgregarAnexoAsync(id, usuarioId, request);

            if (!success)
                return BadRequest(new { Message = error });

            return Ok(new { Message = "Anexo agregado correctamente" });
        }

        /// <summary>
        /// Elimina un anexo de un radicado
        /// </summary>
        [HttpDelete("anexos/{anexoId}")]
        public async Task<IActionResult> EliminarAnexo(long anexoId)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (success, error) = await _service.EliminarAnexoAsync(anexoId, usuarioId);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }
    }
}