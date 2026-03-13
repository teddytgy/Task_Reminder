using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/recurring-tasks")]
[RequireOfficePermission(OfficePermission.ManageRecurringTasks)]
public sealed class RecurringTasksController(IRecurringTaskService recurringTaskService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecurringTaskDefinitionDto>>> ListAsync(CancellationToken cancellationToken)
        => Ok(await recurringTaskService.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RecurringTaskDefinitionDto>> CreateAsync([FromBody] CreateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definition = await recurringTaskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ListAsync), new { id = definition.Id }, definition);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RecurringTaskDefinitionDto>> UpdateAsync(Guid id, [FromBody] UpdateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definition = await recurringTaskService.UpdateAsync(id, request, cancellationToken);
        return definition is null ? NotFound() : Ok(definition);
    }

    [HttpPost("{id:guid}/active")]
    public async Task<ActionResult<RecurringTaskDefinitionDto>> SetActiveAsync(Guid id, [FromBody] SetRecurringTaskDefinitionActiveRequest request, CancellationToken cancellationToken)
    {
        var definition = await recurringTaskService.SetActiveAsync(id, request.IsActive, cancellationToken);
        return definition is null ? NotFound() : Ok(definition);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
        => await recurringTaskService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
