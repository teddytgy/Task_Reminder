namespace Task_Reminder.Wpf.Notifications;

public interface IToastNotificationService
{
    void ShowTaskReminder(string title, string body);
}
