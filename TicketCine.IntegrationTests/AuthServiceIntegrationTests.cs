using Microsoft.EntityFrameworkCore;
using TicketCine.Application.DTOs;
using TicketCine.Application.Services;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using Xunit;

namespace TicketCine.IntegrationTests;

[Collection("Postgres")]
public class AuthServiceIntegrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public AuthServiceIntegrationTests(PostgresContainerFixture fixture)
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
    public async Task Autenticar_ConCredencialesCorrectas_DebeRetornarLoginResponseDesdeBaseDeDatosReal()
    {
        using var context = CrearContext();
        var usuarioRepo = new UsuarioRepository(context);
        var authService = new AuthService(usuarioRepo);

        // Arrange: registrar un usuario real (esto genera el hash real con BCrypt y lo persiste)
        var correo = $"test_{Guid.NewGuid()}@test.com";
        var registroRequest = new RegistroUsuarioRequest
        {
            Nombre = "Usuario Test",
            Correo = correo,
            Contrasena = "Clave123!"
        };

        await authService.RegistrarAsync(registroRequest);

        var loginRequest = new LoginRequest
        {
            Correo = correo,
            Contrasena = "Clave123!"
        };

        // Act
        var response = await authService.AutenticarAsync(loginRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(correo.ToLowerInvariant(), response!.Correo);
        Assert.Equal("Usuario Test", response.Nombre);
        Assert.Equal("Cliente", response.Rol);
    }

    [Fact]
    public async Task Autenticar_ConContrasenaIncorrecta_DebeRetornarNullDesdeBaseDeDatosReal()
    {
        using var context = CrearContext();
        var usuarioRepo = new UsuarioRepository(context);
        var authService = new AuthService(usuarioRepo);

        var correo = $"test_{Guid.NewGuid()}@test.com";
        var registroRequest = new RegistroUsuarioRequest
        {
            Nombre = "Usuario Test",
            Correo = correo,
            Contrasena = "Clave123!"
        };

        await authService.RegistrarAsync(registroRequest);

        var loginRequest = new LoginRequest
        {
            Correo = correo,
            Contrasena = "ClaveIncorrecta"
        };

        // Act
        var response = await authService.AutenticarAsync(loginRequest);

        // Assert
        Assert.Null(response);
    }
}