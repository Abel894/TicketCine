namespace TicketCine.Application.DTOs
{
    public class EditarFuncionRequest
    {
        public Guid Id { get; set; }
        public Guid PeliculaId { get; set; }
        public Guid SalaId { get; set; }
        public DateTime FechaHora { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; } = true;
    }
}
