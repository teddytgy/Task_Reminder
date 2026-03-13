using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class AppointmentWorkItem
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientReference { get; set; } = string.Empty;
    public DateOnly AppointmentDateLocal { get; set; }
    public TimeSpan AppointmentTimeLocal { get; set; }
    public string? ProviderName { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentConfirmationStatus ConfirmationStatus { get; set; } = AppointmentConfirmationStatus.NotStarted;
    public AppointmentInsuranceStatus InsuranceStatus { get; set; } = AppointmentInsuranceStatus.PendingVerification;
    public AppointmentBalanceStatus BalanceStatus { get; set; } = AppointmentBalanceStatus.Unknown;
    public string? Notes { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    public ICollection<InsuranceWorkItem> InsuranceWorkItems { get; set; } = new List<InsuranceWorkItem>();
    public ICollection<BalanceFollowUpWorkItem> BalanceFollowUpWorkItems { get; set; } = new List<BalanceFollowUpWorkItem>();
}
