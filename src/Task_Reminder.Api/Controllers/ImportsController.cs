using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/imports")]
[RequireOfficePermission(OfficePermission.ManageImports)]
public sealed class ImportsController(IImportService importService) : ControllerBase
{
    [HttpPost("appointments")]
    public async Task<ActionResult<ImportResultDto>> ImportAppointmentsAsync([FromBody] ImportAppointmentsRequest request, CancellationToken cancellationToken)
        => Ok(await importService.ImportAppointmentsAsync(request, cancellationToken));

    [HttpPost("insurance")]
    public async Task<ActionResult<ImportResultDto>> ImportInsuranceAsync([FromBody] ImportInsuranceWorkItemsRequest request, CancellationToken cancellationToken)
        => Ok(await importService.ImportInsuranceAsync(request, cancellationToken));
}
