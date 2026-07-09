using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class AsientoRepositoryIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public AsientoRepositoryIntegrationTests(PostgresContainerFixture fixture)
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

    private async Task<Funcion> CrearFuncionBaseAsync(TicketCineDbContext context)
    {
        var pelicula = new Pelicula
        {
            Id = Guid.NewGuid(),
            Titulo = "Pelicula Test",
            Sinopsis = "Sinopsis de prueba",
            Genero = "Accion",
            DuracionMinutos = 120,
            Clasificacion = "PG-13",
            Activo = true
        };

        var sala = new Sala
        {
            Id = Guid.NewGuid(),
            Nombre = $"Sala Test {Guid.NewGuid()}",
            Filas = 10,
            Columnas = 10,
            Activo = true
        };

        context.Peliculas.Add(pelicula);
        context.Salas.Add(sala);
        await context.SaveChangesAsync();

        var funcion = new Funcion
        {
            Id = Guid.NewGuid(),
            PeliculaId = pelicula.Id,
            SalaId = sala.Id,
            FechaHora = DateTime.Now.AddDays(1),
            Precio = 20.00m,
            Activo = true
        };
        context.Funciones.Add(funcion);
        await context.SaveChangesAsync();

        return funcion;
    }

    [Fact]
    public async Task CrearAsync_DeberiaGuardarAsientoEnBaseDeDatos()
    {
        using var context = CrearContext();
        var funcion = await CrearFuncionBaseAsync(context);
        var repo = new AsientoRepository(context);

        var asiento = new Asiento
        {
            Id = Guid.NewGuid(),
            FuncionId = funcion.Id,
            Fila = 1,
            Columna = 1,
            Estado = EstadoAsiento.Libre
        };

        await repo.CrearAsync(asiento);

        var guardado = await context.Asientos.FindAsync(asiento.Id);
        Assert.NotNull(guardado);
        Assert.Equal(EstadoAsiento.Libre, guardado!.Estado);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaRetornarAsiento_CuandoExiste()
    {
        using var context = CrearContext();
        var funcion = await CrearFuncionBaseAsync(context);
        var asiento = new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 2, Columna = 3, Estado = EstadoAsiento.Libre };
        context.Asientos.Add(asiento);
        await context.SaveChangesAsync();

        var repo = new AsientoRepository(context);
        var resultado = await repo.ObtenerPorIdAsync(asiento.Id);

        Assert.NotNull(resultado);
        Assert.Equal(2, resultado!.Fila);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaRetornarNull_CuandoNoExiste()
    {
        using var context = CrearContext();
        var repo = new AsientoRepository(context);

        var resultado = await repo.ObtenerPorIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObtenerPorFuncionAsync_DeberiaRetornarAsientosOrdenados()
    {
        using var context = CrearContext();
        var funcion = await CrearFuncionBaseAsync(context);

        context.Asientos.AddRange(
            new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 2, Columna = 1, Estado = EstadoAsiento.Libre },
            new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 2, Estado = EstadoAsiento.Libre },
            new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 1, Estado = EstadoAsiento.Libre }
        );
        await context.SaveChangesAsync();

        var repo = new AsientoRepository(context);
        var resultado = (await repo.ObtenerPorFuncionAsync(funcion.Id)).ToList();

        Assert.Equal(3, resultado.Count);
        // Debe venir ordenado: fila 1 col 1, fila 1 col 2, fila 2 col 1
        Assert.Equal(1, resultado[0].Fila);
        Assert.Equal(1, resultado[0].Columna);
        Assert.Equal(1, resultado[1].Fila);
        Assert.Equal(2, resultado[1].Columna);
        Assert.Equal(2, resultado[2].Fila);
    }

    [Fact]
    public async Task CrearVariosAsync_DeberiaGuardarTodosLosAsientos()
    {
        using var context = CrearContext();
        var funcion = await CrearFuncionBaseAsync(context);
        var repo = new AsientoRepository(context);

        var asientos = new List<Asiento>
        {
            new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 1, Estado = EstadoAsiento.Libre },
            new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 2, Estado = EstadoAsiento.Libre }
        };

        await repo.CrearVariosAsync(asientos);

        var guardados = await context.Asientos.Where(a => a.FuncionId == funcion.Id).ToListAsync();
        Assert.Equal(2, guardados.Count);
    }

    [Fact]
    public async Task ActualizarAsync_DeberiaCambiarEstadoDelAsiento()
    {
        using var context = CrearContext();
        var funcion = await CrearFuncionBaseAsync(context);
        var asiento = new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 1, Estado = EstadoAsiento.Libre };
        context.Asientos.Add(asiento);
        await context.SaveChangesAsync();

        asiento.Estado = EstadoAsiento.Reservado;
        var repo = new AsientoRepository(context);
        await repo.ActualizarAsync(asiento);

        var actualizado = await context.Asientos.FindAsync(asiento.Id);
        Assert.Equal(EstadoAsiento.Reservado, actualizado!.Estado);
    }

    [Fact]
    public async Task EliminarAsync_DeberiaBorrarAsientoDeLaBaseDeDatos()
    {
        using var context = CrearContext();
        var funcion = await CrearFuncionBaseAsync(context);
        var asiento = new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 1, Estado = EstadoAsiento.Libre };
        context.Asientos.Add(asiento);
        await context.SaveChangesAsync();

        var repo = new AsientoRepository(context);
        await repo.EliminarAsync(asiento.Id);

        var eliminado = await context.Asientos.FindAsync(asiento.Id);
        Assert.Null(eliminado);
    }

    [Fact]
    public async Task EliminarAsync_NoDeberiaLanzarExcepcion_CuandoNoExiste()
    {
        using var context = CrearContext();
        var repo = new AsientoRepository(context);

        var excepcion = await Record.ExceptionAsync(() => repo.EliminarAsync(Guid.NewGuid()));

        Assert.Null(excepcion);
    }

    [Fact]
    public async Task ExisteAsync_DeberiaRetornarTrue_CuandoAsientoExiste()
    {
        using var context = CrearContext();
        var funcion = await CrearFuncionBaseAsync(context);
        var asiento = new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 1, Estado = EstadoAsiento.Libre };
        context.Asientos.Add(asiento);
        await context.SaveChangesAsync();

        var repo = new AsientoRepository(context);
        var existe = await repo.ExisteAsync(asiento.Id);

        Assert.True(existe);
    }

    [Fact]
    public async Task ExisteAsync_DeberiaRetornarFalse_CuandoNoExiste()
    {
        using var context = CrearContext();
        var repo = new AsientoRepository(context);

        var existe = await repo.ExisteAsync(Guid.NewGuid());

        Assert.False(existe);
    }

    [Fact]
    public async Task ContarLibresPorFuncionAsync_DeberiaContarSoloLosLibres()
    {
        using var context = CrearContext();
        var funcion = await CrearFuncionBaseAsync(context);

        context.Asientos.AddRange(
            new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 1, Estado = EstadoAsiento.Libre },
            new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 2, Estado = EstadoAsiento.Libre },
            new Asiento { Id = Guid.NewGuid(), FuncionId = funcion.Id, Fila = 1, Columna = 3, Estado = EstadoAsiento.Reservado }
        );
        await context.SaveChangesAsync();

        var repo = new AsientoRepository(context);
        var totalLibres = await repo.ContarLibresPorFuncionAsync(funcion.Id);

        Assert.Equal(2, totalLibres);
    }
}