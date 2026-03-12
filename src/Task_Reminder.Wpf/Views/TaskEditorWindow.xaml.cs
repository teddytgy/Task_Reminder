using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class TaskEditorWindow : Window
{
    public TaskEditorWindow()
        : this(new TaskEditorViewModel())
    {
    }

    public TaskEditorWindow(TaskEditorViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public TaskEditorViewModel ViewModel { get; }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Title))
        {
            MessageBox.Show(this, "Title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(ViewModel.DueAtLocalText) && !DateTime.TryParse(ViewModel.DueAtLocalText, out _))
        {
            MessageBox.Show(this, "Due time must use a valid local date/time format like 2026-03-12 14:30.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
