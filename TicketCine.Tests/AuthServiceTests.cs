using Moq;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Application.Services;
using TicketCine.Domain.Entities;

namespace TicketCine.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegistrarAsync_ConDatosValidos_DebeCrearYRetornarUsuarioResponse()
    {
        var service = CrearService(out var usuarioRepo);
        var request = new RegistroUsuarioRequest
        {
            Nombre = "  Abel  ",
            Correo = "  ABEL@MAIL.COM  ",
            Contrasena = "Password123!"
        };

        usuarioRepo.Setup(r => r.CorreoExisteAsync(request.Correo)).ReturnsAsync(false);
        usuarioRepo
            .Setup(r => r.CrearAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario u) => u);

        var response = await service.RegistrarAsync(request);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Abel", response.Nombre);
        Assert.Equal("abel@mail.com", response.Correo);
        Assert.Equal("Cliente", response.Rol);
        Assert.True(response.Activo);

        usuarioRepo.Verify(r => r.CrearAsync(It.Is<Usuario>(u =>
            u.Nombre == "Abel" &&
            u.Correo == "abel@mail.com" &&
            u.RolId == 1 &&
            u.Activo &&
            u.PasswordHash != request.Contrasena)), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_ConNombreVacio_DebeLanzarArgumentException()
    {
        var service = CrearService(out _);
        var request = new RegistroUsuarioRequest
        {
            Nombre = "   ",
            Correo = "correo@mail.com",
            Contrasena = "Password123!"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegistrarAsync(request));
    }

    [Fact]
    public async Task RegistrarAsync_ConCorreoVacio_DebeLanzarArgumentException()
    {
        var service = CrearService(out _);
        var request = new RegistroUsuarioRequest
        {
            Nombre = "Abel",
            Correo = " ",
            Contrasena = "Password123!"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegistrarAsync(request));
    }

    [Fact]
    public async Task RegistrarAsync_ConContrasenaVacia_DebeLanzarArgumentException()
    {
        var service = CrearService(out _);
        var request = new RegistroUsuarioRequest
        {
            Nombre = "Abel",
            Correo = "correo@mail.com",
            Contrasena = " "
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegistrarAsync(request));
    }

    [Fact]
    public async Task RegistrarAsync_ConCorreoYaRegistrado_DebeLanzarInvalidOperationException()
    {
        var service = CrearService(out var usuarioRepo);
        var request = new RegistroUsuarioRequest
        {
            Nombre = "Abel",
            Correo = "correo@mail.com",
            Contrasena = "Password123!"
        };

        usuarioRepo.Setup(r => r.CorreoExisteAsync(request.Correo)).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarAsync(request));

        usuarioRepo.Verify(r => r.CrearAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task AutenticarAsync_ConCredencialesValidas_DebeRetornarLoginResponseConRolDelUsuario()
    {
        var service = CrearService(out var usuarioRepo);
        var correo = "usuario@mail.com";
        var contrasena = "Password123!";
        var usuarioId = Guid.NewGuid();

        var usuario = new Usuario
        {
            Id = usuarioId,
            Nombre = "Usuario",
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(contrasena),
            Activo = true,
            Rol = new Rol { Id = 2, Nombre = "Admin" }
        };

        usuarioRepo.Setup(r => r.ObtenerPorCorreoAsync(correo)).ReturnsAsync(usuario);

        var response = await service.AutenticarAsync(new LoginRequest
        {
            Correo = correo,
            Contrasena = contrasena
        });

        Assert.NotNull(response);
        Assert.Equal(usuarioId, response.UsuarioId);
        Assert.Equal("Usuario", response.Nombre);
        Assert.Equal(correo, response.Correo);
        Assert.Equal("Admin", response.Rol);
    }

    [Fact]
    public async Task AutenticarAsync_ConRolNulo_DebeRetornarRolClientePorDefecto()
    {
        var service = CrearService(out var usuarioRepo);
        var correo = "cliente@mail.com";
        var contrasena = "Password123!";

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Cliente",
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(contrasena),
            Activo = true,
            Rol = null!
        };

        usuarioRepo.Setup(r => r.ObtenerPorCorreoAsync(correo)).ReturnsAsync(usuario);

        var response = await service.AutenticarAsync(new LoginRequest
        {
            Correo = correo,
            Contrasena = contrasena
        });

        Assert.NotNull(response);
        Assert.Equal("Cliente", response.Rol);
    }

    [Fact]
    public async Task AutenticarAsync_ConCorreoVacio_DebeLanzarArgumentException()
    {
        var service = CrearService(out _);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AutenticarAsync(new LoginRequest
        {
            Correo = " ",
            Contrasena = "Password123!"
        }));
    }

    [Fact]
    public async Task AutenticarAsync_ConContrasenaVacia_DebeLanzarArgumentException()
    {
        var service = CrearService(out _);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AutenticarAsync(new LoginRequest
        {
            Correo = "usuario@mail.com",
            Contrasena = " "
        }));
    }

    [Fact]
    public async Task AutenticarAsync_ConUsuarioNoExistente_DebeRetornarNull()
    {
        var service = CrearService(out var usuarioRepo);

        usuarioRepo.Setup(r => r.ObtenerPorCorreoAsync("inexistente@mail.com")).ReturnsAsync((Usuario?)null);

        var response = await service.AutenticarAsync(new LoginRequest
        {
            Correo = "inexistente@mail.com",
            Contrasena = "Password123!"
        });

        Assert.Null(response);
    }

    [Fact]
    public async Task AutenticarAsync_ConContrasenaIncorrecta_DebeRetornarNull()
    {
        var service = CrearService(out var usuarioRepo);
        var correo = "usuario@mail.com";

        usuarioRepo.Setup(r => r.ObtenerPorCorreoAsync(correo)).ReturnsAsync(new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Usuario",
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correcta123!"),
            Activo = true,
            Rol = new Rol { Id = 1, Nombre = "Cliente" }
        });

        var response = await service.AutenticarAsync(new LoginRequest
        {
            Correo = correo,
            Contrasena = "Incorrecta123!"
        });

        Assert.Null(response);
    }

    [Fact]
    public async Task AutenticarAsync_ConUsuarioInactivo_DebeRetornarNull()
    {
        var service = CrearService(out var usuarioRepo);
        var correo = "inactivo@mail.com";
        var contrasena = "Password123!";

        usuarioRepo.Setup(r => r.ObtenerPorCorreoAsync(correo)).ReturnsAsync(new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Inactivo",
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(contrasena),
            Activo = false,
            Rol = new Rol { Id = 1, Nombre = "Cliente" }
        });

        var response = await service.AutenticarAsync(new LoginRequest
        {
            Correo = correo,
            Contrasena = contrasena
        });

        Assert.Null(response);
    }

    [Fact]
    public async Task AutenticarAsync_ConCredencialesInvalidas_DebeRetornarNullSinDiferenciarCausa()
    {
        var service = CrearService(out var usuarioRepo);

        usuarioRepo.Setup(r => r.ObtenerPorCorreoAsync("noexiste@mail.com")).ReturnsAsync((Usuario?)null);
        usuarioRepo.Setup(r => r.ObtenerPorCorreoAsync("existe@mail.com")).ReturnsAsync(new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Usuario",
            Correo = "existe@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correcta123!"),
            Activo = true,
            Rol = new Rol { Id = 1, Nombre = "Cliente" }
        });

        var usuarioNoExiste = await service.AutenticarAsync(new LoginRequest
        {
            Correo = "noexiste@mail.com",
            Contrasena = "Password123!"
        });

        var contrasenaInvalida = await service.AutenticarAsync(new LoginRequest
        {
            Correo = "existe@mail.com",
            Contrasena = "Incorrecta123!"
        });

        Assert.Null(usuarioNoExiste);
        Assert.Null(contrasenaInvalida);
    }

    private static AuthService CrearService(out Mock<IUsuarioRepository> usuarioRepo)
    {
        usuarioRepo = new Mock<IUsuarioRepository>();
        return new AuthService(usuarioRepo.Object);
    }
}
