using TicketCine.Domain.Entities;

namespace TicketCine.Application.Interfaces
{
    public interface IReservaRepository
    {
        Task<Reserva> CrearAsync(Reserva reserva);
        Task<Reserva?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Reserva>> ObtenerActivasPorUsuarioAsync(Guid usuarioId);
        Task<IEnumerable<Reserva>> ObtenerExpiradasPorUsuarioAsync(Guid usuarioId);
        Task<IEnumerable<Reserva>> ObtenerPendientesVencidasAsync(DateTime fechaActual);
        Task<int> ContarAsientosReservadosPorFuncionYUsuarioAsync(Guid funcionId, Guid usuarioId);
        Task CrearAsientosReservaAsync(IEnumerable<AsientoReserva> asientosReserva);
        Task<IEnumerable<AsientoReserva>> ObtenerAsientosPorReservaAsync(Guid reservaId);
        Task ActualizarAsync(Reserva reserva);
    }
}
