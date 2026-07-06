using TicketCine.Application.DTOs;

namespace TicketCine.Application.Interfaces
{
    public interface IReporteVentasService
    {
        Task<ReporteVentasResponse> GenerarAsync(ReporteVentasRequest request);
    }
}
