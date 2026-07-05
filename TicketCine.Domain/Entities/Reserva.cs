namespace TicketCine.Domain.Entities
{
    public class Reserva
    {
        public Guid Id { get; set; }
        public Guid UsuarioId { get; set; }
        public Guid FuncionId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaExpiracion { get; set; } = DateTime.Now.AddMinutes(30);
        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

        // Relaciones
        public Usuario Usuario { get; set; } = null!;
        public Funcion Funcion { get; set; } = null!;

        // Relación inversa
        public ICollection<AsientoReserva> Asientos { get; set; } = new List<AsientoReserva>();
    }
}
