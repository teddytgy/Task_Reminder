using Task_Reminder.Shared;

namespace Task_Reminder.Wpf.Services;

public interface ISignalRTaskUpdatesClient
{
    event Func<TaskChangedMessage, Task>? TaskChanged;
    event Func<Task>? Reconnected;
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
