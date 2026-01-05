using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface IDatosEstaticosService
    {
        // Catálogos de Carpetas/Archivos
        Task<IEnumerable<Catalogo>> ObtenerTiposCarpetasAsync();
        Task<IEnumerable<Catalogo>> ObtenerEstadosCarpetasAsync();
        Task<IEnumerable<Catalogo>> ObtenerTiposArchivosAsync();
        Task<IEnumerable<Catalogo>> ObtenerTiposDocumentalesAsync();
        Task<IEnumerable<Catalogo>> ObtenerNivelesVisualizacionAsync();
        Task<IEnumerable<CatalogoString>> ObtenerSoportesDocumentoAsync();
        Task<IEnumerable<CatalogoString>> ObtenerFrecuenciasConsultaAsync();
        Task<IEnumerable<CatalogoString>> ObtenerOrigenesDocumentosAsync();
        Task<IEnumerable<CatalogoString>> ObtenerDisposicionesFinalesAsync();

        // Catálogos de Ventanilla Única
        Task<IEnumerable<TipoSolicitud>> ObtenerTiposSolicitudAsync();
        Task<IEnumerable<EstadoRadicado>> ObtenerEstadosRadicadoAsync();
        Task<IEnumerable<CatalogoString>> ObtenerTiposComunicacionAsync();
        Task<IEnumerable<CatalogoString>> ObtenerMediosRecepcionAsync();
        Task<IEnumerable<CatalogoString>> ObtenerPrioridadesRadicadoAsync();

        // Catálogos Generales
        Task<IEnumerable<Catalogo>> ObtenerRolesAsync();
        Task<IEnumerable<CatalogoString>> ObtenerTiposDocumentoIdentidadAsync();
        Task<IEnumerable<CatalogoString>> ObtenerTiposFirmaAsync();
    }
}