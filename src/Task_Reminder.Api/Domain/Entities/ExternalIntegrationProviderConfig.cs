using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Entities;

public sealed class ExternalIntegrationProviderConfig
{
    public Guid Id { get; set; }
    public ExternalIntegrationProviderType ProviderType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? BaseUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ExternalIntegrationRun> Runs { get; set; } = new List<ExternalIntegrationRun>();
}
