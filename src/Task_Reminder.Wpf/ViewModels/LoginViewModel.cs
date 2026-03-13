using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class LoginViewModel(
    ITaskReminderApiClient apiClient,
    SessionState sessionState,
    ILogger<LoginViewModel> logger) : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<UserDto> _users = [];

    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    [ObservableProperty]
    private UserDto? _selectedUser;

    [ObservableProperty]
    private string _statusMessage = "Loading users...";

    [ObservableProperty]
    private bool _hasLoadingError;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            StatusMessage = "Loading users...";
            HasLoadingError = false;
            Users = await apiClient.GetUsersAsync(cancellationToken);
            SelectedUser = Users.FirstOrDefault();
            logger.LogInformation("Loaded {UserCount} users for desktop login.", Users.Count);
            StatusMessage = Users.Count == 0
                ? "No users found. Seed data may not be loaded yet."
                : "Select your front desk user.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize the login screen.");
            Users = [];
            SelectedUser = null;
            HasLoadingError = true;
            StatusMessage = "Unable to reach the Task Reminder API. Check that the API is running and the desktop app configuration points to the correct server.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue(System.Windows.Window window)
    {
        if (SelectedUser is null)
        {
            return;
        }

        sessionState.SetCurrentUser(SelectedUser);
        window.DialogResult = true;
        window.Close();
    }

    private bool CanContinue() => SelectedUser is not null;
}
