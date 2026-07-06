using TicketCine.Domain.Entities;

namespace TicketCine.Web.Models
{
    public class MisReservasViewModel
    {
        public List<ReservaItemViewModel> Reservas { get; set; } = new();
        public string? FiltroEstado { get; set; }
    }

    public class ReservaItemViewModel
    {
        public Guid ReservaId { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public string TituloPelicula { get; set; } = string.Empty;
        public string NombreSala { get; set; } = string.Empty;
        public DateTime FechaHoraFuncion { get; set; }
        public List<string> Asientos { get; set; } = new();
        public EstadoReserva Estado { get; set; }
        public bool PuedeCancelar { get; set; }
        public bool PuedeContinuarPago { get; set; }
        public Guid? VentaId { get; set; }
    }
}
