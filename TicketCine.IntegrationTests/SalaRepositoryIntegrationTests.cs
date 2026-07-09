using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class SalaRepositoryIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public SalaRepositoryIntegrationTests(PostgresContainerFixture fixture)
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

    private Sala CrearSala(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = $"Sala {Guid.NewGuid()}",
        Filas = 10,
        Columnas = 10,
        Activo = activo
    };

    [Fact]
    public async Task CrearAsync_DeberiaGuardarSala()
    {
        using var context = CrearContext();
        var repo = new SalaRepository(context);
        var sala = CrearSala();

        await repo.CrearAsync(sala);

        var guardada = await context.Salas.FindAsync(sala.Id);
        Assert.NotNull(guardada);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaRetornarSala_CuandoExiste()
    {
        using var context = CrearContext();
        var sala = CrearSala();
        context.Salas.Add(sala);
        await context.SaveChangesAsync();

        var repo = new SalaRepository(context);
        var resultado = await repo.ObtenerPorIdAsync(sala.Id);

        Assert.NotNull(resultado);
        Assert.Equal(sala.Nombre, resultado!.Nombre);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaRetornarNull_CuandoNoExiste()
    {
        using var context = CrearContext();
        var repo = new SalaRepository(context);

        var resultado = await repo.ObtenerPorIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObtenerTodosAsync_DeberiaIncluirActivasEInactivas()
    {
        using var context = CrearContext();
        var activa = CrearSala(true);
        var inactiva = CrearSala(false);
        context.Salas.AddRange(activa, inactiva);
        await context.SaveChangesAsync();

        var repo = new SalaRepository(context);
        var resultado = await repo.ObtenerTodosAsync();

        Assert.Contains(resultado, s => s.Id == activa.Id);
        Assert.Contains(resultado, s => s.Id == inactiva.Id);
    }

    [Fact]
    public async Task ObtenerActivosAsync_DeberiaExcluirInactivas()
    {
        using var context = CrearContext();
        var activa = CrearSala(true);
        var inactiva = CrearSala(false);
        context.Salas.AddRange(activa, inactiva);
        await context.SaveChangesAsync();

        var repo = new SalaRepository(context);
        var resultado = await repo.ObtenerActivosAsync();

        Assert.Contains(resultado, s => s.Id == activa.Id);
        Assert.DoesNotContain(resultado, s => s.Id == inactiva.Id);
    }

    [Fact]
    public async Task ActualizarAsync_DeberiaModificarNombre()
    {
        using var context = CrearContext();
        var sala = CrearSala();
        context.Salas.Add(sala);
        await context.SaveChangesAsync();

        sala.Nombre = "Sala Modificada " + Guid.NewGuid();
        var repo = new SalaRepository(context);
        await repo.ActualizarAsync(sala);

        var actualizada = await context.Salas.FindAsync(sala.Id);
        Assert.StartsWith("Sala Modificada", actualizada!.Nombre);
    }

    [Fact]
    public async Task EliminarAsync_DeberiaBorrarSala()
    {
        using var context = CrearContext();
        var sala = CrearSala();
        context.Salas.Add(sala);
        await context.SaveChangesAsync();

        var repo = new SalaRepository(context);
        await repo.EliminarAsync(sala.Id);

        var eliminada = await context.Salas.FindAsync(sala.Id);
        Assert.Null(eliminada);
    }

    [Fact]
    public async Task ExisteAsync_DeberiaRetornarTrueYFalseCorrectamente()
    {
        using var context = CrearContext();
        var sala = CrearSala();
        context.Salas.Add(sala);
        await context.SaveChangesAsync();

        var repo = new SalaRepository(context);

        Assert.True(await repo.ExisteAsync(sala.Id));
        Assert.False(await repo.ExisteAsync(Guid.NewGuid()));
    }
}