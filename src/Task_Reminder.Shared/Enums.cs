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
    General = 5
}
