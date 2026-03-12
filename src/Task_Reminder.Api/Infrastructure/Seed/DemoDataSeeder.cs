using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Seed;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(TaskReminderDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var users = new[]
        {
            new User { Id = Guid.NewGuid(), DisplayName = "Mia Front Desk", Username = "mia", IsActive = true, CreatedAtUtc = now },
            new User { Id = Guid.NewGuid(), DisplayName = "Noah Front Desk", Username = "noah", IsActive = true, CreatedAtUtc = now },
            new User { Id = Guid.NewGuid(), DisplayName = "Emma Insurance", Username = "emma", IsActive = true, CreatedAtUtc = now }
        };

        dbContext.Users.AddRange(users);

        var tasks = new[]
        {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Confirm tomorrow morning hygiene appointments",
                Description = "Call patients for the 8 AM to 11 AM schedule block.",
                Category = TaskCategory.AppointmentConfirmation,
                Priority = TaskPriority.High,
                Status = TaskStatus.New,
                CreatedByUserId = users[0].Id,
                DueAtUtc = now.AddMinutes(20),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                PatientReference = "HYGIENE-BLOCK",
                ReminderRepeatMinutes = 15
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Verify insurance for crown consult",
                Description = "Check annual maximum and waiting period.",
                Category = TaskCategory.InsuranceVerification,
                Priority = TaskPriority.Urgent,
                Status = TaskStatus.Assigned,
                AssignedUserId = users[2].Id,
                CreatedByUserId = users[1].Id,
                DueAtUtc = now.AddHours(1),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                PatientReference = "PT-20418",
                ReminderRepeatMinutes = 30
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Collect overdue balance follow-up",
                Description = "Patient requested reminder before end of day.",
                Category = TaskCategory.BalanceCollection,
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Overdue,
                CreatedByUserId = users[0].Id,
                DueAtUtc = now.AddHours(-2),
                CreatedAtUtc = now.AddDays(-1),
                UpdatedAtUtc = now,
                PatientReference = "PT-10992",
                ReminderRepeatMinutes = 60
            }
        };

        dbContext.Tasks.AddRange(tasks);
        dbContext.TaskHistory.AddRange(tasks.Select(task => new TaskHistory
        {
            Id = Guid.NewGuid(),
            TaskItemId = task.Id,
            ActionType = "Seeded",
            OldStatus = null,
            NewStatus = task.Status,
            PerformedByUserId = task.CreatedByUserId,
            PerformedAtUtc = now,
            Details = "Demo task created during initial seed."
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
