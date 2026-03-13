namespace Task_Reminder.Api.Configuration;

public sealed class ApiStartupOptions
{
    public const string SectionName = "App";

    public bool RunMigrationsOnStartup { get; set; } = true;
    public bool SeedDemoDataOnStartup { get; set; }
}
