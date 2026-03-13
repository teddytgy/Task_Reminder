using System.ComponentModel.DataAnnotations;

namespace Task_Reminder.Shared;

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public UserRole Role { get; set; } = UserRole.FrontDesk;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
}

public sealed class TaskItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskCategory Category { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserDisplayName { get; set; }
    public Guid? ClaimedByUserId { get; set; }
    public string? ClaimedByDisplayName { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByDisplayName { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? SnoozeUntilUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? PatientReference { get; set; }
    public string? Notes { get; set; }
    public int ReminderRepeatMinutes { get; set; }
    public int? EscalateAfterMinutes { get; set; }
    public Guid? EscalateToUserId { get; set; }
    public DateTime? EscalatedAtUtc { get; set; }
    public Guid? GeneratedFromRecurringTaskDefinitionId { get; set; }
    public DateOnly? GeneratedForDateLocal { get; set; }
    public Guid? AppointmentWorkItemId { get; set; }
    public Guid? InsuranceWorkItemId { get; set; }
    public Guid? BalanceFollowUpWorkItemId { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsDueNow { get; set; }
}

public sealed class CreateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public TaskCategory Category { get; set; } = TaskCategory.General;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public Guid? AssignedUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? DueAtUtc { get; set; }

    [MaxLength(100)]
    public string? PatientReference { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public int? ReminderRepeatMinutes { get; set; }
    public int? EscalateAfterMinutes { get; set; }
    public Guid? EscalateToUserId { get; set; }
    public Guid? AppointmentWorkItemId { get; set; }
    public Guid? InsuranceWorkItemId { get; set; }
    public Guid? BalanceFollowUpWorkItemId { get; set; }
}

public sealed class AssignTaskRequest
{
    [Required]
    public Guid AssignedUserId { get; set; }

    public Guid? PerformedByUserId { get; set; }
}

public sealed class ClaimTaskRequest
{
    [Required]
    public Guid UserId { get; set; }
}

public sealed class SnoozeTaskRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public DateTime SnoozeUntilUtc { get; set; }
}

public sealed class CompleteTaskRequest
{
    [Required]
    public Guid UserId { get; set; }
}

public sealed class CancelTaskRequest
{
    public Guid? UserId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public sealed class TaskHistoryDto
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public TaskStatus? OldStatus { get; set; }
    public TaskStatus? NewStatus { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByDisplayName { get; set; }
    public DateTime PerformedAtUtc { get; set; }
    public string? Details { get; set; }
}

public sealed class AddTaskCommentRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;
}

public sealed class UserNotificationPreferencesDto
{
    public Guid UserId { get; set; }
    public bool ReceiveAssignedTaskReminders { get; set; } = true;
    public bool ReceiveUnassignedTaskReminders { get; set; } = true;
    public bool ReceiveOverdueEscalationAlerts { get; set; } = true;
    public bool ReceiveRecurringTaskGenerationAlerts { get; set; } = true;
    public bool EnableSoundForUrgentReminders { get; set; }
}

public sealed class UpdateUserNotificationPreferencesRequest
{
    public bool ReceiveAssignedTaskReminders { get; set; } = true;
    public bool ReceiveUnassignedTaskReminders { get; set; } = true;
    public bool ReceiveOverdueEscalationAlerts { get; set; } = true;
    public bool ReceiveRecurringTaskGenerationAlerts { get; set; } = true;
    public bool EnableSoundForUrgentReminders { get; set; }
}

public sealed class RecurringTaskDefinitionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskCategory Category { get; set; }
    public TaskPriority Priority { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserDisplayName { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByDisplayName { get; set; }
    public string? PatientReference { get; set; }
    public string? Notes { get; set; }
    public int ReminderRepeatMinutes { get; set; }
    public int? EscalateAfterMinutes { get; set; }
    public Guid? EscalateToUserId { get; set; }
    public string? EscalateToUserDisplayName { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int RecurrenceInterval { get; set; }
    public string? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public TimeSpan? TimeOfDayLocal { get; set; }
    public DateOnly StartDateLocal { get; set; }
    public DateOnly? EndDateLocal { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastGeneratedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateRecurringTaskDefinitionRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public TaskCategory Category { get; set; } = TaskCategory.General;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public Guid? AssignedUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    [MaxLength(100)]
    public string? PatientReference { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public int? ReminderRepeatMinutes { get; set; }
    public int? EscalateAfterMinutes { get; set; }
    public Guid? EscalateToUserId { get; set; }
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.Daily;
    public int RecurrenceInterval { get; set; } = 1;
    public string? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public TimeSpan? TimeOfDayLocal { get; set; }
    public DateOnly StartDateLocal { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? EndDateLocal { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateRecurringTaskDefinitionRequest : CreateRecurringTaskDefinitionRequest
{
}

public sealed class SetRecurringTaskDefinitionActiveRequest
{
    public bool IsActive { get; set; }
}

public sealed class ManagerMetricsQuery
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int? PresetDays { get; set; }
}

public sealed class MetricBreakdownItemDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class UserPerformanceDto
{
    public Guid? UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public int CompletedCount { get; set; }
}

public sealed class ManagerMetricsDto
{
    public DateTime RangeStartUtc { get; set; }
    public DateTime RangeEndUtc { get; set; }
    public int TotalOpenTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int CompletedInRange { get; set; }
    public int UnassignedTasks { get; set; }
    public double AverageCompletionMinutes { get; set; }
    public IReadOnlyList<MetricBreakdownItemDto> TasksByCategory { get; set; } = [];
    public IReadOnlyList<MetricBreakdownItemDto> TasksByPriority { get; set; } = [];
    public IReadOnlyList<UserPerformanceDto> CompletedPerUser { get; set; } = [];
}

public sealed class TaskQueryParameters
{
    public Guid? UserId { get; set; }
    public TaskStatus? Status { get; set; }
    public bool OverdueOnly { get; set; }
    public bool UnassignedOnly { get; set; }
    public bool AssignedToMeOnly { get; set; }
    public bool CompletedTodayOnly { get; set; }
    public bool DueTodayOnly { get; set; }
    public bool DueNowOnly { get; set; }
}

public sealed class TaskChangedMessage
{
    public string EventType { get; set; } = string.Empty;
    public TaskItemDto Task { get; set; } = new();
}
