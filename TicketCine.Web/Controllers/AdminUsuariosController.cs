using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketCine.Application.Interfaces;
using TicketCine.Web.Models;

namespace TicketCine.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminUsuariosController : Controller
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<AdminUsuariosController> _logger;

        public AdminUsuariosController(
            IUsuarioRepository usuarioRepository,
            ILogger<AdminUsuariosController> logger)
        {
            _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarios = await _usuarioRepository.ObtenerTodosAsync();
                var usuarioAutenticadoId = ObtenerUsuarioId();

                var model = new AdminUsuariosIndexViewModel
                {
                    UsuarioAutenticadoId = usuarioAutenticadoId,
                    Usuarios = usuarios.Select(u => new AdminUsuarioItemViewModel
                    {
                        Id = u.Id,
                        Nombre = u.Nombre,
                        Correo = u.Correo,
                        Rol = u.Rol.Nombre,
                        Activo = u.Activo
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de usuarios");
                TempData["ErrorMessage"] = "No se pudo cargar la lista de usuarios.";
                return View(new AdminUsuariosIndexViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(Guid usuarioId)
        {
            try
            {
                if (usuarioId == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Usuario inválido.";
                    return RedirectToAction(nameof(Index));
                }

                var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "El usuario no existe.";
                    return RedirectToAction(nameof(Index));
                }

                var usuarioAutenticadoId = ObtenerUsuarioId();
                if (usuarioId == usuarioAutenticadoId)
                {
                    TempData["ErrorMessage"] = "No puedes cambiar el estado de tu propia cuenta.";
                    return RedirectToAction(nameof(Index));
                }

                usuario.Activo = !usuario.Activo;
                await _usuarioRepository.ActualizarAsync(usuario);

                TempData["SuccessMessage"] = usuario.Activo
                    ? "Usuario activado correctamente."
                    : "Usuario desactivado correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del usuario {UsuarioId}", usuarioId);
                TempData["ErrorMessage"] = "No se pudo cambiar el estado del usuario.";
            }

            return RedirectToAction(nameof(Index));
        }

        private Guid ObtenerUsuarioId()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(usuarioIdClaim, out var usuarioId) ? usuarioId : Guid.Empty;
        }
    }
}
