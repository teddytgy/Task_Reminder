using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class OperationsBoardsViewModel(
    ITaskReminderApiClient apiClient,
    ILogger<OperationsBoardsViewModel> logger) : ObservableObject
{
    [ObservableProperty] private string _selectedRange = "Last 7 Days";
    [ObservableProperty] private string _statusMessage = "Loading operations boards...";
    public IReadOnlyList<string> RangeOptions { get; } = ["Today", "Last 7 Days", "Last 30 Days"];

    public ObservableCollection<AppointmentWorkItemDto> TodayAppointments { get; } = [];
    public ObservableCollection<AppointmentWorkItemDto> TomorrowAppointments { get; } = [];
    public ObservableCollection<InsuranceWorkItemDto> InsurancePendingItems { get; } = [];
    public ObservableCollection<BalanceFollowUpWorkItemDto> BalanceDueItems { get; } = [];
    public ObservableCollection<TaskItemDto> OverdueTasks { get; } = [];
    public ObservableCollection<AppointmentWorkItemDto> RecallCandidates { get; } = [];
    public ObservableCollection<TaskItemDto> ManagerEscalations { get; } = [];
    public ObservableCollection<UserWorkloadDto> Workloads { get; } = [];
    public ObservableCollection<MetricBreakdownItemDto> ContactOutcomeDistribution { get; } = [];

    [ObservableProperty] private double _appointmentConfirmationRate;
    [ObservableProperty] private double _noShowRate;
    [ObservableProperty] private double _cancellationRate;
    [ObservableProperty] private double _insuranceVerificationCompletionRate;

    public Task InitializeAsync(CancellationToken cancellationToken) => RefreshAsync(cancellationToken);

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var board = await apiClient.GetOperationsBoardAsync(cancellationToken);
            var kpis = await apiClient.GetOperationsKpisAsync(BuildQuery(), cancellationToken);

            Replace(TodayAppointments, board.TodayAppointments);
            Replace(TomorrowAppointments, board.TomorrowAppointments);
            Replace(InsurancePendingItems, board.InsurancePendingItems);
            Replace(BalanceDueItems, board.BalanceDueItems);
            Replace(OverdueTasks, board.OverdueTasks);
            Replace(RecallCandidates, board.RecallCandidates);
            Replace(ManagerEscalations, board.ManagerEscalations);
            Replace(Workloads, board.WorkloadByUser);
            Replace(ContactOutcomeDistribution, kpis.ContactOutcomeDistribution);

            AppointmentConfirmationRate = kpis.AppointmentConfirmationRate;
            NoShowRate = kpis.NoShowRate;
            CancellationRate = kpis.CancellationRate;
            InsuranceVerificationCompletionRate = kpis.InsuranceVerificationCompletionRate;
            StatusMessage = "Operations boards loaded.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load operations boards.");
            StatusMessage = "Operations boards could not be loaded.";
        }
    }

    public async Task ExportAsync(string exportType, string path, CancellationToken cancellationToken)
    {
        var csv = await apiClient.ExportOperationsCsvAsync(exportType, BuildQuery(), cancellationToken);
        await File.WriteAllTextAsync(path, csv, cancellationToken);
        StatusMessage = $"Exported {exportType} report.";
    }

    private ManagerMetricsQuery BuildQuery() => SelectedRange switch
    {
        "Today" => new ManagerMetricsQuery { PresetDays = 1 },
        "Last 30 Days" => new ManagerMetricsQuery { PresetDays = 30 },
        _ => new ManagerMetricsQuery { PresetDays = 7 }
    };

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }
}
