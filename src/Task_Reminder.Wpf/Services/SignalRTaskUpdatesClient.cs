using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;

namespace Task_Reminder.Wpf.Services;

public sealed class SignalRTaskUpdatesClient : ISignalRTaskUpdatesClient, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;

    public SignalRTaskUpdatesClient(IOptions<ClientOptions> options)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(options.Value.SignalRHubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<TaskChangedMessage>("TaskChanged", async message =>
        {
            if (TaskChanged is not null)
            {
                await TaskChanged.Invoke(message);
            }
        });
    }

    public event Func<TaskChangedMessage, Task>? TaskChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
