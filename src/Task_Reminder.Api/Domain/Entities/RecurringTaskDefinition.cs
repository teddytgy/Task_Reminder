using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class RecurringTaskDefinition
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskCategory Category { get; set; }
    public TaskPriority Priority { get; set; }
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public string? PatientReference { get; set; }
    public string? Notes { get; set; }
    public int ReminderRepeatMinutes { get; set; } = 30;
    public int? EscalateAfterMinutes { get; set; }
    public Guid? EscalateToUserId { get; set; }
    public User? EscalateToUser { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int RecurrenceInterval { get; set; } = 1;
    public string? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public TimeSpan? TimeOfDayLocal { get; set; }
    public DateOnly StartDateLocal { get; set; }
    public DateOnly? EndDateLocal { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastGeneratedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<TaskItem> GeneratedTasks { get; set; } = new List<TaskItem>();
}
