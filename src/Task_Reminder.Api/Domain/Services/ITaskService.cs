using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface ITaskService
{
    Task<TaskItemDto> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskItemDto>> ListAsync(TaskQueryParameters query, CancellationToken cancellationToken);
    Task<TaskItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TaskItemDto?> AssignAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto?> ClaimAsync(Guid taskId, ClaimTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto?> SnoozeAsync(Guid taskId, SnoozeTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto?> CompleteAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto?> CancelAsync(Guid taskId, CancelTaskRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskHistoryDto>> GetHistoryAsync(Guid taskId, CancellationToken cancellationToken);
    Task<int> MarkOverdueTasksAsync(CancellationToken cancellationToken);
}
