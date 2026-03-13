using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class AdminOperationsViewModel(
    ITaskReminderApiClient apiClient,
    ILogger<AdminOperationsViewModel> logger) : ObservableObject
{
    public ObservableCollection<AuditEntryDto> AuditEntries { get; } = [];
    public ObservableCollection<ExternalIntegrationProviderStatusDto> Integrations { get; } = [];

    [ObservableProperty]
    private SystemStatusSummaryDto? _systemSummary;

    [ObservableProperty]
    private string _statusMessage = "Loading admin operations data...";

    [ObservableProperty]
    private ExternalIntegrationProviderStatusDto? _selectedIntegration;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            SystemSummary = await apiClient.GetSystemSummaryAsync(cancellationToken);
            var audit = await apiClient.GetAuditEntriesAsync(new AuditQueryParameters
            {
                FromUtc = DateTime.UtcNow.AddDays(-7)
            }, cancellationToken);
            var integrations = await apiClient.GetIntegrationsAsync(cancellationToken);

            AuditEntries.Clear();
            foreach (var entry in audit)
            {
                AuditEntries.Add(entry);
            }

            Integrations.Clear();
            foreach (var integration in integrations)
            {
                Integrations.Add(integration);
            }

            SelectedIntegration = Integrations.FirstOrDefault();
            StatusMessage = $"Loaded {AuditEntries.Count} audit entries and {Integrations.Count} integration statuses.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Permission denied. Manager or Admin access is required.";
            MessageBox.Show(StatusMessage, "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load admin operations data.");
            StatusMessage = "Unable to load admin operations data right now.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunIntegration))]
    private async Task RunIntegrationAsync()
    {
        if (SelectedIntegration is null)
        {
            return;
        }

        try
        {
            await apiClient.RunIntegrationAsync(SelectedIntegration.Id, new RunExternalIntegrationRequest(), CancellationToken.None);
            await RefreshAsync(CancellationToken.None);
            StatusMessage = $"Ran integration stub for {SelectedIntegration.DisplayName}.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Permission denied. Only Admin users can run integrations.";
            MessageBox.Show(StatusMessage, "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run integration {IntegrationId}.", SelectedIntegration.Id);
            StatusMessage = "Integration run failed. Check the API logs for details.";
        }
    }

    private bool CanRunIntegration() => SelectedIntegration is not null;
}
