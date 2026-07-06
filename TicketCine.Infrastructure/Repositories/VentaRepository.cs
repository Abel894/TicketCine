using Microsoft.EntityFrameworkCore;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;

namespace TicketCine.Infrastructure.Repositories
{
    public class VentaRepository : IVentaRepository
    {
        private readonly TicketCineDbContext _context;

        public VentaRepository(TicketCineDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Venta> CrearAsync(Venta venta)
        {
            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();
            return venta;
        }

        public async Task<Venta?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Ventas
                .Include(v => v.Reserva)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Venta?> ObtenerPorReservaIdAsync(Guid reservaId)
        {
            return await _context.Ventas
                .FirstOrDefaultAsync(v => v.ReservaId == reservaId);
        }

        public async Task<Venta?> ObtenerConDetalleAsync(Guid id)
        {
            return await _context.Ventas
                .Include(v => v.Reserva)
                    .ThenInclude(r => r.Funcion)
                        .ThenInclude(f => f.Pelicula)
                .Include(v => v.Reserva)
                    .ThenInclude(r => r.Funcion)
                        .ThenInclude(f => f.Sala)
                .Include(v => v.Reserva)
                    .ThenInclude(r => r.Asientos)
                        .ThenInclude(ar => ar.Asiento)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<IEnumerable<Venta>> ObtenerParaReporteAsync(DateTime? fechaInicio, DateTime? fechaFin, Guid? peliculaId)
        {
            var query = _context.Ventas
                .AsNoTracking()
                .Include(v => v.Reserva)
                    .ThenInclude(r => r.Funcion)
                        .ThenInclude(f => f.Pelicula)
                .Include(v => v.Reserva)
                    .ThenInclude(r => r.Asientos)
                .AsQueryable();

            if (fechaInicio.HasValue)
            {
                query = query.Where(v => v.FechaVenta >= fechaInicio.Value.Date);
            }

            if (fechaFin.HasValue)
            {
                var fechaFinIncluyente = fechaFin.Value.Date.AddDays(1);
                query = query.Where(v => v.FechaVenta < fechaFinIncluyente);
            }

            if (peliculaId.HasValue && peliculaId.Value != Guid.Empty)
            {
                query = query.Where(v => v.Reserva.Funcion.PeliculaId == peliculaId.Value);
            }

            return await query
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();
        }
    }
}
