using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class AuditService(
    TaskReminderDbContext dbContext,
    IRequestUserContextAccessor requestUserContextAccessor) : IAuditService
{
    public async Task WriteAsync(
        string entityType,
        Guid? entityId,
        string actionType,
        string summary,
        string? details,
        Guid? performedByUserId,
        CancellationToken cancellationToken)
    {
        var requestUser = requestUserContextAccessor.Current;
        var effectiveUserId = performedByUserId ?? requestUser.UserId;
        var effectiveDisplayName = requestUser.DisplayName;

        if (effectiveUserId.HasValue && string.IsNullOrWhiteSpace(effectiveDisplayName))
        {
            effectiveDisplayName = await dbContext.Users
                .Where(x => x.Id == effectiveUserId.Value)
                .Select(x => x.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        dbContext.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            ActionType = actionType,
            Summary = summary,
            Details = details,
            PerformedByUserId = effectiveUserId,
            PerformedByDisplayName = effectiveDisplayName ?? "System",
            PerformedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryDto>> ListAsync(AuditQueryParameters query, CancellationToken cancellationToken)
    {
        var auditEntries = dbContext.AuditEntries.AsNoTracking().AsQueryable();

        if (query.FromUtc.HasValue)
        {
            auditEntries = auditEntries.Where(x => x.PerformedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            auditEntries = auditEntries.Where(x => x.PerformedAtUtc <= query.ToUtc.Value);
        }

        if (query.UserId.HasValue)
        {
            auditEntries = auditEntries.Where(x => x.PerformedByUserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            auditEntries = auditEntries.Where(x => x.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            auditEntries = auditEntries.Where(x => x.ActionType == query.ActionType);
        }

        return await auditEntries
            .OrderByDescending(x => x.PerformedAtUtc)
            .Take(500)
            .Select(x => new AuditEntryDto
            {
                Id = x.Id,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                ActionType = x.ActionType,
                Summary = x.Summary,
                Details = x.Details,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByDisplayName = x.PerformedByDisplayName,
                PerformedAtUtc = x.PerformedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
