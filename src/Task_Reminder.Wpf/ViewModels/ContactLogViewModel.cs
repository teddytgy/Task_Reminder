using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Task_Reminder.Shared;

namespace Task_Reminder.Wpf.ViewModels;

public partial class ContactLogViewModel : ObservableObject
{
    [ObservableProperty] private ContactType _contactType = ContactType.Call;
    [ObservableProperty] private ContactOutcome _outcome = ContactOutcome.Reached;
    [ObservableProperty] private string? _notes;

    public IReadOnlyList<ContactType> ContactTypes { get; } = Enum.GetValues<ContactType>();
    public IReadOnlyList<ContactOutcome> ContactOutcomes { get; } = Enum.GetValues<ContactOutcome>();

    [RelayCommand]
    private void Save(System.Windows.Window window)
    {
        window.DialogResult = true;
        window.Close();
    }
}
