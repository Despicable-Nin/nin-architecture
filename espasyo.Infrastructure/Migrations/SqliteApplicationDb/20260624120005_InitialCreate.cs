using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace espasyo.Infrastructure.Migrations.SqliteApplicationDb
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParametersJson = table.Column<string>(type: "TEXT", nullable: false),
                    ClusterGroupsJson = table.Column<string>(type: "TEXT", nullable: false),
                    QualityMetricsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<string>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Precinct",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Barangay = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Population = table.Column<int>(type: "INTEGER", nullable: true),
                    AreaKm2 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ContactInfo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Precinct", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
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
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
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
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
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
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
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
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrecinctId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunAt = table.Column<string>(type: "TEXT", nullable: false),
                    Horizon = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceLevel = table.Column<double>(type: "float", nullable: false),
                    ModelType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSeries = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    GeneratedById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: "")
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    SanitizedAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    CrimeType = table.Column<int>(type: "INTEGER", nullable: false),
                    Motive = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecinctId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdditionalInformation = table.Column<string>(type: "TEXT", nullable: true),
                    Weather = table.Column<int>(type: "INTEGER", nullable: false),
                    IncidentDateTime = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeOfDay = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampInUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrecinctId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Shift = table.Column<int>(type: "INTEGER", nullable: false),
                    HeadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PrecinctId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ForecastRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Precinct = table.Column<int>(type: "INTEGER", nullable: false),
                    CrimeType = table.Column<int>(type: "INTEGER", nullable: false),
                    ClusterId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
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

            migrationBuilder.InsertData(
                table: "Precinct",
                columns: new[] { "Id", "AreaKm2", "Barangay", "Code", "ContactInfo", "CreatedAt", "Description", "IsActive", "Latitude", "Longitude", "Population", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 23.5m, 0, "ALB", null, "2025-01-01T00:00:00.0000000+00:00", "Commercial and business district", true, 14.41988703m, 121.04215000m, 54000, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 8.2m, 7, "AAL", null, "2025-01-01T00:00:00.0000000+00:00", "High-income residential area", true, 14.40921556m, 121.02792663m, 25000, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 15.7m, 8, "SUC", null, "2025-01-01T00:00:00.0000000+00:00", "Mixed residential and commercial area", true, 14.45617214m, 121.05150790m, 42000, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 5.3m, 4, "POB", null, "2025-01-01T00:00:00.0000000+00:00", "City center and administrative area", true, 14.38412727m, 121.03881203m, 18000, null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 12.8m, 5, "PUT", null, "2025-01-01T00:00:00.0000000+00:00", "Residential area with moderate density", true, 14.39666349m, 121.03928228m, 35000, null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 10.4m, 6, "TUN", null, "2025-01-01T00:00:00.0000000+00:00", "Residential with some commercial areas", true, 14.37248274m, 121.03912851m, 28000, null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), 8.9m, 3, "CUP", null, "2025-01-01T00:00:00.0000000+00:00", "Smaller residential area", true, 14.43160272m, 121.04099563m, 22000, null },
                    { new Guid("88888888-8888-8888-8888-888888888888"), 11.6m, 1, "BAY", null, "2025-01-01T00:00:00.0000000+00:00", "Residential area", true, 14.40965584m, 121.04746765m, 31000, null },
                    { new Guid("99999999-9999-9999-9999-999999999999"), 9.8m, 2, "BUL", null, "2025-01-01T00:00:00.0000000+00:00", "Residential area", true, 14.44418345m, 121.04930401m, 26000, null }
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
                unique: true);

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
                unique: true);

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
                unique: true);

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
