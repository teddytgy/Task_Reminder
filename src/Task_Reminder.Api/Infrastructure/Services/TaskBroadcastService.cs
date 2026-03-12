using Microsoft.AspNetCore.SignalR;
using Task_Reminder.Api.Hubs;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class TaskBroadcastService(IHubContext<TaskUpdatesHub> hubContext)
{
    public Task BroadcastTaskChangedAsync(string eventType, TaskItemDto task, CancellationToken cancellationToken)
    {
        var payload = new TaskChangedMessage
        {
            EventType = eventType,
            Task = task
        };

        return hubContext.Clients.All.SendAsync("TaskChanged", payload, cancellationToken);
    }
}
