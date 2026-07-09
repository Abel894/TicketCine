using Microsoft.EntityFrameworkCore;
using TicketCine.Application.DTOs;
using TicketCine.Application.Services;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class ReservaServiceIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public ReservaServiceIntegrationTests(PostgresContainerFixture fixture)
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

    private async Task<(Usuario usuario, Funcion funcion, Asiento asiento)> CrearDatosBaseAsync(TicketCineDbContext context)
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
            Nombre = "Sala Test",
            Filas = 10,
            Columnas = 10,
            Activo = true
        };

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Usuario Test",
            Correo = $"test_{Guid.NewGuid()}@test.com",
            PasswordHash = "hash_falso",
            RolId = 1,
            Activo = true
        };

        var funcion = new Funcion
        {
            Id = Guid.NewGuid(),
            PeliculaId = pelicula.Id,
            SalaId = sala.Id,
            FechaHora = DateTime.Now.AddHours(3),
            Precio = 20.00m,
            Activo = true
        };

        var asiento = new Asiento
        {
            Id = Guid.NewGuid(),
            FuncionId = funcion.Id,
            Fila = 1,
            Columna = 1,
            Estado = EstadoAsiento.Libre
        };

        context.Peliculas.Add(pelicula);
        context.Salas.Add(sala);
        context.Usuarios.Add(usuario);
        context.Funciones.Add(funcion);
        context.Asientos.Add(asiento);
        await context.SaveChangesAsync();

        return (usuario, funcion, asiento);
    }

    [Fact]
    public async Task CrearReserva_ConAsientoLibre_DebePersistirReservaYActualizarAsientoEnBaseDeDatosReal()
    {
        using var context = CrearContext();
        var (usuario, funcion, asiento) = await CrearDatosBaseAsync(context);

        var reservaRepo = new ReservaRepository(context);
        var asientoRepo = new AsientoRepository(context);
        var funcionRepo = new FuncionRepository(context);
        var usuarioRepo = new UsuarioRepository(context);

        var service = new ReservaService(reservaRepo, asientoRepo, funcionRepo, usuarioRepo);

        var request = new CrearReservaRequest
        {
            FuncionId = funcion.Id,
            AsientoIds = new List<Guid> { asiento.Id }
        };

        var response = await service.CrearReservaAsync(usuario.Id, request);

        using var contextVerificacion = CrearContext();

        var reservaGuardada = await contextVerificacion.Reservas.FindAsync(response.ReservaId);
        Assert.NotNull(reservaGuardada);
        Assert.Equal(EstadoReserva.Pendiente, reservaGuardada!.Estado);
        Assert.Equal(usuario.Id, reservaGuardada.UsuarioId);

        var asientoActualizado = await contextVerificacion.Asientos.FindAsync(asiento.Id);
        Assert.NotNull(asientoActualizado);
        Assert.Equal(EstadoAsiento.Reservado, asientoActualizado!.Estado);
    }

    [Fact]
    public async Task Cancelar_ConReservaPendiente_DebeLiberarAsientoEnBaseDeDatosReal()
    {
        using var context = CrearContext();
        var (usuario, funcion, asiento) = await CrearDatosBaseAsync(context);

        var reservaRepo = new ReservaRepository(context);
        var asientoRepo = new AsientoRepository(context);
        var funcionRepo = new FuncionRepository(context);
        var usuarioRepo = new UsuarioRepository(context);

        var service = new ReservaService(reservaRepo, asientoRepo, funcionRepo, usuarioRepo);

        var request = new CrearReservaRequest
        {
            FuncionId = funcion.Id,
            AsientoIds = new List<Guid> { asiento.Id }
        };

        var response = await service.CrearReservaAsync(usuario.Id, request);

        await service.CancelarAsync(usuario.Id, response.ReservaId);

        using var contextVerificacion = CrearContext();

        var reservaCancelada = await contextVerificacion.Reservas.FindAsync(response.ReservaId);
        Assert.Equal(EstadoReserva.Cancelada, reservaCancelada!.Estado);

        var asientoLiberado = await contextVerificacion.Asientos.FindAsync(asiento.Id);
        Assert.Equal(EstadoAsiento.Libre, asientoLiberado!.Estado);
    }
}