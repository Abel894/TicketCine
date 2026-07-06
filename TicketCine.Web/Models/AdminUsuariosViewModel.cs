namespace TicketCine.Web.Models
{
    public class AdminUsuariosIndexViewModel
    {
        public Guid UsuarioAutenticadoId { get; set; }
        public List<AdminUsuarioItemViewModel> Usuarios { get; set; } = new();
    }

    public class AdminUsuarioItemViewModel
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
