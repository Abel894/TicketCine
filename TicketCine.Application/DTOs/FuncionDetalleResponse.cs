namespace TicketCine.Application.DTOs
{
    public class FuncionDetalleResponse
    {
        public Guid Id { get; set; }
        public Guid PeliculaId { get; set; }
        public string TituloPelicula { get; set; } = string.Empty;
        public Guid SalaId { get; set; }
        public string NombreSala { get; set; } = string.Empty;
        public int FilasSala { get; set; }
        public int ColumnaSala { get; set; }
        public DateTime FechaHora { get; set; }
        public decimal Precio { get; set; }
        public int AsientosDisponibles { get; set; }
        public int TotalAsientos { get; set; }
    }
}
