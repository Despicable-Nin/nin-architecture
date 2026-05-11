using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Migrations
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrecinctId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    ConfidenceLevel = table.Column<double>(type: "float", nullable: false),
                    ModelType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalSeries = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    GeneratedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DefaultHorizon = table.Column<int>(type: "int", nullable: false, defaultValue: 6),
                    DefaultConfidenceLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.94999999999999996),
                    DefaultModelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "SSA"),
                    ShowEnsembleView = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowHotspotTimeline = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EnabledTimeAnimation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PreferredTopN = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    PreferredPrecincts = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreferredCrimeTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserForecastPreference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForecastResult",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForecastRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Precinct = table.Column<int>(type: "int", nullable: false),
                    CrimeType = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    PredictedValue = table.Column<double>(type: "float", nullable: false),
                    LowerBound = table.Column<double>(type: "float", nullable: false),
                    UpperBound = table.Column<double>(type: "float", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Trend = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
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

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(4120), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(5052), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(5054), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(5055), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(5056), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(5057), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(5058), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(5059), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 2, 47, 16, 820, DateTimeKind.Unspecified).AddTicks(5059), new TimeSpan(0, 0, 0, 0, 0)));

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

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(5872), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6757), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6759), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6760), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6762), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6763), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6764), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6766), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6767), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
