using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddClusterIdToForecastResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "ClusterId",
                table: "ForecastResult",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "ForecastResult");
        }
    }
}
