using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace espasyo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrecinctSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Precinct",
                columns: new[] { "Id", "AreaKm2", "Barangay", "Code", "ContactInfo", "CreatedAt", "Description", "IsActive", "Latitude", "Longitude", "Population", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 23.5m, 0, "ALB", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(5872), new TimeSpan(0, 0, 0, 0, 0)), "Commercial and business district", true, null, null, 54000, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 8.2m, 7, "AAL", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6757), new TimeSpan(0, 0, 0, 0, 0)), "High-income residential area", true, null, null, 25000, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 15.7m, 8, "SUC", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6759), new TimeSpan(0, 0, 0, 0, 0)), "Mixed residential and commercial area", true, null, null, 42000, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 5.3m, 4, "POB", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6760), new TimeSpan(0, 0, 0, 0, 0)), "City center and administrative area", true, null, null, 18000, null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 12.8m, 5, "PUT", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6762), new TimeSpan(0, 0, 0, 0, 0)), "Residential area with moderate density", true, null, null, 35000, null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 10.4m, 6, "TUN", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6763), new TimeSpan(0, 0, 0, 0, 0)), "Residential with some commercial areas", true, null, null, 28000, null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), 8.9m, 3, "CUP", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6764), new TimeSpan(0, 0, 0, 0, 0)), "Smaller residential area", true, null, null, 22000, null },
                    { new Guid("88888888-8888-8888-8888-888888888888"), 11.6m, 1, "BAY", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6766), new TimeSpan(0, 0, 0, 0, 0)), "Residential area", true, null, null, 31000, null },
                    { new Guid("99999999-9999-9999-9999-999999999999"), 9.8m, 2, "BUL", null, new DateTimeOffset(new DateTime(2025, 9, 25, 12, 1, 42, 237, DateTimeKind.Unspecified).AddTicks(6767), new TimeSpan(0, 0, 0, 0, 0)), "Residential area", true, null, null, 26000, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DeleteData(
                table: "Precinct",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));
        }
    }
}
