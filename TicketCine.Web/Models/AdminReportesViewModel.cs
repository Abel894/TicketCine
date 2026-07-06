using TicketCine.Application.DTOs;

namespace TicketCine.Web.Models
{
    public class AdminReportesViewModel
    {
        public ReporteVentasRequest Filtros { get; set; } = new();
        public List<PeliculaResponse> Peliculas { get; set; } = new();
        public ReporteVentasResponse? Resultado { get; set; }
        public bool ReporteGenerado { get; set; }
    }
}
