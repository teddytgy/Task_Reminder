using Microsoft.Extensions.Options;
using Task_Reminder.Api.Configuration;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Hubs;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class SystemInfoService(
    IOptions<ApiStartupOptions> startupOptions,
    IOptions<DeploymentOptions> deploymentOptions,
    IOptions<FileLoggingOptions> fileLoggingOptions,
    IOfficeSettingsService officeSettingsService,
    IExternalIntegrationService externalIntegrationService,
    IWebHostEnvironment environment) : ISystemInfoService
{
    public async Task<SystemVersionInfoDto> GetVersionAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new SystemVersionInfoDto
        {
            ApiVersion = GetAssemblyVersion(),
            MinimumSupportedDesktopVersion = deploymentOptions.Value.MinimumSupportedDesktopVersion,
            RecommendedDesktopVersion = deploymentOptions.Value.RecommendedDesktopVersion,
            EnvironmentName = environment.EnvironmentName,
            ServerTimeUtc = DateTime.UtcNow
        };
    }

    public async Task<SystemStatusSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var settings = await officeSettingsService.GetAsync(cancellationToken);
        var integrations = await externalIntegrationService.ListAsync(cancellationToken);

        return new SystemStatusSummaryDto
        {
            VersionInfo = await GetVersionAsync(cancellationToken),
            OfficeName = settings.OfficeName,
            HealthUrl = "/health",
            SignalRHubPath = TaskUpdatesHub.HubPath,
            LogPath = Environment.ExpandEnvironmentVariables(fileLoggingOptions.Value.Path),
            RunMigrationsOnStartup = startupOptions.Value.RunMigrationsOnStartup,
            SeedDemoDataOnStartup = startupOptions.Value.SeedDemoDataOnStartup,
            AuditEnabled = deploymentOptions.Value.EnableAudit,
            Integrations = integrations
        };
    }

    private static string GetAssemblyVersion() =>
        typeof(SystemInfoService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
}
