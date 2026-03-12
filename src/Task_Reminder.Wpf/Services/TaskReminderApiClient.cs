using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;

namespace Task_Reminder.Wpf.Services;

public sealed class TaskReminderApiClient(
    HttpClient httpClient,
    IOptions<ClientOptions> options) : ITaskReminderApiClient
{
    private readonly ClientOptions _options = options.Value;

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured();
        return await httpClient.GetFromJsonAsync<List<UserDto>>("api/users", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TaskItemDto>> GetTasksAsync(TaskQueryParameters query, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var uri = BuildTasksUri(query);
        return await httpClient.GetFromJsonAsync<List<TaskItemDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<TaskHistoryDto>> GetTaskHistoryAsync(Guid taskId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        return await httpClient.GetFromJsonAsync<List<TaskHistoryDto>>($"api/tasks/{taskId}/history", cancellationToken) ?? [];
    }

    public Task<TaskItemDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken)
        => SendAsync<TaskItemDto>(HttpMethod.Post, "api/tasks", request, cancellationToken);

    public Task<TaskItemDto> AssignTaskAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken)
        => SendAsync<TaskItemDto>(HttpMethod.Post, $"api/tasks/{taskId}/assign", request, cancellationToken);

    public Task<TaskItemDto> ClaimTaskAsync(Guid taskId, ClaimTaskRequest request, CancellationToken cancellationToken)
        => SendAsync<TaskItemDto>(HttpMethod.Post, $"api/tasks/{taskId}/claim", request, cancellationToken);

    public Task<TaskItemDto> SnoozeTaskAsync(Guid taskId, SnoozeTaskRequest request, CancellationToken cancellationToken)
        => SendAsync<TaskItemDto>(HttpMethod.Post, $"api/tasks/{taskId}/snooze", request, cancellationToken);

    public Task<TaskItemDto> CompleteTaskAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken)
        => SendAsync<TaskItemDto>(HttpMethod.Post, $"api/tasks/{taskId}/complete", request, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string requestUri, object payload, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"API request failed ({(int)response.StatusCode}): {details}");
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("API returned an empty response.");
    }

    private string BuildTasksUri(TaskQueryParameters query)
    {
        var values = new List<string>();
        if (query.UserId.HasValue) values.Add($"UserId={Uri.EscapeDataString(query.UserId.Value.ToString())}");
        if (query.Status.HasValue) values.Add($"Status={Uri.EscapeDataString(query.Status.Value.ToString())}");
        if (query.OverdueOnly) values.Add("OverdueOnly=true");
        if (query.UnassignedOnly) values.Add("UnassignedOnly=true");
        if (query.AssignedToMeOnly) values.Add("AssignedToMeOnly=true");
        if (query.CompletedTodayOnly) values.Add("CompletedTodayOnly=true");
        if (query.DueTodayOnly) values.Add("DueTodayOnly=true");
        if (query.DueNowOnly) values.Add("DueNowOnly=true");
        return values.Count == 0 ? "api/tasks" : $"api/tasks?{string.Join("&", values)}";
    }

    private void EnsureConfigured()
    {
        if (httpClient.BaseAddress is null)
        {
            httpClient.BaseAddress = new Uri(_options.ApiBaseUrl, UriKind.Absolute);
        }
    }
}
