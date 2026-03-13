using Task_Reminder.Shared;

namespace Task_Reminder.Api.Domain.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserNotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserNotificationPreferencesDto?> UpdatePreferencesAsync(Guid userId, UpdateUserNotificationPreferencesRequest request, CancellationToken cancellationToken);
}
