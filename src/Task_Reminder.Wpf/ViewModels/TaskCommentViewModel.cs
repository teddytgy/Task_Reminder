using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Task_Reminder.Wpf.ViewModels;

public partial class TaskCommentViewModel : ObservableObject
{
    [ObservableProperty] private string _comment = string.Empty;

    [RelayCommand]
    private void Save(System.Windows.Window window)
    {
        if (string.IsNullOrWhiteSpace(Comment))
        {
            return;
        }

        window.DialogResult = true;
        window.Close();
    }
}
