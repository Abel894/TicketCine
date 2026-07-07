using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Web.Models;

namespace TicketCine.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminReportesController : Controller
    {
        private readonly IPeliculaRepository _peliculaRepository;
        private readonly IReporteVentasService _reporteVentasService;
        private readonly ILogger<AdminReportesController> _logger;

        public AdminReportesController(
            IPeliculaRepository peliculaRepository,
            IReporteVentasService reporteVentasService,
            ILogger<AdminReportesController> logger)
        {
            _peliculaRepository = peliculaRepository ?? throw new ArgumentNullException(nameof(peliculaRepository));
            _reporteVentasService = reporteVentasService ?? throw new ArgumentNullException(nameof(reporteVentasService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AdminReportesViewModel
            {
                Peliculas = await ObtenerPeliculasParaFiltroAsync(),
                Filtros = new ReporteVentasRequest(),
                ReporteGenerado = false
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AdminReportesViewModel model)
        {
            if (model == null)
            {
                model = new AdminReportesViewModel();
            }

            model.Peliculas = await ObtenerPeliculasParaFiltroAsync();
            model.ReporteGenerado = true;

            if (model.Filtros?.FechaInicio.HasValue == true
                && model.Filtros.FechaFin.HasValue
                && model.Filtros.FechaInicio.Value.Date > model.Filtros.FechaFin.Value.Date)
            {
                ModelState.AddModelError(string.Empty, "La fecha de inicio no puede ser mayor que la fecha fin.");
                return View(model);
            }

            try
            {
                model.Resultado = await _reporteVentasService.GenerarAsync(model.Filtros);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de ventas");
                ModelState.AddModelError(string.Empty, "No se pudo generar el reporte de ventas.");
                return View(model);
            }
        }

        private async Task<List<PeliculaResponse>> ObtenerPeliculasParaFiltroAsync()
        {
            var peliculas = await _peliculaRepository.ObtenerTodosAsync();

            return peliculas
                .OrderBy(p => p.Titulo)
                .Select(p => new PeliculaResponse
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    Sinopsis = p.Sinopsis,
                    Genero = p.Genero,
                    DuracionMinutos = p.DuracionMinutos,
                    Clasificacion = p.Clasificacion,
                    RutaPoster = p.RutaPoster,
                    Activo = p.Activo
                })
                .ToList();
        }
    }
}
