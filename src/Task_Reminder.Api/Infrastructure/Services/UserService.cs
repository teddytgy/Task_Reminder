using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class UserService(
    TaskReminderDbContext dbContext,
    IAuditService auditService) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .Select(x => new UserDto
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Username = x.Username,
                IsActive = x.IsActive,
                Role = x.Role,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(x => x.Username == normalizedUsername, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("A user with that username already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName.Trim(),
            Username = normalizedUsername,
            IsActive = true,
            Role = UserRole.FrontDesk,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.UserNotificationPreferences.Add(new UserNotificationPreference
        {
            UserId = user.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("User", user.Id, "Created", $"Created user {user.DisplayName}.", $"Role: {user.Role}", user.Id, cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Username = user.Username,
            IsActive = user.IsActive,
            Role = user.Role,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
    }

    public async Task<UserNotificationPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await GetOrCreatePreferencesAsync(userId, cancellationToken);
        return MapPreferences(preference);
    }

    public async Task<UserNotificationPreferencesDto?> UpdatePreferencesAsync(Guid userId, UpdateUserNotificationPreferencesRequest request, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return null;
        }

        var preference = await GetOrCreatePreferencesAsync(userId, cancellationToken);
        preference.ReceiveAssignedTaskReminders = request.ReceiveAssignedTaskReminders;
        preference.ReceiveUnassignedTaskReminders = request.ReceiveUnassignedTaskReminders;
        preference.ReceiveOverdueEscalationAlerts = request.ReceiveOverdueEscalationAlerts;
        preference.ReceiveRecurringTaskGenerationAlerts = request.ReceiveRecurringTaskGenerationAlerts;
        preference.EnableSoundForUrgentReminders = request.EnableSoundForUrgentReminders;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("UserNotificationPreference", userId, "Updated", "Updated user notification preferences.", null, userId, cancellationToken);
        return MapPreferences(preference);
    }

    private async Task<UserNotificationPreference> GetOrCreatePreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await dbContext.UserNotificationPreferences.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (preference is not null)
        {
            return preference;
        }

        preference = new UserNotificationPreference
        {
            UserId = userId
        };

        dbContext.UserNotificationPreferences.Add(preference);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("UserNotificationPreference", userId, "Created", "Created default user notification preferences.", null, userId, cancellationToken);
        return preference;
    }

    private static UserNotificationPreferencesDto MapPreferences(UserNotificationPreference preference) =>
        new()
        {
            UserId = preference.UserId,
            ReceiveAssignedTaskReminders = preference.ReceiveAssignedTaskReminders,
            ReceiveUnassignedTaskReminders = preference.ReceiveUnassignedTaskReminders,
            ReceiveOverdueEscalationAlerts = preference.ReceiveOverdueEscalationAlerts,
            ReceiveRecurringTaskGenerationAlerts = preference.ReceiveRecurringTaskGenerationAlerts,
            EnableSoundForUrgentReminders = preference.EnableSoundForUrgentReminders
        };
}
