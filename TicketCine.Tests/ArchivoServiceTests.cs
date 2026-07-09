using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using TicketCine.Infrastructure.Services;
using Xunit;

namespace TicketCine.Tests;

public class ArchivoServiceTests
{
    private static IConfiguration CrearConfiguracionValida()
    {
        var datos = new Dictionary<string, string?>
        {
            { "Cloudinary:CloudName", "cloud-de-prueba" },
            { "Cloudinary:ApiKey", "api-key-de-prueba" },
            { "Cloudinary:ApiSecret", "api-secret-de-prueba" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(datos)
            .Build();
    }

    private static IConfiguration CrearConfiguracionIncompleta()
    {
        var datos = new Dictionary<string, string?>
        {
            { "Cloudinary:CloudName", "cloud-de-prueba" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(datos)
            .Build();
    }

    private static IFormFile CrearArchivoFalso(string nombreArchivo, long tamanoBytes)
    {
        var mockArchivo = new Mock<IFormFile>();
        mockArchivo.Setup(a => a.FileName).Returns(nombreArchivo);
        mockArchivo.Setup(a => a.Length).Returns(tamanoBytes);
        return mockArchivo.Object;
    }

    [Fact]
    public void Constructor_DeberiaLanzarExcepcion_CuandoFaltaConfiguracionDeCloudinary()
    {
        var configuracionIncompleta = CrearConfiguracionIncompleta();

        Assert.Throws<InvalidOperationException>(() => new ArchivoService(configuracionIncompleta));
    }

    [Fact]
    public void Constructor_NoDeberiaLanzarExcepcion_CuandoConfiguracionEsValida()
    {
        var configuracionValida = CrearConfiguracionValida();

        var excepcion = Record.Exception(() => new ArchivoService(configuracionValida));

        Assert.Null(excepcion);
    }

    [Fact]
    public async Task GuardarPosterAsync_DeberiaLanzarExcepcion_CuandoArchivoEsNulo()
    {
        var servicio = new ArchivoService(CrearConfiguracionValida());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.GuardarPosterAsync(null!));
    }

    [Fact]
    public async Task GuardarPosterAsync_DeberiaLanzarExcepcion_CuandoArchivoEstaVacio()
    {
        var servicio = new ArchivoService(CrearConfiguracionValida());
        var archivoVacio = CrearArchivoFalso("poster.jpg", 0);

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.GuardarPosterAsync(archivoVacio));
    }

    [Fact]
    public async Task GuardarPosterAsync_DeberiaLanzarExcepcion_CuandoExtensionNoEstaPermitida()
    {
        var servicio = new ArchivoService(CrearConfiguracionValida());
        var archivoConExtensionInvalida = CrearArchivoFalso("documento.pdf", 1000);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.GuardarPosterAsync(archivoConExtensionInvalida));

        Assert.Contains(".pdf", excepcion.Message);
    }

    [Fact]
    public async Task GuardarPosterAsync_DeberiaLanzarExcepcion_CuandoExcedeTamanoMaximo()
    {
        var servicio = new ArchivoService(CrearConfiguracionValida());
        const long seisMB = 6 * 1024 * 1024;
        var archivoDemasiadoGrande = CrearArchivoFalso("poster.jpg", seisMB);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.GuardarPosterAsync(archivoDemasiadoGrande));

        Assert.Contains("5 MB", excepcion.Message);
    }

    [Fact]
    public void EliminarPoster_NoDeberiaLanzarExcepcion_CuandoRutaEsNulaOVacia()
    {
        var servicio = new ArchivoService(CrearConfiguracionValida());

        void Accion() => servicio.EliminarPoster(string.Empty);
        var excepcion = Record.Exception(Accion);

        Assert.Null(excepcion);
    }

    [Fact]
    public void EliminarPoster_NoDeberiaLanzarExcepcion_CuandoRutaEsNull()
    {
        var servicio = new ArchivoService(CrearConfiguracionValida());

        void Accion() => servicio.EliminarPoster(null!);
        var excepcion = Record.Exception(Accion);

        Assert.Null(excepcion);
    }
}