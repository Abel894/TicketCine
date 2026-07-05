using Microsoft.AspNetCore.Http;

namespace TicketCine.Application.DTOs
{
    public class EditarPeliculaRequest
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Sinopsis { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; }
        public string Clasificacion { get; set; } = string.Empty;
        public IFormFile? Poster { get; set; }
        public string? RutaPosterActual { get; set; }
        public bool Activo { get; set; } = true;
    }
}
