namespace TicketCine.Application.DTOs
{
    public class ConfirmarCompraRequest
    {
        public Guid ReservaId { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
    }
}
