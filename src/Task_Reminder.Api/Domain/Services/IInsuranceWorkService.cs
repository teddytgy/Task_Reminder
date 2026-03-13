using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IInsuranceWorkService
{
    Task<IReadOnlyList<InsuranceWorkItemDto>> ListAsync(InsuranceQueryParameters query, CancellationToken cancellationToken);
    Task<InsuranceWorkItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<InsuranceWorkItemDto> CreateAsync(CreateInsuranceWorkItemRequest request, CancellationToken cancellationToken);
    Task<InsuranceWorkItemDto?> UpdateAsync(Guid id, UpdateInsuranceWorkItemRequest request, CancellationToken cancellationToken);
    Task<InsuranceWorkItemDto?> UpdateStatusAsync(Guid id, InsuranceStatusUpdateRequest request, CancellationToken cancellationToken);
    Task<TaskItemDto?> CreateFollowUpTaskAsync(Guid id, Guid? userId, bool managerEscalation, CancellationToken cancellationToken);
}
