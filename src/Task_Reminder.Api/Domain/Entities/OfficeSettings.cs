namespace Task_Reminder.Api.Domain.Entities;

public sealed class OfficeSettings
{
    public Guid Id { get; set; }
    public string OfficeName { get; set; } = "Task Reminder Dental Office";
    public string BusinessHoursSummary { get; set; } = "Mon-Fri 8:00 AM - 5:00 PM";
    public int ConfirmationLeadHours { get; set; } = 24;
    public int InsuranceVerificationLeadDays { get; set; } = 2;
    public int OverdueEscalationMinutes { get; set; } = 45;
    public int NoShowFollowUpDelayHours { get; set; } = 4;
    public Guid? ManagerEscalationUserId { get; set; }
    public User? ManagerEscalationUser { get; set; }
    public int DefaultReminderIntervalMinutes { get; set; } = 30;
    public string TimeZoneId { get; set; } = "Eastern Standard Time";
    public bool EnableTodayBoard { get; set; } = true;
    public bool EnableTomorrowPrepBoard { get; set; } = true;
    public bool EnableCollectionsBoard { get; set; } = true;
    public bool EnableRecallBoard { get; set; } = true;
    public bool EnableManagerQueue { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
