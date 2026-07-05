using TicketCine.Domain.Entities;

namespace TicketCine.Application.Interfaces
{
    public interface ISalaRepository
    {
        Task<Sala?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Sala>> ObtenerTodosAsync();
        Task<IEnumerable<Sala>> ObtenerActivosAsync();
        Task<Sala> CrearAsync(Sala sala);
        Task ActualizarAsync(Sala sala);
        Task EliminarAsync(Guid id);
        Task<bool> ExisteAsync(Guid id);
    }
}
