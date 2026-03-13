using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[RequireOfficePermission(OfficePermission.ViewOperationalData)]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskItemDto>> CreateAsync([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var task = await taskService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = task.Id }, task);
        }
        catch (InvalidOperationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskItemDto>>> ListAsync([FromQuery] TaskQueryParameters query, CancellationToken cancellationToken)
    {
        return Ok(await taskService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await taskService.GetByIdAsync(id, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost("{id:guid}/assign")]
    public Task<ActionResult<TaskItemDto>> AssignAsync(Guid id, [FromBody] AssignTaskRequest request, CancellationToken cancellationToken)
        => ExecuteMutationAsync(() => taskService.AssignAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/claim")]
    public Task<ActionResult<TaskItemDto>> ClaimAsync(Guid id, [FromBody] ClaimTaskRequest request, CancellationToken cancellationToken)
        => ExecuteMutationAsync(() => taskService.ClaimAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/snooze")]
    public Task<ActionResult<TaskItemDto>> SnoozeAsync(Guid id, [FromBody] SnoozeTaskRequest request, CancellationToken cancellationToken)
        => ExecuteMutationAsync(() => taskService.SnoozeAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/complete")]
    public Task<ActionResult<TaskItemDto>> CompleteAsync(Guid id, [FromBody] CompleteTaskRequest request, CancellationToken cancellationToken)
        => ExecuteMutationAsync(() => taskService.CompleteAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public Task<ActionResult<TaskItemDto>> CancelAsync(Guid id, [FromBody] CancelTaskRequest request, CancellationToken cancellationToken)
        => ExecuteMutationAsync(() => taskService.CancelAsync(id, request, cancellationToken));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<TaskHistoryDto>>> HistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await taskService.GetHistoryAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<TaskHistoryDto>> AddCommentAsync(Guid id, [FromBody] AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await taskService.AddCommentAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    private async Task<ActionResult<TaskItemDto>> ExecuteMutationAsync(Func<Task<TaskItemDto?>> action)
    {
        try
        {
            var result = await action();
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }
}
