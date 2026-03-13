using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class TaskHistory
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public string ActionType { get; set; } = string.Empty;
    public TaskStatus? OldStatus { get; set; }
    public TaskStatus? NewStatus { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }
    public DateTime PerformedAtUtc { get; set; }
    public string? Details { get; set; }
}
