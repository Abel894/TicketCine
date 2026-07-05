using Microsoft.EntityFrameworkCore;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;

namespace TicketCine.Infrastructure.Repositories
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly TicketCineDbContext _context;

        public ReservaRepository(TicketCineDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Reserva> CrearAsync(Reserva reserva)
        {
            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();
            return reserva;
        }

        public async Task<Reserva?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Reservas
                .Include(r => r.Asientos)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Reserva>> ObtenerActivasPorUsuarioAsync(Guid usuarioId)
        {
            return await _context.Reservas
                .Include(r => r.Asientos)
                .Where(r => r.UsuarioId == usuarioId &&
                            (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reserva>> ObtenerExpiradasPorUsuarioAsync(Guid usuarioId)
        {
            return await _context.Reservas
                .Where(r => r.UsuarioId == usuarioId && r.Estado == EstadoReserva.Expirada)
                .OrderByDescending(r => r.FechaExpiracion)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reserva>> ObtenerPendientesVencidasAsync(DateTime fechaActual)
        {
            return await _context.Reservas
                .Where(r => r.Estado == EstadoReserva.Pendiente && r.FechaExpiracion <= fechaActual)
                .ToListAsync();
        }

        public async Task<int> ContarAsientosReservadosPorFuncionYUsuarioAsync(Guid funcionId, Guid usuarioId)
        {
            return await _context.Reservas
                .Where(r => r.FuncionId == funcionId
                            && r.UsuarioId == usuarioId
                            && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
                .SelectMany(r => r.Asientos)
                .CountAsync();
        }

        public async Task CrearAsientosReservaAsync(IEnumerable<AsientoReserva> asientosReserva)
        {
            _context.AsientosReserva.AddRange(asientosReserva);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AsientoReserva>> ObtenerAsientosPorReservaAsync(Guid reservaId)
        {
            return await _context.AsientosReserva
                .Where(ar => ar.ReservaId == reservaId)
                .ToListAsync();
        }

        public async Task ActualizarAsync(Reserva reserva)
        {
            _context.Reservas.Update(reserva);
            await _context.SaveChangesAsync();
        }
    }
}
