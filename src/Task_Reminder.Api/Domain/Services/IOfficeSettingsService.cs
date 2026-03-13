using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IOfficeSettingsService
{
    Task<OfficeSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<OfficeSettingsDto> UpdateAsync(UpdateOfficeSettingsRequest request, CancellationToken cancellationToken);
}
