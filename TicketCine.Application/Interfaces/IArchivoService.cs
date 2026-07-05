using Microsoft.AspNetCore.Http;

namespace TicketCine.Application.Interfaces
{
    public interface IArchivoService
    {
        Task<string> GuardarPosterAsync(IFormFile archivo);
        void EliminarPoster(string rutaRelativa);
    }
}
