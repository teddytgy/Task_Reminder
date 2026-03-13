using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class ExternalIntegrationRun
{
    public Guid Id { get; set; }
    public Guid ProviderConfigId { get; set; }
    public ExternalIntegrationRunStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Message { get; set; }

    public ExternalIntegrationProviderConfig ProviderConfig { get; set; } = null!;
}
