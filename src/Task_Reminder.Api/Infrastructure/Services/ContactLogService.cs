using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class ContactLogService(
    TaskReminderDbContext dbContext,
    IAuditService auditService) : IContactLogService
{
    public async Task<IReadOnlyList<ContactLogDto>> ListAsync(Guid? taskItemId, Guid? appointmentWorkItemId, Guid? insuranceWorkItemId, Guid? balanceFollowUpWorkItemId, CancellationToken cancellationToken)
    {
        var query = dbContext.ContactLogs
            .AsNoTracking()
            .Include(x => x.PerformedByUser)
            .AsQueryable();

        if (taskItemId.HasValue)
        {
            query = query.Where(x => x.TaskItemId == taskItemId);
        }

        if (appointmentWorkItemId.HasValue)
        {
            query = query.Where(x => x.AppointmentWorkItemId == appointmentWorkItemId);
        }

        if (insuranceWorkItemId.HasValue)
        {
            query = query.Where(x => x.InsuranceWorkItemId == insuranceWorkItemId);
        }

        if (balanceFollowUpWorkItemId.HasValue)
        {
            query = query.Where(x => x.BalanceFollowUpWorkItemId == balanceFollowUpWorkItemId);
        }

        var items = await query
            .OrderByDescending(x => x.PerformedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<ContactLogDto> CreateAsync(CreateContactLogRequest request, CancellationToken cancellationToken)
    {
        var item = new ContactLog
        {
            Id = Guid.NewGuid(),
            TaskItemId = request.TaskItemId,
            AppointmentWorkItemId = request.AppointmentWorkItemId,
            InsuranceWorkItemId = request.InsuranceWorkItemId,
            BalanceFollowUpWorkItemId = request.BalanceFollowUpWorkItemId,
            ContactType = request.ContactType,
            Outcome = request.Outcome,
            Notes = request.Notes?.Trim(),
            PerformedByUserId = request.PerformedByUserId,
            PerformedAtUtc = DateTime.UtcNow
        };

        dbContext.ContactLogs.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("ContactLog", item.Id, "Created", $"Created contact log ({item.ContactType}/{item.Outcome}).", item.Notes, item.PerformedByUserId, cancellationToken);

        var saved = await dbContext.ContactLogs
            .AsNoTracking()
            .Include(x => x.PerformedByUser)
            .FirstAsync(x => x.Id == item.Id, cancellationToken);

        return Map(saved);
    }

    public static ContactLogDto Map(ContactLog item) => new()
    {
        Id = item.Id,
        TaskItemId = item.TaskItemId,
        AppointmentWorkItemId = item.AppointmentWorkItemId,
        InsuranceWorkItemId = item.InsuranceWorkItemId,
        BalanceFollowUpWorkItemId = item.BalanceFollowUpWorkItemId,
        ContactType = item.ContactType,
        Outcome = item.Outcome,
        Notes = item.Notes,
        PerformedByUserId = item.PerformedByUserId,
        PerformedByDisplayName = item.PerformedByUser?.DisplayName,
        PerformedAtUtc = item.PerformedAtUtc
    };
}
