using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinAppdePromo.Migrations.PinDb
{
    /// <inheritdoc />
    public partial class AddBusinessSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessSchedule_Businesses_BusinessId",
                table: "BusinessSchedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessSchedule",
                table: "BusinessSchedule");

            migrationBuilder.RenameTable(
                name: "BusinessSchedule",
                newName: "BusinessSchedules");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessSchedule_BusinessId",
                table: "BusinessSchedules",
                newName: "IX_BusinessSchedules_BusinessId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessSchedules",
                table: "BusinessSchedules",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessSchedules_Businesses_BusinessId",
                table: "BusinessSchedules",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessSchedules_Businesses_BusinessId",
                table: "BusinessSchedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessSchedules",
                table: "BusinessSchedules");

            migrationBuilder.RenameTable(
                name: "BusinessSchedules",
                newName: "BusinessSchedule");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessSchedules_BusinessId",
                table: "BusinessSchedule",
                newName: "IX_BusinessSchedule_BusinessId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessSchedule",
                table: "BusinessSchedule",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessSchedule_Businesses_BusinessId",
                table: "BusinessSchedule",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
