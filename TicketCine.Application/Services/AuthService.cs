using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;

namespace TicketCine.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public AuthService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// </summary>
        /// <param name="request">Datos de registro (nombre, correo, contraseña)</param>
        /// <returns>Datos del usuario creado</returns>
        /// <exception cref="InvalidOperationException">Si el correo ya está registrado</exception>
        public async Task<UsuarioResponse> RegistrarAsync(RegistroUsuarioRequest request)
        {
            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(request.Nombre))
                throw new ArgumentException("El nombre es requerido.", nameof(request.Nombre));

            if (string.IsNullOrWhiteSpace(request.Correo))
                throw new ArgumentException("El correo es requerido.", nameof(request.Correo));

            if (string.IsNullOrWhiteSpace(request.Contrasena))
                throw new ArgumentException("La contraseña es requerida.", nameof(request.Contrasena));

            // Validar unicidad del correo
            if (await _usuarioRepository.CorreoExisteAsync(request.Correo))
                throw new InvalidOperationException($"El correo '{request.Correo}' ya está registrado.");

            // Crear entidad Usuario
            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nombre = request.Nombre.Trim(),
                Correo = request.Correo.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena),
                RolId = 1, // Por defecto, rol "Cliente"
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            // Persistir
            var usuarioCreado = await _usuarioRepository.CrearAsync(usuario);

            // Retornar DTO de respuesta sin exponer el hash
            return new UsuarioResponse
            {
                Id = usuarioCreado.Id,
                Nombre = usuarioCreado.Nombre,
                Correo = usuarioCreado.Correo,
                Rol = "Cliente", // Ajustar cuando tengamos relación con Rol cargada
                Activo = usuarioCreado.Activo
            };
        }

        /// <summary>
        /// Autentica un usuario validando sus credenciales.
        /// </summary>
        /// <param name="request">Credenciales (correo, contraseña)</param>
        /// <returns>Datos del usuario autenticado o null si las credenciales son inválidas</returns>
        public async Task<LoginResponse?> AutenticarAsync(LoginRequest request)
        {
            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(request.Correo))
                throw new ArgumentException("El correo es requerido.", nameof(request.Correo));

            if (string.IsNullOrWhiteSpace(request.Contrasena))
                throw new ArgumentException("La contraseña es requerida.", nameof(request.Contrasena));

            // Buscar usuario por correo
            var usuario = await _usuarioRepository.ObtenerPorCorreoAsync(request.Correo.Trim().ToLowerInvariant());

            if (usuario == null)
                return null; // Credenciales inválidas

            // Verificar contraseña con BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Contrasena, usuario.PasswordHash))
                return null; // Credenciales inválidas

            // Verificar que el usuario esté activo
            if (!usuario.Activo)
                return null;

            // Retornar datos autenticados
            return new LoginResponse
            {
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Rol = usuario.Rol?.Nombre ?? "Cliente"
            };
        }
    }
}
