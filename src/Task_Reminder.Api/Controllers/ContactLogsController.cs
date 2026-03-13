using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/contact-logs")]
[RequireOfficePermission(OfficePermission.ViewOperationalData)]
public sealed class ContactLogsController(IContactLogService contactLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContactLogDto>>> ListAsync(
        [FromQuery] Guid? taskItemId,
        [FromQuery] Guid? appointmentWorkItemId,
        [FromQuery] Guid? insuranceWorkItemId,
        [FromQuery] Guid? balanceFollowUpWorkItemId,
        CancellationToken cancellationToken)
        => Ok(await contactLogService.ListAsync(taskItemId, appointmentWorkItemId, insuranceWorkItemId, balanceFollowUpWorkItemId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ContactLogDto>> CreateAsync([FromBody] CreateContactLogRequest request, CancellationToken cancellationToken)
        => Ok(await contactLogService.CreateAsync(request, cancellationToken));
}
