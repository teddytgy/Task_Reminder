using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class InsuranceWorkService(
    TaskReminderDbContext dbContext,
    ITaskService taskService,
    IWorkflowAutomationService workflowAutomationService,
    IAuditService auditService,
    ILogger<InsuranceWorkService> logger) : IInsuranceWorkService
{
    public async Task<IReadOnlyList<InsuranceWorkItemDto>> ListAsync(InsuranceQueryParameters query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);

        IQueryable<InsuranceWorkItem> items = dbContext.InsuranceWorkItems.AsNoTracking().Include(x => x.VerifiedByUser);
        items = query.Filter?.ToLowerInvariant() switch
        {
            "today" => items.Where(x => x.AppointmentDateLocal == today),
            "tomorrow" => items.Where(x => x.AppointmentDateLocal == tomorrow),
            "verification-pending" => items.Where(x => x.VerificationStatus == InsuranceVerificationStatus.NotStarted || x.VerificationStatus == InsuranceVerificationStatus.Pending || x.VerificationStatus == InsuranceVerificationStatus.InProgress),
            "issue-found" => items.Where(x => x.IssueType.HasValue || x.VerificationStatus == InsuranceVerificationStatus.Failed || x.VerificationStatus == InsuranceVerificationStatus.NeedsManualReview),
            "inactive-coverage" => items.Where(x => x.EligibilityStatus == InsuranceEligibilityStatus.Inactive),
            "missing-info" => items.Where(x => !string.IsNullOrEmpty(x.MissingInfoNotes) || x.EligibilityStatus == InsuranceEligibilityStatus.MissingSubscriberInfo),
            "manual-review-needed" => items.Where(x => x.VerificationStatus == InsuranceVerificationStatus.NeedsManualReview),
            _ => items
        };

        var workItems = await items
            .OrderBy(x => x.AppointmentDateLocal ?? DateOnly.MaxValue)
            .ThenBy(x => x.PatientName)
            .ToListAsync(cancellationToken);

        return await MapAsync(workItems, cancellationToken);
    }

    public async Task<InsuranceWorkItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.InsuranceWorkItems.AsNoTracking().Include(x => x.VerifiedByUser).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<InsuranceWorkItemDto> CreateAsync(CreateInsuranceWorkItemRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var item = new InsuranceWorkItem
        {
            Id = Guid.NewGuid(),
            PatientName = request.PatientName.Trim(),
            PatientReference = request.PatientReference.Trim(),
            CarrierName = request.CarrierName?.Trim(),
            PlanName = request.PlanName?.Trim(),
            MemberId = request.MemberId?.Trim(),
            GroupNumber = request.GroupNumber?.Trim(),
            PayerId = request.PayerId?.Trim(),
            AppointmentDateLocal = request.AppointmentDateLocal,
            VerificationStatus = request.VerificationStatus,
            EligibilityStatus = request.EligibilityStatus,
            VerificationMethod = request.VerificationMethod,
            CopayAmount = request.CopayAmount,
            DeductibleAmount = request.DeductibleAmount,
            AnnualMaximum = request.AnnualMaximum,
            RemainingMaximum = request.RemainingMaximum,
            FrequencyNotes = request.FrequencyNotes?.Trim(),
            WaitingPeriodNotes = request.WaitingPeriodNotes?.Trim(),
            MissingInfoNotes = request.MissingInfoNotes?.Trim(),
            IssueType = request.IssueType,
            Notes = request.Notes?.Trim(),
            SourceSystem = request.SourceSystem?.Trim(),
            SourceReference = request.SourceReference?.Trim(),
            AppointmentWorkItemId = request.AppointmentWorkItemId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.InsuranceWorkItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await workflowAutomationService.EnsureInsuranceTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("InsuranceWorkItem", item.Id, "Created", $"Created insurance workflow item for {item.PatientName}.", item.Notes, null, cancellationToken);
        logger.LogInformation("Created insurance work item {InsuranceWorkItemId}.", item.Id);
        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<InsuranceWorkItemDto?> UpdateAsync(Guid id, UpdateInsuranceWorkItemRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.InsuranceWorkItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.PatientName = request.PatientName.Trim();
        item.PatientReference = request.PatientReference.Trim();
        item.CarrierName = request.CarrierName?.Trim();
        item.PlanName = request.PlanName?.Trim();
        item.MemberId = request.MemberId?.Trim();
        item.GroupNumber = request.GroupNumber?.Trim();
        item.PayerId = request.PayerId?.Trim();
        item.AppointmentDateLocal = request.AppointmentDateLocal;
        item.VerificationStatus = request.VerificationStatus;
        item.EligibilityStatus = request.EligibilityStatus;
        item.VerificationMethod = request.VerificationMethod;
        item.CopayAmount = request.CopayAmount;
        item.DeductibleAmount = request.DeductibleAmount;
        item.AnnualMaximum = request.AnnualMaximum;
        item.RemainingMaximum = request.RemainingMaximum;
        item.FrequencyNotes = request.FrequencyNotes?.Trim();
        item.WaitingPeriodNotes = request.WaitingPeriodNotes?.Trim();
        item.MissingInfoNotes = request.MissingInfoNotes?.Trim();
        item.IssueType = request.IssueType;
        item.Notes = request.Notes?.Trim();
        item.SourceSystem = request.SourceSystem?.Trim();
        item.SourceReference = request.SourceReference?.Trim();
        item.AppointmentWorkItemId = request.AppointmentWorkItemId;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await workflowAutomationService.EnsureInsuranceTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("InsuranceWorkItem", item.Id, "Updated", $"Updated insurance workflow item for {item.PatientName}.", item.Notes, null, cancellationToken);
        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<InsuranceWorkItemDto?> UpdateStatusAsync(Guid id, InsuranceStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.InsuranceWorkItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (request.VerificationStatus.HasValue)
        {
            item.VerificationStatus = request.VerificationStatus.Value;
            item.VerificationRequestedAtUtc ??= DateTime.UtcNow;
            if (request.VerificationStatus.Value == InsuranceVerificationStatus.Verified)
            {
                item.VerificationCompletedAtUtc = DateTime.UtcNow;
                item.VerifiedByUserId = request.UserId;
            }
        }

        if (request.EligibilityStatus.HasValue) item.EligibilityStatus = request.EligibilityStatus.Value;
        if (request.IssueType.HasValue) item.IssueType = request.IssueType.Value;
        if (request.VerificationMethod.HasValue) item.VerificationMethod = request.VerificationMethod.Value;
        item.CopayAmount = request.CopayAmount ?? item.CopayAmount;
        item.DeductibleAmount = request.DeductibleAmount ?? item.DeductibleAmount;
        item.AnnualMaximum = request.AnnualMaximum ?? item.AnnualMaximum;
        item.RemainingMaximum = request.RemainingMaximum ?? item.RemainingMaximum;
        item.FrequencyNotes = request.FrequencyNotes ?? item.FrequencyNotes;
        item.WaitingPeriodNotes = request.WaitingPeriodNotes ?? item.WaitingPeriodNotes;
        item.MissingInfoNotes = request.MissingInfoNotes ?? item.MissingInfoNotes;
        item.Notes = request.Notes ?? item.Notes;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await workflowAutomationService.EnsureInsuranceTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("InsuranceWorkItem", item.Id, "StatusUpdated", $"Updated insurance status for {item.PatientName}.", item.Notes, request.UserId, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<TaskItemDto?> CreateFollowUpTaskAsync(Guid id, Guid? userId, bool managerEscalation, CancellationToken cancellationToken)
    {
        var item = await dbContext.InsuranceWorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        return await taskService.CreateAsync(new CreateTaskRequest
        {
            Title = managerEscalation
                ? $"Manager insurance review for {item.PatientName}"
                : $"Patient insurance follow-up for {item.PatientName}",
            Description = item.Notes ?? "Insurance follow-up generated from work queue.",
            Category = TaskCategory.InsuranceVerification,
            Priority = managerEscalation ? TaskPriority.Urgent : TaskPriority.High,
            DueAtUtc = DateTime.UtcNow.AddHours(managerEscalation ? 1 : 4),
            CreatedByUserId = userId,
            PatientReference = item.PatientReference,
            Notes = item.Notes,
            AppointmentWorkItemId = item.AppointmentWorkItemId,
            InsuranceWorkItemId = item.Id
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<InsuranceWorkItemDto>> MapAsync(IReadOnlyList<InsuranceWorkItem> items, CancellationToken cancellationToken)
    {
        var insuranceIds = items.Select(x => x.Id).ToList();
        var latestContacts = await dbContext.ContactLogs
            .AsNoTracking()
            .Where(x => x.InsuranceWorkItemId.HasValue && insuranceIds.Contains(x.InsuranceWorkItemId.Value))
            .OrderByDescending(x => x.PerformedAtUtc)
            .ToListAsync(cancellationToken);

        var latestContactLookup = latestContacts
            .GroupBy(x => x.InsuranceWorkItemId!.Value)
            .ToDictionary(x => x.Key, x => x.First());

        var linkedTasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(x => x.InsuranceWorkItemId.HasValue && insuranceIds.Contains(x.InsuranceWorkItemId.Value))
            .ToListAsync(cancellationToken);
        var linkedTaskLookup = linkedTasks
            .GroupBy(x => x.InsuranceWorkItemId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).FirstOrDefault());

        return items.Select(item =>
        {
            latestContactLookup.TryGetValue(item.Id, out var latestContact);
            linkedTaskLookup.TryGetValue(item.Id, out var taskId);
            return new InsuranceWorkItemDto
            {
                Id = item.Id,
                PatientName = item.PatientName,
                PatientReference = item.PatientReference,
                CarrierName = item.CarrierName,
                PlanName = item.PlanName,
                MemberId = item.MemberId,
                GroupNumber = item.GroupNumber,
                PayerId = item.PayerId,
                AppointmentDateLocal = item.AppointmentDateLocal,
                VerificationStatus = item.VerificationStatus,
                EligibilityStatus = item.EligibilityStatus,
                VerificationMethod = item.VerificationMethod,
                VerificationRequestedAtUtc = item.VerificationRequestedAtUtc,
                VerificationCompletedAtUtc = item.VerificationCompletedAtUtc,
                VerifiedByUserId = item.VerifiedByUserId,
                VerifiedByDisplayName = item.VerifiedByUser?.DisplayName,
                CopayAmount = item.CopayAmount,
                DeductibleAmount = item.DeductibleAmount,
                AnnualMaximum = item.AnnualMaximum,
                RemainingMaximum = item.RemainingMaximum,
                FrequencyNotes = item.FrequencyNotes,
                WaitingPeriodNotes = item.WaitingPeriodNotes,
                MissingInfoNotes = item.MissingInfoNotes,
                IssueType = item.IssueType,
                Notes = item.Notes,
                SourceSystem = item.SourceSystem,
                SourceReference = item.SourceReference,
                AppointmentWorkItemId = item.AppointmentWorkItemId,
                LinkedTaskId = taskId == Guid.Empty ? null : taskId,
                LatestContactSummary = latestContact is null ? null : $"{latestContact.ContactType}: {latestContact.Outcome}",
                LatestContactAtUtc = latestContact?.PerformedAtUtc,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            };
        }).ToList();
    }
}
