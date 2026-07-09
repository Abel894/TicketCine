using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using TicketCine.Application.Services;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class AsientoServiceIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public AsientoServiceIntegrationTests(PostgresContainerFixture fixture)
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

    [Fact]
    public async Task ExpirarReservasVencidas_ConReservaExpirada_DebeLiberarAsientoEnBaseDeDatosReal()
    {
        using var context = CrearContext();

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
            Estado = EstadoAsiento.Reservado // ya reservado, simulando una reserva previa
        };

        // Reserva YA EXPIRADA (fecha de expiración en el pasado)
        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcion.Id,
            FechaCreacion = DateTime.Now.AddMinutes(-40),
            FechaExpiracion = DateTime.Now.AddMinutes(-10), // expiró hace 10 minutos
            Estado = EstadoReserva.Pendiente
        };

        var asientoReserva = new AsientoReserva
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            AsientoId = asiento.Id
        };

        context.Peliculas.Add(pelicula);
        context.Salas.Add(sala);
        context.Usuarios.Add(usuario);
        context.Funciones.Add(funcion);
        context.Asientos.Add(asiento);
        context.Reservas.Add(reserva);
        context.AsientosReserva.Add(asientoReserva);
        await context.SaveChangesAsync();

        var reservaRepo = new ReservaRepository(context);
        var asientoRepo = new AsientoRepository(context);
        var funcionRepo = new FuncionRepository(context);
        var usuarioRepo = new UsuarioRepository(context);
        var service = new ReservaService(reservaRepo, asientoRepo, funcionRepo, usuarioRepo);

        // Act
        var cantidadExpiradas = await service.ExpirarReservasVencidasAsync();

        // Assert
        using var contextVerificacion = CrearContext();

        // No verificamos el total exacto (puede haber otras reservas vencidas
        // dejadas por otras pruebas en la misma base de datos compartida).
        // En cambio, confirmamos que el servicio SÍ procesó al menos la nuestra.
        Assert.True(cantidadExpiradas >= 1);

        var reservaActualizada = await contextVerificacion.Reservas.FindAsync(reserva.Id);
        Assert.Equal(EstadoReserva.Expirada, reservaActualizada!.Estado);

        var asientoActualizado = await contextVerificacion.Asientos.FindAsync(asiento.Id);
        Assert.Equal(EstadoAsiento.Libre, asientoActualizado!.Estado);
    }
}