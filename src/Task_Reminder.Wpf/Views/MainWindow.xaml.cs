using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync(CancellationToken.None);
        Closing += (_, _) => _viewModel.ShutdownAsync().GetAwaiter().GetResult();
    }
}
