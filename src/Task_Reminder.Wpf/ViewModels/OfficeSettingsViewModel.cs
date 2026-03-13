using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class OfficeSettingsViewModel(
    ITaskReminderApiClient apiClient,
    ILogger<OfficeSettingsViewModel> logger) : ObservableObject
{
    [ObservableProperty] private string _officeName = string.Empty;
    [ObservableProperty] private string _businessHoursSummary = string.Empty;
    [ObservableProperty] private int _confirmationLeadHours = 24;
    [ObservableProperty] private int _insuranceVerificationLeadDays = 2;
    [ObservableProperty] private int _overdueEscalationMinutes = 45;
    [ObservableProperty] private int _noShowFollowUpDelayHours = 4;
    [ObservableProperty] private int _defaultReminderIntervalMinutes = 30;
    [ObservableProperty] private string _timeZoneId = "Eastern Standard Time";
    [ObservableProperty] private bool _enableTodayBoard = true;
    [ObservableProperty] private bool _enableTomorrowPrepBoard = true;
    [ObservableProperty] private bool _enableCollectionsBoard = true;
    [ObservableProperty] private bool _enableRecallBoard = true;
    [ObservableProperty] private bool _enableManagerQueue = true;
    [ObservableProperty] private string _statusMessage = "Loading office settings...";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await apiClient.GetOfficeSettingsAsync(cancellationToken);
            OfficeName = settings.OfficeName;
            BusinessHoursSummary = settings.BusinessHoursSummary;
            ConfirmationLeadHours = settings.ConfirmationLeadHours;
            InsuranceVerificationLeadDays = settings.InsuranceVerificationLeadDays;
            OverdueEscalationMinutes = settings.OverdueEscalationMinutes;
            NoShowFollowUpDelayHours = settings.NoShowFollowUpDelayHours;
            DefaultReminderIntervalMinutes = settings.DefaultReminderIntervalMinutes;
            TimeZoneId = settings.TimeZoneId;
            EnableTodayBoard = settings.EnableTodayBoard;
            EnableTomorrowPrepBoard = settings.EnableTomorrowPrepBoard;
            EnableCollectionsBoard = settings.EnableCollectionsBoard;
            EnableRecallBoard = settings.EnableRecallBoard;
            EnableManagerQueue = settings.EnableManagerQueue;
            StatusMessage = "Office settings loaded.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load office settings.");
            StatusMessage = "Office settings could not be loaded.";
        }
    }

    [RelayCommand]
    private async Task SaveAsync(System.Windows.Window window)
    {
        await apiClient.UpdateOfficeSettingsAsync(new UpdateOfficeSettingsRequest
        {
            OfficeName = OfficeName,
            BusinessHoursSummary = BusinessHoursSummary,
            ConfirmationLeadHours = ConfirmationLeadHours,
            InsuranceVerificationLeadDays = InsuranceVerificationLeadDays,
            OverdueEscalationMinutes = OverdueEscalationMinutes,
            NoShowFollowUpDelayHours = NoShowFollowUpDelayHours,
            DefaultReminderIntervalMinutes = DefaultReminderIntervalMinutes,
            TimeZoneId = TimeZoneId,
            EnableTodayBoard = EnableTodayBoard,
            EnableTomorrowPrepBoard = EnableTomorrowPrepBoard,
            EnableCollectionsBoard = EnableCollectionsBoard,
            EnableRecallBoard = EnableRecallBoard,
            EnableManagerQueue = EnableManagerQueue
        }, CancellationToken.None);

        window.DialogResult = true;
        window.Close();
    }
}
