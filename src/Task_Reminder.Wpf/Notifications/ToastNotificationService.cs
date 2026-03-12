using CommunityToolkit.WinUI.Notifications;

namespace Task_Reminder.Wpf.Notifications;

public sealed class ToastNotificationService : IToastNotificationService
{
    public void ShowTaskReminder(string title, string body)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .Show();
        }
        catch
        {
            // Best-effort desktop notification. App continues even if toast registration is unavailable.
        }
    }
}
