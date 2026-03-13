using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IContactLogService
{
    Task<IReadOnlyList<ContactLogDto>> ListAsync(Guid? taskItemId, Guid? appointmentWorkItemId, Guid? insuranceWorkItemId, Guid? balanceFollowUpWorkItemId, CancellationToken cancellationToken);
    Task<ContactLogDto> CreateAsync(CreateContactLogRequest request, CancellationToken cancellationToken);
}
