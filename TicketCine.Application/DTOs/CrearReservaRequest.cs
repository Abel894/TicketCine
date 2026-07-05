namespace TicketCine.Application.DTOs
{
    public class CrearReservaRequest
    {
        public Guid FuncionId { get; set; }
        public List<Guid> AsientoIds { get; set; } = new();
    }
}
