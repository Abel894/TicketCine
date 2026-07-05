namespace TicketCine.Domain.Entities
{
    public class AsientoReserva
    {
        public Guid Id { get; set; }
        public Guid ReservaId { get; set; }
        public Guid AsientoId { get; set; }

        // Relaciones
        public Reserva Reserva { get; set; } = null!;
        public Asiento Asiento { get; set; } = null!;
    }
}
