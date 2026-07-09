using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class PeliculaRepositoryIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public PeliculaRepositoryIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private TicketCineDbContext CrearContext()
    {
        var options = new DbContextOptionsBuilder<TicketCineDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        var context = new TicketCineDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private Pelicula CrearPelicula(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Titulo = $"Pelicula {Guid.NewGuid()}",
        Sinopsis = "Sinopsis de prueba",
        Genero = "Accion",
        DuracionMinutos = 120,
        Clasificacion = "PG-13",
        Activo = activo
    };

    [Fact]
    public async Task CrearAsync_DeberiaGuardarPelicula()
    {
        using var context = CrearContext();
        var repo = new PeliculaRepository(context);
        var pelicula = CrearPelicula();

        await repo.CrearAsync(pelicula);

        var guardada = await context.Peliculas.FindAsync(pelicula.Id);
        Assert.NotNull(guardada);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaRetornarPelicula_CuandoExiste()
    {
        using var context = CrearContext();
        var pelicula = CrearPelicula();
        context.Peliculas.Add(pelicula);
        await context.SaveChangesAsync();

        var repo = new PeliculaRepository(context);
        var resultado = await repo.ObtenerPorIdAsync(pelicula.Id);

        Assert.NotNull(resultado);
        Assert.Equal(pelicula.Titulo, resultado!.Titulo);
    }

    [Fact]
    public async Task ObtenerTodosAsync_DeberiaIncluirActivosEInactivos()
    {
        using var context = CrearContext();
        var activa = CrearPelicula(true);
        var inactiva = CrearPelicula(false);
        context.Peliculas.AddRange(activa, inactiva);
        await context.SaveChangesAsync();

        var repo = new PeliculaRepository(context);
        var resultado = await repo.ObtenerTodosAsync();

        Assert.Contains(resultado, p => p.Id == activa.Id);
        Assert.Contains(resultado, p => p.Id == inactiva.Id);
    }

    [Fact]
    public async Task ObtenerActivosAsync_DeberiaExcluirInactivas()
    {
        using var context = CrearContext();
        var activa = CrearPelicula(true);
        var inactiva = CrearPelicula(false);
        context.Peliculas.AddRange(activa, inactiva);
        await context.SaveChangesAsync();

        var repo = new PeliculaRepository(context);
        var resultado = await repo.ObtenerActivosAsync();

        Assert.Contains(resultado, p => p.Id == activa.Id);
        Assert.DoesNotContain(resultado, p => p.Id == inactiva.Id);
    }

    [Fact]
    public async Task ObtenerConFuncionesActivasAsync_DeberiaRetornarSoloConFuncionActiva()
    {
        using var context = CrearContext();
        var conFuncion = CrearPelicula();
        var sinFuncion = CrearPelicula();
        context.Peliculas.AddRange(conFuncion, sinFuncion);

        var sala = new Sala { Id = Guid.NewGuid(), Nombre = $"Sala {Guid.NewGuid()}", Filas = 5, Columnas = 5, Activo = true };
        context.Salas.Add(sala);
        await context.SaveChangesAsync();

        context.Funciones.Add(new Funcion
        {
            Id = Guid.NewGuid(),
            PeliculaId = conFuncion.Id,
            SalaId = sala.Id,
            FechaHora = DateTime.Now.AddDays(1),
            Precio = 15m,
            Activo = true
        });
        await context.SaveChangesAsync();

        var repo = new PeliculaRepository(context);
        var resultado = await repo.ObtenerConFuncionesActivasAsync();

        Assert.Contains(resultado, p => p.Id == conFuncion.Id);
        Assert.DoesNotContain(resultado, p => p.Id == sinFuncion.Id);
    }

    [Fact]
    public async Task ObtenerDestacadasConFuncionesProximasAsync_DeberiaRespetarLimiteYOrden()
    {
        using var context = CrearContext();
        var sala = new Sala { Id = Guid.NewGuid(), Nombre = $"Sala {Guid.NewGuid()}", Filas = 5, Columnas = 5, Activo = true };
        context.Salas.Add(sala);

        var peliculaLejana = CrearPelicula();
        var peliculaCercana = CrearPelicula();
        context.Peliculas.AddRange(peliculaLejana, peliculaCercana);
        await context.SaveChangesAsync();

        context.Funciones.AddRange(
            new Funcion { Id = Guid.NewGuid(), PeliculaId = peliculaLejana.Id, SalaId = sala.Id, FechaHora = DateTime.Now.AddDays(10), Precio = 15m, Activo = true },
            new Funcion { Id = Guid.NewGuid(), PeliculaId = peliculaCercana.Id, SalaId = sala.Id, FechaHora = DateTime.Now.AddDays(1), Precio = 15m, Activo = true }
        );
        await context.SaveChangesAsync();

        var repo = new PeliculaRepository(context);

        // En vez de pedir solo 1 resultado (que puede ser "robado" por datos de otras
        // pruebas en la BD compartida), pedimos varios y verificamos SOLO el orden
        // relativo entre nuestras propias películas.
        var resultado = (await repo.ObtenerDestacadasConFuncionesProximasAsync(100)).ToList();

        var indiceCercana = resultado.FindIndex(p => p.Id == peliculaCercana.Id);
        var indiceLejana = resultado.FindIndex(p => p.Id == peliculaLejana.Id);

        Assert.True(indiceCercana >= 0, "La película cercana debería estar en el resultado.");
        Assert.True(indiceLejana >= 0, "La película lejana debería estar en el resultado.");
        Assert.True(indiceCercana < indiceLejana, "La película con función más próxima debe aparecer primero.");
    }

    [Fact]
    public async Task ActualizarAsync_DeberiaModificarTitulo()
    {
        using var context = CrearContext();
        var pelicula = CrearPelicula();
        context.Peliculas.Add(pelicula);
        await context.SaveChangesAsync();

        pelicula.Titulo = "Titulo Modificado " + Guid.NewGuid();
        var repo = new PeliculaRepository(context);
        await repo.ActualizarAsync(pelicula);

        var actualizada = await context.Peliculas.FindAsync(pelicula.Id);
        Assert.StartsWith("Titulo Modificado", actualizada!.Titulo);
    }

    [Fact]
    public async Task EliminarAsync_DeberiaBorrarPelicula()
    {
        using var context = CrearContext();
        var pelicula = CrearPelicula();
        context.Peliculas.Add(pelicula);
        await context.SaveChangesAsync();

        var repo = new PeliculaRepository(context);
        await repo.EliminarAsync(pelicula.Id);

        var eliminada = await context.Peliculas.FindAsync(pelicula.Id);
        Assert.Null(eliminada);
    }

    [Fact]
    public async Task ExisteAsync_DeberiaRetornarTrueYFalseCorrectamente()
    {
        using var context = CrearContext();
        var pelicula = CrearPelicula();
        context.Peliculas.Add(pelicula);
        await context.SaveChangesAsync();

        var repo = new PeliculaRepository(context);

        Assert.True(await repo.ExisteAsync(pelicula.Id));
        Assert.False(await repo.ExisteAsync(Guid.NewGuid()));
    }
}