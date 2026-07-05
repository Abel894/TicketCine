namespace TicketCine.Web.Models
{
    public class ReservaResumenViewModel
    {
        public Guid ReservaId { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public string TituloPelicula { get; set; } = string.Empty;
        public string NombreSala { get; set; } = string.Empty;
        public DateTime FechaHoraFuncion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public int TiempoRestanteSegundos { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int CantidadAsientos { get; set; }
        public decimal Total { get; set; }
        public List<string> Asientos { get; set; } = new();
    }
}
