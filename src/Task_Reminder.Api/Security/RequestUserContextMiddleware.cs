using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;

namespace Task_Reminder.Api.Security;

public sealed class RequestUserContextMiddleware(
    RequestDelegate next,
    ILogger<RequestUserContextMiddleware> logger)
{
    public const string UserIdHeaderName = "X-TaskReminder-UserId";

    public async Task InvokeAsync(HttpContext httpContext, TaskReminderDbContext dbContext, IRequestUserContextAccessor accessor)
    {
        accessor.Current = RequestUserContext.Anonymous;

        if (httpContext.Request.Headers.TryGetValue(UserIdHeaderName, out var values) &&
            Guid.TryParse(values.FirstOrDefault(), out var userId))
        {
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, httpContext.RequestAborted);

            if (user is not null)
            {
                accessor.Current = new RequestUserContext
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    Role = user.Role
                };
            }
            else
            {
                logger.LogWarning("Request provided unknown or inactive user context header for user {UserId}.", userId);
            }
        }

        await next(httpContext);
    }
}
