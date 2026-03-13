using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task_Reminder.Shared;
using Task_Reminder.Wpf.Models;

namespace Task_Reminder.Wpf.Services;

public sealed class TaskReminderApiClient(
    HttpClient httpClient,
    IOptions<ClientOptions> options,
    SessionState sessionState,
    ILogger<TaskReminderApiClient> logger) : ITaskReminderApiClient
{
    private readonly ClientOptions _options = options.Value;

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        try
        {
            EnsureConfigured();
            return await httpClient.GetFromJsonAsync<List<UserDto>>("api/users", cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load users from the API.");
            throw;
        }
    }

    public Task<UserNotificationPreferencesDto> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken)
        => GetAsync<UserNotificationPreferencesDto>($"api/users/{userId}/preferences", cancellationToken, $"load notification preferences for user {userId}");

    public Task<UserNotificationPreferencesDto> UpdateUserPreferencesAsync(Guid userId, UpdateUserNotificationPreferencesRequest request, CancellationToken cancellationToken)
        => SendAsync<UserNotificationPreferencesDto>(HttpMethod.Put, $"api/users/{userId}/preferences", request, cancellationToken);

    public async Task<IReadOnlyList<TaskItemDto>> GetTasksAsync(TaskQueryParameters query, CancellationToken cancellationToken)
    {
        try
        {
            EnsureConfigured();
            var uri = BuildTasksUri(query);
            return await httpClient.GetFromJsonAsync<List<TaskItemDto>>(uri, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load tasks from the API.");
            throw;
        }
    }

    public async Task<IReadOnlyList<TaskHistoryDto>> GetTaskHistoryAsync(Guid taskId, CancellationToken cancellationToken)
    {
        try
        {
            EnsureConfigured();
            return await httpClient.GetFromJsonAsync<List<TaskHistoryDto>>($"api/tasks/{taskId}/history", cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load task history for task {TaskId}.", taskId);
            throw;
        }
    }

    public async Task<IReadOnlyList<AppointmentWorkItemDto>> GetAppointmentsAsync(AppointmentQueryParameters query, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Filter)) values.Add($"Filter={Uri.EscapeDataString(query.Filter)}");
        if (query.AssignedUserId.HasValue) values.Add($"AssignedUserId={Uri.EscapeDataString(query.AssignedUserId.Value.ToString())}");
        var uri = values.Count == 0 ? "api/appointments" : $"api/appointments?{string.Join("&", values)}";
        return await httpClient.GetFromJsonAsync<List<AppointmentWorkItemDto>>(uri, cancellationToken) ?? [];
    }

    public Task<AppointmentWorkItemDto> CreateAppointmentAsync(CreateAppointmentWorkItemRequest request, CancellationToken cancellationToken)
        => SendAsync<AppointmentWorkItemDto>(HttpMethod.Post, "api/appointments", request, cancellationToken);

    public Task<AppointmentWorkItemDto> UpdateAppointmentAsync(Guid id, UpdateAppointmentWorkItemRequest request, CancellationToken cancellationToken)
        => SendAsync<AppointmentWorkItemDto>(HttpMethod.Put, $"api/appointments/{id}", request, cancellationToken);

    public Task<AppointmentWorkItemDto> ApplyAppointmentActionAsync(Guid id, string actionName, AppointmentActionRequest request, CancellationToken cancellationToken)
        => SendAsync<AppointmentWorkItemDto>(HttpMethod.Post, $"api/appointments/{id}/{actionName}", request, cancellationToken);

    public Task<TaskItemDto> CreateAppointmentFollowUpTaskAsync(Guid id, AppointmentActionRequest request, CancellationToken cancellationToken)
        => SendAsync<TaskItemDto>(HttpMethod.Post, $"api/appointments/{id}/follow-up-task", request, cancellationToken);

    public async Task<IReadOnlyList<InsuranceWorkItemDto>> GetInsuranceWorkItemsAsync(InsuranceQueryParameters query, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var uri = string.IsNullOrWhiteSpace(query.Filter) ? "api/insurance" : $"api/insurance?Filter={Uri.EscapeDataString(query.Filter)}";
        return await httpClient.GetFromJsonAsync<List<InsuranceWorkItemDto>>(uri, cancellationToken) ?? [];
    }

    public Task<InsuranceWorkItemDto> CreateInsuranceWorkItemAsync(CreateInsuranceWorkItemRequest request, CancellationToken cancellationToken)
        => SendAsync<InsuranceWorkItemDto>(HttpMethod.Post, "api/insurance", request, cancellationToken);

    public Task<InsuranceWorkItemDto> UpdateInsuranceWorkItemAsync(Guid id, UpdateInsuranceWorkItemRequest request, CancellationToken cancellationToken)
        => SendAsync<InsuranceWorkItemDto>(HttpMethod.Put, $"api/insurance/{id}", request, cancellationToken);

    public Task<InsuranceWorkItemDto> UpdateInsuranceStatusAsync(Guid id, InsuranceStatusUpdateRequest request, CancellationToken cancellationToken)
        => SendAsync<InsuranceWorkItemDto>(HttpMethod.Post, $"api/insurance/{id}/status", request, cancellationToken);

    public Task<TaskItemDto> CreateInsuranceFollowUpTaskAsync(Guid id, Guid? userId, bool managerEscalation, CancellationToken cancellationToken)
        => SendAsync<TaskItemDto>(HttpMethod.Post, $"api/insurance/{id}/follow-up-task?managerEscalation={managerEscalation.ToString().ToLowerInvariant()}", new AppointmentActionRequest { UserId = userId }, cancellationToken);

    public async Task<IReadOnlyList<BalanceFollowUpWorkItemDto>> GetBalanceWorkItemsAsync(BalanceQueryParameters query, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var uri = string.IsNullOrWhiteSpace(query.Filter) ? "api/balances" : $"api/balances?Filter={Uri.EscapeDataString(query.Filter)}";
        return await httpClient.GetFromJsonAsync<List<BalanceFollowUpWorkItemDto>>(uri, cancellationToken) ?? [];
    }

    public Task<BalanceFollowUpWorkItemDto> UpdateBalanceStatusAsync(Guid id, BalanceFollowUpStatus status, AppointmentActionRequest request, CancellationToken cancellationToken)
        => SendAsync<BalanceFollowUpWorkItemDto>(HttpMethod.Post, $"api/balances/{id}/status?status={Uri.EscapeDataString(status.ToString())}", request, cancellationToken);

    public Task<TaskItemDto> CreateBalanceFollowUpTaskAsync(Guid id, AppointmentActionRequest request, CancellationToken cancellationToken)
        => SendAsync<TaskItemDto>(HttpMethod.Post, $"api/balances/{id}/follow-up-task", request, cancellationToken);

    public async Task<IReadOnlyList<ContactLogDto>> GetContactLogsAsync(Guid? taskItemId, Guid? appointmentWorkItemId, Guid? insuranceWorkItemId, Guid? balanceFollowUpWorkItemId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var values = new List<string>();
        if (taskItemId.HasValue) values.Add($"taskItemId={taskItemId}");
        if (appointmentWorkItemId.HasValue) values.Add($"appointmentWorkItemId={appointmentWorkItemId}");
        if (insuranceWorkItemId.HasValue) values.Add($"insuranceWorkItemId={insuranceWorkItemId}");
        if (balanceFollowUpWorkItemId.HasValue) values.Add($"balanceFollowUpWorkItemId={balanceFollowUpWorkItemId}");
        var uri = values.Count == 0 ? "api/contact-logs" : $"api/contact-logs?{string.Join("&", values)}";
        return await httpClient.GetFromJsonAsync<List<ContactLogDto>>(uri, cancellationToken) ?? [];
    }

    public Task<ContactLogDto> CreateContactLogAsync(CreateContactLogRequest request, CancellationToken cancellationToken)
        => SendAsync<ContactLogDto>(HttpMethod.Post, "api/contact-logs", request, cancellationToken);

    public Task<OfficeSettingsDto> GetOfficeSettingsAsync(CancellationToken cancellationToken)
        => GetAsync<OfficeSettingsDto>("api/office-settings", cancellationToken, "load office settings");

    public Task<OfficeSettingsDto> UpdateOfficeSettingsAsync(UpdateOfficeSettingsRequest request, CancellationToken cancellationToken)
        => SendAsync<OfficeSettingsDto>(HttpMethod.Put, "api/office-settings", request, cancellationToken);

    public Task<SystemVersionInfoDto> GetSystemVersionAsync(CancellationToken cancellationToken)
        => GetAsync<SystemVersionInfoDto>("api/system/version", cancellationToken, "load system version");

    public Task<SystemStatusSummaryDto> GetSystemSummaryAsync(CancellationToken cancellationToken)
        => GetAsync<SystemStatusSummaryDto>("api/system/summary", cancellationToken, "load system summary");

    public async Task<IReadOnlyList<AuditEntryDto>> GetAuditEntriesAsync(AuditQueryParameters query, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var values = new List<string>();
        if (query.FromUtc.HasValue) values.Add($"FromUtc={Uri.EscapeDataString(query.FromUtc.Value.ToString("O"))}");
        if (query.ToUtc.HasValue) values.Add($"ToUtc={Uri.EscapeDataString(query.ToUtc.Value.ToString("O"))}");
        if (query.UserId.HasValue) values.Add($"UserId={query.UserId.Value}");
        if (!string.IsNullOrWhiteSpace(query.EntityType)) values.Add($"EntityType={Uri.EscapeDataString(query.EntityType)}");
        if (!string.IsNullOrWhiteSpace(query.ActionType)) values.Add($"ActionType={Uri.EscapeDataString(query.ActionType)}");
        var uri = values.Count == 0 ? "api/audit" : $"api/audit?{string.Join("&", values)}";
        return await httpClient.GetFromJsonAsync<List<AuditEntryDto>>(uri, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<ExternalIntegrationProviderStatusDto>> GetIntegrationsAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured();
        return await httpClient.GetFromJsonAsync<List<ExternalIntegrationProviderStatusDto>>("api/integrations", cancellationToken) ?? [];
    }

    public Task<ExternalIntegrationProviderStatusDto> UpdateIntegrationAsync(Guid id, UpdateExternalIntegrationProviderRequest request, CancellationToken cancellationToken)
        => SendAsync<ExternalIntegrationProviderStatusDto>(HttpMethod.Put, $"api/integrations/{id}", request, cancellationToken);

    public Task<ExternalIntegrationProviderStatusDto> RunIntegrationAsync(Guid id, RunExternalIntegrationRequest request, CancellationToken cancellationToken)
        => SendAsync<ExternalIntegrationProviderStatusDto>(HttpMethod.Post, $"api/integrations/{id}/run", request, cancellationToken);

    public Task<ImportResultDto> ImportAppointmentsAsync(ImportAppointmentsRequest request, CancellationToken cancellationToken)
        => SendAsync<ImportResultDto>(HttpMethod.Post, "api/imports/appointments", request, cancellationToken);

    public Task<ImportResultDto> ImportInsuranceAsync(ImportInsuranceWorkItemsRequest request, CancellationToken cancellationToken)
        => SendAsync<ImportResultDto>(HttpMethod.Post, "api/imports/insurance", request, cancellationToken);

    public Task<OperationsBoardDto> GetOperationsBoardAsync(CancellationToken cancellationToken)
        => GetAsync<OperationsBoardDto>("api/operations/board", cancellationToken, "load operations board");

    public Task<OperationsKpiDto> GetOperationsKpisAsync(ManagerMetricsQuery query, CancellationToken cancellationToken)
        => GetAsync<OperationsKpiDto>(BuildOperationsKpiUri(query), cancellationToken, "load operations KPIs");

    public async Task<string> ExportOperationsCsvAsync(string exportType, ManagerMetricsQuery query, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var response = await httpClient.GetAsync(BuildOperationsExportUri(exportType, query), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"API request failed ({(int)response.StatusCode}): {details}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringTaskDefinitionDto>> GetRecurringTasksAsync(CancellationToken cancellationToken)
    {
        try
        {
            EnsureConfigured();
            return await httpClient.GetFromJsonAsync<List<RecurringTaskDefinitionDto>>("api/recurring-tasks", cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load recurring task definitions.");
            throw;
        }
    }

    public Task<RecurringTaskDefinitionDto> CreateRecurringTaskAsync(CreateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken)
        => SendAsync<RecurringTaskDefinitionDto>(HttpMethod.Post, "api/recurring-tasks", request, cancellationToken);

    public Task<RecurringTaskDefinitionDto> UpdateRecurringTaskAsync(Guid id, UpdateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken)
        => SendAsync<RecurringTaskDefinitionDto>(HttpMethod.Put, $"api/recurring-tasks/{id}", request, cancellationToken);

    public Task<RecurringTaskDefinitionDto> SetRecurringTaskActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
        => SendAsync<RecurringTaskDefinitionDto>(HttpMethod.Post, $"api/recurring-tasks/{id}/active", new SetRecurringTaskDefinitionActiveRequest { IsActive = isActive }, cancellationToken);

    public async Task DeleteRecurringTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var response = await httpClient.DeleteAsync($"api/recurring-tasks/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"API request failed ({(int)response.StatusCode}): {details}");
        }
    }

    public Task<ManagerMetricsDto> GetManagerMetricsAsync(ManagerMetricsQuery query, CancellationToken cancellationToken)
        => GetAsync<ManagerMetricsDto>(BuildManagerMetricsUri(query), cancellationToken, "load manager metrics");

    public async Task<string> ExportManagerMetricsCsvAsync(ManagerMetricsQuery query, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var response = await httpClient.GetAsync(BuildManagerExportUri(query), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"API request failed ({(int)response.StatusCode}): {details}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
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

    public Task<TaskHistoryDto> AddTaskCommentAsync(Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken)
        => SendAsync<TaskHistoryDto>(HttpMethod.Post, $"api/tasks/{taskId}/comments", request, cancellationToken);

    private async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken, string operationDescription)
    {
        try
        {
            EnsureConfigured();
            return await httpClient.GetFromJsonAsync<T>(requestUri, cancellationToken)
                ?? throw new InvalidOperationException("API returned an empty response.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to {OperationDescription}.", operationDescription);
            throw;
        }
    }

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
            logger.LogWarning("API request {Method} {RequestUri} failed with status code {StatusCode}.", method, requestUri, (int)response.StatusCode);
            throw CreateApiException(response.StatusCode, details);
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

    private static string BuildManagerMetricsUri(ManagerMetricsQuery query)
    {
        var values = new List<string>();
        if (query.FromUtc.HasValue) values.Add($"FromUtc={Uri.EscapeDataString(query.FromUtc.Value.ToString("O"))}");
        if (query.ToUtc.HasValue) values.Add($"ToUtc={Uri.EscapeDataString(query.ToUtc.Value.ToString("O"))}");
        if (query.PresetDays.HasValue) values.Add($"PresetDays={query.PresetDays.Value}");
        return values.Count == 0 ? "api/manager/metrics" : $"api/manager/metrics?{string.Join("&", values)}";
    }

    private static string BuildManagerExportUri(ManagerMetricsQuery query)
    {
        var values = new List<string>();
        if (query.FromUtc.HasValue) values.Add($"FromUtc={Uri.EscapeDataString(query.FromUtc.Value.ToString("O"))}");
        if (query.ToUtc.HasValue) values.Add($"ToUtc={Uri.EscapeDataString(query.ToUtc.Value.ToString("O"))}");
        if (query.PresetDays.HasValue) values.Add($"PresetDays={query.PresetDays.Value}");
        return values.Count == 0 ? "api/manager/metrics/export" : $"api/manager/metrics/export?{string.Join("&", values)}";
    }

    private static string BuildOperationsKpiUri(ManagerMetricsQuery query)
    {
        var values = new List<string>();
        if (query.FromUtc.HasValue) values.Add($"FromUtc={Uri.EscapeDataString(query.FromUtc.Value.ToString("O"))}");
        if (query.ToUtc.HasValue) values.Add($"ToUtc={Uri.EscapeDataString(query.ToUtc.Value.ToString("O"))}");
        if (query.PresetDays.HasValue) values.Add($"PresetDays={query.PresetDays.Value}");
        return values.Count == 0 ? "api/operations/kpis" : $"api/operations/kpis?{string.Join("&", values)}";
    }

    private static string BuildOperationsExportUri(string exportType, ManagerMetricsQuery query)
    {
        var values = new List<string>();
        if (query.FromUtc.HasValue) values.Add($"FromUtc={Uri.EscapeDataString(query.FromUtc.Value.ToString("O"))}");
        if (query.ToUtc.HasValue) values.Add($"ToUtc={Uri.EscapeDataString(query.ToUtc.Value.ToString("O"))}");
        if (query.PresetDays.HasValue) values.Add($"PresetDays={query.PresetDays.Value}");
        var baseUri = $"api/operations/export/{Uri.EscapeDataString(exportType)}";
        return values.Count == 0 ? baseUri : $"{baseUri}?{string.Join("&", values)}";
    }

    private void EnsureConfigured()
    {
        if (httpClient.BaseAddress is null)
        {
            httpClient.BaseAddress = new Uri(_options.ApiBaseUrl, UriKind.Absolute);
            logger.LogInformation("Configured API base address: {ApiBaseUrl}", httpClient.BaseAddress);
        }

        httpClient.DefaultRequestHeaders.Remove("X-TaskReminder-UserId");
        if (sessionState.CurrentUser is not null)
        {
            httpClient.DefaultRequestHeaders.Add("X-TaskReminder-UserId", sessionState.CurrentUser.Id.ToString());
        }
    }

    private static Exception CreateApiException(System.Net.HttpStatusCode statusCode, string details)
    {
        return statusCode == System.Net.HttpStatusCode.Forbidden
            ? new UnauthorizedAccessException(string.IsNullOrWhiteSpace(details) ? "Permission denied." : details)
            : new InvalidOperationException($"API request failed ({(int)statusCode}): {details}");
    }
}
