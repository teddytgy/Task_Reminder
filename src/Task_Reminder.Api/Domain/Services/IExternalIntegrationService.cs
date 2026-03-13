using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IExternalIntegrationService
{
    Task<IReadOnlyList<ExternalIntegrationProviderStatusDto>> ListAsync(CancellationToken cancellationToken);
    Task<ExternalIntegrationProviderStatusDto?> UpdateAsync(Guid id, UpdateExternalIntegrationProviderRequest request, CancellationToken cancellationToken);
    Task<ExternalIntegrationProviderStatusDto?> RunAsync(Guid id, RunExternalIntegrationRequest request, CancellationToken cancellationToken);
}
