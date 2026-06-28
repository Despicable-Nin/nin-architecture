using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Migrations.SqliteApplicationDb
{
    /// <inheritdoc />
    public partial class AddSpatialAndSeasonalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeasonalDecompositionResult",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ForecastRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Precinct = table.Column<int>(type: "INTEGER", nullable: false),
                    CrimeType = table.Column<int>(type: "INTEGER", nullable: false),
                    TrendData = table.Column<string>(type: "ntext", nullable: false),
                    SeasonalData = table.Column<string>(type: "ntext", nullable: false),
                    ResidualData = table.Column<string>(type: "ntext", nullable: false),
                    StrengthTrend = table.Column<double>(type: "float", nullable: false),
                    StrengthSeasonal = table.Column<double>(type: "float", nullable: false),
                    PeakMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    TroughMonth = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalDecompositionResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonalDecompositionResult_ForecastRun",
                        column: x => x.ForecastRunId,
                        principalTable: "ForecastRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpatialForecastResult",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ForecastRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Precinct = table.Column<int>(type: "INTEGER", nullable: false),
                    ClusterId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    PredictedValue = table.Column<double>(type: "float", nullable: false),
                    LowerBound = table.Column<double>(type: "float", nullable: false),
                    UpperBound = table.Column<double>(type: "float", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    RiskLevel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Trend = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpatialForecastResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpatialForecastResult_ForecastRun",
                        column: x => x.ForecastRunId,
                        principalTable: "ForecastRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalDecompositionResult_ForecastRunId",
                table: "SeasonalDecompositionResult",
                column: "ForecastRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SpatialForecastResult_ForecastRunId",
                table: "SpatialForecastResult",
                column: "ForecastRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeasonalDecompositionResult");

            migrationBuilder.DropTable(
                name: "SpatialForecastResult");
        }
    }
}
