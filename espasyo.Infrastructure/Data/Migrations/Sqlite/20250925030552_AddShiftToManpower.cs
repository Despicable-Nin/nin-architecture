using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddShiftToManpower : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Manpower_PrecinctId",
                table: "Manpower");

            migrationBuilder.AddColumn<int>(
                name: "Shift",
                table: "Manpower",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_PrecinctId_Shift",
                table: "Manpower",
                columns: new[] { "PrecinctId", "Shift" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Manpower_PrecinctId_Shift",
                table: "Manpower");

            migrationBuilder.DropColumn(
                name: "Shift",
                table: "Manpower");

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_PrecinctId",
                table: "Manpower",
                column: "PrecinctId",
                unique: true);
        }
    }
}
