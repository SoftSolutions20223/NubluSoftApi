using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public interface ITransferenciasService
    {
        // Consultas
        Task<IEnumerable<Transferencia>> ObtenerConFiltrosAsync(long entidadId, FiltrosTransferenciasRequest filtros);
        Task<Transferencia?> ObtenerPorIdAsync(long transferenciaId);
        Task<IEnumerable<TransferenciaDetalle>> ObtenerDetalleAsync(long transferenciaId);
        Task<IEnumerable<ExpedienteCandidato>> ObtenerExpedientesCandidatosAsync(long entidadId, FiltrosExpedientesCandidatosRequest filtros);

        // CRUD Transferencia
        Task<ResultadoTransferencia> CrearAsync(long entidadId, long usuarioId, CrearTransferenciaRequest request);
        Task<(bool Success, string? Error)> EliminarAsync(long transferenciaId);

        // Gestión de expedientes en transferencia
        Task<ResultadoTransferencia> AgregarExpedienteAsync(long transferenciaId, long usuarioId, AgregarExpedienteTransferenciaRequest request);
        Task<(bool Success, string? Error)> RemoverExpedienteAsync(long transferenciaId, long expedienteId);

        // Flujo de transferencia
        Task<ResultadoTransferencia> EnviarAsync(long transferenciaId, long usuarioId, EnviarTransferenciaRequest request);
        Task<ResultadoTransferencia> RecibirAsync(long transferenciaId, long usuarioId, RecibirTransferenciaRequest request);
        Task<ResultadoTransferencia> RechazarAsync(long transferenciaId, long usuarioId, RechazarTransferenciaRequest request);
        Task<ResultadoTransferencia> EjecutarAsync(long transferenciaId, long usuarioId);
    }
}