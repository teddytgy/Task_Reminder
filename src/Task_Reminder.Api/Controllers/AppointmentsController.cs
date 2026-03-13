using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[RequireOfficePermission(OfficePermission.ViewOperationalData)]
public sealed class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentWorkItemDto>>> ListAsync([FromQuery] AppointmentQueryParameters query, CancellationToken cancellationToken)
        => Ok(await appointmentService.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentWorkItemDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await appointmentService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentWorkItemDto>> CreateAsync([FromBody] CreateAppointmentWorkItemRequest request, CancellationToken cancellationToken)
        => Ok(await appointmentService.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AppointmentWorkItemDto>> UpdateAsync(Guid id, [FromBody] UpdateAppointmentWorkItemRequest request, CancellationToken cancellationToken)
    {
        var item = await appointmentService.UpdateAsync(id, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:guid}/{actionName}")]
    public async Task<ActionResult<AppointmentWorkItemDto>> ApplyActionAsync(Guid id, string actionName, [FromBody] AppointmentActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await appointmentService.ApplyActionAsync(id, actionName, request, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpPost("{id:guid}/follow-up-task")]
    public async Task<ActionResult<TaskItemDto>> CreateFollowUpTaskAsync(Guid id, [FromBody] AppointmentActionRequest request, CancellationToken cancellationToken)
    {
        var task = await appointmentService.CreateFollowUpTaskAsync(id, request, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }
}
