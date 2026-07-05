using TicketCine.Application.DTOs;

namespace TicketCine.Application.Interfaces
{
    public interface IVentaService
    {
        Task<VentaConfirmadaResponse> ConfirmarCompraAsync(Guid usuarioId, ConfirmarCompraRequest request);
        Task<VentaConfirmadaResponse?> ObtenerComprobanteAsync(Guid usuarioId, Guid ventaId);
    }
}
