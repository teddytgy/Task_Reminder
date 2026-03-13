using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class AppointmentBoardWindow : Window
{
    private readonly AppointmentBoardViewModel _viewModel;

    public AppointmentBoardWindow(AppointmentBoardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync(CancellationToken.None);
    }
}
