using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class ContactLog
{
    public Guid Id { get; set; }
    public Guid? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
    public Guid? AppointmentWorkItemId { get; set; }
    public AppointmentWorkItem? AppointmentWorkItem { get; set; }
    public Guid? InsuranceWorkItemId { get; set; }
    public InsuranceWorkItem? InsuranceWorkItem { get; set; }
    public Guid? BalanceFollowUpWorkItemId { get; set; }
    public BalanceFollowUpWorkItem? BalanceFollowUpWorkItem { get; set; }
    public ContactType ContactType { get; set; }
    public ContactOutcome Outcome { get; set; }
    public string? Notes { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }
    public DateTime PerformedAtUtc { get; set; }
}
