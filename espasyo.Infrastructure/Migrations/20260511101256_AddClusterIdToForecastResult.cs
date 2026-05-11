using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClusterIdToForecastResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ClusterId",
                table: "ForecastResult",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(6378), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(7200), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(7202), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(7203), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(7205), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(7206), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(7207), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(7208), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 5, 11, 10, 12, 55, 332, DateTimeKind.Unspecified).AddTicks(7217), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "ForecastResult");

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
        }
    }
}
