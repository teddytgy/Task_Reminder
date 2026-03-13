using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IRecurringTaskService
{
    Task<IReadOnlyList<RecurringTaskDefinitionDto>> ListAsync(CancellationToken cancellationToken);
    Task<RecurringTaskDefinitionDto> CreateAsync(CreateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken);
    Task<RecurringTaskDefinitionDto?> UpdateAsync(Guid id, UpdateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken);
    Task<RecurringTaskDefinitionDto?> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<int> GenerateDueTasksAsync(CancellationToken cancellationToken);
}
