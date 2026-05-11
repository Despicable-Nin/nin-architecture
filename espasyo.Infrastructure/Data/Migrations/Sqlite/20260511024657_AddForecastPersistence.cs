using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddForecastPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForecastRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrecinctId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunAt = table.Column<string>(type: "TEXT", nullable: false),
                    Horizon = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceLevel = table.Column<double>(type: "float", nullable: false),
                    ModelType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSeries = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    GeneratedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastRun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastRun_Precinct",
                        column: x => x.PrecinctId,
                        principalTable: "Precinct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserForecastPreference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    DefaultHorizon = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 6),
                    DefaultConfidenceLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.94999999999999996),
                    DefaultModelType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "SSA"),
                    ShowEnsembleView = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    ShowHotspotTimeline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    EnabledTimeAnimation = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    PreferredTopN = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 10),
                    PreferredPrecincts = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PreferredCrimeTypes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserForecastPreference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForecastResult",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ForecastRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Precinct = table.Column<int>(type: "INTEGER", nullable: false),
                    CrimeType = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_ForecastResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastResult_ForecastRun",
                        column: x => x.ForecastRunId,
                        principalTable: "ForecastRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastResult_ForecastRunId",
                table: "ForecastResult",
                column: "ForecastRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastResult_Run_Precinct_Month_Year",
                table: "ForecastResult",
                columns: new[] { "ForecastRunId", "Precinct", "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastRun_PrecinctId",
                table: "ForecastRun",
                column: "PrecinctId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastRun_RunAt",
                table: "ForecastRun",
                column: "RunAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastRun_Status",
                table: "ForecastRun",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserForecastPreference_UserId",
                table: "UserForecastPreference",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForecastResult");

            migrationBuilder.DropTable(
                name: "UserForecastPreference");

            migrationBuilder.DropTable(
                name: "ForecastRun");
        }
    }
}
