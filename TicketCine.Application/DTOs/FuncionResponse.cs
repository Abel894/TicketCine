namespace TicketCine.Application.DTOs
{
    public class FuncionResponse
    {
        public Guid Id { get; set; }
        public Guid PeliculaId { get; set; }
        public string TituloPelicula { get; set; } = string.Empty;
        public Guid SalaId { get; set; }
        public string NombreSala { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; }
    }
}
