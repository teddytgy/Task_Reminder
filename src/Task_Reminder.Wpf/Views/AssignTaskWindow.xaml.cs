using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class AssignTaskWindow : Window
{
    public AssignTaskWindow()
        : this(new AssignTaskViewModel())
    {
    }

    public AssignTaskWindow(AssignTaskViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public AssignTaskViewModel ViewModel { get; }

    private void Assign_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedUser is null)
        {
            MessageBox.Show(this, "Select a user to assign the task.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
