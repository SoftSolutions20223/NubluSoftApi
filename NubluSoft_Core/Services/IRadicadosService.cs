using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface IRadicadosService
    {
        // Consultas
        Task<IEnumerable<Radicado>> ObtenerConFiltrosAsync(long entidadId, FiltrosRadicadosRequest filtros);
        Task<Radicado?> ObtenerPorIdAsync(long radicadoId);
        Task<Radicado?> ObtenerPorNumeroAsync(long entidadId, string numeroRadicado);
        Task<IEnumerable<RadicadoTrazabilidad>> ObtenerTrazabilidadAsync(long radicadoId);
        Task<IEnumerable<RadicadoAnexo>> ObtenerAnexosAsync(long radicadoId);
        Task<IEnumerable<Radicado>> ObtenerPorVencerAsync(long entidadId, int diasAnticipacion = 3);
        Task<IEnumerable<Radicado>> ObtenerVencidosAsync(long entidadId);

        // Radicación (usan funciones PostgreSQL)
        Task<ResultadoRadicado> RadicarEntradaAsync(long entidadId, long usuarioId, RadicarEntradaRequest request);
        Task<ResultadoRadicado> RadicarSalidaAsync(long entidadId, long usuarioId, RadicarSalidaRequest request);
        Task<ResultadoRadicado> RadicarInternaAsync(long entidadId, long usuarioId, RadicarInternaRequest request);

        // Gestión de radicados (usan funciones PostgreSQL)
        Task<ResultadoRadicado> AsignarAsync(long radicadoId, long usuarioId, AsignarRadicadoRequest request);
        Task<ResultadoRadicado> TrasladarAsync(long radicadoId, long usuarioId, TrasladarRadicadoRequest request);
        Task<ResultadoRadicado> ArchivarAsync(long radicadoId, long usuarioId, ArchivarRadicadoRequest request);
        Task<ResultadoRadicado> SolicitarProrrogaAsync(long radicadoId, long usuarioId, SolicitarProrrogaRequest request);
        Task<ResultadoRadicado> AnularAsync(long radicadoId, long usuarioId, AnularRadicadoRequest request);

        // Anexos
        Task<(bool Success, string? Error)> AgregarAnexoAsync(long radicadoId, long usuarioId, AgregarAnexoRequest request);
        Task<(bool Success, string? Error)> EliminarAnexoAsync(long anexoId, long usuarioId);
    }
}