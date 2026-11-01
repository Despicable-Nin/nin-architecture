using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Migrations.SqliteApplicationDb
{
    /// <inheritdoc />
    public partial class AddForecastCalibrationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EffectiveModelType",
                table: "ForecastRun",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HistoricalDataJson",
                table: "ForecastRun",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModelFallbackReason",
                table: "ForecastRun",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedModelType",
                table: "ForecastRun",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectiveModelType",
                table: "ForecastRun");

            migrationBuilder.DropColumn(
                name: "HistoricalDataJson",
                table: "ForecastRun");

            migrationBuilder.DropColumn(
                name: "ModelFallbackReason",
                table: "ForecastRun");

            migrationBuilder.DropColumn(
                name: "RequestedModelType",
                table: "ForecastRun");
        }
    }
}
