using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface ISystemInfoService
{
    Task<SystemVersionInfoDto> GetVersionAsync(CancellationToken cancellationToken);
    Task<SystemStatusSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
