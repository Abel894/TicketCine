namespace TicketCine.Application.DTOs
{
    public class LoginResponse
    {
        public Guid UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
