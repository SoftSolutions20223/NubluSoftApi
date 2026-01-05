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
    public class ArchivosController : ControllerBase
    {
        private readonly IArchivosService _service;
        private readonly ILogger<ArchivosController> _logger;

        public ArchivosController(IArchivosService service, ILogger<ArchivosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene archivos con filtros opcionales
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] FiltrosArchivosRequest filtros)
        {
            var archivos = await _service.ObtenerConFiltrosAsync(filtros);
            return Ok(archivos);
        }

        /// <summary>
        /// Obtiene un archivo por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var archivo = await _service.ObtenerPorIdAsync(id);
            if (archivo == null)
                return NotFound(new { Message = "Archivo no encontrado" });

            return Ok(archivo);
        }

        /// <summary>
        /// Obtiene archivos de una carpeta específica
        /// </summary>
        [HttpGet("carpeta/{carpetaId}")]
        public async Task<IActionResult> GetPorCarpeta(long carpetaId)
        {
            var archivos = await _service.ObtenerPorCarpetaAsync(carpetaId);
            return Ok(archivos);
        }

        /// <summary>
        /// Obtiene el historial de versiones de un archivo
        /// </summary>
        [HttpGet("{id}/versiones")]
        public async Task<IActionResult> GetVersiones(long id)
        {
            var versiones = await _service.ObtenerVersionesAsync(id);
            return Ok(versiones);
        }

        /// <summary>
        /// Crea un archivo (metadatos, usa función PostgreSQL F_CrearArchivo)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearArchivoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.CrearAsync(usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            var archivo = await _service.ObtenerPorIdAsync(resultado.ArchivoCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.ArchivoCod }, archivo);
        }

        /// <summary>
        /// Actualiza los metadatos de un archivo
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ActualizarArchivoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (success, error) = await _service.ActualizarAsync(id, usuarioId, request);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        /// <summary>
        /// Elimina un archivo (usa función PostgreSQL F_EliminarArchivo)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.EliminarAsync(id, usuarioId);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return NoContent();
        }

        /// <summary>
        /// Crea una nueva versión de un archivo (usa función PostgreSQL F_CrearVersionArchivo)
        /// </summary>
        [HttpPost("{id}/versiones")]
        public async Task<IActionResult> CrearVersion(long id, [FromBody] CrearVersionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.CrearVersionAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje, resultado.ArchivoCod, resultado.Version });
        }

        /// <summary>
        /// Restaura una versión anterior del archivo (usa función PostgreSQL F_RestaurarVersionArchivo)
        /// </summary>
        [HttpPost("{id}/restaurar")]
        public async Task<IActionResult> RestaurarVersion(long id, [FromBody] RestaurarVersionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.RestaurarVersionAsync(id, usuarioId, request);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje, resultado.Version });
        }
    }
}