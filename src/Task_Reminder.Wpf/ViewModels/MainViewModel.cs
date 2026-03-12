using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Notifications;
using Task_Reminder.Wpf.Services;
using Task_Reminder.Wpf.Views;

namespace Task_Reminder.Wpf.ViewModels;

public partial class MainViewModel(
    ITaskReminderApiClient apiClient,
    ISignalRTaskUpdatesClient signalRClient,
    IToastNotificationService toastNotificationService,
    SessionState sessionState,
    IOptions<ClientOptions> options,
    IServiceProvider serviceProvider) : ObservableObject
{
    private readonly Dictionary<Guid, DateTime> _lastNotificationSentUtc = new();
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _refreshCts;

    public ObservableCollection<TaskItemDto> Tasks { get; } = [];
    public ObservableCollection<TaskHistoryDto> TaskHistory { get; } = [];
    public ObservableCollection<TaskFilterOption> Filters { get; } =
    [
        new() { Key = "due-now", DisplayName = "Due Now" },
        new() { Key = "due-today", DisplayName = "Due Today" },
        new() { Key = "overdue", DisplayName = "Overdue" },
        new() { Key = "assigned-to-me", DisplayName = "Assigned To Me" },
        new() { Key = "unassigned", DisplayName = "Unassigned" },
        new() { Key = "completed-today", DisplayName = "Completed Today" },
        new() { Key = "all", DisplayName = "All Open Tasks" }
    ];

    [NotifyCanExecuteChangedFor(nameof(AssignCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClaimCommand))]
    [NotifyCanExecuteChangedFor(nameof(SnoozeCommand))]
    [NotifyCanExecuteChangedFor(nameof(CompleteCommand))]
    [ObservableProperty]
    private TaskItemDto? _selectedTask;

    [ObservableProperty]
    private TaskFilterOption? _selectedFilter;

    [ObservableProperty]
    private string _statusMessage = "Connecting...";

    public string CurrentUserDisplayName => sessionState.CurrentUser?.DisplayName ?? "Unknown User";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        SelectedFilter = Filters.FirstOrDefault();
        signalRClient.TaskChanged += OnTaskChangedAsync;
        await signalRClient.StartAsync(cancellationToken);
        await RefreshAsync(cancellationToken);
        _ = StartReminderLoopAsync();
    }

    partial void OnSelectedTaskChanged(TaskItemDto? value)
    {
        if (value is null)
        {
            TaskHistory.Clear();
            return;
        }

        _ = LoadHistoryAsync(value.Id);
    }

    partial void OnSelectedFilterChanged(TaskFilterOption? value)
    {
        _ = RefreshAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            _refreshCts?.Cancel();
            _refreshCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var currentToken = _refreshCts.Token;

            var tasks = await apiClient.GetTasksAsync(BuildQuery(), currentToken);

            App.Current.Dispatcher.Invoke(() =>
            {
                Tasks.Clear();
                foreach (var task in tasks)
                {
                    Tasks.Add(task);
                }

                SelectedTask = Tasks.FirstOrDefault(x => x.Id == SelectedTask?.Id) ?? Tasks.FirstOrDefault();
            });

            StatusMessage = $"Loaded {tasks.Count} tasks at {DateTime.Now:t}.";
            NotifyDueTasks(tasks);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedTask))]
    private async Task ClaimAsync()
    {
        if (SelectedTask is null || sessionState.CurrentUser is null)
        {
            return;
        }

        await apiClient.ClaimTaskAsync(SelectedTask.Id, new ClaimTaskRequest { UserId = sessionState.CurrentUser.Id }, CancellationToken.None);
        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedTask))]
    private async Task CompleteAsync()
    {
        if (SelectedTask is null || sessionState.CurrentUser is null)
        {
            return;
        }

        await apiClient.CompleteTaskAsync(SelectedTask.Id, new CompleteTaskRequest { UserId = sessionState.CurrentUser.Id }, CancellationToken.None);
        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task NewTaskAsync()
    {
        var window = serviceProvider.GetRequiredService<TaskEditorWindow>();
        if (window.ShowDialog() != true)
        {
            return;
        }

        await apiClient.CreateTaskAsync(window.ViewModel.ToRequest(sessionState.CurrentUser?.Id), CancellationToken.None);
        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedTask))]
    private async Task AssignAsync()
    {
        var dialog = serviceProvider.GetRequiredService<AssignTaskWindow>();
        dialog.ViewModel.Users = await apiClient.GetUsersAsync(CancellationToken.None);
        dialog.ViewModel.SelectedUser = dialog.ViewModel.Users.FirstOrDefault();

        if (dialog.ShowDialog() != true || SelectedTask is null || dialog.ViewModel.SelectedUser is null)
        {
            return;
        }

        await apiClient.AssignTaskAsync(SelectedTask.Id, new AssignTaskRequest
        {
            AssignedUserId = dialog.ViewModel.SelectedUser.Id,
            PerformedByUserId = sessionState.CurrentUser?.Id
        }, CancellationToken.None);

        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedTask))]
    private async Task SnoozeAsync()
    {
        var dialog = serviceProvider.GetRequiredService<SnoozeTaskWindow>();
        if (dialog.ShowDialog() != true || SelectedTask is null || sessionState.CurrentUser is null)
        {
            return;
        }

        await apiClient.SnoozeTaskAsync(SelectedTask.Id, new SnoozeTaskRequest
        {
            UserId = sessionState.CurrentUser.Id,
            SnoozeUntilUtc = DateTime.Parse(dialog.ViewModel.SnoozeUntilLocalText).ToUniversalTime()
        }, CancellationToken.None);

        await RefreshAsync(CancellationToken.None);
    }

    public async Task ShutdownAsync()
    {
        signalRClient.TaskChanged -= OnTaskChangedAsync;
        _refreshCts?.Cancel();
        if (_timer is not null)
        {
            _timer.Dispose();
        }

        await signalRClient.StopAsync(CancellationToken.None);
    }

    private bool CanEditSelectedTask()
    {
        return SelectedTask is not null && SelectedTask.Status is not TaskStatus.Completed and not TaskStatus.Cancelled;
    }

    private TaskQueryParameters BuildQuery()
    {
        var query = new TaskQueryParameters();
        var userId = sessionState.CurrentUser?.Id;

        switch (SelectedFilter?.Key)
        {
            case "due-now":
                query.DueNowOnly = true;
                break;
            case "due-today":
                query.DueTodayOnly = true;
                break;
            case "overdue":
                query.OverdueOnly = true;
                break;
            case "assigned-to-me":
                query.AssignedToMeOnly = true;
                query.UserId = userId;
                break;
            case "unassigned":
                query.UnassignedOnly = true;
                break;
            case "completed-today":
                query.CompletedTodayOnly = true;
                break;
        }

        return query;
    }

    private async Task LoadHistoryAsync(Guid taskId)
    {
        var history = await apiClient.GetTaskHistoryAsync(taskId, CancellationToken.None);
        App.Current.Dispatcher.Invoke(() =>
        {
            TaskHistory.Clear();
            foreach (var item in history)
            {
                TaskHistory.Add(item);
            }
        });
    }

    private async Task OnTaskChangedAsync(TaskChangedMessage _)
    {
        await RefreshAsync(CancellationToken.None);
    }

    private async Task StartReminderLoopAsync()
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(15, options.Value.ReminderPollingSeconds)));
        try
        {
            while (await _timer.WaitForNextTickAsync())
            {
                await RefreshAsync(CancellationToken.None);
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void NotifyDueTasks(IEnumerable<TaskItemDto> tasks)
    {
        var nowUtc = DateTime.UtcNow;
        foreach (var task in tasks.Where(x => x.IsDueNow || x.IsOverdue))
        {
            var repeatMinutes = Math.Max(5, task.ReminderRepeatMinutes > 0 ? task.ReminderRepeatMinutes : options.Value.DefaultRepeatMinutes);
            if (_lastNotificationSentUtc.TryGetValue(task.Id, out var lastSent) && lastSent > nowUtc.AddMinutes(-repeatMinutes))
            {
                continue;
            }

            var body = task.IsOverdue
                ? $"{task.Title} is overdue. Assigned: {task.AssignedUserDisplayName ?? "Unassigned"}."
                : $"{task.Title} is due now. Assigned: {task.AssignedUserDisplayName ?? "Unassigned"}.";

            toastNotificationService.ShowTaskReminder("Dental Front Desk Reminder", body);
            _lastNotificationSentUtc[task.Id] = nowUtc;
        }
    }
}
