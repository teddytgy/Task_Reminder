using Microsoft.AspNetCore.SignalR;

namespace Task_Reminder.Api.Hubs;

public sealed class TaskUpdatesHub : Hub
{
    public const string HubPath = "/hubs/tasks";
}
