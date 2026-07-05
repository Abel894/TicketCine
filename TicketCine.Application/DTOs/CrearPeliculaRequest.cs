using Microsoft.AspNetCore.Http;

namespace TicketCine.Application.DTOs
{
    public class CrearPeliculaRequest
    {
        public string Titulo { get; set; } = string.Empty;
        public string Sinopsis { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; }
        public string Clasificacion { get; set; } = string.Empty;
        public IFormFile? Poster { get; set; }
    }
}
