using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(ISystemInfoService systemInfoService) : ControllerBase
{
    [HttpGet("version")]
    public async Task<ActionResult<SystemVersionInfoDto>> GetVersionAsync(CancellationToken cancellationToken)
        => Ok(await systemInfoService.GetVersionAsync(cancellationToken));

    [HttpGet("summary")]
    [RequireOfficePermission(OfficePermission.ViewAudit)]
    public async Task<ActionResult<SystemStatusSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken)
        => Ok(await systemInfoService.GetSummaryAsync(cancellationToken));
}
