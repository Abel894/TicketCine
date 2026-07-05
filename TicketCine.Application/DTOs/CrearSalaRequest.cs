namespace TicketCine.Application.DTOs
{
    public class CrearSalaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public int Filas { get; set; }
        public int Columnas { get; set; }
    }
}
