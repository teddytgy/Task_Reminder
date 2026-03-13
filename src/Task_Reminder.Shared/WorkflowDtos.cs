using System.ComponentModel.DataAnnotations;

namespace Task_Reminder.Shared;

public sealed class AppointmentWorkItemDto
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientReference { get; set; } = string.Empty;
    public DateOnly AppointmentDateLocal { get; set; }
    public TimeSpan AppointmentTimeLocal { get; set; }
    public string? ProviderName { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public AppointmentConfirmationStatus ConfirmationStatus { get; set; }
    public AppointmentInsuranceStatus InsuranceStatus { get; set; }
    public AppointmentBalanceStatus BalanceStatus { get; set; }
    public string? Notes { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }
    public Guid? InsuranceWorkItemId { get; set; }
    public Guid? BalanceFollowUpWorkItemId { get; set; }
    public string? LatestContactSummary { get; set; }
    public DateTime? LatestContactAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateAppointmentWorkItemRequest
{
    [Required]
    [MaxLength(200)]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PatientReference { get; set; } = string.Empty;

    public DateOnly AppointmentDateLocal { get; set; }
    public TimeSpan AppointmentTimeLocal { get; set; }

    [MaxLength(100)]
    public string? ProviderName { get; set; }

    [Required]
    [MaxLength(100)]
    public string AppointmentType { get; set; } = string.Empty;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentConfirmationStatus ConfirmationStatus { get; set; } = AppointmentConfirmationStatus.NotStarted;
    public AppointmentInsuranceStatus InsuranceStatus { get; set; } = AppointmentInsuranceStatus.PendingVerification;
    public AppointmentBalanceStatus BalanceStatus { get; set; } = AppointmentBalanceStatus.Unknown;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? SourceSystem { get; set; }

    [MaxLength(100)]
    public string? SourceReference { get; set; }
}

public sealed class UpdateAppointmentWorkItemRequest : CreateAppointmentWorkItemRequest
{
}

public sealed class AppointmentQueryParameters
{
    public string? Filter { get; set; }
    public Guid? AssignedUserId { get; set; }
}

public sealed class AppointmentActionRequest
{
    public Guid? UserId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public sealed class InsuranceWorkItemDto
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
    public InsuranceVerificationStatus VerificationStatus { get; set; }
    public InsuranceEligibilityStatus EligibilityStatus { get; set; }
    public InsuranceVerificationMethod VerificationMethod { get; set; }
    public DateTime? VerificationRequestedAtUtc { get; set; }
    public DateTime? VerificationCompletedAtUtc { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public string? VerifiedByDisplayName { get; set; }
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
    public Guid? LinkedTaskId { get; set; }
    public string? LatestContactSummary { get; set; }
    public DateTime? LatestContactAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateInsuranceWorkItemRequest
{
    [Required]
    [MaxLength(200)]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PatientReference { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CarrierName { get; set; }

    [MaxLength(100)]
    public string? PlanName { get; set; }

    [MaxLength(100)]
    public string? MemberId { get; set; }

    [MaxLength(100)]
    public string? GroupNumber { get; set; }

    [MaxLength(100)]
    public string? PayerId { get; set; }

    public DateOnly? AppointmentDateLocal { get; set; }
    public InsuranceVerificationStatus VerificationStatus { get; set; } = InsuranceVerificationStatus.NotStarted;
    public InsuranceEligibilityStatus EligibilityStatus { get; set; } = InsuranceEligibilityStatus.Unknown;
    public InsuranceVerificationMethod VerificationMethod { get; set; } = InsuranceVerificationMethod.Unknown;
    public decimal? CopayAmount { get; set; }
    public decimal? DeductibleAmount { get; set; }
    public decimal? AnnualMaximum { get; set; }
    public decimal? RemainingMaximum { get; set; }

    [MaxLength(500)]
    public string? FrequencyNotes { get; set; }

    [MaxLength(500)]
    public string? WaitingPeriodNotes { get; set; }

    [MaxLength(500)]
    public string? MissingInfoNotes { get; set; }

    public InsuranceIssueType? IssueType { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? SourceSystem { get; set; }

    [MaxLength(100)]
    public string? SourceReference { get; set; }
    public Guid? AppointmentWorkItemId { get; set; }
}

public sealed class UpdateInsuranceWorkItemRequest : CreateInsuranceWorkItemRequest
{
}

public sealed class InsuranceQueryParameters
{
    public string? Filter { get; set; }
}

public sealed class InsuranceStatusUpdateRequest
{
    public Guid? UserId { get; set; }
    public InsuranceVerificationStatus? VerificationStatus { get; set; }
    public InsuranceEligibilityStatus? EligibilityStatus { get; set; }
    public InsuranceIssueType? IssueType { get; set; }
    public InsuranceVerificationMethod? VerificationMethod { get; set; }
    public decimal? CopayAmount { get; set; }
    public decimal? DeductibleAmount { get; set; }
    public decimal? AnnualMaximum { get; set; }
    public decimal? RemainingMaximum { get; set; }
    public string? FrequencyNotes { get; set; }
    public string? WaitingPeriodNotes { get; set; }
    public string? MissingInfoNotes { get; set; }
    public string? Notes { get; set; }
}

public sealed class BalanceFollowUpWorkItemDto
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientReference { get; set; } = string.Empty;
    public Guid? AppointmentWorkItemId { get; set; }
    public decimal AmountDue { get; set; }
    public string? DueReasonNote { get; set; }
    public BalanceFollowUpStatus Status { get; set; }
    public DateOnly? FollowUpDateLocal { get; set; }
    public string? Notes { get; set; }
    public Guid? LinkedTaskId { get; set; }
    public string? LatestContactSummary { get; set; }
    public DateTime? LatestContactAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateBalanceFollowUpWorkItemRequest
{
    [Required]
    [MaxLength(200)]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PatientReference { get; set; } = string.Empty;

    public Guid? AppointmentWorkItemId { get; set; }
    public decimal AmountDue { get; set; }

    [MaxLength(500)]
    public string? DueReasonNote { get; set; }

    public BalanceFollowUpStatus Status { get; set; } = BalanceFollowUpStatus.NotReviewed;
    public DateOnly? FollowUpDateLocal { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public sealed class UpdateBalanceFollowUpWorkItemRequest : CreateBalanceFollowUpWorkItemRequest
{
}

public sealed class BalanceQueryParameters
{
    public string? Filter { get; set; }
}

public sealed class ContactLogDto
{
    public Guid Id { get; set; }
    public Guid? TaskItemId { get; set; }
    public Guid? AppointmentWorkItemId { get; set; }
    public Guid? InsuranceWorkItemId { get; set; }
    public Guid? BalanceFollowUpWorkItemId { get; set; }
    public ContactType ContactType { get; set; }
    public ContactOutcome Outcome { get; set; }
    public string? Notes { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByDisplayName { get; set; }
    public DateTime PerformedAtUtc { get; set; }
}

public sealed class CreateContactLogRequest
{
    public Guid? TaskItemId { get; set; }
    public Guid? AppointmentWorkItemId { get; set; }
    public Guid? InsuranceWorkItemId { get; set; }
    public Guid? BalanceFollowUpWorkItemId { get; set; }
    public ContactType ContactType { get; set; }
    public ContactOutcome Outcome { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public Guid? PerformedByUserId { get; set; }
}

public sealed class OfficeSettingsDto
{
    public Guid Id { get; set; }
    public string OfficeName { get; set; } = "Task Reminder Dental Office";
    public string BusinessHoursSummary { get; set; } = "Mon-Fri 8:00 AM - 5:00 PM";
    public int ConfirmationLeadHours { get; set; } = 24;
    public int InsuranceVerificationLeadDays { get; set; } = 2;
    public int OverdueEscalationMinutes { get; set; } = 45;
    public int NoShowFollowUpDelayHours { get; set; } = 4;
    public Guid? ManagerEscalationUserId { get; set; }
    public int DefaultReminderIntervalMinutes { get; set; } = 30;
    public string TimeZoneId { get; set; } = "Eastern Standard Time";
    public bool EnableTodayBoard { get; set; } = true;
    public bool EnableTomorrowPrepBoard { get; set; } = true;
    public bool EnableCollectionsBoard { get; set; } = true;
    public bool EnableRecallBoard { get; set; } = true;
    public bool EnableManagerQueue { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UpdateOfficeSettingsRequest
{
    [MaxLength(200)]
    public string OfficeName { get; set; } = "Task Reminder Dental Office";

    [MaxLength(200)]
    public string BusinessHoursSummary { get; set; } = "Mon-Fri 8:00 AM - 5:00 PM";

    public int ConfirmationLeadHours { get; set; } = 24;
    public int InsuranceVerificationLeadDays { get; set; } = 2;
    public int OverdueEscalationMinutes { get; set; } = 45;
    public int NoShowFollowUpDelayHours { get; set; } = 4;
    public Guid? ManagerEscalationUserId { get; set; }
    public int DefaultReminderIntervalMinutes { get; set; } = 30;

    [MaxLength(100)]
    public string TimeZoneId { get; set; } = "Eastern Standard Time";

    public bool EnableTodayBoard { get; set; } = true;
    public bool EnableTomorrowPrepBoard { get; set; } = true;
    public bool EnableCollectionsBoard { get; set; } = true;
    public bool EnableRecallBoard { get; set; } = true;
    public bool EnableManagerQueue { get; set; } = true;
}

public sealed class ImportAppointmentsRequest
{
    public ImportFormat Format { get; set; } = ImportFormat.Json;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SourceSystem { get; set; }
}

public sealed class ImportInsuranceWorkItemsRequest
{
    public ImportFormat Format { get; set; } = ImportFormat.Json;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SourceSystem { get; set; }
}

public sealed class ImportResultDto
{
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int UpdatedCount { get; set; }
    public IReadOnlyList<string> Messages { get; set; } = [];
}

public sealed class UserWorkloadDto
{
    public Guid? UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int OpenItems { get; set; }
    public int OverdueItems { get; set; }
    public int CompletedToday { get; set; }
    public double AverageCompletionMinutes { get; set; }
}

public sealed class UserActivityTimelineItemDto
{
    public DateTime OccurredAtUtc { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class OperationsBoardDto
{
    public IReadOnlyList<AppointmentWorkItemDto> TodayAppointments { get; set; } = [];
    public IReadOnlyList<AppointmentWorkItemDto> TomorrowAppointments { get; set; } = [];
    public IReadOnlyList<AppointmentWorkItemDto> UnconfirmedAppointments { get; set; } = [];
    public IReadOnlyList<InsuranceWorkItemDto> InsurancePendingItems { get; set; } = [];
    public IReadOnlyList<BalanceFollowUpWorkItemDto> BalanceDueItems { get; set; } = [];
    public IReadOnlyList<TaskItemDto> OverdueTasks { get; set; } = [];
    public IReadOnlyList<AppointmentWorkItemDto> RecallCandidates { get; set; } = [];
    public IReadOnlyList<AppointmentWorkItemDto> NoShowOrCancelledAppointments { get; set; } = [];
    public IReadOnlyList<InsuranceWorkItemDto> UnresolvedInsuranceIssues { get; set; } = [];
    public IReadOnlyList<TaskItemDto> ManagerEscalations { get; set; } = [];
    public IReadOnlyList<UserWorkloadDto> WorkloadByUser { get; set; } = [];
}

public sealed class OperationsKpiDto
{
    public DateTime RangeStartUtc { get; set; }
    public DateTime RangeEndUtc { get; set; }
    public double AppointmentConfirmationRate { get; set; }
    public double NoShowRate { get; set; }
    public double CancellationRate { get; set; }
    public double InsuranceVerificationCompletionRate { get; set; }
    public IReadOnlyList<MetricBreakdownItemDto> InsuranceIssueRateByType { get; set; } = [];
    public IReadOnlyList<MetricBreakdownItemDto> BalanceCollectionProgress { get; set; } = [];
    public double AverageTaskCompletionTimeMinutes { get; set; }
    public IReadOnlyList<UserPerformanceDto> TaskCompletionCountByUser { get; set; } = [];
    public IReadOnlyList<MetricBreakdownItemDto> OverdueRateByCategory { get; set; } = [];
    public IReadOnlyList<MetricBreakdownItemDto> ContactOutcomeDistribution { get; set; } = [];
}

public sealed class BulkTaskUpdateRequest
{
    public IReadOnlyList<Guid> TaskIds { get; set; } = [];
    public Guid? AssignedUserId { get; set; }
    public TaskPriority? Priority { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public Guid? PerformedByUserId { get; set; }
}
