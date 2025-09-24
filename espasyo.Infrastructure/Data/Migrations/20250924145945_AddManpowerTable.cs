using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManpowerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Manpower",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Precinct = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AllocatedCount = table.Column<int>(type: "int", nullable: false),
                    MildThreshold = table.Column<int>(type: "int", nullable: false),
                    ModerateThreshold = table.Column<int>(type: "int", nullable: false),
                    CriticalThreshold = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manpower", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_Precinct",
                table: "Manpower",
                column: "Precinct");

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_Precinct_Year",
                table: "Manpower",
                columns: new[] { "Precinct", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_Year",
                table: "Manpower",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Manpower");
        }
    }
}