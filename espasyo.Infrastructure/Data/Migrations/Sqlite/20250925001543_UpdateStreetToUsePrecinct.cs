using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace espasyo.Infrastructure.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UpdateStreetToUsePrecinct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "Street");

            migrationBuilder.AddColumn<Guid>(
                name: "PrecinctId",
                table: "Street",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Street_Name",
                table: "Street",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Street_PrecinctId",
                table: "Street",
                column: "PrecinctId");

            migrationBuilder.AddForeignKey(
                name: "FK_Street_Precinct",
                table: "Street",
                column: "PrecinctId",
                principalTable: "Precinct",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Street_Precinct",
                table: "Street");

            migrationBuilder.DropIndex(
                name: "IX_Street_Name",
                table: "Street");

            migrationBuilder.DropIndex(
                name: "IX_Street_PrecinctId",
                table: "Street");

            migrationBuilder.DropColumn(
                name: "PrecinctId",
                table: "Street");

            migrationBuilder.AddColumn<int>(
                name: "Barangay",
                table: "Street",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
