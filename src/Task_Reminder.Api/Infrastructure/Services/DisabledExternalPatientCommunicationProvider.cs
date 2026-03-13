using Task_Reminder.Api.Domain.Services;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class DisabledExternalPatientCommunicationProvider : IExternalPatientCommunicationProvider
{
    public string ProviderName => "Patient Communication Stub";

    public Task<string> RunAsync(CancellationToken cancellationToken) =>
        Task.FromResult("Patient communication sync is scaffolded but disabled. Add a real provider when SMS or email integrations are approved.");
}
