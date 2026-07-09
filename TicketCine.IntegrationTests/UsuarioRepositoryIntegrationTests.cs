using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class UsuarioRepositoryIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public UsuarioRepositoryIntegrationTests(PostgresContainerFixture fixture)
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

    private async Task<Rol> CrearRolAsync(TicketCineDbContext context)
    {
        var existente = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Cliente");
        if (existente != null)
            return existente;

        var rol = new Rol { Nombre = "Cliente" };
        context.Roles.Add(rol);
        await context.SaveChangesAsync();
        return rol;
    }

    [Fact]
    public async Task CrearAsync_DeberiaGuardarUsuarioEnBaseDeDatos()
    {
        using var context = CrearContext();
        var rol = await CrearRolAsync(context);
        var repo = new UsuarioRepository(context);

        var correo = $"prueba_{Guid.NewGuid()}@ticketcine.com";
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Usuario Prueba",
            Correo = correo,
            PasswordHash = "hash_falso_123",
            RolId = rol.Id
        };

        await repo.CrearAsync(usuario);

        var guardado = await context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuario.Id);
        Assert.NotNull(guardado);
        Assert.Equal(correo, guardado!.Correo);
    }

    [Fact]
    public async Task ObtenerPorCorreoAsync_DeberiaRetornarUsuario_CuandoCorreoExiste()
    {
        using var context = CrearContext();
        var rol = await CrearRolAsync(context);
        var repo = new UsuarioRepository(context);

        var correo = $"existente_{Guid.NewGuid()}@ticketcine.com";
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Existente",
            Correo = correo,
            PasswordHash = "hash123",
            RolId = rol.Id
        };
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var resultado = await repo.ObtenerPorCorreoAsync(correo);

        Assert.NotNull(resultado);
        Assert.Equal(usuario.Id, resultado!.Id);
        Assert.NotNull(resultado.Rol);
    }

    [Fact]
    public async Task ObtenerPorCorreoAsync_DeberiaRetornarNull_CuandoNoExiste()
    {
        using var context = CrearContext();
        var repo = new UsuarioRepository(context);

        var correoInexistente = $"noexiste_{Guid.NewGuid()}@ticketcine.com";
        var resultado = await repo.ObtenerPorCorreoAsync(correoInexistente);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DeberiaRetornarUsuario_CuandoExiste()
    {
        using var context = CrearContext();
        var rol = await CrearRolAsync(context);
        var repo = new UsuarioRepository(context);

        var correo = $"porid_{Guid.NewGuid()}@ticketcine.com";
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Por Id",
            Correo = correo,
            PasswordHash = "hash123",
            RolId = rol.Id
        };
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var resultado = await repo.ObtenerPorIdAsync(usuario.Id);

        Assert.NotNull(resultado);
        Assert.Equal(usuario.Correo, resultado!.Correo);
    }

    [Fact]
    public async Task CorreoExisteAsync_DeberiaRetornarTrue_CuandoCorreoYaRegistrado()
    {
        using var context = CrearContext();
        var rol = await CrearRolAsync(context);
        var repo = new UsuarioRepository(context);

        var correo = $"duplicado_{Guid.NewGuid()}@ticketcine.com";
        context.Usuarios.Add(new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Duplicado",
            Correo = correo,
            PasswordHash = "hash123",
            RolId = rol.Id
        });
        await context.SaveChangesAsync();

        var existe = await repo.CorreoExisteAsync(correo);

        Assert.True(existe);
    }

    [Fact]
    public async Task ObtenerTodosAsync_DeberiaRetornarListaCompleta()
    {
        using var context = CrearContext();
        var rol = await CrearRolAsync(context);
        var repo = new UsuarioRepository(context);

        var correoUno = $"uno_{Guid.NewGuid()}@ticketcine.com";
        var correoDos = $"dos_{Guid.NewGuid()}@ticketcine.com";

        context.Usuarios.AddRange(
            new Usuario { Id = Guid.NewGuid(), Nombre = "Uno", Correo = correoUno, PasswordHash = "h1", RolId = rol.Id },
            new Usuario { Id = Guid.NewGuid(), Nombre = "Dos", Correo = correoDos, PasswordHash = "h2", RolId = rol.Id }
        );
        await context.SaveChangesAsync();

        var resultado = await repo.ObtenerTodosAsync();

        // En vez de contar el total (puede haber datos de otras pruebas),
        // confirmamos que los dos usuarios que creamos SÍ estén presentes
        Assert.Contains(resultado, u => u.Correo == correoUno);
        Assert.Contains(resultado, u => u.Correo == correoDos);
    }

    [Fact]
    public async Task ActualizarAsync_DeberiaModificarUsuarioExistente()
    {
        using var context = CrearContext();
        var rol = await CrearRolAsync(context);
        var repo = new UsuarioRepository(context);

        var correoOriginal = $"original_{Guid.NewGuid()}@ticketcine.com";
        var correoModificado = $"modificado_{Guid.NewGuid()}@ticketcine.com";

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Original",
            Correo = correoOriginal,
            PasswordHash = "h1",
            RolId = rol.Id
        };
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        usuario.Correo = correoModificado;
        await repo.ActualizarAsync(usuario);

        var actualizado = await context.Usuarios.FindAsync(usuario.Id);
        Assert.Equal(correoModificado, actualizado!.Correo);
    }
}