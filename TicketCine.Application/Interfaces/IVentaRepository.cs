using TicketCine.Domain.Entities;

namespace TicketCine.Application.Interfaces
{
    public interface IVentaRepository
    {
        Task<Venta> CrearAsync(Venta venta);
        Task<Venta?> ObtenerPorIdAsync(Guid id);
        Task<Venta?> ObtenerPorReservaIdAsync(Guid reservaId);
        Task<Venta?> ObtenerConDetalleAsync(Guid id);
        Task<IEnumerable<Venta>> ObtenerParaReporteAsync(DateTime? fechaInicio, DateTime? fechaFin, Guid? peliculaId);
    }
}
