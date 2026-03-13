namespace Task_Reminder.Shared;

public enum AppointmentStatus
{
    Scheduled = 0,
    Confirmed = 1,
    CheckedIn = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5,
    Rescheduled = 6
}

public enum AppointmentConfirmationStatus
{
    NotStarted = 0,
    Pending = 1,
    Confirmed = 2,
    LeftVoicemail = 3,
    NoAnswer = 4,
    TextSent = 5,
    EmailSent = 6,
    Declined = 7
}

public enum AppointmentInsuranceStatus
{
    NotNeeded = 0,
    PendingVerification = 1,
    Verified = 2,
    IssueFound = 3,
    Ineligible = 4
}

public enum AppointmentBalanceStatus
{
    Unknown = 0,
    NoBalance = 1,
    BalanceDue = 2,
    PaymentArranged = 3,
    Collected = 4
}

public enum InsuranceVerificationStatus
{
    NotStarted = 0,
    Pending = 1,
    InProgress = 2,
    Verified = 3,
    Failed = 4,
    NeedsManualReview = 5
}

public enum InsuranceEligibilityStatus
{
    Active = 0,
    Inactive = 1,
    Unknown = 2,
    OutOfNetwork = 3,
    MissingSubscriberInfo = 4,
    UnsupportedPayer = 5
}

public enum InsuranceVerificationMethod
{
    ManualPortal = 0,
    Phone = 1,
    Clearinghouse = 2,
    Imported = 3,
    PatientXpress = 4,
    OpenDental = 5,
    Unknown = 6
}

public enum InsuranceIssueType
{
    MissingMemberId = 0,
    MissingGroupNumber = 1,
    UnsupportedPayer = 2,
    InactiveCoverage = 3,
    NameMismatch = 4,
    DOBMismatch = 5,
    WaitingPeriod = 6,
    FrequencyLimitation = 7,
    AnnualMaxConcern = 8,
    Other = 9
}

public enum ContactType
{
    Call = 0,
    Voicemail = 1,
    Text = 2,
    Email = 3,
    InPerson = 4
}

public enum ContactOutcome
{
    Reached = 0,
    NoAnswer = 1,
    LeftVoicemail = 2,
    Declined = 3,
    CallbackRequested = 4,
    InvalidNumber = 5,
    Completed = 6
}

public enum BalanceFollowUpStatus
{
    NotReviewed = 0,
    InformedPatient = 1,
    PaymentRequested = 2,
    PaymentArranged = 3,
    Collected = 4,
    DisputeNeedsManager = 5
}

public enum ImportFormat
{
    Json = 0,
    Csv = 1
}
