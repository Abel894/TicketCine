using TicketCine.Application.DTOs;

namespace TicketCine.Application.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// </summary>
        Task<UsuarioResponse> RegistrarAsync(RegistroUsuarioRequest request);

        /// <summary>
        /// Autentica un usuario y retorna sus datos si las credenciales son válidas.
        /// </summary>
        Task<LoginResponse?> AutenticarAsync(LoginRequest request);
    }
}
