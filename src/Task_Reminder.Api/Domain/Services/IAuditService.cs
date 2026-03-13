using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IAuditService
{
    Task WriteAsync(
        string entityType,
        Guid? entityId,
        string actionType,
        string summary,
        string? details,
        Guid? performedByUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditEntryDto>> ListAsync(AuditQueryParameters query, CancellationToken cancellationToken);
}
