using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinAppdePromo.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoAuth",
                table: "Usuarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoAuth",
                table: "Usuarios");
        }
    }
}
