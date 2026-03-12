using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Task_Reminder.Api.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "task_items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                AssignedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                ClaimedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                SnoozeUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PatientReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                ReminderRepeatMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_task_items", x => x.Id);
                table.ForeignKey(
                    name: "FK_task_items_users_AssignedUserId",
                    column: x => x.AssignedUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_task_items_users_ClaimedByUserId",
                    column: x => x.ClaimedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_task_items_users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "task_history",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TaskItemId = table.Column<Guid>(type: "uuid", nullable: false),
                ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                OldStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                PerformedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_task_history", x => x.Id);
                table.ForeignKey(
                    name: "FK_task_history_task_items_TaskItemId",
                    column: x => x.TaskItemId,
                    principalTable: "task_items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_task_history_users_PerformedByUserId",
                    column: x => x.PerformedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(name: "IX_task_history_PerformedByUserId", table: "task_history", column: "PerformedByUserId");
        migrationBuilder.CreateIndex(name: "IX_task_history_TaskItemId", table: "task_history", column: "TaskItemId");
        migrationBuilder.CreateIndex(name: "IX_task_items_AssignedUserId", table: "task_items", column: "AssignedUserId");
        migrationBuilder.CreateIndex(name: "IX_task_items_ClaimedByUserId", table: "task_items", column: "ClaimedByUserId");
        migrationBuilder.CreateIndex(name: "IX_task_items_CreatedByUserId", table: "task_items", column: "CreatedByUserId");
        migrationBuilder.CreateIndex(name: "IX_task_items_DueAtUtc", table: "task_items", column: "DueAtUtc");
        migrationBuilder.CreateIndex(name: "IX_task_items_Status", table: "task_items", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_users_Username", table: "users", column: "Username", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "task_history");
        migrationBuilder.DropTable(name: "task_items");
        migrationBuilder.DropTable(name: "users");
    }
}
