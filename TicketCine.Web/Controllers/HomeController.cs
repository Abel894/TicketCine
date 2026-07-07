using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TicketCine.Application.Interfaces;
using TicketCine.Web.Models;

namespace TicketCine.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPeliculaRepository _peliculaRepository;

        public HomeController(IPeliculaRepository peliculaRepository)
        {
            _peliculaRepository = peliculaRepository ?? throw new ArgumentNullException(nameof(peliculaRepository));
        }

        public async Task<IActionResult> Index()
        {
            var peliculasDestacadas = await _peliculaRepository.ObtenerDestacadasConFuncionesProximasAsync(6);

            var model = peliculasDestacadas
                .Select(p => new PeliculaDestacadaViewModel
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    RutaPoster = p.RutaPoster
                })
                .ToList();

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
