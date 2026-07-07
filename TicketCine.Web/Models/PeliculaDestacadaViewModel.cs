namespace TicketCine.Web.Models
{
    public class PeliculaDestacadaViewModel
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? RutaPoster { get; set; }
    }
}
