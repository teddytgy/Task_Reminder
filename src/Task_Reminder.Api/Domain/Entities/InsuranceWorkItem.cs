using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class InsuranceWorkItem
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientReference { get; set; } = string.Empty;
    public string? CarrierName { get; set; }
    public string? PlanName { get; set; }
    public string? MemberId { get; set; }
    public string? GroupNumber { get; set; }
    public string? PayerId { get; set; }
    public DateOnly? AppointmentDateLocal { get; set; }
    public InsuranceVerificationStatus VerificationStatus { get; set; } = InsuranceVerificationStatus.NotStarted;
    public InsuranceEligibilityStatus EligibilityStatus { get; set; } = InsuranceEligibilityStatus.Unknown;
    public InsuranceVerificationMethod VerificationMethod { get; set; } = InsuranceVerificationMethod.Unknown;
    public DateTime? VerificationRequestedAtUtc { get; set; }
    public DateTime? VerificationCompletedAtUtc { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public User? VerifiedByUser { get; set; }
    public decimal? CopayAmount { get; set; }
    public decimal? DeductibleAmount { get; set; }
    public decimal? AnnualMaximum { get; set; }
    public decimal? RemainingMaximum { get; set; }
    public string? FrequencyNotes { get; set; }
    public string? WaitingPeriodNotes { get; set; }
    public string? MissingInfoNotes { get; set; }
    public InsuranceIssueType? IssueType { get; set; }
    public string? Notes { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }
    public Guid? AppointmentWorkItemId { get; set; }
    public AppointmentWorkItem? AppointmentWorkItem { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
}
