using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentWorkItemDto>> ListAsync(AppointmentQueryParameters query, CancellationToken cancellationToken);
    Task<AppointmentWorkItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AppointmentWorkItemDto> CreateAsync(CreateAppointmentWorkItemRequest request, CancellationToken cancellationToken);
    Task<AppointmentWorkItemDto?> UpdateAsync(Guid id, UpdateAppointmentWorkItemRequest request, CancellationToken cancellationToken);
    Task<AppointmentWorkItemDto?> ApplyActionAsync(Guid id, string actionName, AppointmentActionRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto?> CreateFollowUpTaskAsync(Guid id, AppointmentActionRequest request, CancellationToken cancellationToken);
}
