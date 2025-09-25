using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class SimplifyManpowerStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Manpower_Precinct",
                table: "Manpower");

            migrationBuilder.DropIndex(
                name: "IX_Manpower_Precinct_Year",
                table: "Manpower");

            migrationBuilder.DropIndex(
                name: "IX_Manpower_Year",
                table: "Manpower");

            migrationBuilder.DropColumn(
                name: "AllocatedCount",
                table: "Manpower");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Manpower");

            migrationBuilder.DropColumn(
                name: "CriticalThreshold",
                table: "Manpower");

            migrationBuilder.DropColumn(
                name: "MildThreshold",
                table: "Manpower");

            migrationBuilder.DropColumn(
                name: "ModerateThreshold",
                table: "Manpower");

            migrationBuilder.DropColumn(
                name: "Precinct",
                table: "Manpower");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "Manpower",
                newName: "HeadCount");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Manpower",
                newName: "PrecinctId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "Manpower",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')");

            migrationBuilder.AddColumn<Guid>(
                name: "PrecinctId",
                table: "Incident",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Precinct",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Population = table.Column<int>(type: "INTEGER", nullable: true),
                    AreaKm2 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ContactInfo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Precinct", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_PrecinctId",
                table: "Manpower",
                column: "PrecinctId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incident_PrecinctId",
                table: "Incident",
                column: "PrecinctId");

            migrationBuilder.CreateIndex(
                name: "IX_Precinct_Code",
                table: "Precinct",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Precinct_Name",
                table: "Precinct",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Incident_Precinct",
                table: "Incident",
                column: "PrecinctId",
                principalTable: "Precinct",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Manpower_Precinct",
                table: "Manpower",
                column: "PrecinctId",
                principalTable: "Precinct",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incident_Precinct",
                table: "Incident");

            migrationBuilder.DropForeignKey(
                name: "FK_Manpower_Precinct",
                table: "Manpower");

            migrationBuilder.DropTable(
                name: "Precinct");

            migrationBuilder.DropIndex(
                name: "IX_Manpower_PrecinctId",
                table: "Manpower");

            migrationBuilder.DropIndex(
                name: "IX_Incident_PrecinctId",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Manpower");

            migrationBuilder.DropColumn(
                name: "PrecinctId",
                table: "Incident");

            migrationBuilder.RenameColumn(
                name: "PrecinctId",
                table: "Manpower",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "HeadCount",
                table: "Manpower",
                newName: "Year");

            migrationBuilder.AddColumn<int>(
                name: "AllocatedCount",
                table: "Manpower",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Manpower",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "CriticalThreshold",
                table: "Manpower",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MildThreshold",
                table: "Manpower",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModerateThreshold",
                table: "Manpower",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Precinct",
                table: "Manpower",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_Precinct",
                table: "Manpower",
                column: "Precinct");

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_Precinct_Year",
                table: "Manpower",
                columns: new[] { "Precinct", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_Year",
                table: "Manpower",
                column: "Year");
        }
    }
}
