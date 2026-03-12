using System.ComponentModel.DataAnnotations;

namespace Task_Reminder.Shared;

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
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
