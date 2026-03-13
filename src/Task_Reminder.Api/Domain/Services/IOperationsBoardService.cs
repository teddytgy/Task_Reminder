using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IOperationsBoardService
{
    Task<OperationsBoardDto> GetBoardAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<UserWorkloadDto>> GetWorkloadAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<UserActivityTimelineItemDto>> GetUserActivityAsync(Guid userId, CancellationToken cancellationToken);
    Task<OperationsKpiDto> GetKpisAsync(ManagerMetricsQuery query, CancellationToken cancellationToken);
    Task<string> ExportOperationsCsvAsync(string exportType, ManagerMetricsQuery query, CancellationToken cancellationToken);
}
