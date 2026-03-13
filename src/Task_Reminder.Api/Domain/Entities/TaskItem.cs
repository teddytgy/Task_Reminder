using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskCategory Category { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public Guid? ClaimedByUserId { get; set; }
    public User? ClaimedByUser { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? SnoozeUntilUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? PatientReference { get; set; }
    public string? Notes { get; set; }
    public int ReminderRepeatMinutes { get; set; } = 30;
    public int? EscalateAfterMinutes { get; set; }
    public Guid? EscalateToUserId { get; set; }
    public User? EscalateToUser { get; set; }
    public DateTime? EscalatedAtUtc { get; set; }
    public Guid? GeneratedFromRecurringTaskDefinitionId { get; set; }
    public RecurringTaskDefinition? GeneratedFromRecurringTaskDefinition { get; set; }
    public DateOnly? GeneratedForDateLocal { get; set; }
    public Guid? AppointmentWorkItemId { get; set; }
    public AppointmentWorkItem? AppointmentWorkItem { get; set; }
    public Guid? InsuranceWorkItemId { get; set; }
    public InsuranceWorkItem? InsuranceWorkItem { get; set; }
    public Guid? BalanceFollowUpWorkItemId { get; set; }
    public BalanceFollowUpWorkItem? BalanceFollowUpWorkItem { get; set; }
    public ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();

    public ICollection<TaskHistory> HistoryEntries { get; set; } = new List<TaskHistory>();
}
