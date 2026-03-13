using System.Windows;
using Task_Reminder.Wpf.ViewModels;

namespace Task_Reminder.Wpf.Views;

public partial class ImportDataWindow : Window
{
    public ImportDataWindow(ImportDataViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
