using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinAppdePromo.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Negocios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", nullable: false),
                    Calificacion = table.Column<double>(type: "REAL", nullable: false),
                    EstaAbierto = table.Column<bool>(type: "INTEGER", nullable: false),
                    Destacado = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImagenUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Horarios = table.Column<string>(type: "TEXT", nullable: false),
                    Contacto = table.Column<string>(type: "TEXT", nullable: false),
                    SitioWeb = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Negocios", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Negocios");
        }
    }
}
