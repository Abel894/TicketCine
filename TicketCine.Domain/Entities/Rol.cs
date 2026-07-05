namespace TicketCine.Domain.Entities
{
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        // Relación inversa
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
