using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinAppdePromo.Migrations
{
    /// <inheritdoc />
    public partial class AddBusquedaUsuarioTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusquedasUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    NegocioId = table.Column<int>(type: "integer", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    Zona = table.Column<string>(type: "text", nullable: false),
                    FechaBusqueda = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TipoInteraccion = table.Column<int>(type: "integer", nullable: false),
                    Calificacion = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusquedasUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusquedasUsuario_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusquedasUsuario_Businesses_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Businesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusquedasUsuario_NegocioId",
                table: "BusquedasUsuario",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_BusquedasUsuario_UsuarioId",
                table: "BusquedasUsuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_BusquedasUsuario_Categoria",
                table: "BusquedasUsuario",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_BusquedasUsuario_Zona",
                table: "BusquedasUsuario",
                column: "Zona");

            migrationBuilder.CreateIndex(
                name: "IX_BusquedasUsuario_FechaBusqueda",
                table: "BusquedasUsuario",
                column: "FechaBusqueda");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusquedasUsuario");
        }
    }
}
