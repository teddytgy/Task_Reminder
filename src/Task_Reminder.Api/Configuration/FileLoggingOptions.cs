namespace Task_Reminder.Api.Configuration;

public sealed class FileLoggingOptions
{
    public bool Enabled { get; set; } = true;
    public string Path { get; set; } = "%LOCALAPPDATA%\\Task_Reminder\\Logs\\api\\task-reminder-api.log";
    public string MinimumLevel { get; set; } = "Information";
    public bool RollDaily { get; set; } = true;
    public int RetainedFileCountLimit { get; set; } = 14;
}
