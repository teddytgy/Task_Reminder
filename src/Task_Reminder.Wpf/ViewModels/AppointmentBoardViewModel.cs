using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Services;
using Task_Reminder.Wpf.Views;

namespace Task_Reminder.Wpf.ViewModels;

public partial class AppointmentBoardViewModel(
    ITaskReminderApiClient apiClient,
    SessionState sessionState,
    IServiceProvider serviceProvider,
    ILogger<AppointmentBoardViewModel> logger) : ObservableObject
{
    [ObservableProperty] private AppointmentWorkItemDto? _selectedAppointment;
    [ObservableProperty] private string _selectedFilter = "today";
    [ObservableProperty] private string _statusMessage = "Loading appointments...";

    public ObservableCollection<AppointmentWorkItemDto> Appointments { get; } = [];
    public ObservableCollection<ContactLogDto> ContactLogs { get; } = [];
    public IReadOnlyList<string> Filters { get; } = ["today", "tomorrow", "this-week", "unconfirmed", "insurance-pending", "balance-due", "no-show-cancelled"];

    partial void OnSelectedAppointmentChanged(AppointmentWorkItemDto? value)
    {
        if (value is not null)
        {
            _ = LoadContactLogsAsync(value.Id);
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken) => RefreshAsync(cancellationToken);

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var items = await apiClient.GetAppointmentsAsync(new AppointmentQueryParameters { Filter = SelectedFilter }, cancellationToken);
            Appointments.Clear();
            foreach (var item in items) Appointments.Add(item);
            SelectedAppointment = Appointments.FirstOrDefault();
            StatusMessage = $"Loaded {items.Count} appointments.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load appointments.");
            StatusMessage = "Appointments could not be loaded.";
        }
    }

    [RelayCommand]
    private async Task ApplyActionAsync(string actionName)
    {
        if (SelectedAppointment is null)
        {
            return;
        }

        await apiClient.ApplyAppointmentActionAsync(SelectedAppointment.Id, actionName, new AppointmentActionRequest { UserId = sessionState.CurrentUser?.Id }, CancellationToken.None);
        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task AddContactLogAsync()
    {
        if (SelectedAppointment is null || sessionState.CurrentUser is null)
        {
            return;
        }

        var dialog = serviceProvider.GetRequiredService<ContactLogWindow>();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await apiClient.CreateContactLogAsync(new CreateContactLogRequest
        {
            AppointmentWorkItemId = SelectedAppointment.Id,
            ContactType = dialog.ViewModel.ContactType,
            Outcome = dialog.ViewModel.Outcome,
            Notes = dialog.ViewModel.Notes,
            PerformedByUserId = sessionState.CurrentUser.Id
        }, CancellationToken.None);

        await LoadContactLogsAsync(SelectedAppointment.Id);
        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task CreateFollowUpTaskAsync()
    {
        if (SelectedAppointment is null)
        {
            return;
        }

        await apiClient.CreateAppointmentFollowUpTaskAsync(SelectedAppointment.Id, new AppointmentActionRequest { UserId = sessionState.CurrentUser?.Id }, CancellationToken.None);
        StatusMessage = "Follow-up task created.";
    }

    private async Task LoadContactLogsAsync(Guid appointmentId)
    {
        var items = await apiClient.GetContactLogsAsync(null, appointmentId, null, null, CancellationToken.None);
        ContactLogs.Clear();
        foreach (var item in items) ContactLogs.Add(item);
    }
}
