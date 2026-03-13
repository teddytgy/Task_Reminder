using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class ManagerDashboardViewModel(
    ITaskReminderApiClient apiClient,
    ILogger<ManagerDashboardViewModel> logger) : ObservableObject
{
    [ObservableProperty] private string _selectedRange = "Last 7 Days";
    [ObservableProperty] private string? _customFromText;
    [ObservableProperty] private string? _customToText;
    [ObservableProperty] private int _totalOpenTasks;
    [ObservableProperty] private int _overdueTasks;
    [ObservableProperty] private int _completedInRange;
    [ObservableProperty] private int _unassignedTasks;
    [ObservableProperty] private double _averageCompletionMinutes;
    [ObservableProperty] private string _statusMessage = "Loading manager metrics...";

    public ObservableCollection<MetricBreakdownItemDto> TasksByCategory { get; } = [];
    public ObservableCollection<MetricBreakdownItemDto> TasksByPriority { get; } = [];
    public ObservableCollection<UserPerformanceDto> CompletedPerUser { get; } = [];
    public IReadOnlyList<string> RangeOptions { get; } = ["Today", "Last 7 Days", "Last 30 Days", "Custom"];

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var query = BuildQuery();
            var metrics = await apiClient.GetManagerMetricsAsync(query, cancellationToken);
            TotalOpenTasks = metrics.TotalOpenTasks;
            OverdueTasks = metrics.OverdueTasks;
            CompletedInRange = metrics.CompletedInRange;
            UnassignedTasks = metrics.UnassignedTasks;
            AverageCompletionMinutes = metrics.AverageCompletionMinutes;

            TasksByCategory.Clear();
            TasksByPriority.Clear();
            CompletedPerUser.Clear();

            foreach (var item in metrics.TasksByCategory) TasksByCategory.Add(item);
            foreach (var item in metrics.TasksByPriority) TasksByPriority.Add(item);
            foreach (var item in metrics.CompletedPerUser) CompletedPerUser.Add(item);

            StatusMessage = $"Manager metrics loaded at {DateTime.Now:t}.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load manager dashboard metrics.");
            StatusMessage = "Manager metrics could not be loaded.";
        }
    }

    public async Task ExportAsync(string filePath, CancellationToken cancellationToken)
    {
        var csv = await apiClient.ExportManagerMetricsCsvAsync(BuildQuery(), cancellationToken);
        await File.WriteAllTextAsync(filePath, csv, cancellationToken);
        StatusMessage = $"Exported manager report to {filePath}.";
    }

    private ManagerMetricsQuery BuildQuery()
    {
        return SelectedRange switch
        {
            "Today" => new ManagerMetricsQuery { PresetDays = 1 },
            "Last 30 Days" => new ManagerMetricsQuery { PresetDays = 30 },
            "Custom" => new ManagerMetricsQuery
            {
                FromUtc = DateTime.TryParse(CustomFromText, out var from) ? from.ToUniversalTime() : null,
                ToUtc = DateTime.TryParse(CustomToText, out var to) ? to.ToUniversalTime() : null
            },
            _ => new ManagerMetricsQuery { PresetDays = 7 }
        };
    }
}
