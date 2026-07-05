namespace TicketCine.Domain.Entities
{
    public class Venta
    {
        public Guid Id { get; set; }
        public Guid ReservaId { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public DateTime FechaVenta { get; set; } = DateTime.Now;
        public string CodigoQr { get; set; } = string.Empty;

        // Relación
        public Reserva Reserva { get; set; } = null!;
    }
}
