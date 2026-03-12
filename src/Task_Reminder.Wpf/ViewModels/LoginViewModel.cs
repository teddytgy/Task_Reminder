using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class LoginViewModel(
    ITaskReminderApiClient apiClient,
    SessionState sessionState) : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<UserDto> _users = [];

    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    [ObservableProperty]
    private UserDto? _selectedUser;

    [ObservableProperty]
    private string _statusMessage = "Loading users...";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Users = await apiClient.GetUsersAsync(cancellationToken);
        SelectedUser = Users.FirstOrDefault();
        StatusMessage = Users.Count == 0 ? "No users found. Seed data may not be loaded yet." : "Select your front desk user.";
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
