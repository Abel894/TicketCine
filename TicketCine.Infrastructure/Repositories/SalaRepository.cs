using Microsoft.EntityFrameworkCore;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;

namespace TicketCine.Infrastructure.Repositories
{
    public class SalaRepository : ISalaRepository
    {
        private readonly TicketCineDbContext _context;

        public SalaRepository(TicketCineDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Sala?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Salas
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Sala>> ObtenerTodosAsync()
        {
            return await _context.Salas
                .ToListAsync();
        }

        public async Task<IEnumerable<Sala>> ObtenerActivosAsync()
        {
            return await _context.Salas
                .Where(s => s.Activo)
                .ToListAsync();
        }

        public async Task<Sala> CrearAsync(Sala sala)
        {
            _context.Salas.Add(sala);
            await _context.SaveChangesAsync();
            return sala;
        }

        public async Task ActualizarAsync(Sala sala)
        {
            _context.Salas.Update(sala);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Guid id)
        {
            var sala = await ObtenerPorIdAsync(id);
            if (sala != null)
            {
                _context.Salas.Remove(sala);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExisteAsync(Guid id)
        {
            return await _context.Salas.AnyAsync(s => s.Id == id);
        }
    }
}
