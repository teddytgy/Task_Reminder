using Microsoft.AspNetCore.SignalR;
using Task_Reminder.Api.Hubs;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class TaskBroadcastService(
    IHubContext<TaskUpdatesHub> hubContext,
    ILogger<TaskBroadcastService> logger)
{
    public Task BroadcastTaskChangedAsync(string eventType, TaskItemDto task, CancellationToken cancellationToken)
    {
        var payload = new TaskChangedMessage
        {
            EventType = eventType,
            Task = task
        };

        return BroadcastAsync(payload, cancellationToken);
    }

    private async Task BroadcastAsync(TaskChangedMessage payload, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Broadcasting task change {EventType} for task {TaskId}.", payload.EventType, payload.Task.Id);
            await hubContext.Clients.All.SendAsync("TaskChanged", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast task change {EventType} for task {TaskId}.", payload.EventType, payload.Task.Id);
            throw;
        }
    }
}
