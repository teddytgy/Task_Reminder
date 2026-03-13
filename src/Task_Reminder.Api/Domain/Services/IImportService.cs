using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IImportService
{
    Task<ImportResultDto> ImportAppointmentsAsync(ImportAppointmentsRequest request, CancellationToken cancellationToken);
    Task<ImportResultDto> ImportInsuranceAsync(ImportInsuranceWorkItemsRequest request, CancellationToken cancellationToken);
}
