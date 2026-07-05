namespace TicketCine.Application.DTOs
{
    public class CrearFuncionRequest
    {
        public Guid PeliculaId { get; set; }
        public Guid SalaId { get; set; }
        public DateTime FechaHora { get; set; }
        public decimal Precio { get; set; }
    }
}
