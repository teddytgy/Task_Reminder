using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class BalanceFollowUpService(
    TaskReminderDbContext dbContext,
    ITaskService taskService,
    IWorkflowAutomationService workflowAutomationService,
    IAuditService auditService,
    ILogger<BalanceFollowUpService> logger) : IBalanceFollowUpService
{
    public async Task<IReadOnlyList<BalanceFollowUpWorkItemDto>> ListAsync(BalanceQueryParameters query, CancellationToken cancellationToken)
    {
        IQueryable<BalanceFollowUpWorkItem> items = dbContext.BalanceFollowUpWorkItems.AsNoTracking();
        items = query.Filter?.ToLowerInvariant() switch
        {
            "balance-due" => items.Where(x => x.Status != BalanceFollowUpStatus.Collected),
            "payment-arranged" => items.Where(x => x.Status == BalanceFollowUpStatus.PaymentArranged),
            "overdue" => items.Where(x => x.FollowUpDateLocal.HasValue && x.FollowUpDateLocal < DateOnly.FromDateTime(DateTime.Today) && x.Status != BalanceFollowUpStatus.Collected),
            _ => items
        };

        var balances = await items.OrderBy(x => x.FollowUpDateLocal ?? DateOnly.MaxValue).ThenByDescending(x => x.AmountDue).ToListAsync(cancellationToken);
        return await MapAsync(balances, cancellationToken);
    }

    public async Task<BalanceFollowUpWorkItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.BalanceFollowUpWorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<BalanceFollowUpWorkItemDto> CreateAsync(CreateBalanceFollowUpWorkItemRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var item = new BalanceFollowUpWorkItem
        {
            Id = Guid.NewGuid(),
            PatientName = request.PatientName.Trim(),
            PatientReference = request.PatientReference.Trim(),
            AppointmentWorkItemId = request.AppointmentWorkItemId,
            AmountDue = request.AmountDue,
            DueReasonNote = request.DueReasonNote?.Trim(),
            Status = request.Status,
            FollowUpDateLocal = request.FollowUpDateLocal,
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.BalanceFollowUpWorkItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await workflowAutomationService.EnsureBalanceTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("BalanceFollowUpWorkItem", item.Id, "Created", $"Created balance follow-up item for {item.PatientName}.", item.Notes, null, cancellationToken);
        logger.LogInformation("Created balance follow-up item {BalanceWorkItemId}.", item.Id);
        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<BalanceFollowUpWorkItemDto?> UpdateAsync(Guid id, UpdateBalanceFollowUpWorkItemRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.BalanceFollowUpWorkItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.PatientName = request.PatientName.Trim();
        item.PatientReference = request.PatientReference.Trim();
        item.AppointmentWorkItemId = request.AppointmentWorkItemId;
        item.AmountDue = request.AmountDue;
        item.DueReasonNote = request.DueReasonNote?.Trim();
        item.Status = request.Status;
        item.FollowUpDateLocal = request.FollowUpDateLocal;
        item.Notes = request.Notes?.Trim();
        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await workflowAutomationService.EnsureBalanceTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("BalanceFollowUpWorkItem", item.Id, "Updated", $"Updated balance follow-up item for {item.PatientName}.", item.Notes, null, cancellationToken);
        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<BalanceFollowUpWorkItemDto?> UpdateStatusAsync(Guid id, BalanceFollowUpStatus status, string? notes, CancellationToken cancellationToken)
    {
        var item = await dbContext.BalanceFollowUpWorkItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.Status = status;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            item.Notes = string.IsNullOrWhiteSpace(item.Notes) ? notes.Trim() : $"{item.Notes}{Environment.NewLine}{notes.Trim()}";
        }

        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await workflowAutomationService.EnsureBalanceTasksAsync(item.Id, cancellationToken);
        await auditService.WriteAsync("BalanceFollowUpWorkItem", item.Id, "StatusUpdated", $"Updated balance status for {item.PatientName}.", item.Notes, null, cancellationToken);
        return (await MapAsync([item], cancellationToken)).Single();
    }

    public async Task<TaskItemDto?> CreateFollowUpTaskAsync(Guid id, Guid? userId, CancellationToken cancellationToken)
    {
        var item = await dbContext.BalanceFollowUpWorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        return await taskService.CreateAsync(new CreateTaskRequest
        {
            Title = $"Balance follow-up for {item.PatientName}",
            Description = item.DueReasonNote ?? "Follow up on patient balance.",
            Category = TaskCategory.BalanceCollection,
            Priority = TaskPriority.Medium,
            DueAtUtc = DateTime.UtcNow.AddHours(2),
            CreatedByUserId = userId,
            PatientReference = item.PatientReference,
            Notes = item.Notes,
            AppointmentWorkItemId = item.AppointmentWorkItemId,
            BalanceFollowUpWorkItemId = item.Id
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<BalanceFollowUpWorkItemDto>> MapAsync(IReadOnlyList<BalanceFollowUpWorkItem> items, CancellationToken cancellationToken)
    {
        var balanceIds = items.Select(x => x.Id).ToList();
        var latestContacts = await dbContext.ContactLogs
            .AsNoTracking()
            .Where(x => x.BalanceFollowUpWorkItemId.HasValue && balanceIds.Contains(x.BalanceFollowUpWorkItemId.Value))
            .OrderByDescending(x => x.PerformedAtUtc)
            .ToListAsync(cancellationToken);
        var latestContactLookup = latestContacts.GroupBy(x => x.BalanceFollowUpWorkItemId!.Value).ToDictionary(x => x.Key, x => x.First());
        var linkedTasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(x => x.BalanceFollowUpWorkItemId.HasValue && balanceIds.Contains(x.BalanceFollowUpWorkItemId.Value))
            .ToListAsync(cancellationToken);
        var linkedTaskLookup = linkedTasks
            .GroupBy(x => x.BalanceFollowUpWorkItemId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).FirstOrDefault());

        return items.Select(item =>
        {
            latestContactLookup.TryGetValue(item.Id, out var latestContact);
            linkedTaskLookup.TryGetValue(item.Id, out var taskId);
            return new BalanceFollowUpWorkItemDto
            {
                Id = item.Id,
                PatientName = item.PatientName,
                PatientReference = item.PatientReference,
                AppointmentWorkItemId = item.AppointmentWorkItemId,
                AmountDue = item.AmountDue,
                DueReasonNote = item.DueReasonNote,
                Status = item.Status,
                FollowUpDateLocal = item.FollowUpDateLocal,
                Notes = item.Notes,
                LinkedTaskId = taskId == Guid.Empty ? null : taskId,
                LatestContactSummary = latestContact is null ? null : $"{latestContact.ContactType}: {latestContact.Outcome}",
                LatestContactAtUtc = latestContact?.PerformedAtUtc,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            };
        }).ToList();
    }
}
