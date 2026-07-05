namespace TicketCine.Domain.Entities
{
    public class Sala
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Filas { get; set; }
        public int Columnas { get; set; }
        public bool Activo { get; set; } = true;

        // Relación inversa
        public ICollection<Funcion> Funciones { get; set; } = new List<Funcion>();
    }
}
