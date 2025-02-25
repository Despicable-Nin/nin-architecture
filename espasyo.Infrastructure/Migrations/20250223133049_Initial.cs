using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incident",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SanitizedAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CrimeType = table.Column<int>(type: "int", nullable: false),
                    Motive = table.Column<int>(type: "int", nullable: false),
                    PoliceDistrict = table.Column<int>(type: "int", nullable: false),
                    AdditionalInformation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weather = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    TimestampInUnix = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_CaseId",
                table: "Incident",
                column: "CaseId",
                unique: true,
                filter: "[CaseId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incident");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
