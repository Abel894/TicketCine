using Microsoft.EntityFrameworkCore;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;

namespace TicketCine.Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly TicketCineDbContext _context;

        public UsuarioRepository(TicketCineDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Obtiene un usuario por correo, incluyendo su rol asociado.
        /// </summary>
        public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo == correo.ToLowerInvariant());
        }

        /// <summary>
        /// Obtiene un usuario por ID, incluyendo su rol asociado.
        /// </summary>
        public async Task<Usuario?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Verifica si un correo ya existe en la base de datos.
        /// </summary>
        public async Task<bool> CorreoExisteAsync(string correo)
        {
            return await _context.Usuarios
                .AnyAsync(u => u.Correo == correo.ToLowerInvariant());
        }

        /// <summary>
        /// Crea un nuevo usuario en la base de datos.
        /// </summary>
        public async Task<Usuario> CrearAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        /// <summary>
        /// Actualiza un usuario existente en la base de datos.
        /// </summary>
        public async Task ActualizarAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Obtiene todos los usuarios de la base de datos.
        /// </summary>
        public async Task<IEnumerable<Usuario>> ObtenerTodosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .ToListAsync();
        }
    }
}
