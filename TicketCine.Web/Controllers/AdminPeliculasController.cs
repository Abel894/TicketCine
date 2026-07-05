using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;

namespace TicketCine.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminPeliculasController : Controller
    {
        private readonly IPeliculaRepository _peliculaRepository;
        private readonly IArchivoService _archivoService;
        private readonly ILogger<AdminPeliculasController> _logger;

        public AdminPeliculasController(
            IPeliculaRepository peliculaRepository,
            IArchivoService archivoService,
            ILogger<AdminPeliculasController> logger)
        {
            _peliculaRepository = peliculaRepository ?? throw new ArgumentNullException(nameof(peliculaRepository));
            _archivoService = archivoService ?? throw new ArgumentNullException(nameof(archivoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Admin/AdminPeliculas - Lista todas las películas
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var peliculas = await _peliculaRepository.ObtenerTodosAsync();
                var peliculasResponse = peliculas.Select(p => new PeliculaResponse
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    Sinopsis = p.Sinopsis,
                    Genero = p.Genero,
                    DuracionMinutos = p.DuracionMinutos,
                    Clasificacion = p.Clasificacion,
                    RutaPoster = p.RutaPoster,
                    Activo = p.Activo
                }).ToList();

                return View(peliculasResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de películas");
                ModelState.AddModelError(string.Empty, "Error al cargar las películas.");
                return View(new List<PeliculaResponse>());
            }
        }

        /// <summary>
        /// GET: /Admin/AdminPeliculas/Crear - Muestra el formulario de creación
        /// </summary>
        public IActionResult Crear()
        {
            return View(new CrearPeliculaRequest());
        }

        /// <summary>
        /// POST: /Admin/AdminPeliculas/Crear - Procesa la creación de una película
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearPeliculaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                // Validar archivo si existe
                string rutaPoster = null;
                if (request.Poster != null && request.Poster.Length > 0)
                {
                    rutaPoster = await _archivoService.GuardarPosterAsync(request.Poster);
                }

                var pelicula = new Pelicula
                {
                    Id = Guid.NewGuid(),
                    Titulo = request.Titulo,
                    Sinopsis = request.Sinopsis,
                    Genero = request.Genero,
                    DuracionMinutos = request.DuracionMinutos,
                    Clasificacion = request.Clasificacion,
                    RutaPoster = rutaPoster,
                    Activo = true
                };

                await _peliculaRepository.CrearAsync(pelicula);

                TempData["SuccessMessage"] = "Película creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear película");
                ModelState.AddModelError(string.Empty, "Error al crear la película.");
                return View(request);
            }
        }

        /// <summary>
        /// GET: /Admin/AdminPeliculas/Editar/{id} - Muestra el formulario de edición
        /// </summary>
        public async Task<IActionResult> Editar(Guid id)
        {
            try
            {
                var pelicula = await _peliculaRepository.ObtenerPorIdAsync(id);
                if (pelicula == null)
                {
                    return NotFound("Película no encontrada");
                }

                var request = new EditarPeliculaRequest
                {
                    Id = pelicula.Id,
                    Titulo = pelicula.Titulo,
                    Sinopsis = pelicula.Sinopsis,
                    Genero = pelicula.Genero,
                    DuracionMinutos = pelicula.DuracionMinutos,
                    Clasificacion = pelicula.Clasificacion,
                    RutaPosterActual = pelicula.RutaPoster,
                    Activo = pelicula.Activo
                };

                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar película para edición {PeliculaId}", id);
                ModelState.AddModelError(string.Empty, "Error al cargar la película.");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Admin/AdminPeliculas/Editar - Procesa la edición de una película
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarPeliculaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                var pelicula = await _peliculaRepository.ObtenerPorIdAsync(request.Id);
                if (pelicula == null)
                {
                    return NotFound("Película no encontrada");
                }

                string rutaPoster = request.RutaPosterActual;

                // Si se subió un nuevo archivo, eliminar el antiguo y guardar el nuevo
                if (request.Poster != null && request.Poster.Length > 0)
                {
                    if (!string.IsNullOrEmpty(request.RutaPosterActual))
                    {
                        _archivoService.EliminarPoster(request.RutaPosterActual);
                    }
                    rutaPoster = await _archivoService.GuardarPosterAsync(request.Poster);
                }

                pelicula.Titulo = request.Titulo;
                pelicula.Sinopsis = request.Sinopsis;
                pelicula.Genero = request.Genero;
                pelicula.DuracionMinutos = request.DuracionMinutos;
                pelicula.Clasificacion = request.Clasificacion;
                pelicula.RutaPoster = rutaPoster;
                pelicula.Activo = request.Activo;

                await _peliculaRepository.ActualizarAsync(pelicula);

                TempData["SuccessMessage"] = "Película actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar película");
                ModelState.AddModelError(string.Empty, "Error al actualizar la película.");
                return View(request);
            }
        }

        /// <summary>
        /// POST: /Admin/AdminPeliculas/Eliminar - Elimina una película
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            try
            {
                var pelicula = await _peliculaRepository.ObtenerPorIdAsync(id);
                if (pelicula == null)
                {
                    return NotFound("Película no encontrada");
                }

                // Eliminar archivo de póster si existe
                if (!string.IsNullOrEmpty(pelicula.RutaPoster))
                {
                    _archivoService.EliminarPoster(pelicula.RutaPoster);
                }

                await _peliculaRepository.EliminarAsync(id);

                TempData["SuccessMessage"] = "Película eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar película {PeliculaId}", id);
                TempData["ErrorMessage"] = "Error al eliminar la película.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
