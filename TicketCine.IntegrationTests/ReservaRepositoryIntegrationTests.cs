using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class ReservaRepositoryIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public ReservaRepositoryIntegrationTests(PostgresContainerFixture fixture)
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
        var rol = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Cliente");
        if (rol == null)
        {
            rol = new Rol { Nombre = "Cliente" };
            context.Roles.Add(rol);
            await context.SaveChangesAsync();
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Usuario Test",
            Correo = $"{Guid.NewGuid()}@ticketcine.com",
            PasswordHash = "hash123",
            RolId = rol.Id
        };

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

        context.Usuarios.Add(usuario);
        context.Peliculas.Add(pelicula);
        context.Salas.Add(sala);
        await context.SaveChangesAsync();

        var funcion = new Funcion
        {
            Id = Guid.NewGuid(),
            PeliculaId = pelicula.Id,
            SalaId = sala.Id,
            FechaHora = new DateTime(2026, 9, 1, 18, 0, 0),
            Precio = 20.00m,
            Activo = true
        };
        context.Funciones.Add(funcion);

        var asiento = new Asiento
        {
            Id = Guid.NewGuid(),
            FuncionId = funcion.Id,
            Fila = 1,
            Columna = 1,
            Estado = EstadoAsiento.Libre
        };
        context.Asientos.Add(asiento);

        await context.SaveChangesAsync();

        return (usuario, funcion, asiento);
    }

    [Fact]
    public async Task CrearAsync_DeberiaGuardarReservaEnBaseDeDatos()
    {
        using var context = CrearContext();
        var (usuario, funcion, _) = await CrearDatosBaseAsync(context);
        var repo = new ReservaRepository(context);

        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcion.Id,
            Estado = EstadoReserva.Pendiente
        };

        await repo.CrearAsync(reserva);

        var guardada = await context.Reservas.FirstOrDefaultAsync(r => r.Id == reserva.Id);
        Assert.NotNull(guardada);
        Assert.Equal(EstadoReserva.Pendiente, guardada!.Estado);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaIncluirAsientos()
    {
        using var context = CrearContext();
        var (usuario, funcion, asiento) = await CrearDatosBaseAsync(context);

        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcion.Id,
            Estado = EstadoReserva.Pendiente
        };
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();

        context.AsientosReserva.Add(new AsientoReserva
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            AsientoId = asiento.Id
        });
        await context.SaveChangesAsync();

        var repo = new ReservaRepository(context);
        var resultado = await repo.ObtenerPorIdAsync(reserva.Id);

        Assert.NotNull(resultado);
        Assert.Single(resultado!.Asientos);
    }

    [Fact]
    public async Task ObtenerActivasPorUsuarioAsync_DeberiaExcluirCanceladasYExpiradas()
    {
        using var context = CrearContext();
        var (usuario, funcion, _) = await CrearDatosBaseAsync(context);

        context.Reservas.AddRange(
            new Reserva { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FuncionId = funcion.Id, Estado = EstadoReserva.Pendiente },
            new Reserva { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FuncionId = funcion.Id, Estado = EstadoReserva.Confirmada },
            new Reserva { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FuncionId = funcion.Id, Estado = EstadoReserva.Expirada }
        );
        await context.SaveChangesAsync();

        var repo = new ReservaRepository(context);
        var resultado = await repo.ObtenerActivasPorUsuarioAsync(usuario.Id);

        Assert.Equal(2, resultado.Count());
    }

    [Fact]
    public async Task ObtenerTodasPorUsuarioAsync_DeberiaRetornarTodasSinImportarEstado()
    {
        using var context = CrearContext();
        var (usuario, funcion, _) = await CrearDatosBaseAsync(context);

        context.Reservas.AddRange(
            new Reserva { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FuncionId = funcion.Id, Estado = EstadoReserva.Pendiente },
            new Reserva { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FuncionId = funcion.Id, Estado = EstadoReserva.Expirada }
        );
        await context.SaveChangesAsync();

        var repo = new ReservaRepository(context);
        var resultado = await repo.ObtenerTodasPorUsuarioAsync(usuario.Id);

        Assert.Equal(2, resultado.Count());
    }

    [Fact]
    public async Task ObtenerExpiradasPorUsuarioAsync_DeberiaRetornarSoloExpiradas()
    {
        using var context = CrearContext();
        var (usuario, funcion, _) = await CrearDatosBaseAsync(context);

        context.Reservas.AddRange(
            new Reserva { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FuncionId = funcion.Id, Estado = EstadoReserva.Pendiente },
            new Reserva { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FuncionId = funcion.Id, Estado = EstadoReserva.Expirada }
        );
        await context.SaveChangesAsync();

        var repo = new ReservaRepository(context);
        var resultado = await repo.ObtenerExpiradasPorUsuarioAsync(usuario.Id);

        Assert.Single(resultado);
        Assert.Equal(EstadoReserva.Expirada, resultado.First().Estado);
    }

    [Fact]
    public async Task ObtenerPendientesVencidasAsync_DeberiaRetornarSoloPendientesConFechaVencida()
    {
        using var context = CrearContext();
        var (usuario, funcion, _) = await CrearDatosBaseAsync(context);

        var ahora = DateTime.Now;

        context.Reservas.AddRange(
            new Reserva
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                FuncionId = funcion.Id,
                Estado = EstadoReserva.Pendiente,
                FechaExpiracion = ahora.AddMinutes(-10)
            },
            new Reserva
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                FuncionId = funcion.Id,
                Estado = EstadoReserva.Pendiente,
                FechaExpiracion = ahora.AddMinutes(30)
            },
            new Reserva
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                FuncionId = funcion.Id,
                Estado = EstadoReserva.Confirmada,
                FechaExpiracion = ahora.AddMinutes(-10)
            }
        );
        await context.SaveChangesAsync();

        var repo = new ReservaRepository(context);
        var resultado = await repo.ObtenerPendientesVencidasAsync(ahora);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task ContarAsientosReservadosPorFuncionYUsuarioAsync_DeberiaContarCorrectamente()
    {
        using var context = CrearContext();
        var (usuario, funcion, asiento) = await CrearDatosBaseAsync(context);

        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcion.Id,
            Estado = EstadoReserva.Confirmada
        };
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();

        context.AsientosReserva.Add(new AsientoReserva
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            AsientoId = asiento.Id
        });
        await context.SaveChangesAsync();

        var repo = new ReservaRepository(context);
        var total = await repo.ContarAsientosReservadosPorFuncionYUsuarioAsync(funcion.Id, usuario.Id);

        Assert.Equal(1, total);
    }

    [Fact]
    public async Task CrearAsientosReservaAsync_DeberiaGuardarVariosAsientos()
    {
        using var context = CrearContext();
        var (usuario, funcion, asiento) = await CrearDatosBaseAsync(context);

        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcion.Id,
            Estado = EstadoReserva.Pendiente
        };
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();

        var repo = new ReservaRepository(context);
        var asientosReserva = new List<AsientoReserva>
        {
            new AsientoReserva { Id = Guid.NewGuid(), ReservaId = reserva.Id, AsientoId = asiento.Id }
        };

        await repo.CrearAsientosReservaAsync(asientosReserva);

        var guardados = await context.AsientosReserva.Where(ar => ar.ReservaId == reserva.Id).ToListAsync();
        Assert.Single(guardados);
    }

    [Fact]
    public async Task ObtenerAsientosPorReservaAsync_DeberiaRetornarAsientosCorrectos()
    {
        using var context = CrearContext();
        var (usuario, funcion, asiento) = await CrearDatosBaseAsync(context);

        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcion.Id,
            Estado = EstadoReserva.Pendiente
        };
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();

        context.AsientosReserva.Add(new AsientoReserva
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            AsientoId = asiento.Id
        });
        await context.SaveChangesAsync();

        var repo = new ReservaRepository(context);
        var resultado = await repo.ObtenerAsientosPorReservaAsync(reserva.Id);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task ActualizarAsync_DeberiaModificarEstadoDeReserva()
    {
        using var context = CrearContext();
        var (usuario, funcion, _) = await CrearDatosBaseAsync(context);

        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcion.Id,
            Estado = EstadoReserva.Pendiente
        };
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();

        reserva.Estado = EstadoReserva.Confirmada;
        var repo = new ReservaRepository(context);
        await repo.ActualizarAsync(reserva);

        var actualizada = await context.Reservas.FindAsync(reserva.Id);
        Assert.Equal(EstadoReserva.Confirmada, actualizada!.Estado);
    }
}