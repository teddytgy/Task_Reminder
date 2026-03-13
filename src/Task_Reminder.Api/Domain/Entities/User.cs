using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; } = UserRole.FrontDesk;
    public DateTime CreatedAtUtc { get; set; }
    public UserNotificationPreference? NotificationPreference { get; set; }

    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskItem> ClaimedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskHistory> HistoryEntries { get; set; } = new List<TaskHistory>();
    public ICollection<TaskItem> EscalatedTasks { get; set; } = new List<TaskItem>();
    public ICollection<RecurringTaskDefinition> AssignedRecurringTasks { get; set; } = new List<RecurringTaskDefinition>();
    public ICollection<RecurringTaskDefinition> CreatedRecurringTasks { get; set; } = new List<RecurringTaskDefinition>();
    public ICollection<RecurringTaskDefinition> EscalatedRecurringTasks { get; set; } = new List<RecurringTaskDefinition>();
    public ICollection<InsuranceWorkItem> VerifiedInsuranceItems { get; set; } = new List<InsuranceWorkItem>();
    public ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    public ICollection<OfficeSettings> EscalationSettings { get; set; } = new List<OfficeSettings>();
    public ICollection<AuditEntry> AuditEntries { get; set; } = new List<AuditEntry>();
}
