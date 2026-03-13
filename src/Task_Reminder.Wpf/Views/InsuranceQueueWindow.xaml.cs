using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class InsuranceQueueWindow : Window
{
    private readonly InsuranceQueueViewModel _viewModel;

    public InsuranceQueueWindow(InsuranceQueueViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync(CancellationToken.None);
    }
}
