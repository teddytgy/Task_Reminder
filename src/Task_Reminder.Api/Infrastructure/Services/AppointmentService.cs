using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class AppointmentService(
    TaskReminderDbContext dbContext,
    ITaskService taskService,
    IWorkflowAutomationService workflowAutomationService,
    IContactLogService contactLogService,
    IAuditService auditService,
    ILogger<AppointmentService> logger) : IAppointmentService
{
    public async Task<IReadOnlyList<AppointmentWorkItemDto>> ListAsync(AppointmentQueryParameters query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);

        IQueryable<AppointmentWorkItem> items = dbContext.AppointmentWorkItems.AsNoTracking();
        items = query.Filter?.ToLowerInvariant() switch
        {
            "today" => items.Where(x => x.AppointmentDateLocal == today),
            "tomorrow" => items.Where(x => x.AppointmentDateLocal == tomorrow),
            "this-week" => items.Where(x => x.AppointmentDateLocal >= today && x.AppointmentDateLocal <= today.AddDays(7)),
            "unconfirmed" => items.Where(x => x.ConfirmationStatus != AppointmentConfirmationStatus.Confirmed && x.Status == AppointmentStatus.Scheduled),
            "insurance-pending" => items.Where(x => x.InsuranceStatus == AppointmentInsuranceStatus.PendingVerification),
            "balance-due" => items.Where(x => x.BalanceStatus == AppointmentBalanceStatus.BalanceDue),
            "no-show-cancelled" => items.Where(x => x.Status == AppointmentStatus.NoShow || x.Status == AppointmentStatus.Cancelled),
            _ => items
        };

        var appointments = await items
            .OrderBy(x => x.AppointmentDateLocal)
            .ThenBy(x => x.AppointmentTimeLocal)
            .ToListAsync(cancellationToken);

        return await MapAsync(appointments, cancellationToken);
    }

    public async Task<AppointmentWorkItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.AppointmentWorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<AppointmentWorkItemDto> CreateAsync(CreateAppointmentWorkItemRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var item = new AppointmentWorkItem
        {
            Id = Guid.NewGuid(),
            PatientName = request.PatientName.Trim(),
            PatientReference = request.PatientReference.Trim(),
            AppointmentDateLocal = request.AppointmentDateLocal,
            AppointmentTimeLocal = request.AppointmentTimeLocal,
            ProviderName = request.ProviderName?.Trim(),
            AppointmentType = request.AppointmentType.Trim(),
            Status = request.Status,
            ConfirmationStatus = request.ConfirmationStatus,
            InsuranceStatus = request.InsuranceStatus,
            BalanceStatus = request.BalanceStatus,
            Notes = request.Notes?.Trim(),
            SourceSystem = request.SourceSystem?.Trim(),
            SourceReference = request.SourceReference?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.AppointmentWorkItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureLinkedWorkItemsAsync(item, cancellationToken);
        await workflowAutomationService.EnsureAppointmentTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("AppointmentWorkItem", item.Id, "Created", $"Created appointment workflow item for {item.PatientName}.", item.Notes, null, cancellationToken);
        logger.LogInformation("Created appointment workflow item {AppointmentId} for patient {PatientReference}.", item.Id, item.PatientReference);
        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<AppointmentWorkItemDto?> UpdateAsync(Guid id, UpdateAppointmentWorkItemRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.AppointmentWorkItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.PatientName = request.PatientName.Trim();
        item.PatientReference = request.PatientReference.Trim();
        item.AppointmentDateLocal = request.AppointmentDateLocal;
        item.AppointmentTimeLocal = request.AppointmentTimeLocal;
        item.ProviderName = request.ProviderName?.Trim();
        item.AppointmentType = request.AppointmentType.Trim();
        item.Status = request.Status;
        item.ConfirmationStatus = request.ConfirmationStatus;
        item.InsuranceStatus = request.InsuranceStatus;
        item.BalanceStatus = request.BalanceStatus;
        item.Notes = request.Notes?.Trim();
        item.SourceSystem = request.SourceSystem?.Trim();
        item.SourceReference = request.SourceReference?.Trim();
        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureLinkedWorkItemsAsync(item, cancellationToken);
        await workflowAutomationService.EnsureAppointmentTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("AppointmentWorkItem", item.Id, "Updated", $"Updated appointment workflow item for {item.PatientName}.", item.Notes, null, cancellationToken);
        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<AppointmentWorkItemDto?> ApplyActionAsync(Guid id, string actionName, AppointmentActionRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.AppointmentWorkItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        switch (actionName.ToLowerInvariant())
        {
            case "confirm":
                item.ConfirmationStatus = AppointmentConfirmationStatus.Confirmed;
                item.Status = AppointmentStatus.Confirmed;
                break;
            case "voicemail":
                item.ConfirmationStatus = AppointmentConfirmationStatus.LeftVoicemail;
                await contactLogService.CreateAsync(new CreateContactLogRequest
                {
                    AppointmentWorkItemId = item.Id,
                    ContactType = ContactType.Voicemail,
                    Outcome = ContactOutcome.LeftVoicemail,
                    Notes = request.Notes,
                    PerformedByUserId = request.UserId
                }, cancellationToken);
                break;
            case "no-answer":
                item.ConfirmationStatus = AppointmentConfirmationStatus.NoAnswer;
                await contactLogService.CreateAsync(new CreateContactLogRequest
                {
                    AppointmentWorkItemId = item.Id,
                    ContactType = ContactType.Call,
                    Outcome = ContactOutcome.NoAnswer,
                    Notes = request.Notes,
                    PerformedByUserId = request.UserId
                }, cancellationToken);
                break;
            case "text-sent":
                item.ConfirmationStatus = AppointmentConfirmationStatus.TextSent;
                await contactLogService.CreateAsync(new CreateContactLogRequest
                {
                    AppointmentWorkItemId = item.Id,
                    ContactType = ContactType.Text,
                    Outcome = ContactOutcome.Completed,
                    Notes = request.Notes,
                    PerformedByUserId = request.UserId
                }, cancellationToken);
                break;
            case "email-sent":
                item.ConfirmationStatus = AppointmentConfirmationStatus.EmailSent;
                await contactLogService.CreateAsync(new CreateContactLogRequest
                {
                    AppointmentWorkItemId = item.Id,
                    ContactType = ContactType.Email,
                    Outcome = ContactOutcome.Completed,
                    Notes = request.Notes,
                    PerformedByUserId = request.UserId
                }, cancellationToken);
                break;
            case "cancel":
                item.Status = AppointmentStatus.Cancelled;
                break;
            case "reschedule":
                item.Status = AppointmentStatus.Rescheduled;
                break;
            case "check-in":
                item.Status = AppointmentStatus.CheckedIn;
                break;
            case "complete":
                item.Status = AppointmentStatus.Completed;
                break;
            default:
                throw new InvalidOperationException("Unknown appointment action.");
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            item.Notes = string.IsNullOrWhiteSpace(item.Notes)
                ? request.Notes.Trim()
                : $"{item.Notes}{Environment.NewLine}{request.Notes.Trim()}";
        }

        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureLinkedWorkItemsAsync(item, cancellationToken);
        await workflowAutomationService.EnsureAppointmentTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("AppointmentWorkItem", item.Id, actionName, $"Applied appointment action {actionName} for {item.PatientName}.", request.Notes, request.UserId, cancellationToken);
        logger.LogInformation("Applied appointment action {ActionName} to {AppointmentId}.", actionName, item.Id);
        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<TaskItemDto?> CreateFollowUpTaskAsync(Guid id, AppointmentActionRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.AppointmentWorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        return await taskService.CreateAsync(new CreateTaskRequest
        {
            Title = $"Follow up with {item.PatientName}",
            Description = request.Notes ?? "General appointment follow-up.",
            Category = TaskCategory.TreatmentFollowUp,
            Priority = TaskPriority.Medium,
            DueAtUtc = DateTime.UtcNow.AddHours(4),
            CreatedByUserId = request.UserId,
            PatientReference = item.PatientReference,
            Notes = item.Notes,
            AppointmentWorkItemId = item.Id
        }, cancellationToken);
    }

    private async Task EnsureLinkedWorkItemsAsync(AppointmentWorkItem item, CancellationToken cancellationToken)
    {
        if (item.InsuranceStatus != AppointmentInsuranceStatus.NotNeeded &&
            !await dbContext.InsuranceWorkItems.AnyAsync(x => x.AppointmentWorkItemId == item.Id, cancellationToken))
        {
            dbContext.InsuranceWorkItems.Add(new InsuranceWorkItem
            {
                Id = Guid.NewGuid(),
                PatientName = item.PatientName,
                PatientReference = item.PatientReference,
                AppointmentDateLocal = item.AppointmentDateLocal,
                VerificationStatus = item.InsuranceStatus == AppointmentInsuranceStatus.Verified ? InsuranceVerificationStatus.Verified : InsuranceVerificationStatus.Pending,
                EligibilityStatus = item.InsuranceStatus == AppointmentInsuranceStatus.Ineligible ? InsuranceEligibilityStatus.Inactive : InsuranceEligibilityStatus.Unknown,
                VerificationMethod = InsuranceVerificationMethod.Unknown,
                AppointmentWorkItemId = item.Id,
                SourceSystem = item.SourceSystem,
                SourceReference = item.SourceReference,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        if (item.BalanceStatus == AppointmentBalanceStatus.BalanceDue &&
            !await dbContext.BalanceFollowUpWorkItems.AnyAsync(x => x.AppointmentWorkItemId == item.Id, cancellationToken))
        {
            dbContext.BalanceFollowUpWorkItems.Add(new BalanceFollowUpWorkItem
            {
                Id = Guid.NewGuid(),
                PatientName = item.PatientName,
                PatientReference = item.PatientReference,
                AppointmentWorkItemId = item.Id,
                AmountDue = 0,
                Status = BalanceFollowUpStatus.NotReviewed,
                FollowUpDateLocal = item.AppointmentDateLocal,
                Notes = item.Notes,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AppointmentWorkItemDto>> MapAsync(IReadOnlyList<AppointmentWorkItem> items, CancellationToken cancellationToken)
    {
        var appointmentIds = items.Select(x => x.Id).ToList();
        var latestContacts = await dbContext.ContactLogs
            .AsNoTracking()
            .Where(x => x.AppointmentWorkItemId.HasValue && appointmentIds.Contains(x.AppointmentWorkItemId.Value))
            .OrderByDescending(x => x.PerformedAtUtc)
            .ToListAsync(cancellationToken);

        var latestContactLookup = latestContacts
            .GroupBy(x => x.AppointmentWorkItemId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var insuranceItems = await dbContext.InsuranceWorkItems
            .AsNoTracking()
            .Where(x => x.AppointmentWorkItemId.HasValue && appointmentIds.Contains(x.AppointmentWorkItemId.Value))
            .ToListAsync(cancellationToken);
        var insuranceLookup = insuranceItems
            .GroupBy(x => x.AppointmentWorkItemId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).FirstOrDefault());

        var balanceItems = await dbContext.BalanceFollowUpWorkItems
            .AsNoTracking()
            .Where(x => x.AppointmentWorkItemId.HasValue && appointmentIds.Contains(x.AppointmentWorkItemId.Value))
            .ToListAsync(cancellationToken);
        var balanceLookup = balanceItems
            .GroupBy(x => x.AppointmentWorkItemId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).FirstOrDefault());

        return items.Select(item =>
        {
            latestContactLookup.TryGetValue(item.Id, out var latestContact);
            insuranceLookup.TryGetValue(item.Id, out var insuranceId);
            balanceLookup.TryGetValue(item.Id, out var balanceId);
            return new AppointmentWorkItemDto
            {
                Id = item.Id,
                PatientName = item.PatientName,
                PatientReference = item.PatientReference,
                AppointmentDateLocal = item.AppointmentDateLocal,
                AppointmentTimeLocal = item.AppointmentTimeLocal,
                ProviderName = item.ProviderName,
                AppointmentType = item.AppointmentType,
                Status = item.Status,
                ConfirmationStatus = item.ConfirmationStatus,
                InsuranceStatus = item.InsuranceStatus,
                BalanceStatus = item.BalanceStatus,
                Notes = item.Notes,
                SourceSystem = item.SourceSystem,
                SourceReference = item.SourceReference,
                InsuranceWorkItemId = insuranceId == Guid.Empty ? null : insuranceId,
                BalanceFollowUpWorkItemId = balanceId == Guid.Empty ? null : balanceId,
                LatestContactSummary = latestContact is null ? null : $"{latestContact.ContactType}: {latestContact.Outcome}",
                LatestContactAtUtc = latestContact?.PerformedAtUtc,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            };
        }).ToList();
    }
}
