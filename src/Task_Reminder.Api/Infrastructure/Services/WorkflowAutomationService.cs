using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class WorkflowAutomationService(
    TaskReminderDbContext dbContext,
    TaskBroadcastService broadcastService,
    ILogger<WorkflowAutomationService> logger) : IWorkflowAutomationService
{
    public Task EnsureAppointmentTasksAsync(Guid appointmentWorkItemId, CancellationToken cancellationToken)
        => EnsureAppointmentTasksInternalAsync(appointmentWorkItemId, cancellationToken);

    public Task EnsureInsuranceTasksAsync(Guid insuranceWorkItemId, CancellationToken cancellationToken)
        => EnsureInsuranceTasksInternalAsync(insuranceWorkItemId, cancellationToken);

    public Task EnsureBalanceTasksAsync(Guid balanceFollowUpWorkItemId, CancellationToken cancellationToken)
        => EnsureBalanceTasksInternalAsync(balanceFollowUpWorkItemId, cancellationToken);

    public async Task EnsureOperationalCoverageAsync(CancellationToken cancellationToken)
    {
        var appointments = await dbContext.AppointmentWorkItems.AsNoTracking().Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (var appointmentId in appointments)
        {
            await EnsureAppointmentTasksInternalAsync(appointmentId, cancellationToken);
        }

        var insuranceItems = await dbContext.InsuranceWorkItems.AsNoTracking().Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (var insuranceId in insuranceItems)
        {
            await EnsureInsuranceTasksInternalAsync(insuranceId, cancellationToken);
        }

        var balances = await dbContext.BalanceFollowUpWorkItems.AsNoTracking().Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (var balanceId in balances)
        {
            await EnsureBalanceTasksInternalAsync(balanceId, cancellationToken);
        }
    }

    private async Task EnsureAppointmentTasksInternalAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.AppointmentWorkItems.FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        if (appointment is null)
        {
            return;
        }

        var settings = await GetSettingsAsync(cancellationToken);
        var appointmentDateTimeLocal = appointment.AppointmentDateLocal.ToDateTime(TimeOnly.FromTimeSpan(appointment.AppointmentTimeLocal));
        var nowLocal = DateTime.Now;
        var leadHours = settings?.ConfirmationLeadHours ?? 24;
        var insuranceLeadDays = settings?.InsuranceVerificationLeadDays ?? 2;

        if (appointment.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed)
        {
            if (appointmentDateTimeLocal <= nowLocal.AddHours(leadHours) &&
                appointment.ConfirmationStatus is AppointmentConfirmationStatus.NotStarted or AppointmentConfirmationStatus.Pending or AppointmentConfirmationStatus.NoAnswer or AppointmentConfirmationStatus.LeftVoicemail)
            {
                await EnsureTaskAsync(
                    $"Confirm appointment for {appointment.PatientName}",
                    "Confirm appointment details and patient attendance.",
                    TaskCategory.AppointmentConfirmation,
                    TaskPriority.High,
                    appointmentDateTimeLocal.ToUniversalTime(),
                    appointment.PatientReference,
                    appointment.Notes,
                    appointmentWorkItemId: appointment.Id,
                    insuranceWorkItemId: null,
                    balanceWorkItemId: null,
                    cancellationToken: cancellationToken);
            }

            if (appointment.InsuranceStatus == AppointmentInsuranceStatus.PendingVerification &&
                appointment.AppointmentDateLocal <= DateOnly.FromDateTime(nowLocal.AddDays(insuranceLeadDays)))
            {
                await EnsureTaskAsync(
                    $"Verify insurance for {appointment.PatientName}",
                    "Review active coverage and benefits before the appointment.",
                    TaskCategory.InsuranceVerification,
                    TaskPriority.High,
                    appointmentDateTimeLocal.AddDays(-1).ToUniversalTime(),
                    appointment.PatientReference,
                    appointment.Notes,
                    appointmentWorkItemId: appointment.Id,
                    insuranceWorkItemId: null,
                    balanceWorkItemId: null,
                    cancellationToken: cancellationToken);
            }

            if (appointment.BalanceStatus == AppointmentBalanceStatus.BalanceDue)
            {
                await EnsureTaskAsync(
                    $"Collect balance reminder for {appointment.PatientName}",
                    "Review outstanding balance before the scheduled appointment.",
                    TaskCategory.BalanceCollection,
                    TaskPriority.Medium,
                    appointmentDateTimeLocal.ToUniversalTime(),
                    appointment.PatientReference,
                    appointment.Notes,
                    appointmentWorkItemId: appointment.Id,
                    insuranceWorkItemId: null,
                    balanceWorkItemId: null,
                    cancellationToken: cancellationToken);
            }
        }

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Rescheduled)
        {
            await EnsureTaskAsync(
                $"Reschedule follow-up for {appointment.PatientName}",
                "Reach back out and help the patient choose a new appointment.",
                TaskCategory.Recall,
                TaskPriority.Medium,
                DateTime.UtcNow.AddHours(4),
                appointment.PatientReference,
                appointment.Notes,
                appointmentWorkItemId: appointment.Id,
                insuranceWorkItemId: null,
                balanceWorkItemId: null,
                cancellationToken: cancellationToken);
        }

        if (appointment.Status == AppointmentStatus.NoShow)
        {
            await EnsureTaskAsync(
                $"No-show follow-up for {appointment.PatientName}",
                "Contact the patient after the missed visit and offer a reschedule.",
                TaskCategory.TreatmentFollowUp,
                TaskPriority.High,
                DateTime.UtcNow.AddHours(settings?.NoShowFollowUpDelayHours ?? 4),
                appointment.PatientReference,
                appointment.Notes,
                appointmentWorkItemId: appointment.Id,
                insuranceWorkItemId: null,
                balanceWorkItemId: null,
                cancellationToken: cancellationToken);
        }
    }

    private async Task EnsureInsuranceTasksInternalAsync(Guid insuranceId, CancellationToken cancellationToken)
    {
        var insurance = await dbContext.InsuranceWorkItems.FirstOrDefaultAsync(x => x.Id == insuranceId, cancellationToken);
        if (insurance is null)
        {
            return;
        }

        var settings = await GetSettingsAsync(cancellationToken);

        if (insurance.VerificationStatus is InsuranceVerificationStatus.NotStarted or InsuranceVerificationStatus.Pending or InsuranceVerificationStatus.InProgress)
        {
            await EnsureTaskAsync(
                $"Insurance verification for {insurance.PatientName}",
                "Verify eligibility, benefits, and plan details.",
                TaskCategory.InsuranceVerification,
                TaskPriority.High,
                DateTime.UtcNow.AddHours(2),
                insurance.PatientReference,
                insurance.Notes,
                appointmentWorkItemId: insurance.AppointmentWorkItemId,
                insuranceWorkItemId: insurance.Id,
                balanceWorkItemId: null,
                cancellationToken: cancellationToken);
        }

        if (insurance.IssueType.HasValue || insurance.VerificationStatus is InsuranceVerificationStatus.Failed or InsuranceVerificationStatus.NeedsManualReview)
        {
            await EnsureTaskAsync(
                $"Insurance issue follow-up for {insurance.PatientName}",
                $"Resolve insurance issue: {insurance.IssueType?.ToString() ?? insurance.VerificationStatus.ToString()}.",
                TaskCategory.InsuranceVerification,
                TaskPriority.Urgent,
                DateTime.UtcNow.AddHours(1),
                insurance.PatientReference,
                insurance.Notes,
                appointmentWorkItemId: insurance.AppointmentWorkItemId,
                insuranceWorkItemId: insurance.Id,
                balanceWorkItemId: null,
                cancellationToken: cancellationToken,
                assignedUserId: settings?.ManagerEscalationUserId);
        }
    }

    private async Task EnsureBalanceTasksInternalAsync(Guid balanceId, CancellationToken cancellationToken)
    {
        var balance = await dbContext.BalanceFollowUpWorkItems.FirstOrDefaultAsync(x => x.Id == balanceId, cancellationToken);
        if (balance is null)
        {
            return;
        }

        if (balance.Status is BalanceFollowUpStatus.Collected)
        {
            return;
        }

        var dueAtUtc = balance.FollowUpDateLocal?.ToDateTime(new TimeOnly(9, 0)).ToUniversalTime() ?? DateTime.UtcNow.AddHours(2);
        await EnsureTaskAsync(
            $"Balance follow-up for {balance.PatientName}",
            $"Outstanding balance: {balance.AmountDue:C}.",
            TaskCategory.BalanceCollection,
            TaskPriority.Medium,
            dueAtUtc,
            balance.PatientReference,
            balance.Notes,
            appointmentWorkItemId: balance.AppointmentWorkItemId,
            insuranceWorkItemId: null,
            balanceWorkItemId: balance.Id,
            cancellationToken: cancellationToken);
    }

    private async Task EnsureTaskAsync(
        string title,
        string description,
        TaskCategory category,
        TaskPriority priority,
        DateTime? dueAtUtc,
        string patientReference,
        string? notes,
        Guid? appointmentWorkItemId,
        Guid? insuranceWorkItemId,
        Guid? balanceWorkItemId,
        CancellationToken cancellationToken,
        Guid? assignedUserId = null)
    {
        var existing = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Title == title &&
                x.AppointmentWorkItemId == appointmentWorkItemId &&
                x.InsuranceWorkItemId == insuranceWorkItemId &&
                x.BalanceFollowUpWorkItemId == balanceWorkItemId &&
                x.Status != TaskStatus.Completed &&
                x.Status != TaskStatus.Cancelled,
                cancellationToken);

        if (existing is not null)
        {
            return;
        }

        var settings = await GetSettingsAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Category = category,
            Priority = priority,
            Status = assignedUserId.HasValue ? TaskStatus.Assigned : TaskStatus.New,
            AssignedUserId = assignedUserId,
            DueAtUtc = dueAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PatientReference = patientReference,
            Notes = notes,
            ReminderRepeatMinutes = settings?.DefaultReminderIntervalMinutes ?? 30,
            EscalateAfterMinutes = settings?.OverdueEscalationMinutes,
            EscalateToUserId = settings?.ManagerEscalationUserId,
            AppointmentWorkItemId = appointmentWorkItemId,
            InsuranceWorkItemId = insuranceWorkItemId,
            BalanceFollowUpWorkItemId = balanceWorkItemId
        };

        dbContext.Tasks.Add(task);
        dbContext.TaskHistory.Add(new TaskHistory
        {
            Id = Guid.NewGuid(),
            TaskItemId = task.Id,
            ActionType = "WorkflowGenerated",
            OldStatus = null,
            NewStatus = task.Status,
            PerformedByUserId = null,
            PerformedAtUtc = now,
            Details = "Automatically generated from office workflow rules."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Generated workflow task {TaskId} for appointment {AppointmentWorkItemId}, insurance {InsuranceWorkItemId}, balance {BalanceFollowUpWorkItemId}.",
            task.Id, appointmentWorkItemId, insuranceWorkItemId, balanceWorkItemId);

        var dto = await dbContext.Tasks
            .AsNoTracking()
            .Include(x => x.AssignedUser)
            .Include(x => x.ClaimedByUser)
            .Include(x => x.CreatedByUser)
            .FirstAsync(x => x.Id == task.Id, cancellationToken);

        await broadcastService.BroadcastTaskChangedAsync("workflow-generated", TaskService.MapTask(dto, DateTime.UtcNow), cancellationToken);
    }

    private Task<OfficeSettings?> GetSettingsAsync(CancellationToken cancellationToken)
        => dbContext.OfficeSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
}
