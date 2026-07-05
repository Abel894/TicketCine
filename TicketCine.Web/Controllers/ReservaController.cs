using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;
using TicketCine.Web.Models;

namespace TicketCine.Web.Controllers
{
    [Authorize]
    public class ReservaController : Controller
    {
        private readonly IReservaService _reservaService;
        private readonly IVentaService _ventaService;
        private readonly IFuncionRepository _funcionRepository;
        private readonly IAsientoRepository _asientoRepository;
        private readonly IVentaRepository _ventaRepository;
        private readonly ILogger<ReservaController> _logger;

        public ReservaController(
            IReservaService reservaService,
            IVentaService ventaService,
            IFuncionRepository funcionRepository,
            IAsientoRepository asientoRepository,
            IVentaRepository ventaRepository,
            ILogger<ReservaController> logger)
        {
            _reservaService = reservaService ?? throw new ArgumentNullException(nameof(reservaService));
            _ventaService = ventaService ?? throw new ArgumentNullException(nameof(ventaService));
            _funcionRepository = funcionRepository ?? throw new ArgumentNullException(nameof(funcionRepository));
            _asientoRepository = asientoRepository ?? throw new ArgumentNullException(nameof(asientoRepository));
            _ventaRepository = ventaRepository ?? throw new ArgumentNullException(nameof(ventaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearReservaRequest request)
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                if (usuarioId == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Usuario no autenticado." });
                }

                var reservaCreada = await _reservaService.CrearReservaAsync(usuarioId, request);

                return Ok(new
                {
                    success = true,
                    reservaId = reservaCreada.ReservaId,
                    redirectUrl = Url.Action(nameof(Resumen), "Reserva", new { id = reservaCreada.ReservaId }),
                    tiempoRestanteSegundos = reservaCreada.TiempoRestanteSegundos
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo crear la reserva");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Resumen(Guid id)
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                if (usuarioId == Guid.Empty)
                {
                    return Unauthorized();
                }

                var reserva = await _reservaService.ObtenerReservaPorIdAsync(usuarioId, id);
                if (reserva == null)
                {
                    return NotFound("Reserva no encontrada.");
                }

                var funcion = await _funcionRepository.ObtenerPorIdAsync(reserva.FuncionId);
                if (funcion == null)
                {
                    return NotFound("Función no encontrada.");
                }

                var asientosEtiquetas = new List<string>();
                foreach (var asientoReserva in reserva.Asientos)
                {
                    var asiento = await _asientoRepository.ObtenerPorIdAsync(asientoReserva.AsientoId);
                    if (asiento != null)
                    {
                        var etiqueta = $"{(char)('A' + asiento.Fila - 1)}{asiento.Columna}";
                        asientosEtiquetas.Add(etiqueta);
                    }
                }

                var cantidadAsientos = asientosEtiquetas.Count;

                var model = new ReservaResumenViewModel
                {
                    ReservaId = reserva.Id,
                    CodigoReserva = reserva.Id.ToString("N").ToUpperInvariant(),
                    TituloPelicula = funcion.Pelicula.Titulo,
                    NombreSala = funcion.Sala.Nombre,
                    FechaHoraFuncion = funcion.FechaHora,
                    FechaCreacion = reserva.FechaCreacion,
                    FechaExpiracion = reserva.FechaExpiracion,
                    TiempoRestanteSegundos = (int)Math.Max(0, (reserva.FechaExpiracion - DateTime.Now).TotalSeconds),
                    PrecioUnitario = funcion.Precio,
                    CantidadAsientos = cantidadAsientos,
                    Total = funcion.Precio * cantidadAsientos,
                    Asientos = asientosEtiquetas.OrderBy(x => x).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar resumen de reserva {ReservaId}", id);
                return BadRequest("No se pudo cargar el resumen de reserva.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPago(Guid reservaId, string metodoPago)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == Guid.Empty)
            {
                return Unauthorized();
            }

            try
            {
                var response = await _ventaService.ConfirmarCompraAsync(usuarioId, new ConfirmarCompraRequest
                {
                    ReservaId = reservaId,
                    MetodoPago = metodoPago
                });

                TempData["SuccessMessage"] = "Pago confirmado correctamente.";
                return RedirectToAction(nameof(Comprobante), new { id = response.VentaId });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al confirmar pago para reserva {ReservaId}", reservaId);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Resumen), new { id = reservaId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Comprobante(Guid id)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == Guid.Empty)
            {
                return Unauthorized();
            }

            try
            {
                var ventaResponse = await _ventaService.ObtenerComprobanteAsync(usuarioId, id);
                if (ventaResponse == null)
                {
                    return NotFound("Comprobante no encontrado.");
                }

                var ventaDetalle = await _ventaRepository.ObtenerConDetalleAsync(id);
                if (ventaDetalle == null || ventaDetalle.Reserva.UsuarioId != usuarioId)
                {
                    return NotFound("Comprobante no encontrado.");
                }

                var asientos = ventaDetalle.Reserva.Asientos
                    .Select(x => $"{(char)('A' + x.Asiento.Fila - 1)}{x.Asiento.Columna}")
                    .OrderBy(x => x)
                    .ToList();

                var model = new ComprobanteViewModel
                {
                    VentaId = ventaResponse.VentaId,
                    CodigoQr = ventaResponse.CodigoQr,
                    CodigoQrBase64 = ventaResponse.CodigoQrBase64,
                    TituloPelicula = ventaDetalle.Reserva.Funcion.Pelicula.Titulo,
                    NombreSala = ventaDetalle.Reserva.Funcion.Sala.Nombre,
                    FechaHoraFuncion = ventaDetalle.Reserva.Funcion.FechaHora,
                    Asientos = asientos,
                    MontoTotal = ventaResponse.MontoTotal,
                    MetodoPago = ventaResponse.MetodoPago,
                    FechaVenta = ventaResponse.FechaVenta
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar comprobante {VentaId}", id);
                TempData["ErrorMessage"] = "No se pudo cargar el comprobante.";
                return RedirectToAction("Index", "Catalogo");
            }
        }

        [HttpGet]
        public async Task<IActionResult> MisReservas()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == Guid.Empty)
            {
                return Unauthorized();
            }

            var reservas = await _reservaService.ObtenerReservasActivasAsync(usuarioId);
            var items = new List<ReservaItemViewModel>();

            foreach (var reserva in reservas)
            {
                var funcion = await _funcionRepository.ObtenerPorIdAsync(reserva.FuncionId);
                if (funcion == null)
                    continue;

                var asientosEtiquetas = new List<string>();
                foreach (var asientoReserva in reserva.Asientos)
                {
                    var asiento = await _asientoRepository.ObtenerPorIdAsync(asientoReserva.AsientoId);
                    if (asiento != null)
                    {
                        asientosEtiquetas.Add($"{(char)('A' + asiento.Fila - 1)}{asiento.Columna}");
                    }
                }

                Guid? ventaId = null;
                if (reserva.Estado == EstadoReserva.Confirmada)
                {
                    var venta = await _ventaRepository.ObtenerPorReservaIdAsync(reserva.Id);
                    ventaId = venta?.Id;
                }

                var puedeContinuarPago = reserva.Estado == EstadoReserva.Pendiente
                                         && reserva.FechaExpiracion > DateTime.Now;
                var puedeCancelar = reserva.Estado == EstadoReserva.Pendiente
                                    && funcion.FechaHora > DateTime.Now.AddHours(1);

                items.Add(new ReservaItemViewModel
                {
                    ReservaId = reserva.Id,
                    CodigoReserva = reserva.Id.ToString("N").ToUpperInvariant(),
                    TituloPelicula = funcion.Pelicula.Titulo,
                    NombreSala = funcion.Sala.Nombre,
                    FechaHoraFuncion = funcion.FechaHora,
                    Asientos = asientosEtiquetas.OrderBy(x => x).ToList(),
                    Estado = reserva.Estado,
                    PuedeContinuarPago = puedeContinuarPago,
                    PuedeCancelar = puedeCancelar,
                    VentaId = ventaId
                });
            }

            var model = new MisReservasViewModel
            {
                Reservas = items
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(Guid reservaId)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == Guid.Empty)
            {
                return Unauthorized();
            }

            try
            {
                await _reservaService.CancelarAsync(usuarioId, reservaId);
                TempData["SuccessMessage"] = "Reserva cancelada correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al cancelar reserva {ReservaId}", reservaId);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(MisReservas));
        }

        private Guid ObtenerUsuarioId()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(usuarioIdClaim, out var usuarioId) ? usuarioId : Guid.Empty;
        }
    }
}
