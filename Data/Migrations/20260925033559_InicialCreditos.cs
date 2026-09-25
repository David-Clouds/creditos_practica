using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditosPlataforma.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InicialCreditos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    IngresosMensuales = table.Column<decimal>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.CheckConstraint("CK_Cliente_IngresosMensuales", "IngresosMensuales > 0");
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoSolicitado = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    MotivoRechazo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCredito", x => x.Id);
                    table.CheckConstraint("CK_SolicitudCredito_Monto", "MontoSolicitado > 0");
                    table.ForeignKey(
                        name: "FK_SolicitudesCredito_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudCredito_UnaPendientePorCliente",
                table: "SolicitudesCredito",
                column: "ClienteId",
                unique: true,
                filter: "Estado = 'Pendiente'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesCredito");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
