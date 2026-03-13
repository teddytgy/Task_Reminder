using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Services;
using Task_Reminder.Wpf.Views;

namespace Task_Reminder.Wpf.ViewModels;

public partial class InsuranceQueueViewModel(
    ITaskReminderApiClient apiClient,
    SessionState sessionState,
    IServiceProvider serviceProvider,
    ILogger<InsuranceQueueViewModel> logger) : ObservableObject
{
    [ObservableProperty] private InsuranceWorkItemDto? _selectedItem;
    [ObservableProperty] private string _selectedFilter = "verification-pending";
    [ObservableProperty] private string _statusMessage = "Loading insurance queue...";

    public ObservableCollection<InsuranceWorkItemDto> Items { get; } = [];
    public ObservableCollection<ContactLogDto> ContactLogs { get; } = [];
    public IReadOnlyList<string> Filters { get; } = ["today", "tomorrow", "verification-pending", "issue-found", "inactive-coverage", "missing-info", "manual-review-needed"];

    partial void OnSelectedItemChanged(InsuranceWorkItemDto? value)
    {
        if (value is not null)
        {
            _ = LoadContactLogsAsync(value.Id);
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken) => RefreshAsync(cancellationToken);

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var items = await apiClient.GetInsuranceWorkItemsAsync(new InsuranceQueryParameters { Filter = SelectedFilter }, cancellationToken);
            Items.Clear();
            foreach (var item in items) Items.Add(item);
            SelectedItem = Items.FirstOrDefault();
            StatusMessage = $"Loaded {items.Count} insurance items.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load insurance queue.");
            StatusMessage = "Insurance queue could not be loaded.";
        }
    }

    [RelayCommand]
    private async Task MarkStartedAsync() => await UpdateStatusAsync(new InsuranceStatusUpdateRequest
    {
        UserId = sessionState.CurrentUser?.Id,
        VerificationStatus = InsuranceVerificationStatus.InProgress
    });

    [RelayCommand]
    private async Task MarkVerifiedAsync() => await UpdateStatusAsync(new InsuranceStatusUpdateRequest
    {
        UserId = sessionState.CurrentUser?.Id,
        VerificationStatus = InsuranceVerificationStatus.Verified
    });

    [RelayCommand]
    private async Task MarkFailedAsync() => await UpdateStatusAsync(new InsuranceStatusUpdateRequest
    {
        UserId = sessionState.CurrentUser?.Id,
        VerificationStatus = InsuranceVerificationStatus.Failed
    });

    [RelayCommand]
    private async Task MarkManualReviewAsync() => await UpdateStatusAsync(new InsuranceStatusUpdateRequest
    {
        UserId = sessionState.CurrentUser?.Id,
        VerificationStatus = InsuranceVerificationStatus.NeedsManualReview
    });

    [RelayCommand]
    private async Task MarkInactiveCoverageAsync() => await UpdateStatusAsync(new InsuranceStatusUpdateRequest
    {
        UserId = sessionState.CurrentUser?.Id,
        EligibilityStatus = InsuranceEligibilityStatus.Inactive,
        IssueType = InsuranceIssueType.InactiveCoverage
    });

    [RelayCommand]
    private async Task AddContactLogAsync()
    {
        if (SelectedItem is null || sessionState.CurrentUser is null)
        {
            return;
        }

        var dialog = serviceProvider.GetRequiredService<ContactLogWindow>();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await apiClient.CreateContactLogAsync(new CreateContactLogRequest
        {
            InsuranceWorkItemId = SelectedItem.Id,
            ContactType = dialog.ViewModel.ContactType,
            Outcome = dialog.ViewModel.Outcome,
            Notes = dialog.ViewModel.Notes,
            PerformedByUserId = sessionState.CurrentUser.Id
        }, CancellationToken.None);

        await LoadContactLogsAsync(SelectedItem.Id);
        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task CreateFollowUpTaskAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        await apiClient.CreateInsuranceFollowUpTaskAsync(SelectedItem.Id, sessionState.CurrentUser?.Id, false, CancellationToken.None);
        StatusMessage = "Insurance follow-up task created.";
    }

    [RelayCommand]
    private async Task CreateManagerTaskAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        await apiClient.CreateInsuranceFollowUpTaskAsync(SelectedItem.Id, sessionState.CurrentUser?.Id, true, CancellationToken.None);
        StatusMessage = "Manager escalation task created.";
    }

    private async Task UpdateStatusAsync(InsuranceStatusUpdateRequest request)
    {
        if (SelectedItem is null)
        {
            return;
        }

        await apiClient.UpdateInsuranceStatusAsync(SelectedItem.Id, request, CancellationToken.None);
        await RefreshAsync(CancellationToken.None);
    }

    private async Task LoadContactLogsAsync(Guid insuranceId)
    {
        var items = await apiClient.GetContactLogsAsync(null, null, insuranceId, null, CancellationToken.None);
        ContactLogs.Clear();
        foreach (var item in items) ContactLogs.Add(item);
    }
}
