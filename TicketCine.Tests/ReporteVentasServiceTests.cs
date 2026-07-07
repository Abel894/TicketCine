using Moq;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Application.Services;
using TicketCine.Domain.Entities;

namespace TicketCine.Tests;

public class ReporteVentasServiceTests
{
    [Fact]
    public async Task GenerarAsync_ConDatosValidos_DebeCalcularTotalesYOrdenarPorIngresos()
    {
        var service = CrearService(out var ventaRepo);
        var fechaInicio = new DateTime(2026, 1, 1);
        var fechaFin = new DateTime(2026, 1, 31);
        var peliculaId = Guid.NewGuid();
        var request = new ReporteVentasRequest
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            PeliculaId = peliculaId
        };

        var ventas = new List<Venta>
        {
            CrearVenta("Pelicula B", 1, 10.125m),
            CrearVenta("Pelicula A", 2, 20m),
            CrearVenta("Pelicula A", 1, 5.335m)
        };

        ventaRepo
            .Setup(r => r.ObtenerParaReporteAsync(fechaInicio, fechaFin, peliculaId))
            .ReturnsAsync(ventas);

        var response = await service.GenerarAsync(request);

        Assert.Equal(4, response.TotalEntradas);
        Assert.Equal(35.46m, response.IngresoTotal);
        Assert.Equal(2, response.DetallePorPelicula.Count);
        Assert.Equal("Pelicula A", response.DetallePorPelicula[0].Titulo);
        Assert.Equal(3, response.DetallePorPelicula[0].EntradasVendidas);
        Assert.Equal(25.335m, response.DetallePorPelicula[0].Ingreso);
        Assert.Equal("Pelicula B", response.DetallePorPelicula[1].Titulo);
        Assert.Equal(1, response.DetallePorPelicula[1].EntradasVendidas);
        Assert.Equal(10.125m, response.DetallePorPelicula[1].Ingreso);

        ventaRepo.Verify(r => r.ObtenerParaReporteAsync(fechaInicio, fechaFin, peliculaId), Times.Once);
    }

    [Fact]
    public async Task GenerarAsync_ConUnSoloRegistro_DebeRetornarTotalesDelRegistro()
    {
        var service = CrearService(out var ventaRepo);
        var fechaInicio = new DateTime(2026, 2, 1);
        var fechaFin = new DateTime(2026, 2, 28);
        var request = new ReporteVentasRequest
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            PeliculaId = null
        };

        var ventas = new List<Venta>
        {
            CrearVenta("Una Pelicula", 1, 18.50m)
        };

        ventaRepo
            .Setup(r => r.ObtenerParaReporteAsync(fechaInicio, fechaFin, null))
            .ReturnsAsync(ventas);

        var response = await service.GenerarAsync(request);

        Assert.Equal(1, response.TotalEntradas);
        Assert.Equal(18.50m, response.IngresoTotal);
        Assert.Single(response.DetallePorPelicula);
        Assert.Equal("Una Pelicula", response.DetallePorPelicula[0].Titulo);
        Assert.Equal(1, response.DetallePorPelicula[0].EntradasVendidas);
        Assert.Equal(18.50m, response.DetallePorPelicula[0].Ingreso);

        ventaRepo.Verify(r => r.ObtenerParaReporteAsync(fechaInicio, fechaFin, null), Times.Once);
    }

    [Fact]
    public async Task GenerarAsync_ConFiltrosVacios_DebeRetornarReporteVacio()
    {
        var service = CrearService(out var ventaRepo);
        var request = new ReporteVentasRequest();

        ventaRepo
            .Setup(r => r.ObtenerParaReporteAsync(null, null, null))
            .ReturnsAsync(Enumerable.Empty<Venta>());

        var response = await service.GenerarAsync(request);

        Assert.Equal(0, response.TotalEntradas);
        Assert.Equal(0m, response.IngresoTotal);
        Assert.Empty(response.DetallePorPelicula);

        ventaRepo.Verify(r => r.ObtenerParaReporteAsync(null, null, null), Times.Once);
    }

    [Fact]
    public async Task GenerarAsync_ConRangoFechasInvalido_DebeRetornarCerosCuandoNoHayResultados()
    {
        var service = CrearService(out var ventaRepo);
        var fechaInicio = new DateTime(2026, 3, 31);
        var fechaFin = new DateTime(2026, 3, 1);
        var request = new ReporteVentasRequest
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            PeliculaId = null
        };

        ventaRepo
            .Setup(r => r.ObtenerParaReporteAsync(fechaInicio, fechaFin, null))
            .ReturnsAsync(Enumerable.Empty<Venta>());

        var response = await service.GenerarAsync(request);

        Assert.Equal(0, response.TotalEntradas);
        Assert.Equal(0m, response.IngresoTotal);
        Assert.Empty(response.DetallePorPelicula);

        ventaRepo.Verify(r => r.ObtenerParaReporteAsync(fechaInicio, fechaFin, null), Times.Once);
    }

    [Fact]
    public async Task GenerarAsync_ConRequestNulo_DebeLanzarArgumentNullException()
    {
        var service = CrearService(out _);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GenerarAsync(null!));
    }

    private static ReporteVentasService CrearService(out Mock<IVentaRepository> ventaRepo)
    {
        ventaRepo = new Mock<IVentaRepository>();
        ventaRepo
            .Setup(r => r.ObtenerParaReporteAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Enumerable.Empty<Venta>());

        return new ReporteVentasService(ventaRepo.Object);
    }

    private static Venta CrearVenta(string tituloPelicula, int cantidadAsientos, decimal montoTotal)
    {
        return new Venta
        {
            Id = Guid.NewGuid(),
            MontoTotal = montoTotal,
            Reserva = new Reserva
            {
                Id = Guid.NewGuid(),
                Asientos = Enumerable.Range(0, cantidadAsientos)
                    .Select(_ => new AsientoReserva { Id = Guid.NewGuid() })
                    .ToList(),
                Funcion = new Funcion
                {
                    Id = Guid.NewGuid(),
                    Pelicula = new Pelicula
                    {
                        Id = Guid.NewGuid(),
                        Titulo = tituloPelicula
                    }
                }
            }
        };
    }
}
