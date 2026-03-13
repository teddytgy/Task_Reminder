namespace Task_Reminder.Api.Security;

public interface IRequestUserContextAccessor
{
    RequestUserContext Current { get; set; }
}
