using TicketCine.Domain.Entities;

namespace TicketCine.Application.Interfaces
{
    public interface IAsientoRepository
    {
        Task<Asiento?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Asiento>> ObtenerTodosAsync();
        Task<IEnumerable<Asiento>> ObtenerPorFuncionAsync(Guid funcionId);
        Task<Asiento> CrearAsync(Asiento asiento);
        Task CrearVariosAsync(IEnumerable<Asiento> asientos);
        Task ActualizarAsync(Asiento asiento);
        Task EliminarAsync(Guid id);
        Task<bool> ExisteAsync(Guid id);
        Task<int> ContarLibresPorFuncionAsync(Guid funcionId);
    }
}
