namespace TicketCine.Web.Models
{
    public class ComprobanteViewModel
    {
        public Guid VentaId { get; set; }
        public string CodigoQr { get; set; } = string.Empty;
        public string CodigoQrBase64 { get; set; } = string.Empty;
        public string TituloPelicula { get; set; } = string.Empty;
        public string NombreSala { get; set; } = string.Empty;
        public DateTime FechaHoraFuncion { get; set; }
        public List<string> Asientos { get; set; } = new();
        public decimal MontoTotal { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
    }
}
