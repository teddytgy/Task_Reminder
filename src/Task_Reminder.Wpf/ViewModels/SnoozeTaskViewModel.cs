using CommunityToolkit.Mvvm.ComponentModel;

namespace Task_Reminder.Wpf.ViewModels;

public partial class SnoozeTaskViewModel : ObservableObject
{
    [ObservableProperty]
    private string _snoozeUntilLocalText = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm");
}
