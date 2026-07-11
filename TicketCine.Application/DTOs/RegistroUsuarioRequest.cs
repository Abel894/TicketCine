using System.ComponentModel.DataAnnotations;

namespace TicketCine.Application.DTOs
{
    public class RegistroUsuarioRequest
    {
        public string Nombre { get; set; } = string.Empty;

        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", 
            ErrorMessage = "Solo se aceptan correos de Gmail (@gmail.com)")]
        public string Correo { get; set; } = string.Empty;

        public string Contrasena { get; set; } = string.Empty;
    }
}
