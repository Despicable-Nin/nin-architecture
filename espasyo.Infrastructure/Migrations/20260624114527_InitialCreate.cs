using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace espasyo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Precinct",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Barangay = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Population = table.Column<int>(type: "int", nullable: true),
                    AreaKm2 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Precinct", x => x.Id);
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
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    GeneratedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "")
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
                    PrecinctId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdditionalInformation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weather = table.Column<int>(type: "int", nullable: false),
                    IncidentDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TimeOfDay = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampInUnix = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incident_Precinct",
                        column: x => x.PrecinctId,
                        principalTable: "Precinct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Manpower",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrecinctId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Shift = table.Column<int>(type: "int", nullable: false),
                    HeadCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manpower", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manpower_Precinct",
                        column: x => x.PrecinctId,
                        principalTable: "Precinct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Street",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrecinctId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Street", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Street_Precinct",
                        column: x => x.PrecinctId,
                        principalTable: "Precinct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ForecastResult",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForecastRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Precinct = table.Column<int>(type: "int", nullable: false),
                    CrimeType = table.Column<int>(type: "int", nullable: false),
                    ClusterId = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
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

            migrationBuilder.InsertData(
                table: "Precinct",
                columns: new[] { "Id", "AreaKm2", "Barangay", "Code", "ContactInfo", "CreatedAt", "Description", "IsActive", "Latitude", "Longitude", "Population", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 23.5m, 0, "ALB", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(5740), new TimeSpan(0, 0, 0, 0, 0)), "Commercial and business district", true, null, null, 54000, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 8.2m, 7, "AAL", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(6676), new TimeSpan(0, 0, 0, 0, 0)), "High-income residential area", true, null, null, 25000, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 15.7m, 8, "SUC", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(6678), new TimeSpan(0, 0, 0, 0, 0)), "Mixed residential and commercial area", true, null, null, 42000, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 5.3m, 4, "POB", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(6679), new TimeSpan(0, 0, 0, 0, 0)), "City center and administrative area", true, null, null, 18000, null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 12.8m, 5, "PUT", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(6680), new TimeSpan(0, 0, 0, 0, 0)), "Residential area with moderate density", true, null, null, 35000, null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 10.4m, 6, "TUN", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(6681), new TimeSpan(0, 0, 0, 0, 0)), "Residential with some commercial areas", true, null, null, 28000, null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), 8.9m, 3, "CUP", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(6682), new TimeSpan(0, 0, 0, 0, 0)), "Smaller residential area", true, null, null, 22000, null },
                    { new Guid("88888888-8888-8888-8888-888888888888"), 11.6m, 1, "BAY", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(6683), new TimeSpan(0, 0, 0, 0, 0)), "Residential area", true, null, null, 31000, null },
                    { new Guid("99999999-9999-9999-9999-999999999999"), 9.8m, 2, "BUL", null, new DateTimeOffset(new DateTime(2026, 6, 24, 11, 45, 27, 217, DateTimeKind.Unspecified).AddTicks(6684), new TimeSpan(0, 0, 0, 0, 0)), "Residential area", true, null, null, 26000, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRun_CreatedAt",
                table: "AnalysisRun",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

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
                name: "IX_Incident_CaseId",
                table: "Incident",
                column: "CaseId",
                unique: true,
                filter: "[CaseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_PrecinctId",
                table: "Incident",
                column: "PrecinctId");

            migrationBuilder.CreateIndex(
                name: "IX_Manpower_PrecinctId_Shift",
                table: "Manpower",
                columns: new[] { "PrecinctId", "Shift" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Precinct_Barangay",
                table: "Precinct",
                column: "Barangay",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Precinct_Code",
                table: "Precinct",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Street_Name",
                table: "Street",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Street_PrecinctId",
                table: "Street",
                column: "PrecinctId");

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
                name: "AnalysisRun");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ForecastResult");

            migrationBuilder.DropTable(
                name: "Incident");

            migrationBuilder.DropTable(
                name: "Manpower");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Street");

            migrationBuilder.DropTable(
                name: "UserForecastPreference");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ForecastRun");

            migrationBuilder.DropTable(
                name: "Precinct");
        }
    }
}
