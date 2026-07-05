namespace TicketCine.Domain.Entities
{
    public class Asiento
    {
        public Guid Id { get; set; }
        public Guid FuncionId { get; set; }
        public int Fila { get; set; }
        public int Columna { get; set; }
        public EstadoAsiento Estado { get; set; } = EstadoAsiento.Libre;

        // Relación
        public Funcion Funcion { get; set; } = null!;
    }
}
