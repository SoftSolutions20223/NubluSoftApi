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
        /// También elimina el archivo físico de GCS
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
        /// Mueve un archivo a otra carpeta
        /// </summary>
        [HttpPost("{id}/mover")]
        public async Task<IActionResult> Mover(long id, [FromBody] MoverArchivoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.MoverAsync(id, usuarioId, request.CarpetaDestino);

            if (!resultado.Exito)
                return BadRequest(new { resultado.Mensaje });

            return Ok(new { resultado.Mensaje, resultado.ArchivoCod });
        }

        /// <summary>
        /// Copia un archivo a otra carpeta
        /// </summary>
        [HttpPost("{id}/copiar")]
        public async Task<IActionResult> Copiar(long id, [FromBody] CopiarArchivoRequest request)
        {
            _logger.LogInformation("Copiar archivo {ArchivoId} - Request: CarpetaDestino={CarpetaDestino}, NuevoNombre={NuevoNombre}",
                id, request?.CarpetaDestino, request?.NuevoNombre);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Copiar archivo {ArchivoId} - ModelState inválido: {Errors}", id, string.Join(", ", errors));
                return BadRequest(ModelState);
            }

            var usuarioId = User.GetUserId();
            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var resultado = await _service.CopiarAsync(id, usuarioId, request.CarpetaDestino, request.NuevoNombre);

            if (!resultado.Exito)
            {
                _logger.LogWarning("Copiar archivo {ArchivoId} - Error en servicio: {Mensaje}", id, resultado.Mensaje);
                return BadRequest(new { resultado.Mensaje });
            }

            var archivo = await _service.ObtenerPorIdAsync(resultado.ArchivoCod!.Value);
            return CreatedAtAction(nameof(GetById), new { id = resultado.ArchivoCod }, archivo);
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

        // ==================== UPLOAD DIRECTO (Opción C) ====================

        /// <summary>
        /// Inicia el proceso de upload directo a GCS
        /// Retorna URL firmada para que el frontend suba directamente
        /// </summary>
        [HttpPost("iniciar-upload")]
        public async Task<IActionResult> IniciarUpload([FromBody] IniciarUploadRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = User.GetUserId();
            var entidadId = User.GetEntidadId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            // Obtener token para pasar a Storage
            var authToken = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "") ?? string.Empty;

            var resultado = await _service.IniciarUploadAsync(usuarioId, entidadId, request, authToken);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Confirma que el upload se completó exitosamente
        /// </summary>
        [HttpPost("{id}/confirmar-upload")]
        public async Task<IActionResult> ConfirmarUpload(long id, [FromBody] ConfirmarUploadRequest request)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var authToken = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "") ?? string.Empty;

            var resultado = await _service.ConfirmarUploadAsync(id, usuarioId, request, authToken);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Cancela un upload pendiente
        /// </summary>
        [HttpPost("{id}/cancelar-upload")]
        public async Task<IActionResult> CancelarUpload(long id, [FromBody] CancelarUploadRequest? request)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var authToken = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "") ?? string.Empty;

            var resultado = await _service.CancelarUploadAsync(id, usuarioId, request?.Motivo, authToken);

            if (!resultado.Exito)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene URL firmada para descargar un archivo
        /// </summary>
        [HttpGet("{id}/download-url")]
        public async Task<IActionResult> GetDownloadUrl(long id)
        {
            var usuarioId = User.GetUserId();

            if (usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            _logger.LogInformation("[DOWNLOAD-URL] Solicitando URL para archivo {ArchivoId}, Usuario {UsuarioId}", id, usuarioId);

            var authToken = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "") ?? string.Empty;

            var resultado = await _service.ObtenerUrlDescargaAsync(id, usuarioId, authToken);

            _logger.LogInformation("[DOWNLOAD-URL] Resultado: Exito={Exito}, Mensaje={Mensaje}", resultado.Exito, resultado.Mensaje);

            if (!resultado.Exito)
            {
                _logger.LogWarning("[DOWNLOAD-URL] Archivo {ArchivoId} no encontrado o error: {Mensaje}", id, resultado.Mensaje);
                return NotFound(resultado);
            }

            return Ok(resultado);
        }

        /// <summary>
        /// DIAGNÓSTICO: Ver estado real de un archivo (incluye PENDIENTES)
        /// </summary>
        [HttpGet("{id}/diagnostico")]
        public async Task<IActionResult> GetDiagnostico(long id)
        {
            var archivo = await _service.ObtenerPorIdAsync(id);

            if (archivo == null)
            {
                return Ok(new {
                    Encontrado = false,
                    Mensaje = $"No existe archivo con ID {id} en la base de datos",
                    ArchivoId = id
                });
            }

            return Ok(new {
                Encontrado = true,
                ArchivoId = archivo.Cod,
                Nombre = archivo.Nombre,
                Ruta = archivo.Ruta,
                Estado = archivo.Estado,
                EstadoUpload = archivo.EstadoUpload,
                ContentType = archivo.ContentType,
                Tamano = archivo.Tamano,
                Carpeta = archivo.Carpeta,
                FechaSubida = archivo.FechaSubida
            });
        }

        /// <summary>
        /// DIAGNÓSTICO: Lista todos los archivos de la BD (sin filtros)
        /// </summary>
        [HttpGet("diagnostico/listar")]
        public async Task<IActionResult> ListarTodosArchivos([FromQuery] int limit = 20)
        {
            var archivos = await _service.ListarTodosParaDiagnosticoAsync(limit);
            return Ok(new {
                Total = archivos.Count(),
                Archivos = archivos
            });
        }
    }
}