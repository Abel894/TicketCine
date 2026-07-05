using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;

namespace TicketCine.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminFuncionesController : Controller
    {
        private readonly IFuncionRepository _funcionRepository;
        private readonly IPeliculaRepository _peliculaRepository;
        private readonly ISalaRepository _salaRepository;
        private readonly IAsientoRepository _asientoRepository;
        private readonly ILogger<AdminFuncionesController> _logger;

        public AdminFuncionesController(
            IFuncionRepository funcionRepository,
            IPeliculaRepository peliculaRepository,
            ISalaRepository salaRepository,
            IAsientoRepository asientoRepository,
            ILogger<AdminFuncionesController> logger)
        {
            _funcionRepository = funcionRepository ?? throw new ArgumentNullException(nameof(funcionRepository));
            _peliculaRepository = peliculaRepository ?? throw new ArgumentNullException(nameof(peliculaRepository));
            _salaRepository = salaRepository ?? throw new ArgumentNullException(nameof(salaRepository));
            _asientoRepository = asientoRepository ?? throw new ArgumentNullException(nameof(asientoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Admin/AdminFunciones - Lista todas las funciones
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var funciones = await _funcionRepository.ObtenerTodosAsync();
                var funcionesResponse = funciones
                    .OrderBy(f => f.FechaHora)
                    .Select(f => new FuncionResponse
                    {
                        Id = f.Id,
                        PeliculaId = f.PeliculaId,
                        TituloPelicula = f.Pelicula.Titulo,
                        SalaId = f.SalaId,
                        NombreSala = f.Sala.Nombre,
                        FechaHora = f.FechaHora,
                        Precio = f.Precio,
                        Activo = f.Activo
                    }).ToList();

                return View(funcionesResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de funciones");
                ModelState.AddModelError(string.Empty, "Error al cargar las funciones.");
                return View(new List<FuncionResponse>());
            }
        }

        /// <summary>
        /// GET: /Admin/AdminFunciones/Crear - Muestra el formulario de creación
        /// </summary>
        public async Task<IActionResult> Crear()
        {
            try
            {
                await CargarCatalogosAsync();
                return View(new CrearFuncionRequest());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar datos para crear función");
                ModelState.AddModelError(string.Empty, "Error al cargar datos.");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Admin/AdminFunciones/Crear - Procesa la creación de una función
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearFuncionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarCatalogosAsync();
                    return View(request);
                }

                // Obtener película para obtener duración
                var pelicula = await _peliculaRepository.ObtenerPorIdAsync(request.PeliculaId);
                if (pelicula == null)
                {
                    ModelState.AddModelError(string.Empty, "Película no encontrada.");
                    await CargarCatalogosAsync();
                    return View(request);
                }

                // Validar conflicto de horario (RN-07)
                var existe = await _funcionRepository.ExisteConflictoHorarioAsync(
                    request.SalaId,
                    request.FechaHora,
                    pelicula.DuracionMinutos);

                if (existe)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una función en esa sala en ese horario.");
                    await CargarCatalogosAsync();
                    return View(request);
                }

                // Crear función
                var funcion = new Funcion
                {
                    Id = Guid.NewGuid(),
                    PeliculaId = request.PeliculaId,
                    SalaId = request.SalaId,
                    FechaHora = request.FechaHora,
                    Precio = request.Precio,
                    Activo = true
                };

                var funcionCreada = await _funcionRepository.CrearAsync(funcion);

                // Generar automáticamente asientos
                var sala = await _salaRepository.ObtenerPorIdAsync(request.SalaId);
                if (sala == null)
                {
                    ModelState.AddModelError(string.Empty, "Sala no encontrada.");
                    await CargarCatalogosAsync();
                    return View(request);
                }

                var asientos = new List<Asiento>();

                for (int fila = 1; fila <= sala.Filas; fila++)
                {
                    for (int columna = 1; columna <= sala.Columnas; columna++)
                    {
                        asientos.Add(new Asiento
                        {
                            Id = Guid.NewGuid(),
                            FuncionId = funcionCreada.Id,
                            Fila = fila,
                            Columna = columna,
                            Estado = EstadoAsiento.Libre
                        });
                    }
                }

                await _asientoRepository.CrearVariosAsync(asientos);

                TempData["SuccessMessage"] = $"Función creada correctamente. Se generaron {asientos.Count} asientos.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear función");
                ModelState.AddModelError(string.Empty, "Error al crear la función.");
                await CargarCatalogosAsync();
                return View(request);
            }
        }

        /// <summary>
        /// GET: /Admin/AdminFunciones/Editar/{id} - Muestra el formulario de edición
        /// </summary>
        public async Task<IActionResult> Editar(Guid id)
        {
            try
            {
                var funcion = await _funcionRepository.ObtenerPorIdAsync(id);
                if (funcion == null)
                {
                    return NotFound("Función no encontrada");
                }

                await CargarCatalogosAsync();

                var request = new EditarFuncionRequest
                {
                    Id = funcion.Id,
                    PeliculaId = funcion.PeliculaId,
                    SalaId = funcion.SalaId,
                    FechaHora = funcion.FechaHora,
                    Precio = funcion.Precio,
                    Activo = funcion.Activo
                };

                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar función para edición {FuncionId}", id);
                ModelState.AddModelError(string.Empty, "Error al cargar la función.");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Admin/AdminFunciones/Editar - Procesa la edición de una función
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarFuncionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarCatalogosAsync();
                    return View(request);
                }

                var funcion = await _funcionRepository.ObtenerPorIdAsync(request.Id);
                if (funcion == null)
                {
                    return NotFound("Función no encontrada");
                }

                // Obtener película para obtener duración
                var pelicula = await _peliculaRepository.ObtenerPorIdAsync(request.PeliculaId);
                if (pelicula == null)
                {
                    ModelState.AddModelError(string.Empty, "Película no encontrada.");
                    await CargarCatalogosAsync();
                    return View(request);
                }

                // Validar conflicto de horario (excluir la función actual)
                var existe = await _funcionRepository.ExisteConflictoHorarioAsync(
                    request.SalaId,
                    request.FechaHora,
                    pelicula.DuracionMinutos,
                    request.Id);

                if (existe)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una función en esa sala en ese horario.");
                    await CargarCatalogosAsync();
                    return View(request);
                }

                funcion.PeliculaId = request.PeliculaId;
                funcion.SalaId = request.SalaId;
                funcion.FechaHora = request.FechaHora;
                funcion.Precio = request.Precio;
                funcion.Activo = request.Activo;

                await _funcionRepository.ActualizarAsync(funcion);

                TempData["SuccessMessage"] = "Función actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar función");
                ModelState.AddModelError(string.Empty, "Error al actualizar la función.");
                await CargarCatalogosAsync();
                return View(request);
            }
        }

        /// <summary>
        /// POST: /Admin/AdminFunciones/Eliminar - Elimina una función
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            try
            {
                var funcion = await _funcionRepository.ObtenerPorIdAsync(id);
                if (funcion == null)
                {
                    return NotFound("Función no encontrada");
                }

                await _funcionRepository.EliminarAsync(id);

                TempData["SuccessMessage"] = "Función eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar función {FuncionId}", id);
                TempData["ErrorMessage"] = "Error al eliminar la función.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task CargarCatalogosAsync()
        {
            var peliculas = await _peliculaRepository.ObtenerActivosAsync();
            var salas = await _salaRepository.ObtenerActivosAsync();

            ViewBag.Peliculas = peliculas
                .Select(p => new Pelicula { Id = p.Id, Titulo = p.Titulo, DuracionMinutos = p.DuracionMinutos })
                .ToList();

            ViewBag.Salas = salas
                .Select(s => new Sala { Id = s.Id, Nombre = s.Nombre, Filas = s.Filas, Columnas = s.Columnas })
                .ToList();
        }
    }
}
