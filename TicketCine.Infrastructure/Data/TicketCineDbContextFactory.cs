using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TicketCine.Infrastructure.Data
{
    public class TicketCineDbContextFactory : IDesignTimeDbContextFactory<TicketCineDbContext>
    {
        public TicketCineDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TicketCineDbContext>();

            // Usar la cadena de conexión por defecto para desarrollo
            var connectionString = "Host=localhost;Port=5432;Database=ticketcine_db;Username=postgres;Password=71441200";

            optionsBuilder.UseNpgsql(connectionString);

            return new TicketCineDbContext(optionsBuilder.Options);
        }
    }
}
