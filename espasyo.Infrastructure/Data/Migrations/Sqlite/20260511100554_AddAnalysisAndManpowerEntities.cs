using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddAnalysisAndManpowerEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParametersJson = table.Column<string>(type: "TEXT", nullable: false),
                    ClusterGroupsJson = table.Column<string>(type: "TEXT", nullable: false),
                    QualityMetricsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManpowerRecommendation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ForecastRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrecinctId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Shift = table.Column<int>(type: "INTEGER", nullable: false),
                    RecommendedHeadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PredictedWorkloadHours = table.Column<float>(type: "real", nullable: false),
                    ComplexityScore = table.Column<float>(type: "real", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Justification = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManpowerRecommendation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManpowerRecommendation_ForecastRun",
                        column: x => x.ForecastRunId,
                        principalTable: "ForecastRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManpowerRecommendation_Precinct",
                        column: x => x.PrecinctId,
                        principalTable: "Precinct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRun_CreatedAt",
                table: "AnalysisRun",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerRecommendation_ForecastRunId",
                table: "ManpowerRecommendation",
                column: "ForecastRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerRecommendation_PrecinctId",
                table: "ManpowerRecommendation",
                column: "PrecinctId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisRun");

            migrationBuilder.DropTable(
                name: "ManpowerRecommendation");
        }
    }
}
