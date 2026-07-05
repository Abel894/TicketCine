namespace TicketCine.Application.DTOs
{
    public class VentaConfirmadaResponse
    {
        public Guid VentaId { get; set; }
        public Guid ReservaId { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public DateTime FechaVenta { get; set; }
        public string CodigoQr { get; set; } = string.Empty;
        public string CodigoQrBase64 { get; set; } = string.Empty;
    }
}
