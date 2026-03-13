using System.Windows;
using Microsoft.Win32;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class OperationsBoardsWindow : Window
{
    private readonly OperationsBoardsViewModel _viewModel;

    public OperationsBoardsWindow(OperationsBoardsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync(CancellationToken.None);
    }

    private async void ExportAppointments_Click(object sender, RoutedEventArgs e) => await ExportAsync("appointments");
    private async void ExportInsurance_Click(object sender, RoutedEventArgs e) => await ExportAsync("insurance");
    private async void ExportBalances_Click(object sender, RoutedEventArgs e) => await ExportAsync("balances");
    private async void ExportContacts_Click(object sender, RoutedEventArgs e) => await ExportAsync("contacts");

    private async Task ExportAsync(string exportType)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"task-reminder-{exportType}-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        await _viewModel.ExportAsync(exportType, dialog.FileName, CancellationToken.None);
    }
}
