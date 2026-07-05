namespace TicketCine.Application.DTOs
{
    public class SalaResponse
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Filas { get; set; }
        public int Columnas { get; set; }
        public bool Activo { get; set; }
    }
}
