using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class VentaRepositoryIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public VentaRepositoryIntegrationTests(PostgresContainerFixture fixture)
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

    private async Task<(Usuario usuario, Pelicula pelicula, Funcion funcion, Reserva reserva)> CrearDatosBaseAsync(TicketCineDbContext context)
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
            Nombre = "Usuario Venta Test",
            Correo = $"{Guid.NewGuid()}@ticketcine.com",
            PasswordHash = "hash123",
            RolId = rol.Id
        };

        var pelicula = new Pelicula
        {
            Id = Guid.NewGuid(),
            Titulo = $"Pelicula {Guid.NewGuid()}",
            Sinopsis = "Sinopsis de prueba",
            Genero = "Accion",
            DuracionMinutos = 120,
            Clasificacion = "PG-13",
            Activo = true
        };

        var sala = new Sala
        {
            Id = Guid.NewGuid(),
            Nombre = $"Sala {Guid.NewGuid()}",
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
            FechaHora = DateTime.Now.AddDays(1),
            Precio = 20.00m,
            Activo = true
        };
        context.Funciones.Add(funcion);
        await context.SaveChangesAsync();

        var reserva = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcion.Id,
            Estado = EstadoReserva.Confirmada
        };
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();

        return (usuario, pelicula, funcion, reserva);
    }

    [Fact]
    public async Task CrearAsync_DeberiaGuardarVentaEnBaseDeDatos()
    {
        using var context = CrearContext();
        var (_, _, _, reserva) = await CrearDatosBaseAsync(context);
        var repo = new VentaRepository(context);

        var venta = new Venta
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            MetodoPago = "Tarjeta",
            MontoTotal = 40.00m,
            CodigoQr = Guid.NewGuid().ToString()
        };

        await repo.CrearAsync(venta);

        var guardada = await context.Ventas.FindAsync(venta.Id);
        Assert.NotNull(guardada);
        Assert.Equal(40.00m, guardada!.MontoTotal);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaIncluirReserva()
    {
        using var context = CrearContext();
        var (_, _, _, reserva) = await CrearDatosBaseAsync(context);

        var venta = new Venta
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            MetodoPago = "Efectivo",
            MontoTotal = 20.00m,
            CodigoQr = Guid.NewGuid().ToString()
        };
        context.Ventas.Add(venta);
        await context.SaveChangesAsync();

        var repo = new VentaRepository(context);
        var resultado = await repo.ObtenerPorIdAsync(venta.Id);

        Assert.NotNull(resultado);
        Assert.NotNull(resultado!.Reserva);
        Assert.Equal(reserva.Id, resultado.Reserva.Id);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaRetornarNull_CuandoNoExiste()
    {
        using var context = CrearContext();
        var repo = new VentaRepository(context);

        var resultado = await repo.ObtenerPorIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObtenerPorReservaIdAsync_DeberiaRetornarVentaCorrecta()
    {
        using var context = CrearContext();
        var (_, _, _, reserva) = await CrearDatosBaseAsync(context);

        var venta = new Venta
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            MetodoPago = "Tarjeta",
            MontoTotal = 30.00m,
            CodigoQr = Guid.NewGuid().ToString()
        };
        context.Ventas.Add(venta);
        await context.SaveChangesAsync();

        var repo = new VentaRepository(context);
        var resultado = await repo.ObtenerPorReservaIdAsync(reserva.Id);

        Assert.NotNull(resultado);
        Assert.Equal(venta.Id, resultado!.Id);
    }

    [Fact]
    public async Task ObtenerConDetalleAsync_DeberiaIncluirPeliculaSalaYAsientos()
    {
        using var context = CrearContext();
        var (usuario, pelicula, funcion, reserva) = await CrearDatosBaseAsync(context);

        var asiento = new Asiento
        {
            Id = Guid.NewGuid(),
            FuncionId = funcion.Id,
            Fila = 1,
            Columna = 1,
            Estado = EstadoAsiento.Reservado
        };
        context.Asientos.Add(asiento);
        await context.SaveChangesAsync();

        context.AsientosReserva.Add(new AsientoReserva
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            AsientoId = asiento.Id
        });

        var venta = new Venta
        {
            Id = Guid.NewGuid(),
            ReservaId = reserva.Id,
            MetodoPago = "Tarjeta",
            MontoTotal = 20.00m,
            CodigoQr = Guid.NewGuid().ToString()
        };
        context.Ventas.Add(venta);
        await context.SaveChangesAsync();

        var repo = new VentaRepository(context);
        var resultado = await repo.ObtenerConDetalleAsync(venta.Id);

        Assert.NotNull(resultado);
        Assert.Equal(pelicula.Id, resultado!.Reserva.Funcion.Pelicula.Id);
        Assert.NotNull(resultado.Reserva.Funcion.Sala);
        Assert.Single(resultado.Reserva.Asientos);
    }

    [Fact]
    public async Task ObtenerParaReporteAsync_DeberiaFiltrarPorRangoDeFechas()
    {
        using var context = CrearContext();
        var (_, _, _, reservaDentro) = await CrearDatosBaseAsync(context);
        var (_, _, _, reservaFuera) = await CrearDatosBaseAsync(context);

        // Usamos una fecha muy específica y poco probable de chocar con otras pruebas
        var fechaDentro = new DateTime(2031, 3, 15, 12, 0, 0);
        var fechaFuera = new DateTime(2020, 1, 1, 12, 0, 0);

        var ventaDentroDelRango = new Venta
        {
            Id = Guid.NewGuid(),
            ReservaId = reservaDentro.Id,
            MetodoPago = "Tarjeta",
            MontoTotal = 25.00m,
            FechaVenta = fechaDentro,
            CodigoQr = Guid.NewGuid().ToString()
        };

        var ventaFueraDelRango = new Venta
        {
            Id = Guid.NewGuid(),
            ReservaId = reservaFuera.Id,
            MetodoPago = "Tarjeta",
            MontoTotal = 25.00m,
            FechaVenta = fechaFuera,
            CodigoQr = Guid.NewGuid().ToString()
        };

        context.Ventas.AddRange(ventaDentroDelRango, ventaFueraDelRango);
        await context.SaveChangesAsync();

        var repo = new VentaRepository(context);
        var resultado = (await repo.ObtenerParaReporteAsync(
            new DateTime(2031, 3, 1),
            new DateTime(2031, 3, 31),
            null)).ToList();

        Assert.Contains(resultado, v => v.Id == ventaDentroDelRango.Id);
        Assert.DoesNotContain(resultado, v => v.Id == ventaFueraDelRango.Id);
    }

    [Fact]
    public async Task ObtenerParaReporteAsync_DeberiaFiltrarPorPelicula()
    {
        using var context = CrearContext();
        var (usuario, peliculaUno, funcionUno, reservaUno) = await CrearDatosBaseAsync(context);

        // Segunda película/función/reserva independiente
        var peliculaDos = new Pelicula
        {
            Id = Guid.NewGuid(),
            Titulo = $"Otra Pelicula {Guid.NewGuid()}",
            Sinopsis = "Otra sinopsis",
            Genero = "Drama",
            DuracionMinutos = 100,
            Clasificacion = "PG",
            Activo = true
        };
        var salaDos = new Sala { Id = Guid.NewGuid(), Nombre = $"Sala {Guid.NewGuid()}", Filas = 5, Columnas = 5, Activo = true };
        context.Peliculas.Add(peliculaDos);
        context.Salas.Add(salaDos);
        await context.SaveChangesAsync();

        var funcionDos = new Funcion
        {
            Id = Guid.NewGuid(),
            PeliculaId = peliculaDos.Id,
            SalaId = salaDos.Id,
            FechaHora = DateTime.Now.AddDays(2),
            Precio = 18.00m,
            Activo = true
        };
        context.Funciones.Add(funcionDos);
        await context.SaveChangesAsync();

        var reservaDos = new Reserva
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            FuncionId = funcionDos.Id,
            Estado = EstadoReserva.Confirmada
        };
        context.Reservas.Add(reservaDos);
        await context.SaveChangesAsync();

        var ventaPeliculaUno = new Venta
        {
            Id = Guid.NewGuid(),
            ReservaId = reservaUno.Id,
            MetodoPago = "Tarjeta",
            MontoTotal = 20.00m,
            CodigoQr = Guid.NewGuid().ToString()
        };
        var ventaPeliculaDos = new Venta
        {
            Id = Guid.NewGuid(),
            ReservaId = reservaDos.Id,
            MetodoPago = "Tarjeta",
            MontoTotal = 18.00m,
            CodigoQr = Guid.NewGuid().ToString()
        };
        context.Ventas.AddRange(ventaPeliculaUno, ventaPeliculaDos);
        await context.SaveChangesAsync();

        var repo = new VentaRepository(context);
        var resultado = (await repo.ObtenerParaReporteAsync(null, null, peliculaUno.Id)).ToList();

        Assert.Contains(resultado, v => v.Id == ventaPeliculaUno.Id);
        Assert.DoesNotContain(resultado, v => v.Id == ventaPeliculaDos.Id);
    }
}