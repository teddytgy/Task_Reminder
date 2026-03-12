using CommunityToolkit.Mvvm.ComponentModel;
using Task_Reminder.Shared;

namespace Task_Reminder.Wpf.ViewModels;

public partial class TaskEditorViewModel : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private TaskCategory _category = TaskCategory.General;
    [ObservableProperty] private TaskPriority _priority = TaskPriority.Medium;
    [ObservableProperty] private string _dueAtLocalText = DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm");
    [ObservableProperty] private string? _patientReference;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int _reminderRepeatMinutes = 30;

    public IReadOnlyList<TaskCategory> Categories { get; } = Enum.GetValues<TaskCategory>();
    public IReadOnlyList<TaskPriority> Priorities { get; } = Enum.GetValues<TaskPriority>();

    public CreateTaskRequest ToRequest(Guid? createdByUserId)
    {
        DateTime? dueAtUtc = null;
        if (!string.IsNullOrWhiteSpace(DueAtLocalText) && DateTime.TryParse(DueAtLocalText, out var parsed))
        {
            dueAtUtc = parsed.ToUniversalTime();
        }

        return new CreateTaskRequest
        {
            Title = Title,
            Description = Description,
            Category = Category,
            Priority = Priority,
            DueAtUtc = dueAtUtc,
            PatientReference = PatientReference,
            Notes = Notes,
            CreatedByUserId = createdByUserId,
            ReminderRepeatMinutes = ReminderRepeatMinutes
        };
    }
}
