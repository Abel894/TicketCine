using TicketCine.Domain.Entities;

namespace TicketCine.Application.Interfaces
{
    public interface IFuncionRepository
    {
        Task<Funcion?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Funcion>> ObtenerTodosAsync();
        Task<IEnumerable<Funcion>> ObtenerActivosAsync();
        Task<IEnumerable<Funcion>> ObtenerPorPeliculaAsync(Guid peliculaId);
        Task<IEnumerable<Funcion>> ObtenerPorPeliculaActivasAsync(Guid peliculaId);
        Task<IEnumerable<Funcion>> ObtenerPorSalaAsync(Guid salaId);
        Task<Funcion> CrearAsync(Funcion funcion);
        Task ActualizarAsync(Funcion funcion);
        Task EliminarAsync(Guid id);
        Task<bool> ExisteAsync(Guid id);
        Task<bool> ExisteConflictoHorarioAsync(Guid salaId, DateTime fechaHora, int duracionMinutos, Guid? funcionIdExcluir = null);
    }
}
