using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketCine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReservasYVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FuncionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservas", x => x.Id);
                    table.ForeignKey(
                        name: "fk_reservas_funcion_id",
                        column: x => x.FuncionId,
                        principalTable: "funciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reservas_usuario_id",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asientos_reserva",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsientoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asientos_reserva", x => x.Id);
                    table.ForeignKey(
                        name: "fk_asientos_reserva_asiento_id",
                        column: x => x.AsientoId,
                        principalTable: "asientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asientos_reserva_reserva_id",
                        column: x => x.ReservaId,
                        principalTable: "reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ventas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FechaVenta = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CodigoQr = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas", x => x.Id);
                    table.ForeignKey(
                        name: "fk_ventas_reserva_id",
                        column: x => x.ReservaId,
                        principalTable: "reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_asientos_reserva_reserva_asiento_unique",
                table: "asientos_reserva",
                columns: new[] { "ReservaId", "AsientoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asientos_reserva_AsientoId",
                table: "asientos_reserva",
                column: "AsientoId");

            migrationBuilder.CreateIndex(
                name: "idx_reservas_usuario_estado",
                table: "reservas",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_reservas_FuncionId",
                table: "reservas",
                column: "FuncionId");

            migrationBuilder.CreateIndex(
                name: "idx_ventas_reserva_id_unique",
                table: "ventas",
                column: "ReservaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asientos_reserva");

            migrationBuilder.DropTable(
                name: "ventas");

            migrationBuilder.DropTable(
                name: "reservas");
        }
    }
}
