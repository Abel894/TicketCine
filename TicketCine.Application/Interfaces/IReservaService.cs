using TicketCine.Application.DTOs;
using TicketCine.Domain.Entities;

namespace TicketCine.Application.Interfaces
{
    public interface IReservaService
    {
        Task<ReservaCreadaResponse> CrearReservaAsync(Guid usuarioId, CrearReservaRequest request);
        Task<Reserva?> ObtenerReservaPorIdAsync(Guid usuarioId, Guid reservaId);
        Task<IEnumerable<Reserva>> ObtenerReservasActivasAsync(Guid usuarioId);
        Task<IEnumerable<Reserva>> ObtenerReservasExpiradasAsync(Guid usuarioId);
        Task<int> ExpirarReservasVencidasAsync();
        Task CancelarAsync(Guid usuarioId, Guid reservaId);
    }
}
