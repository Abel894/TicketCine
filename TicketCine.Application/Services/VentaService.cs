using System.Transactions;
using QRCoder;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;

namespace TicketCine.Application.Services
{
    public class VentaService : IVentaService
    {
        private static readonly Dictionary<string, string> MetodosPagoPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Tarjeta"] = "Tarjeta",
            ["Yape"] = "Yape",
            ["Plin"] = "Plin"
        };

        private readonly IVentaRepository _ventaRepository;
        private readonly IReservaRepository _reservaRepository;
        private readonly IAsientoRepository _asientoRepository;
        private readonly IFuncionRepository _funcionRepository;
        private readonly IReservaService _reservaService;

        public VentaService(
            IVentaRepository ventaRepository,
            IReservaRepository reservaRepository,
            IAsientoRepository asientoRepository,
            IFuncionRepository funcionRepository,
            IReservaService reservaService)
        {
            _ventaRepository = ventaRepository ?? throw new ArgumentNullException(nameof(ventaRepository));
            _reservaRepository = reservaRepository ?? throw new ArgumentNullException(nameof(reservaRepository));
            _asientoRepository = asientoRepository ?? throw new ArgumentNullException(nameof(asientoRepository));
            _funcionRepository = funcionRepository ?? throw new ArgumentNullException(nameof(funcionRepository));
            _reservaService = reservaService ?? throw new ArgumentNullException(nameof(reservaService));
        }

        public async Task<VentaConfirmadaResponse> ConfirmarCompraAsync(Guid usuarioId, ConfirmarCompraRequest request)
        {
            ValidarUsuarioAutenticado(usuarioId);

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.ReservaId == Guid.Empty)
                throw new ArgumentException("La reserva es requerida.", nameof(request.ReservaId));

            if (string.IsNullOrWhiteSpace(request.MetodoPago))
                throw new ArgumentException("El método de pago es requerido.", nameof(request.MetodoPago));

            if (!MetodosPagoPermitidos.TryGetValue(request.MetodoPago.Trim(), out var metodoPago))
                throw new InvalidOperationException("Método de pago no válido. Use Tarjeta, Yape o Plin.");

            await _reservaService.ExpirarReservasVencidasAsync();

            var reserva = await _reservaRepository.ObtenerPorIdAsync(request.ReservaId)
                ?? throw new InvalidOperationException("La reserva no existe.");

            if (reserva.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("La reserva no pertenece al usuario autenticado.");

            if (reserva.Estado != EstadoReserva.Pendiente)
                throw new InvalidOperationException("Solo se pueden pagar reservas pendientes.");

            if (reserva.FechaExpiracion <= DateTime.Now)
                throw new InvalidOperationException("La reserva ya expiró y no puede pagarse.");

            var ventaExistente = await _ventaRepository.ObtenerPorReservaIdAsync(reserva.Id);
            if (ventaExistente != null)
                throw new InvalidOperationException("La reserva ya tiene una venta registrada.");

            var funcion = await _funcionRepository.ObtenerPorIdAsync(reserva.FuncionId)
                ?? throw new InvalidOperationException("La función asociada no existe.");

            if (funcion.FechaHora <= DateTime.Now)
                throw new InvalidOperationException("No se puede comprar una función que ya inició.");

            var asientosReserva = (await _reservaRepository.ObtenerAsientosPorReservaAsync(reserva.Id)).ToList();
            if (asientosReserva.Count == 0)
                throw new InvalidOperationException("La reserva no contiene asientos.");

            var asientos = new List<Asiento>();
            foreach (var asientoReserva in asientosReserva)
            {
                var asiento = await _asientoRepository.ObtenerPorIdAsync(asientoReserva.AsientoId)
                    ?? throw new InvalidOperationException("Uno de los asientos reservados no existe.");

                if (asiento.Estado != EstadoAsiento.Reservado)
                    throw new InvalidOperationException("Uno o más asientos ya no están en estado reservado.");

                asientos.Add(asiento);
            }

            var montoTotal = funcion.Precio * asientos.Count;
            var fechaVenta = DateTime.Now;
            var codigoQr = $"TC-{reserva.Id:N}-{fechaVenta:yyyyMMddHHmmss}";
            var codigoQrBase64 = GenerarQrBase64(codigoQr);

            var venta = new Venta
            {
                Id = Guid.NewGuid(),
                ReservaId = reserva.Id,
                MetodoPago = metodoPago,
                MontoTotal = montoTotal,
                FechaVenta = fechaVenta,
                CodigoQr = codigoQr
            };

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _ventaRepository.CrearAsync(venta);

                foreach (var asiento in asientos)
                {
                    asiento.Estado = EstadoAsiento.Vendido;
                    await _asientoRepository.ActualizarAsync(asiento);
                }

                reserva.Estado = EstadoReserva.Confirmada;
                await _reservaRepository.ActualizarAsync(reserva);

                scope.Complete();
            }

            return new VentaConfirmadaResponse
            {
                VentaId = venta.Id,
                ReservaId = venta.ReservaId,
                MetodoPago = venta.MetodoPago,
                MontoTotal = venta.MontoTotal,
                FechaVenta = venta.FechaVenta,
                CodigoQr = codigoQr,
                CodigoQrBase64 = codigoQrBase64
            };
        }

        public async Task<VentaConfirmadaResponse?> ObtenerComprobanteAsync(Guid usuarioId, Guid ventaId)
        {
            ValidarUsuarioAutenticado(usuarioId);

            var venta = await _ventaRepository.ObtenerConDetalleAsync(ventaId);
            if (venta == null || venta.Reserva.UsuarioId != usuarioId)
                return null;

            return new VentaConfirmadaResponse
            {
                VentaId = venta.Id,
                ReservaId = venta.ReservaId,
                MetodoPago = venta.MetodoPago,
                MontoTotal = venta.MontoTotal,
                FechaVenta = venta.FechaVenta,
                CodigoQr = venta.CodigoQr,
                CodigoQrBase64 = GenerarQrBase64(venta.CodigoQr)
            };
        }

        private static string GenerarQrBase64(string contenidoQr)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(contenidoQr, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrBytes);
        }

        private static void ValidarUsuarioAutenticado(Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
                throw new UnauthorizedAccessException("Usuario no autenticado.");
        }
    }
}
