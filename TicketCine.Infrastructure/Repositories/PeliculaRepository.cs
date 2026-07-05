using Microsoft.EntityFrameworkCore;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;

namespace TicketCine.Infrastructure.Repositories
{
    public class PeliculaRepository : IPeliculaRepository
    {
        private readonly TicketCineDbContext _context;

        public PeliculaRepository(TicketCineDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Pelicula?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Peliculas
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Pelicula>> ObtenerTodosAsync()
        {
            return await _context.Peliculas
                .ToListAsync();
        }

        public async Task<IEnumerable<Pelicula>> ObtenerActivosAsync()
        {
            return await _context.Peliculas
                .Where(p => p.Activo)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pelicula>> ObtenerConFuncionesActivasAsync()
        {
            return await _context.Peliculas
                .Where(p => p.Activo && p.Funciones.Any(f => f.Activo))
                .Include(p => p.Funciones)
                .ToListAsync();
        }

        public async Task<Pelicula> CrearAsync(Pelicula pelicula)
        {
            _context.Peliculas.Add(pelicula);
            await _context.SaveChangesAsync();
            return pelicula;
        }

        public async Task ActualizarAsync(Pelicula pelicula)
        {
            _context.Peliculas.Update(pelicula);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Guid id)
        {
            var pelicula = await ObtenerPorIdAsync(id);
            if (pelicula != null)
            {
                _context.Peliculas.Remove(pelicula);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExisteAsync(Guid id)
        {
            return await _context.Peliculas.AnyAsync(p => p.Id == id);
        }
    }
}
