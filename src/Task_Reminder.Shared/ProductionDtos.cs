using System.ComponentModel.DataAnnotations;

namespace Task_Reminder.Shared;

public sealed class AuditEntryDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByDisplayName { get; set; }
    public DateTime PerformedAtUtc { get; set; }
}

public sealed class AuditQueryParameters
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public Guid? UserId { get; set; }
    public string? EntityType { get; set; }
    public string? ActionType { get; set; }
}

public sealed class SystemVersionInfoDto
{
    public string ApiVersion { get; set; } = string.Empty;
    public string MinimumSupportedDesktopVersion { get; set; } = string.Empty;
    public string RecommendedDesktopVersion { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public DateTime ServerTimeUtc { get; set; }
}

public sealed class SystemStatusSummaryDto
{
    public SystemVersionInfoDto VersionInfo { get; set; } = new();
    public string OfficeName { get; set; } = string.Empty;
    public string HealthUrl { get; set; } = "/health";
    public string SignalRHubPath { get; set; } = "/hubs/tasks";
    public string LogPath { get; set; } = string.Empty;
    public bool RunMigrationsOnStartup { get; set; }
    public bool SeedDemoDataOnStartup { get; set; }
    public bool AuditEnabled { get; set; }
    public IReadOnlyList<ExternalIntegrationProviderStatusDto> Integrations { get; set; } = [];
}

public sealed class ExternalIntegrationProviderStatusDto
{
    public Guid Id { get; set; }
    public ExternalIntegrationProviderType ProviderType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? BaseUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastRunStartedAtUtc { get; set; }
    public DateTime? LastRunCompletedAtUtc { get; set; }
    public ExternalIntegrationRunStatus? LastRunStatus { get; set; }
    public string? LastRunMessage { get; set; }
}

public sealed class UpdateExternalIntegrationProviderRequest
{
    public bool IsEnabled { get; set; }

    [MaxLength(200)]
    public string? BaseUrl { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public sealed class RunExternalIntegrationRequest
{
    [MaxLength(500)]
    public string? Notes { get; set; }
}
