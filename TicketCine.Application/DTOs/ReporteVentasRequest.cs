namespace TicketCine.Application.DTOs
{
    public class ReporteVentasRequest
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public Guid? PeliculaId { get; set; }
    }
}
