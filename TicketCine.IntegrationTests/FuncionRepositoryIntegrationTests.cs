using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class FuncionRepositoryIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public FuncionRepositoryIntegrationTests(PostgresContainerFixture fixture)
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

    private async Task<(Pelicula pelicula, Sala sala)> CrearDatosBaseAsync(TicketCineDbContext context)
    {
        var pelicula = new Pelicula
        {
            Id = Guid.NewGuid(),
            Titulo = "Pelicula Test",
            Sinopsis = "Sinopsis de prueba",
            Genero = "Accion",
            DuracionMinutos = 120, // 2 horas
            Clasificacion = "PG-13",
            Activo = true
        };

        var sala = new Sala
        {
            Id = Guid.NewGuid(),
            Nombre = "Sala Test",
            Filas = 10,
            Columnas = 10,
            Activo = true
        };

        context.Peliculas.Add(pelicula);
        context.Salas.Add(sala);
        await context.SaveChangesAsync();

        return (pelicula, sala);
    }

    [Fact]
    public async Task ExisteConflictoHorario_ConHorarioSolapado_DebeRetornarTrue()
    {
        using var context = CrearContext();
        var (pelicula, sala) = await CrearDatosBaseAsync(context);

        var funcionExistente = new Funcion
        {
            Id = Guid.NewGuid(),
            PeliculaId = pelicula.Id,
            SalaId = sala.Id,
            FechaHora = new DateTime(2026, 8, 1, 18, 0, 0), // 6:00 PM, dura 2h → termina 8:00 PM
            Precio = 20.00m,
            Activo = true
        };

        context.Funciones.Add(funcionExistente);
        await context.SaveChangesAsync();

        var repo = new FuncionRepository(context);

        // Nueva función propuesta a las 7:00 PM (se solapa con la de 6:00-8:00 PM)
        var nuevaFechaHora = new DateTime(2026, 8, 1, 19, 0, 0);

        // Act
        var existeConflicto = await repo.ExisteConflictoHorarioAsync(sala.Id, nuevaFechaHora, pelicula.DuracionMinutos);

        // Assert
        Assert.True(existeConflicto);
    }

    [Fact]
    public async Task ExisteConflictoHorario_ConHorarioLibre_DebeRetornarFalse()
    {
        using var context = CrearContext();
        var (pelicula, sala) = await CrearDatosBaseAsync(context);

        var funcionExistente = new Funcion
        {
            Id = Guid.NewGuid(),
            PeliculaId = pelicula.Id,
            SalaId = sala.Id,
            FechaHora = new DateTime(2026, 8, 1, 18, 0, 0), // 6:00 PM, dura 2h → termina 8:00 PM
            Precio = 20.00m,
            Activo = true
        };

        context.Funciones.Add(funcionExistente);
        await context.SaveChangesAsync();

        var repo = new FuncionRepository(context);

        // Nueva función propuesta a las 9:00 PM (empieza 1h después de que termina la anterior)
        var nuevaFechaHora = new DateTime(2026, 8, 1, 21, 0, 0);

        // Act
        var existeConflicto = await repo.ExisteConflictoHorarioAsync(sala.Id, nuevaFechaHora, pelicula.DuracionMinutos);

        // Assert
        Assert.False(existeConflicto);
    }
}