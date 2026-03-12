using CommunityToolkit.Mvvm.ComponentModel;
using Task_Reminder.Shared;

namespace Task_Reminder.Wpf.ViewModels;

public partial class AssignTaskViewModel : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<UserDto> _users = [];

    [ObservableProperty]
    private UserDto? _selectedUser;
}
