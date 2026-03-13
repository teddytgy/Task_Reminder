using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Services;

namespace Task_Reminder.Wpf.ViewModels;

public partial class RecurringTasksViewModel(
    ITaskReminderApiClient apiClient,
    SessionState sessionState,
    ILogger<RecurringTasksViewModel> logger) : ObservableObject
{
    [ObservableProperty] private RecurringTaskDefinitionDto? _selectedDefinition;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private TaskCategory _category = TaskCategory.General;
    [ObservableProperty] private TaskPriority _priority = TaskPriority.Medium;
    [ObservableProperty] private UserDto? _selectedAssignedUser;
    [ObservableProperty] private UserDto? _selectedEscalateToUser;
    [ObservableProperty] private string? _patientReference;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int _reminderRepeatMinutes = 30;
    [ObservableProperty] private int? _escalateAfterMinutes;
    [ObservableProperty] private RecurrenceType _recurrenceType = RecurrenceType.Weekdays;
    [ObservableProperty] private int _recurrenceInterval = 1;
    [ObservableProperty] private string? _daysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday";
    [ObservableProperty] private int? _dayOfMonth;
    [ObservableProperty] private string _timeOfDayLocalText = "15:00";
    [ObservableProperty] private string _startDateLocalText = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string? _endDateLocalText;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _statusMessage = "Loading recurring tasks...";

    public ObservableCollection<RecurringTaskDefinitionDto> Definitions { get; } = [];
    public ObservableCollection<UserDto> Users { get; } = [];
    public IReadOnlyList<TaskCategory> Categories { get; } = Enum.GetValues<TaskCategory>();
    public IReadOnlyList<TaskPriority> Priorities { get; } = Enum.GetValues<TaskPriority>();
    public IReadOnlyList<RecurrenceType> RecurrenceTypes { get; } = Enum.GetValues<RecurrenceType>();

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var users = await apiClient.GetUsersAsync(cancellationToken);
            var definitions = await apiClient.GetRecurringTasksAsync(cancellationToken);
            Users.Clear();
            Definitions.Clear();

            foreach (var user in users)
            {
                Users.Add(user);
            }

            foreach (var definition in definitions)
            {
                Definitions.Add(definition);
            }

            SelectedDefinition = Definitions.FirstOrDefault();
            if (SelectedDefinition is null)
            {
                NewDefinition();
            }

            StatusMessage = "Manage recurring office templates.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize recurring task manager.");
            StatusMessage = "Recurring task definitions could not be loaded.";
        }
    }

    partial void OnSelectedDefinitionChanged(RecurringTaskDefinitionDto? value)
    {
        if (value is null)
        {
            return;
        }

        Title = value.Title;
        Description = value.Description;
        Category = value.Category;
        Priority = value.Priority;
        SelectedAssignedUser = Users.FirstOrDefault(x => x.Id == value.AssignedUserId);
        SelectedEscalateToUser = Users.FirstOrDefault(x => x.Id == value.EscalateToUserId);
        PatientReference = value.PatientReference;
        Notes = value.Notes;
        ReminderRepeatMinutes = value.ReminderRepeatMinutes;
        EscalateAfterMinutes = value.EscalateAfterMinutes;
        RecurrenceType = value.RecurrenceType;
        RecurrenceInterval = value.RecurrenceInterval;
        DaysOfWeek = value.DaysOfWeek;
        DayOfMonth = value.DayOfMonth;
        TimeOfDayLocalText = value.TimeOfDayLocal?.ToString(@"hh\:mm") ?? "15:00";
        StartDateLocalText = value.StartDateLocal.ToString("yyyy-MM-dd");
        EndDateLocalText = value.EndDateLocal?.ToString("yyyy-MM-dd");
        IsActive = value.IsActive;
    }

    [RelayCommand]
    private void NewDefinition()
    {
        SelectedDefinition = null;
        Title = string.Empty;
        Description = null;
        Category = TaskCategory.General;
        Priority = TaskPriority.Medium;
        SelectedAssignedUser = null;
        SelectedEscalateToUser = null;
        PatientReference = null;
        Notes = null;
        ReminderRepeatMinutes = 30;
        EscalateAfterMinutes = null;
        RecurrenceType = RecurrenceType.Weekdays;
        RecurrenceInterval = 1;
        DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday";
        DayOfMonth = null;
        TimeOfDayLocalText = "15:00";
        StartDateLocalText = DateTime.Today.ToString("yyyy-MM-dd");
        EndDateLocalText = null;
        IsActive = true;
        StatusMessage = "Creating a new recurring task definition.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (SelectedDefinition is null)
            {
                await apiClient.CreateRecurringTaskAsync(BuildRequest(), CancellationToken.None);
            }
            else
            {
                await apiClient.UpdateRecurringTaskAsync(SelectedDefinition.Id, new UpdateRecurringTaskDefinitionRequest
                {
                    Title = Title,
                    Description = Description,
                    Category = Category,
                    Priority = Priority,
                    AssignedUserId = SelectedAssignedUser?.Id,
                    CreatedByUserId = sessionState.CurrentUser?.Id,
                    PatientReference = PatientReference,
                    Notes = Notes,
                    ReminderRepeatMinutes = ReminderRepeatMinutes,
                    EscalateAfterMinutes = EscalateAfterMinutes,
                    EscalateToUserId = SelectedEscalateToUser?.Id,
                    RecurrenceType = RecurrenceType,
                    RecurrenceInterval = RecurrenceInterval,
                    DaysOfWeek = DaysOfWeek,
                    DayOfMonth = DayOfMonth,
                    TimeOfDayLocal = ParseTime(),
                    StartDateLocal = ParseStartDate(),
                    EndDateLocal = ParseEndDate(),
                    IsActive = IsActive
                }, CancellationToken.None);
            }

            await InitializeAsync(CancellationToken.None);
            StatusMessage = "Recurring task definition saved.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save recurring task definition.");
            StatusMessage = "Recurring task definition could not be saved.";
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedDefinition is null)
        {
            return;
        }

        await apiClient.SetRecurringTaskActiveAsync(SelectedDefinition.Id, !SelectedDefinition.IsActive, CancellationToken.None);
        await InitializeAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedDefinition is null)
        {
            return;
        }

        await apiClient.DeleteRecurringTaskAsync(SelectedDefinition.Id, CancellationToken.None);
        await InitializeAsync(CancellationToken.None);
    }

    private CreateRecurringTaskDefinitionRequest BuildRequest() => new()
    {
        Title = Title,
        Description = Description,
        Category = Category,
        Priority = Priority,
        AssignedUserId = SelectedAssignedUser?.Id,
        CreatedByUserId = sessionState.CurrentUser?.Id,
        PatientReference = PatientReference,
        Notes = Notes,
        ReminderRepeatMinutes = ReminderRepeatMinutes,
        EscalateAfterMinutes = EscalateAfterMinutes,
        EscalateToUserId = SelectedEscalateToUser?.Id,
        RecurrenceType = RecurrenceType,
        RecurrenceInterval = RecurrenceInterval,
        DaysOfWeek = DaysOfWeek,
        DayOfMonth = DayOfMonth,
        TimeOfDayLocal = ParseTime(),
        StartDateLocal = ParseStartDate(),
        EndDateLocal = ParseEndDate(),
        IsActive = IsActive
    };

    private TimeSpan? ParseTime() => TimeSpan.TryParse(TimeOfDayLocalText, out var value) ? value : null;
    private DateOnly ParseStartDate() => DateOnly.TryParse(StartDateLocalText, out var value) ? value : DateOnly.FromDateTime(DateTime.Today);
    private DateOnly? ParseEndDate() => DateOnly.TryParse(EndDateLocalText, out var value) ? value : null;
}
