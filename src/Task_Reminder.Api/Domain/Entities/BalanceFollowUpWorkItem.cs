using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class BalanceFollowUpWorkItem
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientReference { get; set; } = string.Empty;
    public Guid? AppointmentWorkItemId { get; set; }
    public AppointmentWorkItem? AppointmentWorkItem { get; set; }
    public decimal AmountDue { get; set; }
    public string? DueReasonNote { get; set; }
    public BalanceFollowUpStatus Status { get; set; } = BalanceFollowUpStatus.NotReviewed;
    public DateOnly? FollowUpDateLocal { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
}
