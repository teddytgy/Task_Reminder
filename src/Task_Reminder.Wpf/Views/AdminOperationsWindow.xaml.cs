using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class AdminOperationsWindow : Window
{
    public AdminOperationsViewModel ViewModel { get; }

    public AdminOperationsWindow(AdminOperationsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await ViewModel.InitializeAsync(CancellationToken.None);
    }
}
