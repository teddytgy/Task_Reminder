using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IManagerReportService
{
    Task<ManagerMetricsDto> GetMetricsAsync(ManagerMetricsQuery query, CancellationToken cancellationToken);
    Task<string> ExportCsvAsync(ManagerMetricsQuery query, CancellationToken cancellationToken);
}
