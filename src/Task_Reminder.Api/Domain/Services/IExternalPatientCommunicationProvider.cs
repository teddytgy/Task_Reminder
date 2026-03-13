namespace Task_Reminder.Api.Domain.Services;

public interface IExternalPatientCommunicationProvider
{
    string ProviderName { get; }
    Task<string> RunAsync(CancellationToken cancellationToken);
}
