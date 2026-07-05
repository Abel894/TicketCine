namespace TicketCine.Domain.Entities
{
    public class Funcion
    {
        public Guid Id { get; set; }
        public Guid PeliculaId { get; set; }
        public Guid SalaId { get; set; }
        public DateTime FechaHora { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; } = true;

        // Relaciones
        public Pelicula Pelicula { get; set; } = null!;
        public Sala Sala { get; set; } = null!;

        // Relación inversa
        public ICollection<Asiento> Asientos { get; set; } = new List<Asiento>();
    }
}
