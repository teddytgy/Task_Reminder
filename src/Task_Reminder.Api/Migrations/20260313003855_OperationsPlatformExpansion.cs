using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Task_Reminder.Api.Migrations
{
    /// <inheritdoc />
    public partial class OperationsPlatformExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentWorkItemId",
                table: "task_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BalanceFollowUpWorkItemId",
                table: "task_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InsuranceWorkItemId",
                table: "task_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "appointment_work_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PatientReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AppointmentDateLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    AppointmentTimeLocal = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppointmentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfirmationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InsuranceStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BalanceStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_work_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "office_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfficeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BusinessHoursSummary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConfirmationLeadHours = table.Column<int>(type: "integer", nullable: false),
                    InsuranceVerificationLeadDays = table.Column<int>(type: "integer", nullable: false),
                    OverdueEscalationMinutes = table.Column<int>(type: "integer", nullable: false),
                    NoShowFollowUpDelayHours = table.Column<int>(type: "integer", nullable: false),
                    ManagerEscalationUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultReminderIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnableTodayBoard = table.Column<bool>(type: "boolean", nullable: false),
                    EnableTomorrowPrepBoard = table.Column<bool>(type: "boolean", nullable: false),
                    EnableCollectionsBoard = table.Column<bool>(type: "boolean", nullable: false),
                    EnableRecallBoard = table.Column<bool>(type: "boolean", nullable: false),
                    EnableManagerQueue = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_office_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_office_settings_users_ManagerEscalationUserId",
                        column: x => x.ManagerEscalationUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "balance_follow_up_work_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PatientReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AppointmentWorkItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmountDue = table.Column<decimal>(type: "numeric", nullable: false),
                    DueReasonNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FollowUpDateLocal = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_balance_follow_up_work_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_balance_follow_up_work_items_appointment_work_items_Appoint~",
                        column: x => x.AppointmentWorkItemId,
                        principalTable: "appointment_work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "insurance_work_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PatientReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CarrierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PlanName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MemberId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GroupNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PayerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppointmentDateLocal = table.Column<DateOnly>(type: "date", nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EligibilityStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationRequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CopayAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    DeductibleAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    AnnualMaximum = table.Column<decimal>(type: "numeric", nullable: true),
                    RemainingMaximum = table.Column<decimal>(type: "numeric", nullable: true),
                    FrequencyNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WaitingPeriodNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MissingInfoNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IssueType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SourceSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppointmentWorkItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insurance_work_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_insurance_work_items_appointment_work_items_AppointmentWork~",
                        column: x => x.AppointmentWorkItemId,
                        principalTable: "appointment_work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_insurance_work_items_users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "contact_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppointmentWorkItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    InsuranceWorkItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    BalanceFollowUpWorkItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contact_logs_appointment_work_items_AppointmentWorkItemId",
                        column: x => x.AppointmentWorkItemId,
                        principalTable: "appointment_work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contact_logs_balance_follow_up_work_items_BalanceFollowUpWo~",
                        column: x => x.BalanceFollowUpWorkItemId,
                        principalTable: "balance_follow_up_work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contact_logs_insurance_work_items_InsuranceWorkItemId",
                        column: x => x.InsuranceWorkItemId,
                        principalTable: "insurance_work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contact_logs_task_items_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "task_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contact_logs_users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_items_AppointmentWorkItemId",
                table: "task_items",
                column: "AppointmentWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_task_items_BalanceFollowUpWorkItemId",
                table: "task_items",
                column: "BalanceFollowUpWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_task_items_InsuranceWorkItemId",
                table: "task_items",
                column: "InsuranceWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_work_items_AppointmentDateLocal",
                table: "appointment_work_items",
                column: "AppointmentDateLocal");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_work_items_SourceSystem_SourceReference",
                table: "appointment_work_items",
                columns: new[] { "SourceSystem", "SourceReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_balance_follow_up_work_items_AppointmentWorkItemId",
                table: "balance_follow_up_work_items",
                column: "AppointmentWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_balance_follow_up_work_items_FollowUpDateLocal",
                table: "balance_follow_up_work_items",
                column: "FollowUpDateLocal");

            migrationBuilder.CreateIndex(
                name: "IX_contact_logs_AppointmentWorkItemId",
                table: "contact_logs",
                column: "AppointmentWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_contact_logs_BalanceFollowUpWorkItemId",
                table: "contact_logs",
                column: "BalanceFollowUpWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_contact_logs_InsuranceWorkItemId",
                table: "contact_logs",
                column: "InsuranceWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_contact_logs_PerformedAtUtc",
                table: "contact_logs",
                column: "PerformedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_contact_logs_PerformedByUserId",
                table: "contact_logs",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_contact_logs_TaskItemId",
                table: "contact_logs",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_work_items_AppointmentDateLocal",
                table: "insurance_work_items",
                column: "AppointmentDateLocal");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_work_items_AppointmentWorkItemId",
                table: "insurance_work_items",
                column: "AppointmentWorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_insurance_work_items_SourceSystem_SourceReference",
                table: "insurance_work_items",
                columns: new[] { "SourceSystem", "SourceReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_insurance_work_items_VerifiedByUserId",
                table: "insurance_work_items",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_office_settings_ManagerEscalationUserId",
                table: "office_settings",
                column: "ManagerEscalationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_task_items_appointment_work_items_AppointmentWorkItemId",
                table: "task_items",
                column: "AppointmentWorkItemId",
                principalTable: "appointment_work_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_task_items_balance_follow_up_work_items_BalanceFollowUpWork~",
                table: "task_items",
                column: "BalanceFollowUpWorkItemId",
                principalTable: "balance_follow_up_work_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_task_items_insurance_work_items_InsuranceWorkItemId",
                table: "task_items",
                column: "InsuranceWorkItemId",
                principalTable: "insurance_work_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_items_appointment_work_items_AppointmentWorkItemId",
                table: "task_items");

            migrationBuilder.DropForeignKey(
                name: "FK_task_items_balance_follow_up_work_items_BalanceFollowUpWork~",
                table: "task_items");

            migrationBuilder.DropForeignKey(
                name: "FK_task_items_insurance_work_items_InsuranceWorkItemId",
                table: "task_items");

            migrationBuilder.DropTable(
                name: "contact_logs");

            migrationBuilder.DropTable(
                name: "office_settings");

            migrationBuilder.DropTable(
                name: "balance_follow_up_work_items");

            migrationBuilder.DropTable(
                name: "insurance_work_items");

            migrationBuilder.DropTable(
                name: "appointment_work_items");

            migrationBuilder.DropIndex(
                name: "IX_task_items_AppointmentWorkItemId",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "IX_task_items_BalanceFollowUpWorkItemId",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "IX_task_items_InsuranceWorkItemId",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "AppointmentWorkItemId",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "BalanceFollowUpWorkItemId",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "InsuranceWorkItemId",
                table: "task_items");
        }
    }
}
