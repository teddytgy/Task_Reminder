using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Task_Reminder.Api.Migrations
{
    /// <inheritdoc />
    public partial class FinalProductionReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedByDisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PerformedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_entries_users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "external_integration_provider_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_integration_provider_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "external_integration_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_integration_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_external_integration_runs_external_integration_provider_con~",
                        column: x => x.ProviderConfigId,
                        principalTable: "external_integration_provider_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_EntityType_PerformedAtUtc",
                table: "audit_entries",
                columns: new[] { "EntityType", "PerformedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_PerformedAtUtc",
                table: "audit_entries",
                column: "PerformedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_PerformedByUserId",
                table: "audit_entries",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_external_integration_provider_configs_ProviderType",
                table: "external_integration_provider_configs",
                column: "ProviderType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_integration_runs_ProviderConfigId_StartedAtUtc",
                table: "external_integration_runs",
                columns: new[] { "ProviderConfigId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "external_integration_runs");

            migrationBuilder.DropTable(
                name: "external_integration_provider_configs");
        }
    }
}
