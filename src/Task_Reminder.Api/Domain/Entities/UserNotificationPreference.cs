namespace Task_Reminder.Api.Domain.Entities;

public sealed class UserNotificationPreference
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool ReceiveAssignedTaskReminders { get; set; } = true;
    public bool ReceiveUnassignedTaskReminders { get; set; } = true;
    public bool ReceiveOverdueEscalationAlerts { get; set; } = true;
    public bool ReceiveRecurringTaskGenerationAlerts { get; set; } = true;
    public bool EnableSoundForUrgentReminders { get; set; }
}
