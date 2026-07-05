using Microsoft.EntityFrameworkCore;
using TicketCine.Domain.Entities;

namespace TicketCine.Infrastructure.Data
{
    public class TicketCineDbContext : DbContext
    {
        public TicketCineDbContext(DbContextOptions<TicketCineDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Pelicula> Peliculas { get; set; } = null!;
        public DbSet<Sala> Salas { get; set; } = null!;
        public DbSet<Funcion> Funciones { get; set; } = null!;
        public DbSet<Asiento> Asientos { get; set; } = null!;
        public DbSet<Reserva> Reservas { get; set; } = null!;
        public DbSet<AsientoReserva> AsientosReserva { get; set; } = null!;
        public DbSet<Venta> Ventas { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar tabla Rol
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).ValueGeneratedNever();
                entity.Property(r => r.Nombre)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // Configurar tabla Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Nombre)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(u => u.Correo)
                    .IsRequired()
                    .HasMaxLength(255);

                // Índice único para Correo
                entity.HasIndex(u => u.Correo)
                    .IsUnique()
                    .HasDatabaseName("idx_usuarios_correo_unique");

                entity.Property(u => u.PasswordHash)
                    .IsRequired();

                entity.Property(u => u.Activo)
                    .HasDefaultValue(true);

                entity.Property(u => u.FechaCreacion)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relación con Rol (FK)
                entity.HasOne(u => u.Rol)
                    .WithMany(r => r.Usuarios)
                    .HasForeignKey(u => u.RolId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_usuarios_rol_id");
            });

            // Configurar tabla Pelicula
            modelBuilder.Entity<Pelicula>(entity =>
            {
                entity.ToTable("peliculas");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Titulo)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(p => p.Sinopsis)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(p => p.Genero)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.DuracionMinutos)
                    .IsRequired();

                entity.Property(p => p.Clasificacion)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.RutaPoster)
                    .HasMaxLength(500);

                entity.Property(p => p.Activo)
                    .HasDefaultValue(true);

                // Relación inversa con Función
                entity.HasMany(p => p.Funciones)
                    .WithOne(f => f.Pelicula)
                    .HasForeignKey(f => f.PeliculaId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_funciones_pelicula_id");
            });

            // Configurar tabla Sala
            modelBuilder.Entity<Sala>(entity =>
            {
                entity.ToTable("salas");
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.Filas)
                    .IsRequired();

                entity.Property(s => s.Columnas)
                    .IsRequired();

                entity.Property(s => s.Activo)
                    .HasDefaultValue(true);

                // Relación inversa con Función
                entity.HasMany(s => s.Funciones)
                    .WithOne(f => f.Sala)
                    .HasForeignKey(f => f.SalaId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_funciones_sala_id");
            });

            // Configurar tabla Funcion
            modelBuilder.Entity<Funcion>(entity =>
            {
                entity.ToTable("funciones");
                entity.HasKey(f => f.Id);

                entity.Property(f => f.PeliculaId)
                    .IsRequired();

                entity.Property(f => f.SalaId)
                    .IsRequired();

                entity.Property(f => f.FechaHora)
                    .IsRequired()
                    .HasColumnType("timestamp without time zone");

                entity.Property(f => f.Precio)
                    .IsRequired()
                    .HasColumnType("numeric(10, 2)");

                entity.Property(f => f.Activo)
                    .HasDefaultValue(true);

                // Relación con Pelicula
                entity.HasOne(f => f.Pelicula)
                    .WithMany(p => p.Funciones)
                    .HasForeignKey(f => f.PeliculaId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_funciones_pelicula_id");

                // Relación con Sala
                entity.HasOne(f => f.Sala)
                    .WithMany(s => s.Funciones)
                    .HasForeignKey(f => f.SalaId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_funciones_sala_id");

                // Relación inversa con Asiento
                entity.HasMany(f => f.Asientos)
                    .WithOne(a => a.Funcion)
                    .HasForeignKey(a => a.FuncionId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_asientos_funcion_id");

                // Índice en FechaHora para consultas rápidas
                entity.HasIndex(f => f.FechaHora)
                    .HasDatabaseName("idx_funciones_fechahora");
            });

            // Configurar tabla Asiento
            modelBuilder.Entity<Asiento>(entity =>
            {
                entity.ToTable("asientos");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.FuncionId)
                    .IsRequired();

                entity.Property(a => a.Fila)
                    .IsRequired();

                entity.Property(a => a.Columna)
                    .IsRequired();

                entity.Property(a => a.Estado)
                    .IsRequired()
                    .HasConversion(
                        v => v.ToString(),
                        v => (EstadoAsiento)Enum.Parse(typeof(EstadoAsiento), v))
                    .HasMaxLength(20);

                // Relación con Función
                entity.HasOne(a => a.Funcion)
                    .WithMany(f => f.Asientos)
                    .HasForeignKey(a => a.FuncionId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_asientos_funcion_id");

                // Índice compuesto para búsquedas rápidas
                entity.HasIndex(a => new { a.FuncionId, a.Fila, a.Columna })
                    .HasDatabaseName("idx_asientos_funcion_fila_columna");
            });

            // Configurar tabla Reserva
            modelBuilder.Entity<Reserva>(entity =>
            {
                entity.ToTable("reservas");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.UsuarioId)
                    .IsRequired();

                entity.Property(r => r.FuncionId)
                    .IsRequired();

                entity.Property(r => r.FechaCreacion)
                    .IsRequired()
                    .HasColumnType("timestamp without time zone");

                entity.Property(r => r.FechaExpiracion)
                    .IsRequired()
                    .HasColumnType("timestamp without time zone");

                entity.Property(r => r.Estado)
                    .IsRequired()
                    .HasConversion(
                        v => v.ToString(),
                        v => (EstadoReserva)Enum.Parse(typeof(EstadoReserva), v))
                    .HasMaxLength(20);

                entity.HasOne(r => r.Usuario)
                    .WithMany()
                    .HasForeignKey(r => r.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_reservas_usuario_id");

                entity.HasOne(r => r.Funcion)
                    .WithMany()
                    .HasForeignKey(r => r.FuncionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_reservas_funcion_id");

                entity.HasIndex(r => new { r.UsuarioId, r.Estado })
                    .HasDatabaseName("idx_reservas_usuario_estado");
            });

            // Configurar tabla AsientoReserva
            modelBuilder.Entity<AsientoReserva>(entity =>
            {
                entity.ToTable("asientos_reserva");
                entity.HasKey(ar => ar.Id);

                entity.Property(ar => ar.ReservaId)
                    .IsRequired();

                entity.Property(ar => ar.AsientoId)
                    .IsRequired();

                entity.HasOne(ar => ar.Reserva)
                    .WithMany(r => r.Asientos)
                    .HasForeignKey(ar => ar.ReservaId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_asientos_reserva_reserva_id");

                entity.HasOne(ar => ar.Asiento)
                    .WithMany()
                    .HasForeignKey(ar => ar.AsientoId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_asientos_reserva_asiento_id");

                entity.HasIndex(ar => new { ar.ReservaId, ar.AsientoId })
                    .IsUnique()
                    .HasDatabaseName("idx_asientos_reserva_reserva_asiento_unique");
            });

            // Configurar tabla Venta
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("ventas");
                entity.HasKey(v => v.Id);

                entity.Property(v => v.ReservaId)
                    .IsRequired();

                entity.Property(v => v.MetodoPago)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(v => v.MontoTotal)
                    .IsRequired()
                    .HasColumnType("numeric(10, 2)");

                entity.Property(v => v.FechaVenta)
                    .IsRequired()
                    .HasColumnType("timestamp without time zone");

                entity.Property(v => v.CodigoQr)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.HasOne(v => v.Reserva)
                    .WithOne()
                    .HasForeignKey<Venta>(v => v.ReservaId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_ventas_reserva_id");

                entity.HasIndex(v => v.ReservaId)
                    .IsUnique()
                    .HasDatabaseName("idx_ventas_reserva_id_unique");
            });

            // Seed inicial de Roles
            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Nombre = "Cliente" },
                new Rol { Id = 2, Nombre = "Administrador" }
            );
        }
    }
}
