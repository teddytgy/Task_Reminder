using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/audit")]
[RequireOfficePermission(OfficePermission.ViewAudit)]
public sealed class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditEntryDto>>> ListAsync([FromQuery] AuditQueryParameters query, CancellationToken cancellationToken)
        => Ok(await auditService.ListAsync(query, cancellationToken));
}
