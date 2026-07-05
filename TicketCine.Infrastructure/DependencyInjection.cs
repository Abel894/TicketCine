using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketCine.Application.Interfaces;
using TicketCine.Application.Services;
using TicketCine.Infrastructure.Data;
using TicketCine.Infrastructure.Repositories;
using TicketCine.Infrastructure.Services;

namespace TicketCine.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Registrar DbContext con PostgreSQL
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<TicketCineDbContext>(options =>
                options.UseNpgsql(connectionString)
            );

            // Registrar repositorios
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IPeliculaRepository, PeliculaRepository>();
            services.AddScoped<ISalaRepository, SalaRepository>();
            services.AddScoped<IFuncionRepository, FuncionRepository>();
            services.AddScoped<IAsientoRepository, AsientoRepository>();

            // Registrar servicios de aplicación
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IArchivoService, ArchivoService>();

            return services;
        }
    }
}
