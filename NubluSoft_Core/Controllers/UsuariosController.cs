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
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuariosService _service;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(IUsuariosService service, ILogger<UsuariosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los usuarios de la entidad
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var usuarios = await _service.ObtenerPorEntidadAsync(entidadId);
            return Ok(usuarios);
        }

        /// <summary>
        /// Obtiene un usuario por su ID con sus roles
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var usuario = await _service.ObtenerPorIdAsync(entidadId, id);
            if (usuario == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            return Ok(usuario);
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearUsuarioRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (usuario, error) = await _service.CrearAsync(entidadId, request);

            if (usuario == null)
                return BadRequest(new { Message = error });

            return CreatedAtAction(nameof(GetById), new { id = usuario.Cod }, usuario);
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ActualizarUsuarioRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.ActualizarAsync(entidadId, id, request);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        /// <summary>
        /// Elimina (soft delete) un usuario
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.EliminarAsync(entidadId, id);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }

        /// <summary>
        /// Permite al usuario cambiar su propia contraseña
        /// </summary>
        [HttpPost("cambiar-contrasena")]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            var usuarioId = User.GetUserId();

            if (entidadId == 0 || usuarioId == 0)
                return Unauthorized(new { Message = "No se pudo determinar el usuario" });

            var (success, error) = await _service.CambiarContrasenaAsync(entidadId, usuarioId, request);

            if (!success)
                return BadRequest(new { Message = error });

            return Ok(new { Message = "Contraseña actualizada correctamente" });
        }

        /// <summary>
        /// Resetea la contraseña de un usuario (admin)
        /// </summary>
        [HttpPost("{id}/resetear-contrasena")]
        public async Task<IActionResult> ResetearContrasena(long id, [FromBody] ResetearContrasenaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.ResetearContrasenaAsync(entidadId, id, request);

            if (!success)
                return BadRequest(new { Message = error });

            return Ok(new { Message = "Contraseña reseteada correctamente" });
        }

        /// <summary>
        /// Obtiene los roles de un usuario
        /// </summary>
        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetRoles(long id)
        {
            var roles = await _service.ObtenerRolesUsuarioAsync(id);
            return Ok(roles);
        }

        /// <summary>
        /// Asigna un rol a un usuario en una oficina
        /// </summary>
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AsignarRol(long id, [FromBody] AsignarRolRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.AsignarRolAsync(entidadId, id, request);

            if (!success)
                return BadRequest(new { Message = error });

            return Ok(new { Message = "Rol asignado correctamente" });
        }

        /// <summary>
        /// Revoca un rol de un usuario
        /// </summary>
        [HttpDelete("{id}/roles/{oficina}/{rol}")]
        public async Task<IActionResult> RevocarRol(long id, long oficina, long rol)
        {
            var entidadId = User.GetEntidadId();
            if (entidadId == 0)
                return Unauthorized(new { Message = "No se pudo determinar la entidad del usuario" });

            var (success, error) = await _service.RevocarRolAsync(entidadId, id, oficina, rol);

            if (!success)
                return BadRequest(new { Message = error });

            return NoContent();
        }
    }
}