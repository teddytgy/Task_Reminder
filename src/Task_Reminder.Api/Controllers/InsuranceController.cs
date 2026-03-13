using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/insurance")]
[RequireOfficePermission(OfficePermission.ViewOperationalData)]
public sealed class InsuranceController(IInsuranceWorkService insuranceWorkService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InsuranceWorkItemDto>>> ListAsync([FromQuery] InsuranceQueryParameters query, CancellationToken cancellationToken)
        => Ok(await insuranceWorkService.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InsuranceWorkItemDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await insuranceWorkService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<InsuranceWorkItemDto>> CreateAsync([FromBody] CreateInsuranceWorkItemRequest request, CancellationToken cancellationToken)
        => Ok(await insuranceWorkService.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InsuranceWorkItemDto>> UpdateAsync(Guid id, [FromBody] UpdateInsuranceWorkItemRequest request, CancellationToken cancellationToken)
    {
        var item = await insuranceWorkService.UpdateAsync(id, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<InsuranceWorkItemDto>> UpdateStatusAsync(Guid id, [FromBody] InsuranceStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var item = await insuranceWorkService.UpdateStatusAsync(id, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:guid}/follow-up-task")]
    public async Task<ActionResult<TaskItemDto>> CreateFollowUpTaskAsync(Guid id, [FromQuery] bool managerEscalation, [FromBody] AppointmentActionRequest request, CancellationToken cancellationToken)
    {
        var task = await insuranceWorkService.CreateFollowUpTaskAsync(id, request.UserId, managerEscalation, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }
}
