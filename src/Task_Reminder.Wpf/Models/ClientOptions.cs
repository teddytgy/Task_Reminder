namespace Task_Reminder.Wpf.Models;

public sealed class ClientOptions
{
    public const string SectionName = "Client";

    public string ApiBaseUrl { get; set; } = "https://localhost:7087/";
    public string SignalRHubUrl { get; set; } = "https://localhost:7087/hubs/tasks";
    public int ReminderPollingSeconds { get; set; } = 60;
    public int DefaultRepeatMinutes { get; set; } = 30;
    public bool AllowInvalidLocalCertificatesInDevelopment { get; set; } = true;
}
