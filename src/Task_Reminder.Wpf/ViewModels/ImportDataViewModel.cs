using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class ImportDataViewModel(
    ITaskReminderApiClient apiClient,
    ILogger<ImportDataViewModel> logger) : ObservableObject
{
    [ObservableProperty] private string _module = "Appointments";
    [ObservableProperty] private string _format = "Json";
    [ObservableProperty] private string _content = string.Empty;
    [ObservableProperty] private string _sourceSystem = "ManualImport";
    [ObservableProperty] private string _statusMessage = "Paste JSON or CSV content to import.";

    public IReadOnlyList<string> Modules { get; } = ["Appointments", "Insurance"];
    public IReadOnlyList<string> Formats { get; } = ["Json", "Csv"];

    [RelayCommand]
    private async Task ImportAsync()
    {
        try
        {
            var result = Module == "Appointments"
                ? await apiClient.ImportAppointmentsAsync(new ImportAppointmentsRequest
                {
                    Format = Format == "Csv" ? ImportFormat.Csv : ImportFormat.Json,
                    Content = Content,
                    SourceSystem = SourceSystem
                }, CancellationToken.None)
                : await apiClient.ImportInsuranceAsync(new ImportInsuranceWorkItemsRequest
                {
                    Format = Format == "Csv" ? ImportFormat.Csv : ImportFormat.Json,
                    Content = Content,
                    SourceSystem = SourceSystem
                }, CancellationToken.None);

            StatusMessage = string.Join(" ", result.Messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import failed.");
            StatusMessage = "Import failed. Review the file format and try again.";
        }
    }
}
