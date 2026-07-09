using Microsoft.EntityFrameworkCore;
using TicketCine.Application.DTOs;
using TicketCine.Application.Services;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class VentaServiceIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public VentaServiceIntegrationTests(PostgresContainerFixture fixture)
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
    public async Task ConfirmarCompra_ConReservaValida_DebeGenerarVentaYMarcarAsientoVendidoEnBaseDeDatosReal()
    {
        using var context = CrearContext();
        var (usuario, funcion, asiento) = await CrearDatosBaseAsync(context);

        var reservaRepo = new ReservaRepository(context);
        var asientoRepo = new AsientoRepository(context);
        var funcionRepo = new FuncionRepository(context);
        var usuarioRepo = new UsuarioRepository(context);
        var ventaRepo = new VentaRepository(context);

        var reservaService = new ReservaService(reservaRepo, asientoRepo, funcionRepo, usuarioRepo);
        var ventaService = new VentaService(ventaRepo, reservaRepo, asientoRepo, funcionRepo, reservaService);

        // Arrange: primero se crea una reserva real (igual que haría un usuario en la app)
        var reservaRequest = new CrearReservaRequest
        {
            FuncionId = funcion.Id,
            AsientoIds = new List<Guid> { asiento.Id }
        };
        var reservaResponse = await reservaService.CrearReservaAsync(usuario.Id, reservaRequest);

        var compraRequest = new ConfirmarCompraRequest
        {
            ReservaId = reservaResponse.ReservaId,
            MetodoPago = "Tarjeta"
        };

        // Act
        var ventaResponse = await ventaService.ConfirmarCompraAsync(usuario.Id, compraRequest);

        // Assert: verificar en la BD real (nuevo contexto para no depender de tracking en memoria)
        using var contextVerificacion = CrearContext();

        var ventaGuardada = await contextVerificacion.Ventas.FindAsync(ventaResponse.VentaId);
        Assert.NotNull(ventaGuardada);
        Assert.Equal(reservaResponse.ReservaId, ventaGuardada!.ReservaId);
        Assert.Equal("Tarjeta", ventaGuardada.MetodoPago);
        Assert.Equal(funcion.Precio, ventaGuardada.MontoTotal);

        var asientoActualizado = await contextVerificacion.Asientos.FindAsync(asiento.Id);
        Assert.Equal(EstadoAsiento.Vendido, asientoActualizado!.Estado);

        var reservaActualizada = await contextVerificacion.Reservas.FindAsync(reservaResponse.ReservaId);
        Assert.Equal(EstadoReserva.Confirmada, reservaActualizada!.Estado);
    }
}