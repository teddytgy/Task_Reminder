using Task_Reminder.Shared;

namespace Task_Reminder.Wpf.Services;

public interface ITaskReminderApiClient
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskItemDto>> GetTasksAsync(TaskQueryParameters query, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskHistoryDto>> GetTaskHistoryAsync(Guid taskId, CancellationToken cancellationToken);
    Task<TaskItemDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> AssignTaskAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> ClaimTaskAsync(Guid taskId, ClaimTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> SnoozeTaskAsync(Guid taskId, SnoozeTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> CompleteTaskAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken);
}
