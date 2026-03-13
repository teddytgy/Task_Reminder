namespace Task_Reminder.Api.Configuration;

public sealed class DeploymentOptions
{
    public const string SectionName = "Deployment";

    public string MinimumSupportedDesktopVersion { get; set; } = "1.0.0";
    public string RecommendedDesktopVersion { get; set; } = "1.0.0";
    public bool EnableAudit { get; set; } = true;
}
