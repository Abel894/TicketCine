using TicketCine.Domain.Entities;

namespace TicketCine.Application.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObtenerPorCorreoAsync(string correo);
        Task<Usuario?> ObtenerPorIdAsync(Guid id);
        Task<bool> CorreoExisteAsync(string correo);
        Task<Usuario> CrearAsync(Usuario usuario);
        Task ActualizarAsync(Usuario usuario);
        Task<IEnumerable<Usuario>> ObtenerTodosAsync();
    }
}
