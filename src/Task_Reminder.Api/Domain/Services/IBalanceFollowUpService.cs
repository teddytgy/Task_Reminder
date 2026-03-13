using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IBalanceFollowUpService
{
    Task<IReadOnlyList<BalanceFollowUpWorkItemDto>> ListAsync(BalanceQueryParameters query, CancellationToken cancellationToken);
    Task<BalanceFollowUpWorkItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<BalanceFollowUpWorkItemDto> CreateAsync(CreateBalanceFollowUpWorkItemRequest request, CancellationToken cancellationToken);
    Task<BalanceFollowUpWorkItemDto?> UpdateAsync(Guid id, UpdateBalanceFollowUpWorkItemRequest request, CancellationToken cancellationToken);
    Task<BalanceFollowUpWorkItemDto?> UpdateStatusAsync(Guid id, BalanceFollowUpStatus status, string? notes, CancellationToken cancellationToken);
    Task<TaskItemDto?> CreateFollowUpTaskAsync(Guid id, Guid? userId, CancellationToken cancellationToken);
}
