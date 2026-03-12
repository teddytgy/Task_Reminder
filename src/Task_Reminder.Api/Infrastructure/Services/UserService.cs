using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class UserService(TaskReminderDbContext dbContext) : IUserService
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
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Username = user.Username,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
    }
}
