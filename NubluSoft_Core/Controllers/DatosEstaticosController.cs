using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NubluSoft_Core.Services;

namespace NubluSoft_Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DatosEstaticosController : ControllerBase
    {
        private readonly IDatosEstaticosService _service;

        public DatosEstaticosController(IDatosEstaticosService service)
        {
            _service = service;
        }

        // ==================== CARPETAS/ARCHIVOS ====================

        /// <summary>
        /// Obtiene los tipos de carpetas (Serie, Subserie, Expediente, Genérica)
        /// </summary>
        [HttpGet("tipos-carpetas")]
        public async Task<IActionResult> GetTiposCarpetas()
        {
            var result = await _service.ObtenerTiposCarpetasAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los estados de carpetas (Abierto, Cerrado, etc.)
        /// </summary>
        [HttpGet("estados-carpetas")]
        public async Task<IActionResult> GetEstadosCarpetas()
        {
            var result = await _service.ObtenerEstadosCarpetasAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los tipos de archivos
        /// </summary>
        [HttpGet("tipos-archivos")]
        public async Task<IActionResult> GetTiposArchivos()
        {
            var result = await _service.ObtenerTiposArchivosAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los tipos documentales según AGN
        /// </summary>
        [HttpGet("tipos-documentales")]
        public async Task<IActionResult> GetTiposDocumentales()
        {
            var result = await _service.ObtenerTiposDocumentalesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los niveles de visualización
        /// </summary>
        [HttpGet("niveles-visualizacion")]
        public async Task<IActionResult> GetNivelesVisualizacion()
        {
            var result = await _service.ObtenerNivelesVisualizacionAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los soportes de documento (Físico, Electrónico, Híbrido)
        /// </summary>
        [HttpGet("soportes-documento")]
        public async Task<IActionResult> GetSoportesDocumento()
        {
            var result = await _service.ObtenerSoportesDocumentoAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene las frecuencias de consulta
        /// </summary>
        [HttpGet("frecuencias-consulta")]
        public async Task<IActionResult> GetFrecuenciasConsulta()
        {
            var result = await _service.ObtenerFrecuenciasConsultaAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los orígenes de documentos
        /// </summary>
        [HttpGet("origenes-documentos")]
        public async Task<IActionResult> GetOrigenesDocumentos()
        {
            var result = await _service.ObtenerOrigenesDocumentosAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene las disposiciones finales TRD
        /// </summary>
        [HttpGet("disposiciones-finales")]
        public async Task<IActionResult> GetDisposicionesFinales()
        {
            var result = await _service.ObtenerDisposicionesFinalesAsync();
            return Ok(result);
        }

        // ==================== VENTANILLA ÚNICA ====================

        /// <summary>
        /// Obtiene los tipos de solicitud con términos legales (Ley 1755/2015)
        /// </summary>
        [HttpGet("tipos-solicitud")]
        public async Task<IActionResult> GetTiposSolicitud()
        {
            var result = await _service.ObtenerTiposSolicitudAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los estados de radicado
        /// </summary>
        [HttpGet("estados-radicado")]
        public async Task<IActionResult> GetEstadosRadicado()
        {
            var result = await _service.ObtenerEstadosRadicadoAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los tipos de comunicación (Entrada, Salida, Interna)
        /// </summary>
        [HttpGet("tipos-comunicacion")]
        public async Task<IActionResult> GetTiposComunicacion()
        {
            var result = await _service.ObtenerTiposComunicacionAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los medios de recepción
        /// </summary>
        [HttpGet("medios-recepcion")]
        public async Task<IActionResult> GetMediosRecepcion()
        {
            var result = await _service.ObtenerMediosRecepcionAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene las prioridades de radicado
        /// </summary>
        [HttpGet("prioridades-radicado")]
        public async Task<IActionResult> GetPrioridadesRadicado()
        {
            var result = await _service.ObtenerPrioridadesRadicadoAsync();
            return Ok(result);
        }

        // ==================== GENERALES ====================

        /// <summary>
        /// Obtiene los roles del sistema
        /// </summary>
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _service.ObtenerRolesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los tipos de documento de identidad
        /// </summary>
        [HttpGet("tipos-documento-identidad")]
        public async Task<IActionResult> GetTiposDocumentoIdentidad()
        {
            var result = await _service.ObtenerTiposDocumentoIdentidadAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los tipos de firma electrónica
        /// </summary>
        [HttpGet("tipos-firma")]
        public async Task<IActionResult> GetTiposFirma()
        {
            var result = await _service.ObtenerTiposFirmaAsync();
            return Ok(result);
        }

        // ==================== ENTIDADES ====================

        /// <summary>
        /// Obtiene los sectores económicos (para clasificación de entidades)
        /// </summary>
        [HttpGet("sectores")]
        public async Task<IActionResult> GetSectores()
        {
            var result = await _service.ObtenerSectoresAsync();
            return Ok(result);
        }

        /// <summary>
        /// Obtiene los tipos de entidad (Pública, Privada, Mixta, etc.)
        /// </summary>
        [HttpGet("tipos-entidad")]
        public async Task<IActionResult> GetTiposEntidad()
        {
            var result = await _service.ObtenerTiposEntidadAsync();
            return Ok(result);
        }

        // ==================== ENDPOINT COMBINADO ====================

        /// <summary>
        /// Obtiene todos los catálogos en una sola llamada (útil para cargar al inicio)
        /// </summary>
        [HttpGet("todos")]
        public async Task<IActionResult> GetTodos()
        {
            var result = new
            {
                // Carpetas/Archivos
                TiposCarpetas = await _service.ObtenerTiposCarpetasAsync(),
                EstadosCarpetas = await _service.ObtenerEstadosCarpetasAsync(),
                TiposArchivos = await _service.ObtenerTiposArchivosAsync(),
                TiposDocumentales = await _service.ObtenerTiposDocumentalesAsync(),
                NivelesVisualizacion = await _service.ObtenerNivelesVisualizacionAsync(),
                SoportesDocumento = await _service.ObtenerSoportesDocumentoAsync(),
                FrecuenciasConsulta = await _service.ObtenerFrecuenciasConsultaAsync(),
                OrigenesDocumentos = await _service.ObtenerOrigenesDocumentosAsync(),
                DisposicionesFinales = await _service.ObtenerDisposicionesFinalesAsync(),

                // Ventanilla Única
                TiposSolicitud = await _service.ObtenerTiposSolicitudAsync(),
                EstadosRadicado = await _service.ObtenerEstadosRadicadoAsync(),
                TiposComunicacion = await _service.ObtenerTiposComunicacionAsync(),
                MediosRecepcion = await _service.ObtenerMediosRecepcionAsync(),
                PrioridadesRadicado = await _service.ObtenerPrioridadesRadicadoAsync(),

                // Generales
                Roles = await _service.ObtenerRolesAsync(),
                TiposDocumentoIdentidad = await _service.ObtenerTiposDocumentoIdentidadAsync(),
                TiposFirma = await _service.ObtenerTiposFirmaAsync(),

                // Entidades 
                Sectores = await _service.ObtenerSectoresAsync(),
                TiposEntidad = await _service.ObtenerTiposEntidadAsync()
            };

            return Ok(result);
        }
    }
}