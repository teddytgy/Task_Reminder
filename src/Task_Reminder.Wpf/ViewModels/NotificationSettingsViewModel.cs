using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class NotificationSettingsViewModel(
    ITaskReminderApiClient apiClient,
    SessionState sessionState,
    ILogger<NotificationSettingsViewModel> logger) : ObservableObject
{
    [ObservableProperty] private bool _receiveAssignedTaskReminders = true;
    [ObservableProperty] private bool _receiveUnassignedTaskReminders = true;
    [ObservableProperty] private bool _receiveOverdueEscalationAlerts = true;
    [ObservableProperty] private bool _receiveRecurringTaskGenerationAlerts = true;
    [ObservableProperty] private bool _enableSoundForUrgentReminders;
    [ObservableProperty] private string _statusMessage = "Loading preferences...";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (sessionState.CurrentUser is null)
        {
            StatusMessage = "No current user selected.";
            return;
        }

        try
        {
            var preferences = await apiClient.GetUserPreferencesAsync(sessionState.CurrentUser.Id, cancellationToken);
            ReceiveAssignedTaskReminders = preferences.ReceiveAssignedTaskReminders;
            ReceiveUnassignedTaskReminders = preferences.ReceiveUnassignedTaskReminders;
            ReceiveOverdueEscalationAlerts = preferences.ReceiveOverdueEscalationAlerts;
            ReceiveRecurringTaskGenerationAlerts = preferences.ReceiveRecurringTaskGenerationAlerts;
            EnableSoundForUrgentReminders = preferences.EnableSoundForUrgentReminders;
            StatusMessage = "Update reminder preferences for this user.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load notification preferences.");
            StatusMessage = "Could not load notification preferences.";
        }
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        if (sessionState.CurrentUser is null)
        {
            return;
        }

        try
        {
            await apiClient.UpdateUserPreferencesAsync(sessionState.CurrentUser.Id, new()
            {
                ReceiveAssignedTaskReminders = ReceiveAssignedTaskReminders,
                ReceiveUnassignedTaskReminders = ReceiveUnassignedTaskReminders,
                ReceiveOverdueEscalationAlerts = ReceiveOverdueEscalationAlerts,
                ReceiveRecurringTaskGenerationAlerts = ReceiveRecurringTaskGenerationAlerts,
                EnableSoundForUrgentReminders = EnableSoundForUrgentReminders
            }, CancellationToken.None);

            window.DialogResult = true;
            window.Close();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save notification preferences.");
            StatusMessage = "Could not save notification preferences.";
        }
    }
}
