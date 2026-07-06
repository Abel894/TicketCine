using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;

namespace TicketCine.Application.Services
{
    public class ReporteVentasService : IReporteVentasService
    {
        private readonly IVentaRepository _ventaRepository;

        public ReporteVentasService(IVentaRepository ventaRepository)
        {
            _ventaRepository = ventaRepository ?? throw new ArgumentNullException(nameof(ventaRepository));
        }

        public async Task<ReporteVentasResponse> GenerarAsync(ReporteVentasRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var ventas = (await _ventaRepository.ObtenerParaReporteAsync(request.FechaInicio, request.FechaFin, request.PeliculaId)).ToList();

            var totalEntradas = ventas.Sum(v => v.Reserva.Asientos.Count);
            var ingresoTotal = ventas.Sum(v => v.MontoTotal);

            var detallePorPelicula = ventas
                .GroupBy(v => v.Reserva.Funcion.Pelicula.Titulo)
                .Select(g => new ReporteVentasDetallePeliculaResponse
                {
                    Titulo = g.Key,
                    EntradasVendidas = g.Sum(v => v.Reserva.Asientos.Count),
                    Ingreso = g.Sum(v => v.MontoTotal)
                })
                .OrderByDescending(x => x.Ingreso)
                .ThenBy(x => x.Titulo)
                .ToList();

            return new ReporteVentasResponse
            {
                TotalEntradas = totalEntradas,
                IngresoTotal = ingresoTotal,
                DetallePorPelicula = detallePorPelicula
            };
        }
    }
}
