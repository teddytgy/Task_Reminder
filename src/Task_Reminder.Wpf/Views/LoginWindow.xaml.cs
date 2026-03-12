using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeAsync(CancellationToken.None);
    }
}
