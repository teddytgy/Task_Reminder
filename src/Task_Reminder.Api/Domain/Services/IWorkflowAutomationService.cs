namespace Task_Reminder.Api.Domain.Services;

public interface IWorkflowAutomationService
{
    Task EnsureAppointmentTasksAsync(Guid appointmentWorkItemId, CancellationToken cancellationToken);
    Task EnsureInsuranceTasksAsync(Guid insuranceWorkItemId, CancellationToken cancellationToken);
    Task EnsureBalanceTasksAsync(Guid balanceFollowUpWorkItemId, CancellationToken cancellationToken);
    Task EnsureOperationalCoverageAsync(CancellationToken cancellationToken);
}
