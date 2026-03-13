using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Media;
using System.IO;
using System.Windows;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Notifications;
using Task_Reminder.Wpf.Services;
using Task_Reminder.Wpf.Views;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Wpf.ViewModels;

public partial class MainViewModel(
    ITaskReminderApiClient apiClient,
    ISignalRTaskUpdatesClient signalRClient,
    IToastNotificationService toastNotificationService,
    SessionState sessionState,
    IOptions<ClientOptions> options,
    IServiceProvider serviceProvider,
    ILogger<MainViewModel> logger) : ObservableObject
{
    private readonly Dictionary<Guid, DateTime> _lastNotificationSentUtc = new();
    private UserNotificationPreferencesDto _notificationPreferences = new();
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

    [ObservableProperty]
    private string _clientVersionDisplay = GetClientVersion();

    [ObservableProperty]
    private string _apiVersionDisplay = "Checking API version...";

    [ObservableProperty]
    private string _versionCompatibilityMessage = string.Empty;

    public string CurrentUserDisplayName => sessionState.CurrentUser?.DisplayName ?? "Unknown User";
    public string CurrentUserRoleDisplayName => sessionState.CurrentUser?.Role.ToString() ?? UserRole.FrontDesk.ToString();
    public bool IsManagerOrAdmin => sessionState.CurrentUser?.Role is UserRole.Manager or UserRole.Admin;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        SelectedFilter = Filters.FirstOrDefault();
        signalRClient.TaskChanged += OnTaskChangedAsync;
        signalRClient.Reconnected += OnSignalRReconnectedAsync;

        try
        {
            await signalRClient.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SignalR could not connect during startup. The client will continue with polling.");
            StatusMessage = "Live updates are temporarily unavailable. The dashboard will keep refreshing.";
        }

        await LoadNotificationPreferencesAsync(cancellationToken);
        await LoadVersionInfoAsync(cancellationToken);
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
            logger.LogError(ex, "Failed to refresh tasks.");
            StatusMessage = "Unable to refresh tasks right now. Check API connectivity and try again.";
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

    [RelayCommand(CanExecute = nameof(CanEditSelectedTask))]
    private async Task AddCommentAsync()
    {
        if (SelectedTask is null || sessionState.CurrentUser is null)
        {
            return;
        }

        var dialog = serviceProvider.GetRequiredService<TaskCommentWindow>();
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.ViewModel.Comment))
        {
            return;
        }

        await apiClient.AddTaskCommentAsync(SelectedTask.Id, new AddTaskCommentRequest
        {
            UserId = sessionState.CurrentUser.Id,
            Comment = dialog.ViewModel.Comment.Trim()
        }, CancellationToken.None);

        await LoadHistoryAsync(SelectedTask.Id);
        StatusMessage = "Comment added.";
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedTask))]
    private async Task AddContactLogAsync()
    {
        if (SelectedTask is null || sessionState.CurrentUser is null)
        {
            return;
        }

        var dialog = serviceProvider.GetRequiredService<ContactLogWindow>();
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await apiClient.CreateContactLogAsync(new CreateContactLogRequest
        {
            TaskItemId = SelectedTask.Id,
            ContactType = dialog.ViewModel.ContactType,
            Outcome = dialog.ViewModel.Outcome,
            Notes = dialog.ViewModel.Notes,
            PerformedByUserId = sessionState.CurrentUser.Id
        }, CancellationToken.None);

        StatusMessage = "Contact log added.";
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

    [RelayCommand]
    private async Task NotificationSettingsAsync()
    {
        var window = serviceProvider.GetRequiredService<NotificationSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            await LoadNotificationPreferencesAsync(CancellationToken.None);
            StatusMessage = "Notification preferences updated.";
        }
    }

    [RelayCommand]
    private void OpenTrainingManual()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "docs", "USER_TRAINING_MANUAL.html"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "USER_TRAINING_MANUAL.html")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "USER_TRAINING_MANUAL.md"))
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
        {
            StatusMessage = "Training manual was not found on this computer.";
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    [RelayCommand(CanExecute = nameof(CanManageOfficeFeatures))]
    private async Task RecurringTasksAsync()
    {
        var window = serviceProvider.GetRequiredService<RecurringTasksWindow>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand(CanExecute = nameof(CanManageOfficeFeatures))]
    private Task ManagerDashboardAsync()
    {
        var window = serviceProvider.GetRequiredService<ManagerDashboardWindow>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task AppointmentsAsync()
    {
        var window = serviceProvider.GetRequiredService<AppointmentBoardWindow>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task InsuranceQueueAsync()
    {
        var window = serviceProvider.GetRequiredService<InsuranceQueueWindow>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OperationsBoardsAsync()
    {
        var window = serviceProvider.GetRequiredService<OperationsBoardsWindow>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanManageOfficeFeatures))]
    private Task OfficeSettingsAsync()
    {
        var window = serviceProvider.GetRequiredService<OfficeSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanManageOfficeFeatures))]
    private Task ImportDataAsync()
    {
        var window = serviceProvider.GetRequiredService<ImportDataWindow>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanManageOfficeFeatures))]
    private async Task AdminOperationsAsync()
    {
        var window = serviceProvider.GetRequiredService<AdminOperationsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            await LoadVersionInfoAsync(CancellationToken.None);
        }
    }

    public async Task ShutdownAsync()
    {
        signalRClient.TaskChanged -= OnTaskChangedAsync;
        signalRClient.Reconnected -= OnSignalRReconnectedAsync;
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

    private bool CanManageOfficeFeatures()
    {
        return IsManagerOrAdmin;
    }

    private async Task LoadVersionInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            var info = await apiClient.GetSystemVersionAsync(cancellationToken);
            ApiVersionDisplay = $"API {info.ApiVersion}";

            if (TryParseVersion(info.MinimumSupportedDesktopVersion, out var minimumVersion) &&
                TryParseVersion(ClientVersionDisplay, out var currentVersion) &&
                currentVersion < minimumVersion)
            {
                VersionCompatibilityMessage = $"This desktop build is older than the minimum supported version ({info.MinimumSupportedDesktopVersion}). Please update this workstation.";
                MessageBox.Show(
                    VersionCompatibilityMessage,
                    "Update Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                VersionCompatibilityMessage = $"Desktop {ClientVersionDisplay} | Recommended {info.RecommendedDesktopVersion}";
            }
        }
        catch (UnauthorizedAccessException)
        {
            ApiVersionDisplay = "API version unavailable";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to load API version information.");
            ApiVersionDisplay = "API version unavailable";
        }
    }

    private static string GetClientVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    private static bool TryParseVersion(string versionText, out Version version)
    {
        var parsed = Version.TryParse(versionText, out var tempVersion);
        version = tempVersion ?? new Version(0, 0, 0);
        return parsed;
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
        try
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load history for task {TaskId}.", taskId);
            StatusMessage = "Task history could not be loaded right now.";
        }
    }

    private async Task OnTaskChangedAsync(TaskChangedMessage message)
    {
        MaybeShowSignalRNotification(message.EventType, message.Task);
        await RefreshAsync(CancellationToken.None);
    }

    private async Task OnSignalRReconnectedAsync()
    {
        logger.LogInformation("Refreshing tasks after SignalR reconnection.");
        StatusMessage = "Live updates reconnected. Refreshing tasks...";
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Reminder loop failed unexpectedly.");
        }
    }

    private void NotifyDueTasks(IEnumerable<TaskItemDto> tasks)
    {
        if (sessionState.CurrentUser is null)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        foreach (var task in tasks.Where(x => x.IsDueNow || x.IsOverdue))
        {
            if (!NotificationRoutingRules.ShouldNotify(
                    sessionState.CurrentUser.Id,
                    sessionState.CurrentUser.Role,
                    _notificationPreferences,
                    task,
                    isRecurringGenerationAlert: false))
            {
                continue;
            }

            var repeatMinutes = Math.Max(5, task.ReminderRepeatMinutes > 0 ? task.ReminderRepeatMinutes : options.Value.DefaultRepeatMinutes);
            if (_lastNotificationSentUtc.TryGetValue(task.Id, out var lastSent) && lastSent > nowUtc.AddMinutes(-repeatMinutes))
            {
                continue;
            }

            var body = task.IsOverdue
                ? $"{task.Title} is overdue. Assigned: {task.AssignedUserDisplayName ?? "Unassigned"}."
                : $"{task.Title} is due now. Assigned: {task.AssignedUserDisplayName ?? "Unassigned"}.";

            toastNotificationService.ShowTaskReminder("Dental Front Desk Reminder", body);
            if (_notificationPreferences.EnableSoundForUrgentReminders && task.IsOverdue)
            {
                SystemSounds.Exclamation.Play();
            }

            _lastNotificationSentUtc[task.Id] = nowUtc;
        }
    }

    private async Task LoadNotificationPreferencesAsync(CancellationToken cancellationToken)
    {
        if (sessionState.CurrentUser is null)
        {
            return;
        }

        try
        {
            _notificationPreferences = await apiClient.GetUserPreferencesAsync(sessionState.CurrentUser.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falling back to default notification preferences for user {UserId}.", sessionState.CurrentUser.Id);
            _notificationPreferences = new UserNotificationPreferencesDto { UserId = sessionState.CurrentUser.Id };
        }
    }

    private void MaybeShowSignalRNotification(string eventType, TaskItemDto task)
    {
        if (sessionState.CurrentUser is null)
        {
            return;
        }

        if (eventType != "recurring-generated" &&
            eventType != "escalated")
        {
            return;
        }

        var shouldNotify = NotificationRoutingRules.ShouldNotify(
            sessionState.CurrentUser.Id,
            sessionState.CurrentUser.Role,
            _notificationPreferences,
            task,
            isRecurringGenerationAlert: eventType == "recurring-generated");

        if (!shouldNotify)
        {
            return;
        }

        var title = eventType == "recurring-generated" ? "New Recurring Task" : "Task Escalated";
        var body = eventType == "recurring-generated"
            ? $"{task.Title} was generated for today's office workflow."
            : $"{task.Title} needs manager attention.";

        toastNotificationService.ShowTaskReminder(title, body);
        if (_notificationPreferences.EnableSoundForUrgentReminders && eventType == "escalated")
        {
            SystemSounds.Exclamation.Play();
        }
    }
}
