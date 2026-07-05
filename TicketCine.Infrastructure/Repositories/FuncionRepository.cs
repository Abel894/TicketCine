using Microsoft.EntityFrameworkCore;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;

namespace TicketCine.Infrastructure.Repositories
{
    public class FuncionRepository : IFuncionRepository
    {
        private readonly TicketCineDbContext _context;

        public FuncionRepository(TicketCineDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Funcion?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Funciones
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<Funcion>> ObtenerTodosAsync()
        {
            return await _context.Funciones
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                .ToListAsync();
        }

        public async Task<IEnumerable<Funcion>> ObtenerActivosAsync()
        {
            return await _context.Funciones
                .Where(f => f.Activo)
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                .ToListAsync();
        }

        public async Task<IEnumerable<Funcion>> ObtenerPorPeliculaAsync(Guid peliculaId)
        {
            return await _context.Funciones
                .Where(f => f.PeliculaId == peliculaId)
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                .ToListAsync();
        }

        public async Task<IEnumerable<Funcion>> ObtenerPorPeliculaActivasAsync(Guid peliculaId)
        {
            return await _context.Funciones
                .Where(f => f.PeliculaId == peliculaId && f.Activo)
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                .OrderBy(f => f.FechaHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<Funcion>> ObtenerPorSalaAsync(Guid salaId)
        {
            return await _context.Funciones
                .Where(f => f.SalaId == salaId)
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                .ToListAsync();
        }

        public async Task<Funcion> CrearAsync(Funcion funcion)
        {
            _context.Funciones.Add(funcion);
            await _context.SaveChangesAsync();
            return funcion;
        }

        public async Task ActualizarAsync(Funcion funcion)
        {
            _context.Funciones.Update(funcion);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Guid id)
        {
            var funcion = await ObtenerPorIdAsync(id);
            if (funcion != null)
            {
                _context.Funciones.Remove(funcion);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExisteAsync(Guid id)
        {
            return await _context.Funciones.AnyAsync(f => f.Id == id);
        }

        /// <summary>
        /// Valida si existe conflicto de horario en una sala (RN-07).
        /// Un conflicto existe si hay una función activa que se solapa en tiempo.
        /// Cada función existente usa su PROPIA duración de película, no la de la nueva.
        /// </summary>
        public async Task<bool> ExisteConflictoHorarioAsync(Guid salaId, DateTime fechaHora, int duracionMinutos, Guid? funcionIdExcluir = null)
        {
            var fechaHoraFin = fechaHora.AddMinutes(duracionMinutos);

            // Obtener todas las funciones activas en la sala (incluyendo sus películas para acceder a la duración)
            var funcionesExistentes = await _context.Funciones
                .Where(f => f.SalaId == salaId && f.Activo)
                .Where(f => funcionIdExcluir == null || f.Id != funcionIdExcluir)
                .Include(f => f.Pelicula)
                .ToListAsync();

            // Verificar si alguna función existente se solapa con la nueva
            var conflicto = funcionesExistentes.Any(f =>
            {
                // Calcular cuándo termina ESTA función existente usando SU duración de película
                var fechaHoraFinExistente = f.FechaHora.AddMinutes(f.Pelicula.DuracionMinutos);

                // Solapamiento: la nueva función comienza antes de que termine la existente
                // y termina después de que comienza la existente
                return fechaHora < fechaHoraFinExistente && fechaHoraFin > f.FechaHora;
            });

            return conflicto;
        }
    }
}
