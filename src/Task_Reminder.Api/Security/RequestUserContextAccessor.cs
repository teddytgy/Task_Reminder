namespace Task_Reminder.Api.Security;

public sealed class RequestUserContextAccessor : IRequestUserContextAccessor
{
    public RequestUserContext Current { get; set; } = RequestUserContext.Anonymous;
}
