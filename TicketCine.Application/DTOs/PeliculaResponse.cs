namespace TicketCine.Application.DTOs
{
    public class PeliculaResponse
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Sinopsis { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; }
        public string Clasificacion { get; set; } = string.Empty;
        public string? RutaPoster { get; set; }
        public bool Activo { get; set; }
    }
}
