using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Task_Reminder.Api.Migrations
{
    /// <inheritdoc />
    public partial class OfficeWorkflowExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "FrontDesk");

            migrationBuilder.AddColumn<int>(
                name: "EscalateAfterMinutes",
                table: "task_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscalateToUserId",
                table: "task_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAtUtc",
                table: "task_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "GeneratedForDateLocal",
                table: "task_items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GeneratedFromRecurringTaskDefinitionId",
                table: "task_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recurring_task_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReminderRepeatMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    EscalateAfterMinutes = table.Column<int>(type: "integer", nullable: true),
                    EscalateToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecurrenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: false),
                    DaysOfWeek = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    TimeOfDayLocal = table.Column<TimeSpan>(type: "interval", nullable: true),
                    StartDateLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDateLocal = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastGeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_task_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_task_definitions_users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_task_definitions_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_task_definitions_users_EscalateToUserId",
                        column: x => x.EscalateToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiveAssignedTaskReminders = table.Column<bool>(type: "boolean", nullable: false),
                    ReceiveUnassignedTaskReminders = table.Column<bool>(type: "boolean", nullable: false),
                    ReceiveOverdueEscalationAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    ReceiveRecurringTaskGenerationAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSoundForUrgentReminders = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notification_preferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_notification_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE users
                SET "Role" = CASE
                    WHEN "Username" = 'emma' THEN 'Manager'
                    WHEN "Username" = 'ava' THEN 'Admin'
                    ELSE 'FrontDesk'
                END;
                """);

            migrationBuilder.Sql("""
                INSERT INTO user_notification_preferences
                    ("UserId", "ReceiveAssignedTaskReminders", "ReceiveUnassignedTaskReminders", "ReceiveOverdueEscalationAlerts", "ReceiveRecurringTaskGenerationAlerts", "EnableSoundForUrgentReminders")
                SELECT
                    "Id",
                    TRUE,
                    TRUE,
                    CASE WHEN "Role" IN ('Manager', 'Admin') THEN TRUE ELSE FALSE END,
                    TRUE,
                    CASE WHEN "Role" IN ('Manager', 'Admin') THEN TRUE ELSE FALSE END
                FROM users
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM user_notification_preferences prefs
                    WHERE prefs."UserId" = users."Id"
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_task_items_EscalateToUserId",
                table: "task_items",
                column: "EscalateToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_task_items_GeneratedFromRecurringTaskDefinitionId_Generated~",
                table: "task_items",
                columns: new[] { "GeneratedFromRecurringTaskDefinitionId", "GeneratedForDateLocal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_task_definitions_AssignedUserId",
                table: "recurring_task_definitions",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_task_definitions_CreatedByUserId",
                table: "recurring_task_definitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_task_definitions_EscalateToUserId",
                table: "recurring_task_definitions",
                column: "EscalateToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_task_definitions_IsActive",
                table: "recurring_task_definitions",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_task_items_recurring_task_definitions_GeneratedFromRecurrin~",
                table: "task_items",
                column: "GeneratedFromRecurringTaskDefinitionId",
                principalTable: "recurring_task_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_task_items_users_EscalateToUserId",
                table: "task_items",
                column: "EscalateToUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_items_recurring_task_definitions_GeneratedFromRecurrin~",
                table: "task_items");

            migrationBuilder.DropForeignKey(
                name: "FK_task_items_users_EscalateToUserId",
                table: "task_items");

            migrationBuilder.DropTable(
                name: "recurring_task_definitions");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropIndex(
                name: "IX_task_items_EscalateToUserId",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "IX_task_items_GeneratedFromRecurringTaskDefinitionId_Generated~",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EscalateAfterMinutes",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "EscalateToUserId",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "EscalatedAtUtc",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "GeneratedForDateLocal",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "GeneratedFromRecurringTaskDefinitionId",
                table: "task_items");
        }
    }
}
