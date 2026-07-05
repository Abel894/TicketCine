using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketCine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPeliculasSalasFuncionesAsientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "peliculas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Sinopsis = table.Column<string>(type: "text", nullable: false),
                    Genero = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    Clasificacion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RutaPoster = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peliculas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "salas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Filas = table.Column<int>(type: "integer", nullable: false),
                    Columnas = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "funciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeliculaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funciones", x => x.Id);
                    table.ForeignKey(
                        name: "fk_funciones_pelicula_id",
                        column: x => x.PeliculaId,
                        principalTable: "peliculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_funciones_sala_id",
                        column: x => x.SalaId,
                        principalTable: "salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FuncionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fila = table.Column<int>(type: "integer", nullable: false),
                    Columna = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asientos", x => x.Id);
                    table.ForeignKey(
                        name: "fk_asientos_funcion_id",
                        column: x => x.FuncionId,
                        principalTable: "funciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_asientos_funcion_fila_columna",
                table: "asientos",
                columns: new[] { "FuncionId", "Fila", "Columna" });

            migrationBuilder.CreateIndex(
                name: "idx_funciones_fechahora",
                table: "funciones",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_funciones_PeliculaId",
                table: "funciones",
                column: "PeliculaId");

            migrationBuilder.CreateIndex(
                name: "IX_funciones_SalaId",
                table: "funciones",
                column: "SalaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asientos");

            migrationBuilder.DropTable(
                name: "funciones");

            migrationBuilder.DropTable(
                name: "peliculas");

            migrationBuilder.DropTable(
                name: "salas");
        }
    }
}
