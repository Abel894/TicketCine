namespace TicketCine.Application.DTOs
{
    public class MapaSalaResponse
    {
        public Guid FuncionId { get; set; }
        public string TituloPelicula { get; set; } = string.Empty;
        public string NombreSala { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public decimal Precio { get; set; }
        public int Filas { get; set; }
        public int Columnas { get; set; }
        public List<AsientoMapaResponse> Asientos { get; set; } = new();
    }

    public class AsientoMapaResponse
    {
        public Guid Id { get; set; }
        public Guid FuncionId { get; set; }
        public int Fila { get; set; }
        public int Columna { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
