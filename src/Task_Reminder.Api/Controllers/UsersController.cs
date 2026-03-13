using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> ListAsync(CancellationToken cancellationToken)
    {
        return Ok(await userService.ListAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ListAsync), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpGet("{id:guid}/preferences")]
    public async Task<ActionResult<UserNotificationPreferencesDto>> GetPreferencesAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await userService.ExistsAsync(id, cancellationToken))
        {
            return NotFound();
        }

        return Ok(await userService.GetPreferencesAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/preferences")]
    public async Task<ActionResult<UserNotificationPreferencesDto>> UpdatePreferencesAsync(Guid id, [FromBody] UpdateUserNotificationPreferencesRequest request, CancellationToken cancellationToken)
    {
        var result = await userService.UpdatePreferencesAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
