using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Infrastructure.Services;
using Task_Reminder.Shared;
using Xunit;

namespace Task_Reminder.Tests;

public sealed class WorkflowServicesTests
{
    [Fact]
    public async Task AppointmentService_Confirm_Action_Updates_Appointment_Status()
    {
        await using var dbContext = CreateDbContext();
        var auditService = new FakeAuditService();
        var contactLogService = new ContactLogService(dbContext, auditService);
        var service = new AppointmentService(dbContext, new FakeTaskService(), new FakeWorkflowAutomationService(), contactLogService, auditService, NullLogger<AppointmentService>.Instance);

        var created = await service.CreateAsync(new CreateAppointmentWorkItemRequest
        {
            PatientName = "Jane Doe",
            PatientReference = "PT-100",
            AppointmentDateLocal = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            AppointmentTimeLocal = new TimeSpan(9, 0, 0),
            AppointmentType = "Hygiene"
        }, CancellationToken.None);

        var updated = await service.ApplyActionAsync(created.Id, "confirm", new AppointmentActionRequest(), CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(AppointmentStatus.Confirmed, updated!.Status);
        Assert.Equal(AppointmentConfirmationStatus.Confirmed, updated.ConfirmationStatus);
    }

    [Fact]
    public async Task InsuranceWorkService_Verified_Status_Sets_Completed_Timestamps()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Users.Add(new Task_Reminder.Api.Domain.Entities.User
        {
            Id = userId,
            DisplayName = "Mia",
            Username = "mia",
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new InsuranceWorkService(dbContext, new FakeTaskService(), new FakeWorkflowAutomationService(), new FakeAuditService(), NullLogger<InsuranceWorkService>.Instance);
        var created = await service.CreateAsync(new CreateInsuranceWorkItemRequest
        {
            PatientName = "John Smith",
            PatientReference = "PT-200",
            CarrierName = "Delta Dental",
            PlanName = "PPO"
        }, CancellationToken.None);

        var updated = await service.UpdateStatusAsync(created.Id, new InsuranceStatusUpdateRequest
        {
            UserId = userId,
            VerificationStatus = InsuranceVerificationStatus.Verified
        }, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(InsuranceVerificationStatus.Verified, updated!.VerificationStatus);
        Assert.Equal(userId, updated.VerifiedByUserId);
        Assert.NotNull(updated.VerificationCompletedAtUtc);
    }

    [Fact]
    public async Task ImportService_Skips_Duplicate_Appointment_Rows()
    {
        await using var dbContext = CreateDbContext();
        var service = new ImportService(dbContext, new FakeWorkflowAutomationService(), new FakeAuditService(), NullLogger<ImportService>.Instance);

        var payload = """
            [
              {
                "patientName": "Alex Patient",
                "patientReference": "PT-300",
                "appointmentDateLocal": "2026-03-20",
                "appointmentTimeLocal": "09:00:00",
                "appointmentType": "Consult",
                "sourceReference": "APT-1"
              },
              {
                "patientName": "Alex Patient",
                "patientReference": "PT-300",
                "appointmentDateLocal": "2026-03-20",
                "appointmentTimeLocal": "09:00:00",
                "appointmentType": "Consult",
                "sourceReference": "APT-1"
              }
            ]
            """;

        var result = await service.ImportAppointmentsAsync(new ImportAppointmentsRequest
        {
            Format = ImportFormat.Json,
            Content = payload,
            SourceSystem = "SampleImport"
        }, CancellationToken.None);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    [Fact]
    public async Task ContactLogService_Creates_And_Lists_Contact_Entries()
    {
        await using var dbContext = CreateDbContext();
        var appointmentId = Guid.NewGuid();
        dbContext.AppointmentWorkItems.Add(new Task_Reminder.Api.Domain.Entities.AppointmentWorkItem
        {
            Id = appointmentId,
            PatientName = "Alex Patient",
            PatientReference = "PT-400",
            AppointmentDateLocal = DateOnly.FromDateTime(DateTime.Today),
            AppointmentTimeLocal = new TimeSpan(8, 0, 0),
            AppointmentType = "Exam",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new ContactLogService(dbContext, new FakeAuditService());
        await service.CreateAsync(new CreateContactLogRequest
        {
            AppointmentWorkItemId = appointmentId,
            ContactType = ContactType.Call,
            Outcome = ContactOutcome.Reached,
            Notes = "Patient confirmed.",
            PerformedByUserId = null
        }, CancellationToken.None);

        var items = await service.ListAsync(null, appointmentId, null, null, CancellationToken.None);
        Assert.Single(items);
        Assert.Equal(ContactOutcome.Reached, items[0].Outcome);
    }

    private static TaskReminderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TaskReminderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TaskReminderDbContext(options);
    }

    private sealed class FakeWorkflowAutomationService : IWorkflowAutomationService
    {
        public Task EnsureAppointmentTasksAsync(Guid appointmentWorkItemId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task EnsureInsuranceTasksAsync(Guid insuranceWorkItemId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task EnsureBalanceTasksAsync(Guid balanceFollowUpWorkItemId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task EnsureOperationalCoverageAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTaskService : ITaskService
    {
        public Task<TaskItemDto> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken) => Task.FromResult(new TaskItemDto { Id = Guid.NewGuid(), Title = request.Title });
        public Task<IReadOnlyList<TaskItemDto>> ListAsync(TaskQueryParameters query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<TaskItemDto>>([]);
        public Task<TaskItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<TaskItemDto?>(null);
        public Task<TaskItemDto?> AssignAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken) => Task.FromResult<TaskItemDto?>(null);
        public Task<TaskItemDto?> ClaimAsync(Guid taskId, ClaimTaskRequest request, CancellationToken cancellationToken) => Task.FromResult<TaskItemDto?>(null);
        public Task<TaskItemDto?> SnoozeAsync(Guid taskId, SnoozeTaskRequest request, CancellationToken cancellationToken) => Task.FromResult<TaskItemDto?>(null);
        public Task<TaskItemDto?> CompleteAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken) => Task.FromResult<TaskItemDto?>(null);
        public Task<TaskItemDto?> CancelAsync(Guid taskId, CancelTaskRequest request, CancellationToken cancellationToken) => Task.FromResult<TaskItemDto?>(null);
        public Task<TaskHistoryDto?> AddCommentAsync(Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken) => Task.FromResult<TaskHistoryDto?>(null);
        public Task<IReadOnlyList<TaskHistoryDto>> GetHistoryAsync(Guid taskId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<TaskHistoryDto>>([]);
        public Task<int> MarkOverdueTasksAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task WriteAsync(string entityType, Guid? entityId, string actionType, string summary, string? details, Guid? performedByUserId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IReadOnlyList<AuditEntryDto>> ListAsync(AuditQueryParameters query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AuditEntryDto>>([]);
    }
}
