using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/balances")]
[RequireOfficePermission(OfficePermission.ViewOperationalData)]
public sealed class BalancesController(IBalanceFollowUpService balanceFollowUpService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BalanceFollowUpWorkItemDto>>> ListAsync([FromQuery] BalanceQueryParameters query, CancellationToken cancellationToken)
        => Ok(await balanceFollowUpService.ListAsync(query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<BalanceFollowUpWorkItemDto>> CreateAsync([FromBody] CreateBalanceFollowUpWorkItemRequest request, CancellationToken cancellationToken)
        => Ok(await balanceFollowUpService.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BalanceFollowUpWorkItemDto>> UpdateAsync(Guid id, [FromBody] UpdateBalanceFollowUpWorkItemRequest request, CancellationToken cancellationToken)
    {
        var item = await balanceFollowUpService.UpdateAsync(id, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<BalanceFollowUpWorkItemDto>> UpdateStatusAsync(Guid id, [FromQuery] BalanceFollowUpStatus status, [FromBody] AppointmentActionRequest request, CancellationToken cancellationToken)
    {
        var item = await balanceFollowUpService.UpdateStatusAsync(id, status, request.Notes, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:guid}/follow-up-task")]
    public async Task<ActionResult<TaskItemDto>> CreateFollowUpTaskAsync(Guid id, [FromBody] AppointmentActionRequest request, CancellationToken cancellationToken)
    {
        var task = await balanceFollowUpService.CreateFollowUpTaskAsync(id, request.UserId, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }
}
