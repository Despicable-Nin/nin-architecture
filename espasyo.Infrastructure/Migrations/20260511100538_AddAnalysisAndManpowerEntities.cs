using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Migrations
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClusterGroupsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QualityMetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManpowerRecommendation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForecastRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrecinctId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Shift = table.Column<int>(type: "int", nullable: false),
                    RecommendedHeadCount = table.Column<int>(type: "int", nullable: false),
                    PredictedWorkloadHours = table.Column<float>(type: "real", nullable: false),
                    ComplexityScore = table.Column<float>(type: "real", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(2808), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(3724), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(3725), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(3726), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(3727), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(3728), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(3729), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(3730), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 5, 32, 237, DateTimeKind.Unspecified).AddTicks(3731), new TimeSpan(0, 0, 0, 0, 0)));

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

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(5482), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(7205), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(7208), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(7209), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(7211), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(7212), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(7213), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(7214), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 3, 16, 36, 261, DateTimeKind.Unspecified).AddTicks(7216), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
