namespace Task_Reminder.Shared;

public enum TaskStatus
{
    New = 0,
    Assigned = 1,
    InProgress = 2,
    Snoozed = 3,
    Completed = 4,
    Cancelled = 5,
    Overdue = 6
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public enum TaskCategory
{
    InsuranceVerification = 0,
    AppointmentConfirmation = 1,
    BalanceCollection = 2,
    Recall = 3,
    TreatmentFollowUp = 4,
    General = 5,
    OpeningChecklist = 6,
    ClosingChecklist = 7
}

public enum UserRole
{
    FrontDesk = 0,
    Manager = 1,
    Admin = 2
}

public enum OfficePermission
{
    ViewOperationalData = 0,
    ManageTasks = 1,
    ViewManagerDashboard = 2,
    ManageOfficeSettings = 3,
    ManageImports = 4,
    ManageRecurringTasks = 5,
    ViewAudit = 6,
    ManageIntegrations = 7
}

public enum RecurrenceType
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Weekdays = 3,
    Custom = 4
}

public enum ExternalIntegrationProviderType
{
    OpenDentalAppointments = 0,
    PatientXpressInsurance = 1,
    CsvManualImport = 2,
    PatientCommunication = 3
}

public enum ExternalIntegrationRunStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Disabled = 3
}
