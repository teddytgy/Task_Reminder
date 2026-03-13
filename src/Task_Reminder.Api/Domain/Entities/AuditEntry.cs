namespace Task_Reminder.Api.Domain.Entities;

public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByDisplayName { get; set; }
    public DateTime PerformedAtUtc { get; set; }

    public User? PerformedByUser { get; set; }
}
