using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;

namespace TicketCine.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Auth/Registro - Muestra el formulario de registro
        /// </summary>
        public IActionResult Registro()
        {
            return View();
        }

        /// <summary>
        /// POST: /Auth/Registro - Procesa el registro de un nuevo usuario
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroUsuarioRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                // Llamar al servicio de aplicación
                var usuarioResponse = await _authService.RegistrarAsync(request);

                // Mostrar mensaje de éxito
                TempData["SuccessMessage"] = "Registro exitoso. Por favor, inicia sesión.";
                return RedirectToAction("Login");
            }
            catch (InvalidOperationException ex)
            {
                // Correo duplicado o error de negocio
                ModelState.AddModelError("Correo", ex.Message);
                _logger.LogWarning($"Intento de registro con correo duplicado o error: {ex.Message}");
                return View(request);
            }
            catch (ArgumentException ex)
            {
                // Validación de campos
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning($"Error de validación en registro: {ex.Message}");
                return View(request);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error al registrarse. Intenta de nuevo.");
                _logger.LogError($"Error no esperado en registro: {ex.Message}");
                return View(request);
            }
        }

        /// <summary>
        /// GET: /Auth/Login - Muestra el formulario de login
        /// </summary>
        public IActionResult Login()
        {
            // Si el usuario ya está autenticado, redirigir al home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        /// <summary>
        /// POST: /Auth/Login - Procesa el login del usuario
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                // Llamar al servicio de autenticación
                var loginResponse = await _authService.AutenticarAsync(request);

                if (loginResponse == null)
                {
                    // Credenciales inválidas
                    ModelState.AddModelError(string.Empty, "Correo o contraseña inválidos.");
                    _logger.LogWarning($"Intento de login fallido para correo: {request.Correo}");
                    return View(request);
                }

                // Crear claims para la cookie de autenticación
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, loginResponse.UsuarioId.ToString()),
                    new Claim(ClaimTypes.Name, loginResponse.Nombre),
                    new Claim(ClaimTypes.Email, loginResponse.Correo),
                    new Claim(ClaimTypes.Role, loginResponse.Rol)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                // Autenticar al usuario
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"Usuario autenticado: {loginResponse.Correo} (Rol: {loginResponse.Rol})");

                // Redirigir según rol
                if (loginResponse.Rol == "Administrador")
                {
                    return RedirectToAction("Index", "Home"); // Replace with admin dashboard later
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning($"Error de validación en login: {ex.Message}");
                return View(request);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error al iniciar sesión. Intenta de nuevo.");
                _logger.LogError($"Error no esperado en login: {ex.Message}");
                return View(request);
            }
        }

        /// <summary>
        /// GET: /Auth/Logout - Cierra la sesión del usuario
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "desconocido";
            _logger.LogInformation($"Usuario cerrando sesión: {userEmail}");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] = "Sesión cerrada exitosamente.";
            return RedirectToAction("Index", "Home");
        }
    }
}
