namespace Task_Reminder.Api.Domain.Services;

public interface IExternalInsuranceVerificationProvider
{
    string ProviderName { get; }
    Task<string> RunAsync(CancellationToken cancellationToken);
}
