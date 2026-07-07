using Moq;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Application.Services;
using TicketCine.Domain.Entities;

namespace TicketCine.Tests;

public class ReservaServiceTests
{
    [Fact]
    public async Task CrearReserva_ConDatosValidos_DebeCrearReservaYReservarAsientos()
    {
        var service = CrearService(out var reservaRepo, out var asientoRepo, out var funcionRepo, out var usuarioRepo);
        var usuarioId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asiento1Id = Guid.NewGuid();
        var asiento2Id = Guid.NewGuid();

        var request = new CrearReservaRequest
        {
            FuncionId = funcionId,
            AsientoIds = new List<Guid> { asiento1Id, asiento2Id }
        };

        usuarioRepo
            .Setup(r => r.ObtenerPorIdAsync(usuarioId))
            .ReturnsAsync(new Usuario { Id = usuarioId, Activo = true });

        funcionRepo
            .Setup(r => r.ObtenerPorIdAsync(funcionId))
            .ReturnsAsync(new Funcion { Id = funcionId, FechaHora = DateTime.Now.AddHours(3) });

        reservaRepo
            .Setup(r => r.ContarAsientosReservadosPorFuncionYUsuarioAsync(funcionId, usuarioId))
            .ReturnsAsync(0);

        var asiento1 = new Asiento { Id = asiento1Id, FuncionId = funcionId, Estado = EstadoAsiento.Libre };
        var asiento2 = new Asiento { Id = asiento2Id, FuncionId = funcionId, Estado = EstadoAsiento.Libre };

        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asiento1Id)).ReturnsAsync(asiento1);
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asiento2Id)).ReturnsAsync(asiento2);

        reservaRepo.Setup(r => r.CrearAsync(It.IsAny<Reserva>())).ReturnsAsync((Reserva r) => r);
        reservaRepo.Setup(r => r.CrearAsientosReservaAsync(It.IsAny<IEnumerable<AsientoReserva>>())).Returns(Task.CompletedTask);
        asientoRepo.Setup(r => r.ActualizarAsync(It.IsAny<Asiento>())).Returns(Task.CompletedTask);

        var response = await service.CrearReservaAsync(usuarioId, request);

        Assert.NotEqual(Guid.Empty, response.ReservaId);
        Assert.Equal(funcionId, response.FuncionId);
        Assert.Equal(EstadoReserva.Pendiente, response.Estado);
        Assert.Equal(2, response.AsientoIds.Count);
        Assert.Contains(asiento1Id, response.AsientoIds);
        Assert.Contains(asiento2Id, response.AsientoIds);

        asientoRepo.Verify(r => r.ActualizarAsync(It.Is<Asiento>(a => a.Id == asiento1Id && a.Estado == EstadoAsiento.Reservado)), Times.Once);
        asientoRepo.Verify(r => r.ActualizarAsync(It.Is<Asiento>(a => a.Id == asiento2Id && a.Estado == EstadoAsiento.Reservado)), Times.Once);
    }

    [Fact]
    public async Task CrearReserva_ConCeroAsientos_DebeLanzarArgumentException()
    {
        var service = CrearService(out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();

        var request = new CrearReservaRequest
        {
            FuncionId = Guid.NewGuid(),
            AsientoIds = new List<Guid>()
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CrearReservaAsync(usuarioId, request));
    }

    [Fact]
    public async Task CrearReserva_ConMasDe8Asientos_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out _, out _, out _, out _);
        var usuarioId = Guid.NewGuid();

        var request = new CrearReservaRequest
        {
            FuncionId = Guid.NewGuid(),
            AsientoIds = Enumerable.Range(0, 9).Select(_ => Guid.NewGuid()).ToList()
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CrearReservaAsync(usuarioId, request));
    }

    [Fact]
    public async Task CrearReserva_ConExactamente8Asientos_DebeCrearReserva()
    {
        var service = CrearService(out var reservaRepo, out var asientoRepo, out var funcionRepo, out var usuarioRepo);
        var usuarioId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asientoIds = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToList();

        var request = new CrearReservaRequest
        {
            FuncionId = funcionId,
            AsientoIds = asientoIds
        };

        usuarioRepo
            .Setup(r => r.ObtenerPorIdAsync(usuarioId))
            .ReturnsAsync(new Usuario { Id = usuarioId, Activo = true });

        funcionRepo
            .Setup(r => r.ObtenerPorIdAsync(funcionId))
            .ReturnsAsync(new Funcion { Id = funcionId, FechaHora = DateTime.Now.AddHours(4) });

        reservaRepo
            .Setup(r => r.ContarAsientosReservadosPorFuncionYUsuarioAsync(funcionId, usuarioId))
            .ReturnsAsync(0);

        foreach (var asientoId in asientoIds)
        {
            asientoRepo
                .Setup(r => r.ObtenerPorIdAsync(asientoId))
                .ReturnsAsync(new Asiento { Id = asientoId, FuncionId = funcionId, Estado = EstadoAsiento.Libre });
        }

        reservaRepo.Setup(r => r.CrearAsync(It.IsAny<Reserva>())).ReturnsAsync((Reserva r) => r);
        reservaRepo.Setup(r => r.CrearAsientosReservaAsync(It.IsAny<IEnumerable<AsientoReserva>>())).Returns(Task.CompletedTask);
        asientoRepo.Setup(r => r.ActualizarAsync(It.IsAny<Asiento>())).Returns(Task.CompletedTask);

        var response = await service.CrearReservaAsync(usuarioId, request);

        Assert.Equal(8, response.AsientoIds.Count);
        asientoRepo.Verify(r => r.ActualizarAsync(It.IsAny<Asiento>()), Times.Exactly(8));
    }

    [Fact]
    public async Task CrearReserva_ConFuncionInexistente_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var reservaRepo, out _, out var funcionRepo, out var usuarioRepo);
        var usuarioId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();

        usuarioRepo.Setup(r => r.ObtenerPorIdAsync(usuarioId)).ReturnsAsync(new Usuario { Id = usuarioId, Activo = true });
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync((Funcion?)null);

        var request = new CrearReservaRequest
        {
            FuncionId = funcionId,
            AsientoIds = new List<Guid> { Guid.NewGuid() }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CrearReservaAsync(usuarioId, request));

        reservaRepo.Verify(r => r.CrearAsync(It.IsAny<Reserva>()), Times.Never);
    }

    [Fact]
    public async Task CrearReserva_ConAsientosYaOcupados_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var reservaRepo, out var asientoRepo, out var funcionRepo, out var usuarioRepo);
        var usuarioId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asientoId = Guid.NewGuid();

        usuarioRepo.Setup(r => r.ObtenerPorIdAsync(usuarioId)).ReturnsAsync(new Usuario { Id = usuarioId, Activo = true });
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(new Funcion { Id = funcionId, FechaHora = DateTime.Now.AddHours(2) });
        reservaRepo.Setup(r => r.ContarAsientosReservadosPorFuncionYUsuarioAsync(funcionId, usuarioId)).ReturnsAsync(0);
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoId)).ReturnsAsync(new Asiento { Id = asientoId, FuncionId = funcionId, Estado = EstadoAsiento.Reservado });

        var request = new CrearReservaRequest
        {
            FuncionId = funcionId,
            AsientoIds = new List<Guid> { asientoId }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CrearReservaAsync(usuarioId, request));
    }

    [Fact]
    public async Task ObtenerReservaPorId_ConReservaDelUsuario_DebeRetornarReserva()
    {
        var service = CrearService(out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var reserva = new Reserva { Id = reservaId, UsuarioId = usuarioId };

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(reserva);

        var result = await service.ObtenerReservaPorIdAsync(usuarioId, reservaId);

        Assert.NotNull(result);
        Assert.Equal(reservaId, result!.Id);
    }

    [Fact]
    public async Task ObtenerReservaPorId_ConReservaDeOtroUsuario_DebeRetornarNull()
    {
        var service = CrearService(out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(new Reserva { Id = reservaId, UsuarioId = Guid.NewGuid() });

        var result = await service.ObtenerReservaPorIdAsync(usuarioId, reservaId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ObtenerReservasActivas_ConUsuarioValido_DebeRetornarReservasActivas()
    {
        var service = CrearService(out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservas = new List<Reserva>
        {
            new() { Id = Guid.NewGuid(), UsuarioId = usuarioId, Estado = EstadoReserva.Pendiente }
        };

        reservaRepo.Setup(r => r.ObtenerActivasPorUsuarioAsync(usuarioId)).ReturnsAsync(reservas);

        var result = (await service.ObtenerReservasActivasAsync(usuarioId)).ToList();

        Assert.Single(result);
        Assert.Equal(reservas[0].Id, result[0].Id);
    }

    [Fact]
    public async Task ObtenerReservasActivas_ConUsuarioNoAutenticado_DebeLanzarUnauthorizedAccessException()
    {
        var service = CrearService(out _, out _, out _, out _);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ObtenerReservasActivasAsync(Guid.Empty));
    }

    [Fact]
    public async Task ObtenerReservasExpiradas_ConUsuarioValido_DebeRetornarReservasExpiradas()
    {
        var service = CrearService(out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservas = new List<Reserva>
        {
            new() { Id = Guid.NewGuid(), UsuarioId = usuarioId, Estado = EstadoReserva.Expirada }
        };

        reservaRepo.Setup(r => r.ObtenerExpiradasPorUsuarioAsync(usuarioId)).ReturnsAsync(reservas);

        var result = (await service.ObtenerReservasExpiradasAsync(usuarioId)).ToList();

        Assert.Single(result);
        Assert.Equal(EstadoReserva.Expirada, result[0].Estado);
    }

    [Fact]
    public async Task ObtenerReservasExpiradas_ConUsuarioNoAutenticado_DebeLanzarUnauthorizedAccessException()
    {
        var service = CrearService(out _, out _, out _, out _);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ObtenerReservasExpiradasAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExpirarReservasVencidas_SinReservasVencidas_DebeRetornarCero()
    {
        var service = CrearService(out _, out _, out _, out _);

        var result = await service.ExpirarReservasVencidasAsync();

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExpirarReservasVencidas_ConReservasVencidas_DebeExpirarYLiberarAsientos()
    {
        var service = CrearService(out var reservaRepo, out var asientoRepo, out _, out _);
        var reservaId = Guid.NewGuid();
        var asientoReservadoId = Guid.NewGuid();
        var asientoVendidoId = Guid.NewGuid();

        var reserva = new Reserva { Id = reservaId, Estado = EstadoReserva.Pendiente };
        var asientoReservado = new Asiento { Id = asientoReservadoId, Estado = EstadoAsiento.Reservado };
        var asientoVendido = new Asiento { Id = asientoVendidoId, Estado = EstadoAsiento.Vendido };

        reservaRepo.Setup(r => r.ObtenerPendientesVencidasAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<Reserva> { reserva });
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asientoReservadoId },
            new() { ReservaId = reservaId, AsientoId = asientoVendidoId }
        });

        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoReservadoId)).ReturnsAsync(asientoReservado);
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoVendidoId)).ReturnsAsync(asientoVendido);
        asientoRepo.Setup(r => r.ActualizarAsync(It.IsAny<Asiento>())).Returns(Task.CompletedTask);
        reservaRepo.Setup(r => r.ActualizarAsync(It.IsAny<Reserva>())).Returns(Task.CompletedTask);

        var result = await service.ExpirarReservasVencidasAsync();

        Assert.Equal(1, result);
        Assert.Equal(EstadoReserva.Expirada, reserva.Estado);
        Assert.Equal(EstadoAsiento.Libre, asientoReservado.Estado);

        asientoRepo.Verify(r => r.ActualizarAsync(It.Is<Asiento>(a => a.Id == asientoReservadoId && a.Estado == EstadoAsiento.Libre)), Times.Once);
        asientoRepo.Verify(r => r.ActualizarAsync(It.Is<Asiento>(a => a.Id == asientoVendidoId)), Times.Never);
        reservaRepo.Verify(r => r.ActualizarAsync(It.Is<Reserva>(x => x.Id == reservaId && x.Estado == EstadoReserva.Expirada)), Times.Once);
    }

    [Fact]
    public async Task Cancelar_ConReservaPendienteYValida_DebeCancelarYLiberarAsientos()
    {
        var service = CrearService(out var reservaRepo, out var asientoRepo, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var asientoId = Guid.NewGuid();

        var reserva = new Reserva { Id = reservaId, UsuarioId = usuarioId, FuncionId = funcionId, Estado = EstadoReserva.Pendiente };
        var asiento = new Asiento { Id = asientoId, FuncionId = funcionId, Estado = EstadoAsiento.Reservado };

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync(reserva);
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(new Funcion { Id = funcionId, FechaHora = DateTime.Now.AddHours(2) });
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asientoId }
        });
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoId)).ReturnsAsync(asiento);
        asientoRepo.Setup(r => r.ActualizarAsync(It.IsAny<Asiento>())).Returns(Task.CompletedTask);
        reservaRepo.Setup(r => r.ActualizarAsync(It.IsAny<Reserva>())).Returns(Task.CompletedTask);

        await service.CancelarAsync(usuarioId, reservaId);

        Assert.Equal(EstadoReserva.Cancelada, reserva.Estado);
        Assert.Equal(EstadoAsiento.Libre, asiento.Estado);

        asientoRepo.Verify(r => r.ActualizarAsync(It.Is<Asiento>(a => a.Id == asientoId && a.Estado == EstadoAsiento.Libre)), Times.Once);
        reservaRepo.Verify(r => r.ActualizarAsync(It.Is<Reserva>(x => x.Id == reservaId && x.Estado == EstadoReserva.Cancelada)), Times.Once);
    }

    [Fact]
    public async Task Cancelar_ConReservaNoEncontrada_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId)).ReturnsAsync((Reserva?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelarAsync(usuarioId, reservaId));
    }

    [Fact]
    public async Task Cancelar_ConReservaDeOtroUsuario_DebeLanzarUnauthorizedAccessException()
    {
        var service = CrearService(out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(new Reserva { Id = reservaId, UsuarioId = Guid.NewGuid(), Estado = EstadoReserva.Pendiente });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CancelarAsync(usuarioId, reservaId));
    }

    [Fact]
    public async Task Cancelar_ConReservaNoPendiente_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var reservaRepo, out _, out _, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(new Reserva { Id = reservaId, UsuarioId = usuarioId, Estado = EstadoReserva.Confirmada });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelarAsync(usuarioId, reservaId));
    }

    [Fact]
    public async Task Cancelar_ConFuncionInexistente_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var reservaRepo, out _, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(new Reserva { Id = reservaId, UsuarioId = usuarioId, FuncionId = funcionId, Estado = EstadoReserva.Pendiente });
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync((Funcion?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelarAsync(usuarioId, reservaId));
    }

    [Fact]
    public async Task Cancelar_ConMenosDeUnaHoraParaFuncion_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var reservaRepo, out _, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(new Reserva { Id = reservaId, UsuarioId = usuarioId, FuncionId = funcionId, Estado = EstadoReserva.Pendiente });
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(new Funcion { Id = funcionId, FechaHora = DateTime.Now.AddMinutes(45) });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelarAsync(usuarioId, reservaId));
    }

    [Fact]
    public async Task Cancelar_ConAsientoVendido_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var reservaRepo, out var asientoRepo, out var funcionRepo, out _);
        var usuarioId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var funcionId = Guid.NewGuid();
        var asientoId = Guid.NewGuid();

        reservaRepo.Setup(r => r.ObtenerPorIdAsync(reservaId))
            .ReturnsAsync(new Reserva { Id = reservaId, UsuarioId = usuarioId, FuncionId = funcionId, Estado = EstadoReserva.Pendiente });
        funcionRepo.Setup(r => r.ObtenerPorIdAsync(funcionId)).ReturnsAsync(new Funcion { Id = funcionId, FechaHora = DateTime.Now.AddHours(2) });
        reservaRepo.Setup(r => r.ObtenerAsientosPorReservaAsync(reservaId)).ReturnsAsync(new List<AsientoReserva>
        {
            new() { ReservaId = reservaId, AsientoId = asientoId }
        });
        asientoRepo.Setup(r => r.ObtenerPorIdAsync(asientoId)).ReturnsAsync(new Asiento { Id = asientoId, FuncionId = funcionId, Estado = EstadoAsiento.Vendido });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelarAsync(usuarioId, reservaId));
    }

    private static ReservaService CrearService(
        out Mock<IReservaRepository> reservaRepo,
        out Mock<IAsientoRepository> asientoRepo,
        out Mock<IFuncionRepository> funcionRepo,
        out Mock<IUsuarioRepository> usuarioRepo)
    {
        reservaRepo = new Mock<IReservaRepository>();
        asientoRepo = new Mock<IAsientoRepository>();
        funcionRepo = new Mock<IFuncionRepository>();
        usuarioRepo = new Mock<IUsuarioRepository>();

        reservaRepo
            .Setup(r => r.ObtenerPendientesVencidasAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Reserva>());

        return new ReservaService(reservaRepo.Object, asientoRepo.Object, funcionRepo.Object, usuarioRepo.Object);
    }
}
