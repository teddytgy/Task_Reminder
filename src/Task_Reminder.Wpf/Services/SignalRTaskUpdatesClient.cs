using System.Net.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;

namespace Task_Reminder.Wpf.Services;

public sealed class SignalRTaskUpdatesClient : ISignalRTaskUpdatesClient, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<SignalRTaskUpdatesClient> _logger;
    private readonly string _hubUrl;

    public SignalRTaskUpdatesClient(
        IOptions<ClientOptions> options,
        LocalCertificateValidator certificateValidator,
        ILogger<SignalRTaskUpdatesClient> logger)
    {
        _logger = logger;
        _hubUrl = options.Value.SignalRHubUrl;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, connectionOptions =>
            {
                connectionOptions.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler httpClientHandler)
                    {
                        httpClientHandler.ServerCertificateCustomValidationCallback = certificateValidator.Validate;
                    }

                    return handler;
                };
            })
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
            .Build();

        _hubConnection.On<TaskChangedMessage>("TaskChanged", async message =>
        {
            if (TaskChanged is not null)
            {
                await TaskChanged.Invoke(message);
            }
        });

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "SignalR reconnecting.");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += async connectionId =>
        {
            _logger.LogInformation("SignalR reconnected with connection ID {ConnectionId}.", connectionId);
            if (Reconnected is not null)
            {
                await Reconnected.Invoke();
            }
        };

        _hubConnection.Closed += error =>
        {
            if (error is null)
            {
                _logger.LogInformation("SignalR connection closed.");
            }
            else
            {
                _logger.LogWarning(error, "SignalR connection closed with an error.");
            }

            return Task.CompletedTask;
        };
    }

    public event Func<TaskChangedMessage, Task>? TaskChanged;
    public event Func<Task>? Reconnected;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            _logger.LogInformation("Starting SignalR connection to {HubUrl}.", _hubUrl);
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("SignalR connection started.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            _logger.LogInformation("Stopping SignalR connection.");
            await _hubConnection.StopAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
