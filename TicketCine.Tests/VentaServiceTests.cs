using Moq;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Application.Services;
using TicketCine.Domain.Entities;

namespace TicketCine.Tests;

public class VentaServiceTests
{
    [Fact]
    public async Task ConfirmarCompra_ConDatosValidos_DebeCrearVentaYConfirmarReserva()
    {
        var service = CrearService(out var ventaRepo, out var reservaRepo, out var asientoRepo, out var funcionRepo, out var reservaService);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asiento1Id = Guid.NewGuid();
        var asiento2Id = Guid.NewGuid();

        var request = new ConfirmarCompraRequest
        {
            ReservaId = reservaId,
            MetodoPago = " tarjeta "
        };

        var reserva = CrearReserva(reservaId, usuarioId, funcionId);
        var funcion = CrearFuncion(funcionId, 18.5m);
        var asiento1 = new Asiento { Id = asiento1Id, FuncionId = funcionId, Estado = EstadoAsiento.Reservado };
        var asiento2 = new Asiento { Id = asiento2Id, FuncionId = funcionId, Estado = EstadoAsiento.Reservado };

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(reserva);
        ventaRepo.Setup(r => r.ObtenerPorReservaIdAsync(reservaId)).ReturnsAsync((Venta?)null);
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(funcion);
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asiento1Id },
            new() { ReservaId = reservaId, AsientoId = asiento2Id }
        });
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asiento1Id)).ReturnsAsync(asiento1);
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asiento2Id)).ReturnsAsync(asiento2);

        var response = await service.ConfirmarCompraAsync(usuarioId, request);

        Assert.NotEqual(Guid.Empty, response.VentaId);
        Assert.Equal(reservaId, response.ReservaId);
        Assert.Equal("Tarjeta", response.MetodoPago);
        Assert.Equal(37m, response.MontoTotal);
        Assert.StartsWith($"TC-{reservaId:N}-", response.CodigoQr);
        Assert.False(string.IsNullOrWhiteSpace(response.CodigoQrBase64));
        Assert.Equal(EstadoReserva.Confirmada, reserva.Estado);
        Assert.Equal(EstadoAsiento.Vendido, asiento1.Estado);
        Assert.Equal(EstadoAsiento.Vendido, asiento2.Estado);

        reservaService.Verify(r => r.ExpirarReservasVencidasAsync(), Times.Once);
        ventaRepo.Verify(r => r.CrearAsync(It.Is<Venta>(v =>
            v.ReservaId == reservaId &&
            v.MetodoPago == "Tarjeta" &&
            v.MontoTotal == 37m &&
            !string.IsNullOrWhiteSpace(v.CodigoQr))), Times.Once);
        asientoRepo.Verify(r => r.ActualizarAsync(It.Is<Asiento>(a => a.Id == asiento1Id && a.Estado == EstadoAsiento.Vendido)), Times.Once);
        asientoRepo.Verify(r => r.ActualizarAsync(It.Is<Asiento>(a => a.Id == asiento2Id && a.Estado == EstadoAsiento.Vendido)), Times.Once);
        reservaRepo.Verify(r => r.ActualizarAsync(It.Is<Reserva>(x => x.Id == reservaId && x.Estado == EstadoReserva.Confirmada)), Times.Once);
    }

    [Fact]
    public async Task ConfirmarCompra_ConUnaSolaEntrada_DebeCalcularMontoMinimoCorrectamente()
    {
        var service = CrearService(out _, out var reservaRepo, out var asientoRepo, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asientoId = Guid.NewGuid();

        var reserva = CrearReserva(reservaId, usuarioId, funcionId);
        var funcion = CrearFuncion(funcionId, 25m);
        var asiento = new Asiento { Id = asientoId, FuncionId = funcionId, Estado = EstadoAsiento.Reservado };

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(reserva);
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(funcion);
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asientoId }
        });
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoId)).ReturnsAsync(asiento);

        var response = await service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId));

        Assert.Equal(25m, response.MontoTotal);
    }

    [Fact]
    public async Task ConfirmarCompra_ConPrecioCero_DebeRetornarMontoCero()
    {
        var service = CrearService(out _, out var reservaRepo, out var asientoRepo, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asientoId = Guid.NewGuid();

        var reserva = CrearReserva(reservaId, usuarioId, funcionId);
        var funcion = CrearFuncion(funcionId, 0m);
        var asiento = new Asiento { Id = asientoId, FuncionId = funcionId, Estado = EstadoAsiento.Reservado };

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(reserva);
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(funcion);
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asientoId }
        });
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoId)).ReturnsAsync(asiento);

        var response = await service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId, "Yape"));

        Assert.Equal(0m, response.MontoTotal);
        Assert.Equal("Yape", response.MetodoPago);
    }

    [Fact]
    public async Task ConfirmarCompra_ConPrecioNegativo_DebeRetornarMontoNegativo()
    {
        var service = CrearService(out _, out var reservaRepo, out var asientoRepo, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asientoId = Guid.NewGuid();

        var reserva = CrearReserva(reservaId, usuarioId, funcionId);
        var funcion = CrearFuncion(funcionId, -10m);
        var asiento = new Asiento { Id = asientoId, FuncionId = funcionId, Estado = EstadoAsiento.Reservado };

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(reserva);
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(funcion);
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asientoId }
        });
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoId)).ReturnsAsync(asiento);

        var response = await service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId, "Plin"));

        Assert.Equal(-10m, response.MontoTotal);
        Assert.Equal("Plin", response.MetodoPago);
    }

    [Fact]
    public async Task ConfirmarCompra_ConUsuarioNoAutenticado_DebeLanzarUnauthorizedAccessException()
    {
        var service = CrearService(out _, out _, out _, out _, out _);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmarCompraAsync(Guid.Empty, CrearRequest(Guid.NewGuid())));
    }

    [Fact]
    public async Task ConfirmarCompra_ConRequestNulo_DebeLanzarArgumentNullException()
    {
        var service = CrearService(out _, out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ConfirmarCompraAsync(usuarioId, null!));
    }

    [Fact]
    public async Task ConfirmarCompra_ConReservaIdVacio_DebeLanzarArgumentException()
    {
        var service = CrearService(out _, out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(Guid.Empty)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConMetodoPagoVacio_DebeLanzarArgumentException()
    {
        var service = CrearService(out _, out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(Guid.NewGuid(), string.Empty)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConMetodoPagoNoValido_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(Guid.NewGuid(), "Efectivo")));
    }

    [Fact]
    public async Task ConfirmarCompra_ConReservaInexistente_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync((Reserva?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConReservaDeOtroUsuario_DebeLanzarUnauthorizedAccessException()
    {
        var service = CrearService(out _, out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(CrearReserva(reservaId, Guid.NewGuid(), Guid.NewGuid()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConReservaNoPendiente_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(CrearReserva(reservaId, usuarioId, Guid.NewGuid(), EstadoReserva.Confirmada));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConReservaExpirada_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(CrearReserva(reservaId, usuarioId, Guid.NewGuid(), EstadoReserva.Pendiente, DateTime.Now.AddMinutes(-1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConVentaExistente_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var ventaRepo, out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(CrearReserva(reservaId, usuarioId, funcionId));
        ventaRepo.Setup(r => r.ObtenerPorReservaIdAsync(reservaId)).ReturnsAsync(new Venta { Id = Guid.NewGuid(), ReservaId = reservaId });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConFuncionInexistente_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out var reservaRepo, out _, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(CrearReserva(reservaId, usuarioId, funcionId));
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync((Funcion?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConFuncionYaIniciada_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out var reservaRepo, out _, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(CrearReserva(reservaId, usuarioId, funcionId));
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(CrearFuncion(funcionId, 20m, DateTime.Now.AddMinutes(-5)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConReservaSinAsientos_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out var reservaRepo, out _, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(CrearReserva(reservaId, usuarioId, funcionId));
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(CrearFuncion(funcionId, 20m));
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConAsientoInexistente_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out var reservaRepo, out var asientoRepo, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asientoId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(CrearReserva(reservaId, usuarioId, funcionId));
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(CrearFuncion(funcionId, 20m));
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asientoId }
        });
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoId)).ReturnsAsync((Asiento?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ConfirmarCompra_ConAsientoNoReservado_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out var reservaRepo, out var asientoRepo, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asientoId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(CrearReserva(reservaId, usuarioId, funcionId));
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(CrearFuncion(funcionId, 20m));
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asientoId }
        });
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoId)).ReturnsAsync(new Asiento
        {
            Id = asientoId,
            FuncionId = funcionId,
            Estado = EstadoAsiento.Libre
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarCompraAsync(usuarioId, CrearRequest(reservaId)));
    }

    [Fact]
    public async Task ObtenerComprobante_ConVentaValida_DebeRetornarComprobante()
    {
        var service = CrearService(out var ventaRepo, out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var fechaVenta = DateTime.Now.AddMinutes(-10);

        ventaRepo.Setup(r => r.ObtenerConDetalleAsync(ventaId)).ReturnsAsync(new Venta
        {
            Id = ventaId,
            ReservaId = reservaId,
            MetodoPago = "Tarjeta",
            MontoTotal = 42m,
            FechaVenta = fechaVenta,
            CodigoQr = "TC-COMPROBANTE",
            Reserva = new Reserva { Id = reservaId, UsuarioId = usuarioId }
        });

        var response = await service.ObtenerComprobanteAsync(usuarioId, ventaId);

        Assert.NotNull(response);
        Assert.Equal(ventaId, response.VentaId);
        Assert.Equal(reservaId, response.ReservaId);
        Assert.Equal("Tarjeta", response.MetodoPago);
        Assert.Equal(42m, response.MontoTotal);
        Assert.Equal(fechaVenta, response.FechaVenta);
        Assert.Equal("TC-COMPROBANTE", response.CodigoQr);
        Assert.False(string.IsNullOrWhiteSpace(response.CodigoQrBase64));
    }

    [Fact]
    public async Task ObtenerComprobante_ConVentaInexistente_DebeRetornarNull()
    {
        var service = CrearService(out var ventaRepo, out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();

        ventaRepo.Setup(r => r.ObtenerConDetalleAsync(ventaId)).ReturnsAsync((Venta?)null);

        var response = await service.ObtenerComprobanteAsync(usuarioId, ventaId);

        Assert.Null(response);
    }

    [Fact]
    public async Task ObtenerComprobante_ConVentaDeOtroUsuario_DebeRetornarNull()
    {
        var service = CrearService(out var ventaRepo, out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();

        ventaRepo.Setup(r => r.ObtenerConDetalleAsync(ventaId)).ReturnsAsync(new Venta
        {
            Id = ventaId,
            ReservaId = Guid.NewGuid(),
            MetodoPago = "Yape",
            MontoTotal = 15m,
            CodigoQr = "TC-OTRO",
            Reserva = new Reserva { Id = Guid.NewGuid(), UsuarioId = Guid.NewGuid() }
        });

        var response = await service.ObtenerComprobanteAsync(usuarioId, ventaId);

        Assert.Null(response);
    }

    [Fact]
    public async Task ObtenerComprobante_ConUsuarioNoAutenticado_DebeLanzarUnauthorizedAccessException()
    {
        var service = CrearService(out _, out _, out _, out _, out _);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ObtenerComprobanteAsync(Guid.Empty, Guid.NewGuid()));
    }

    private static VentaService CrearService(
        out Mock<IVentaRepository> ventaRepo,
        out Mock<IReservaRepository> reservaRepo,
        out Mock<IAsientoRepository> asientoRepo,
        out Mock<IFuncionRepository> funcionRepo,
        out Mock<IReservaService> reservaService)
    {
        ventaRepo = new Mock<IVentaRepository>();
        reservaRepo = new Mock<IReservaRepository>();
        asientoRepo = new Mock<IAsientoRepository>();
        funcionRepo = new Mock<IFuncionRepository>();
        reservaService = new Mock<IReservaService>();

        ventaRepo.Setup(r => r.CrearAsync(It.IsAny<Venta>())).ReturnsAsync((Venta v) => v);
        ventaRepo.Setup(r => r.ObtenerPorReservaIdAsync(It.IsAny<Guid>())).ReturnsAsync((Venta?)null);
        reservaRepo.Setup(r => r.ActualizarAsync(It.IsAny<Reserva>())).Returns(Task.CompletedTask);
        asientoRepo.Setup(r => r.ActualizarAsync(It.IsAny<Asiento>())).Returns(Task.CompletedTask);
        reservaService.Setup(r => r.ExpirarReservasVencidasAsync()).ReturnsAsync(0);

        return new VentaService(ventaRepo.Object, reservaRepo.Object, asientoRepo.Object, funcionRepo.Object, reservaService.Object);
    }

    private static ConfirmarCompraRequest CrearRequest(Guid reservaId, string metodoPago = "Tarjeta") => new()
    {
        ReservaId = reservaId,
        MetodoPago = metodoPago
    };

    private static Reserva CrearReserva(
        Guid reservaId,
        Guid usuarioId,
        Guid funcionId,
        EstadoReserva estado = EstadoReserva.Pendiente,
        DateTime? fechaExpiracion = null) => new()
        {
            Id = reservaId,
            UsuarioId = usuarioId,
            FuncionId = funcionId,
            Estado = estado,
            FechaExpiracion = fechaExpiracion ?? DateTime.Now.AddMinutes(30)
        };

    private static Funcion CrearFuncion(Guid funcionId, decimal precio, DateTime? fechaHora = null) => new()
    {
        Id = funcionId,
        Precio = precio,
        FechaHora = fechaHora ?? DateTime.Now.AddHours(2)
    };
}
