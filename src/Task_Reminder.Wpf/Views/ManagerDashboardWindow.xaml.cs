using System.Windows;
using Microsoft.Win32;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class ManagerDashboardWindow : Window
{
    private readonly ManagerDashboardViewModel _viewModel;

    public ManagerDashboardWindow(ManagerDashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync(CancellationToken.None);
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"task-reminder-report-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        await _viewModel.ExportAsync(dialog.FileName, CancellationToken.None);
        MessageBox.Show(this, "Manager report exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
