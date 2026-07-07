using TicketCine.Domain.Entities;

namespace TicketCine.Application.Interfaces
{
    public interface IPeliculaRepository
    {
        Task<Pelicula?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Pelicula>> ObtenerTodosAsync();
        Task<IEnumerable<Pelicula>> ObtenerActivosAsync();
        Task<IEnumerable<Pelicula>> ObtenerConFuncionesActivasAsync();
        Task<IEnumerable<Pelicula>> ObtenerDestacadasConFuncionesProximasAsync(int cantidadMaxima);
        Task<Pelicula> CrearAsync(Pelicula pelicula);
        Task ActualizarAsync(Pelicula pelicula);
        Task EliminarAsync(Guid id);
        Task<bool> ExisteAsync(Guid id);
    }
}
