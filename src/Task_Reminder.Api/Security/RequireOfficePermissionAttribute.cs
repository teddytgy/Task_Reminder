using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireOfficePermissionAttribute(OfficePermission permission) : Attribute, IAsyncActionFilter
{
    private readonly OfficePermission _permission = permission;

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var accessor = context.HttpContext.RequestServices.GetRequiredService<IRequestUserContextAccessor>();
        var requestUser = accessor.Current;

        if (!requestUser.IsAuthenticated || requestUser.Role is null || !PermissionRules.HasPermission(requestUser.Role.Value, _permission))
        {
            context.Result = new ObjectResult(new { message = "Permission denied for this action." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

            return Task.CompletedTask;
        }

        return next();
    }
}
