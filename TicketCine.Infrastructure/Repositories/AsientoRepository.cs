using Microsoft.EntityFrameworkCore;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;

namespace TicketCine.Infrastructure.Repositories
{
    public class AsientoRepository : IAsientoRepository
    {
        private readonly TicketCineDbContext _context;

        public AsientoRepository(TicketCineDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Asiento?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Asientos
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Asiento>> ObtenerTodosAsync()
        {
            return await _context.Asientos
                .ToListAsync();
        }

        public async Task<IEnumerable<Asiento>> ObtenerPorFuncionAsync(Guid funcionId)
        {
            return await _context.Asientos
                .Where(a => a.FuncionId == funcionId)
                .OrderBy(a => a.Fila)
                .ThenBy(a => a.Columna)
                .ToListAsync();
        }

        public async Task<Asiento> CrearAsync(Asiento asiento)
        {
            _context.Asientos.Add(asiento);
            await _context.SaveChangesAsync();
            return asiento;
        }

        public async Task CrearVariosAsync(IEnumerable<Asiento> asientos)
        {
            _context.Asientos.AddRange(asientos);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Asiento asiento)
        {
            _context.Asientos.Update(asiento);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Guid id)
        {
            var asiento = await ObtenerPorIdAsync(id);
            if (asiento != null)
            {
                _context.Asientos.Remove(asiento);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExisteAsync(Guid id)
        {
            return await _context.Asientos.AnyAsync(a => a.Id == id);
        }

        public async Task<int> ContarLibresPorFuncionAsync(Guid funcionId)
        {
            return await _context.Asientos
                .CountAsync(a => a.FuncionId == funcionId && a.Estado == EstadoAsiento.Libre);
        }
    }
}
