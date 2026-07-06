namespace TicketCine.Application.DTOs
{
    public class ReporteVentasResponse
    {
        public int TotalEntradas { get; set; }
        public decimal IngresoTotal { get; set; }
        public List<ReporteVentasDetallePeliculaResponse> DetallePorPelicula { get; set; } = new();
    }

    public class ReporteVentasDetallePeliculaResponse
    {
        public string Titulo { get; set; } = string.Empty;
        public int EntradasVendidas { get; set; }
        public decimal Ingreso { get; set; }
    }
}
