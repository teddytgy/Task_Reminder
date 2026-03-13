using CommunityToolkit.WinUI.Notifications;
using Microsoft.Extensions.Logging;

namespace Task_Reminder.Wpf.Notifications;

public sealed class ToastNotificationService(ILogger<ToastNotificationService> logger) : IToastNotificationService
{
    public void ShowTaskReminder(string title, string body)
    {
        try
        {
            var content = new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .GetToastContent();

            var xmlDocumentType = Type.GetType("Windows.Data.Xml.Dom.XmlDocument, Windows, ContentType=WindowsRuntime");
            var toastNotificationType = Type.GetType("Windows.UI.Notifications.ToastNotification, Windows, ContentType=WindowsRuntime");
            var toastNotificationManagerType = Type.GetType("Windows.UI.Notifications.ToastNotificationManager, Windows, ContentType=WindowsRuntime");
            if (xmlDocumentType is null || toastNotificationType is null || toastNotificationManagerType is null)
            {
                return;
            }

            var document = Activator.CreateInstance(xmlDocumentType);
            var loadXmlMethod = xmlDocumentType.GetMethod("LoadXml", [typeof(string)]);
            loadXmlMethod?.Invoke(document, [content.GetContent()]);

            var notification = Activator.CreateInstance(toastNotificationType, document);
            var notifier = toastNotificationManagerType
                .GetMethod("CreateToastNotifier", [typeof(string)])?
                .Invoke(null, ["Task_Reminder.Wpf"]);

            notifier?.GetType().GetMethod("Show")?.Invoke(notifier, [notification!]);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Toast notification could not be displayed.");
            // Best-effort desktop notification. App continues even if toast registration is unavailable.
        }
    }
}
