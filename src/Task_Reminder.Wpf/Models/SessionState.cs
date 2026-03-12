using Task_Reminder.Shared;

namespace Task_Reminder.Wpf.Models;

public sealed class SessionState
{
    public UserDto? CurrentUser { get; private set; }

    public void SetCurrentUser(UserDto user)
    {
        CurrentUser = user;
    }
}
