using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class SnoozeTaskWindow : Window
{
    public SnoozeTaskWindow()
        : this(new SnoozeTaskViewModel())
    {
    }

    public SnoozeTaskWindow(SnoozeTaskViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public SnoozeTaskViewModel ViewModel { get; }

    private void Snooze_Click(object sender, RoutedEventArgs e)
    {
        if (!DateTime.TryParse(ViewModel.SnoozeUntilLocalText, out var parsed))
        {
            MessageBox.Show(this, "Use a valid local date/time format like 2026-03-12 15:00.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (parsed <= DateTime.Now)
        {
            MessageBox.Show(this, "Choose a future time for snooze.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
