using TicketCine.Domain.Entities;

namespace TicketCine.Application.DTOs
{
    public class ReservaCreadaResponse
    {
        public Guid ReservaId { get; set; }
        public Guid FuncionId { get; set; }
        public EstadoReserva Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public int TiempoRestanteSegundos { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public List<Guid> AsientoIds { get; set; } = new();
    }
}
