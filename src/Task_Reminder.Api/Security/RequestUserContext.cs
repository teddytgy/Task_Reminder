using Task_Reminder.Shared;

namespace Task_Reminder.Api.Security;

public sealed class RequestUserContext
{
    public static readonly RequestUserContext Anonymous = new();

    public Guid? UserId { get; init; }
    public string? DisplayName { get; init; }
    public UserRole? Role { get; init; }
    public bool IsAuthenticated => UserId.HasValue && Role.HasValue;
}
