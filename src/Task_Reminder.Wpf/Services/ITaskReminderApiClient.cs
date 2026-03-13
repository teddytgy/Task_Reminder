using Task_Reminder.Shared;

namespace Task_Reminder.Wpf.Services;

public interface ITaskReminderApiClient
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<UserNotificationPreferencesDto> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserNotificationPreferencesDto> UpdateUserPreferencesAsync(Guid userId, UpdateUserNotificationPreferencesRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskItemDto>> GetTasksAsync(TaskQueryParameters query, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskHistoryDto>> GetTaskHistoryAsync(Guid taskId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AppointmentWorkItemDto>> GetAppointmentsAsync(AppointmentQueryParameters query, CancellationToken cancellationToken);
    Task<AppointmentWorkItemDto> CreateAppointmentAsync(CreateAppointmentWorkItemRequest request, CancellationToken cancellationToken);
    Task<AppointmentWorkItemDto> UpdateAppointmentAsync(Guid id, UpdateAppointmentWorkItemRequest request, CancellationToken cancellationToken);
    Task<AppointmentWorkItemDto> ApplyAppointmentActionAsync(Guid id, string actionName, AppointmentActionRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> CreateAppointmentFollowUpTaskAsync(Guid id, AppointmentActionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InsuranceWorkItemDto>> GetInsuranceWorkItemsAsync(InsuranceQueryParameters query, CancellationToken cancellationToken);
    Task<InsuranceWorkItemDto> CreateInsuranceWorkItemAsync(CreateInsuranceWorkItemRequest request, CancellationToken cancellationToken);
    Task<InsuranceWorkItemDto> UpdateInsuranceWorkItemAsync(Guid id, UpdateInsuranceWorkItemRequest request, CancellationToken cancellationToken);
    Task<InsuranceWorkItemDto> UpdateInsuranceStatusAsync(Guid id, InsuranceStatusUpdateRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> CreateInsuranceFollowUpTaskAsync(Guid id, Guid? userId, bool managerEscalation, CancellationToken cancellationToken);
    Task<IReadOnlyList<BalanceFollowUpWorkItemDto>> GetBalanceWorkItemsAsync(BalanceQueryParameters query, CancellationToken cancellationToken);
    Task<BalanceFollowUpWorkItemDto> UpdateBalanceStatusAsync(Guid id, BalanceFollowUpStatus status, AppointmentActionRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> CreateBalanceFollowUpTaskAsync(Guid id, AppointmentActionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContactLogDto>> GetContactLogsAsync(Guid? taskItemId, Guid? appointmentWorkItemId, Guid? insuranceWorkItemId, Guid? balanceFollowUpWorkItemId, CancellationToken cancellationToken);
    Task<ContactLogDto> CreateContactLogAsync(CreateContactLogRequest request, CancellationToken cancellationToken);
    Task<OfficeSettingsDto> GetOfficeSettingsAsync(CancellationToken cancellationToken);
    Task<OfficeSettingsDto> UpdateOfficeSettingsAsync(UpdateOfficeSettingsRequest request, CancellationToken cancellationToken);
    Task<SystemVersionInfoDto> GetSystemVersionAsync(CancellationToken cancellationToken);
    Task<SystemStatusSummaryDto> GetSystemSummaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntryDto>> GetAuditEntriesAsync(AuditQueryParameters query, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExternalIntegrationProviderStatusDto>> GetIntegrationsAsync(CancellationToken cancellationToken);
    Task<ExternalIntegrationProviderStatusDto> UpdateIntegrationAsync(Guid id, UpdateExternalIntegrationProviderRequest request, CancellationToken cancellationToken);
    Task<ExternalIntegrationProviderStatusDto> RunIntegrationAsync(Guid id, RunExternalIntegrationRequest request, CancellationToken cancellationToken);
    Task<ImportResultDto> ImportAppointmentsAsync(ImportAppointmentsRequest request, CancellationToken cancellationToken);
    Task<ImportResultDto> ImportInsuranceAsync(ImportInsuranceWorkItemsRequest request, CancellationToken cancellationToken);
    Task<OperationsBoardDto> GetOperationsBoardAsync(CancellationToken cancellationToken);
    Task<OperationsKpiDto> GetOperationsKpisAsync(ManagerMetricsQuery query, CancellationToken cancellationToken);
    Task<string> ExportOperationsCsvAsync(string exportType, ManagerMetricsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecurringTaskDefinitionDto>> GetRecurringTasksAsync(CancellationToken cancellationToken);
    Task<RecurringTaskDefinitionDto> CreateRecurringTaskAsync(CreateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken);
    Task<RecurringTaskDefinitionDto> UpdateRecurringTaskAsync(Guid id, UpdateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken);
    Task<RecurringTaskDefinitionDto> SetRecurringTaskActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken);
    Task DeleteRecurringTaskAsync(Guid id, CancellationToken cancellationToken);
    Task<ManagerMetricsDto> GetManagerMetricsAsync(ManagerMetricsQuery query, CancellationToken cancellationToken);
    Task<string> ExportManagerMetricsCsvAsync(ManagerMetricsQuery query, CancellationToken cancellationToken);
    Task<TaskItemDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> AssignTaskAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> ClaimTaskAsync(Guid taskId, ClaimTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> SnoozeTaskAsync(Guid taskId, SnoozeTaskRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto> CompleteTaskAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken);
    Task<TaskHistoryDto> AddTaskCommentAsync(Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken);
}
