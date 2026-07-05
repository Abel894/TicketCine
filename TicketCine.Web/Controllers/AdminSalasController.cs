using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;

namespace TicketCine.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminSalasController : Controller
    {
        private readonly ISalaRepository _salaRepository;
        private readonly ILogger<AdminSalasController> _logger;

        public AdminSalasController(
            ISalaRepository salaRepository,
            ILogger<AdminSalasController> logger)
        {
            _salaRepository = salaRepository ?? throw new ArgumentNullException(nameof(salaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Admin/AdminSalas - Lista todas las salas
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var salas = await _salaRepository.ObtenerTodosAsync();
                var salasResponse = salas.Select(s => new SalaResponse
                {
                    Id = s.Id,
                    Nombre = s.Nombre,
                    Filas = s.Filas,
                    Columnas = s.Columnas,
                    Activo = s.Activo
                }).ToList();

                return View(salasResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de salas");
                ModelState.AddModelError(string.Empty, "Error al cargar las salas.");
                return View(new List<SalaResponse>());
            }
        }

        /// <summary>
        /// GET: /Admin/AdminSalas/Crear - Muestra el formulario de creación
        /// </summary>
        public IActionResult Crear()
        {
            return View(new CrearSalaRequest());
        }

        /// <summary>
        /// POST: /Admin/AdminSalas/Crear - Procesa la creación de una sala
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearSalaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                var sala = new Sala
                {
                    Id = Guid.NewGuid(),
                    Nombre = request.Nombre,
                    Filas = request.Filas,
                    Columnas = request.Columnas,
                    Activo = true
                };

                await _salaRepository.CrearAsync(sala);

                TempData["SuccessMessage"] = "Sala creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear sala");
                ModelState.AddModelError(string.Empty, "Error al crear la sala.");
                return View(request);
            }
        }

        /// <summary>
        /// GET: /Admin/AdminSalas/Editar/{id} - Muestra el formulario de edición
        /// </summary>
        public async Task<IActionResult> Editar(Guid id)
        {
            try
            {
                var sala = await _salaRepository.ObtenerPorIdAsync(id);
                if (sala == null)
                {
                    return NotFound("Sala no encontrada");
                }

                var request = new EditarSalaRequest
                {
                    Id = sala.Id,
                    Nombre = sala.Nombre,
                    Filas = sala.Filas,
                    Columnas = sala.Columnas,
                    Activo = sala.Activo
                };

                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar sala para edición {SalaId}", id);
                ModelState.AddModelError(string.Empty, "Error al cargar la sala.");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Admin/AdminSalas/Editar - Procesa la edición de una sala
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarSalaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                var sala = await _salaRepository.ObtenerPorIdAsync(request.Id);
                if (sala == null)
                {
                    return NotFound("Sala no encontrada");
                }

                sala.Nombre = request.Nombre;
                sala.Filas = request.Filas;
                sala.Columnas = request.Columnas;
                sala.Activo = request.Activo;

                await _salaRepository.ActualizarAsync(sala);

                TempData["SuccessMessage"] = "Sala actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar sala");
                ModelState.AddModelError(string.Empty, "Error al actualizar la sala.");
                return View(request);
            }
        }

        /// <summary>
        /// POST: /Admin/AdminSalas/Eliminar - Elimina una sala
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            try
            {
                var sala = await _salaRepository.ObtenerPorIdAsync(id);
                if (sala == null)
                {
                    return NotFound("Sala no encontrada");
                }

                await _salaRepository.EliminarAsync(id);

                TempData["SuccessMessage"] = "Sala eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar sala {SalaId}", id);
                TempData["ErrorMessage"] = "Error al eliminar la sala.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
