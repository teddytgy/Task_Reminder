using Task_Reminder.Api.Domain.Services;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class DisabledExternalInsuranceVerificationProvider : IExternalInsuranceVerificationProvider
{
    public string ProviderName => "PatientXpress Insurance Stub";

    public Task<string> RunAsync(CancellationToken cancellationToken) =>
        Task.FromResult("Insurance verification sync is scaffolded but disabled. Use imports or add a real provider implementation.");
}
