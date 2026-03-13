namespace Task_Reminder.Api.Domain.Services;

public interface IExternalAppointmentSyncProvider
{
    string ProviderName { get; }
    Task<string> RunAsync(CancellationToken cancellationToken);
}
