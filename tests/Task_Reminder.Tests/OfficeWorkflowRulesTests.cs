using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Infrastructure.Services;
using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;
using Xunit;

namespace Task_Reminder.Tests;

public sealed class OfficeWorkflowRulesTests
{
    [Fact]
    public void RecurringTaskScheduleRules_Generates_Weekday_Template_On_Weekday()
    {
        var shouldGenerate = RecurringTaskScheduleRules.ShouldGenerateForDate(
            RecurrenceType.Weekdays,
            1,
            null,
            null,
            new DateOnly(2026, 3, 2),
            null,
            new DateOnly(2026, 3, 13));

        Assert.True(shouldGenerate);
    }

    [Fact]
    public void NotificationRoutingRules_Notifies_Only_Assigned_User_For_Assigned_Task()
    {
        var assignedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var preferences = new UserNotificationPreferencesDto
        {
            UserId = assignedUserId,
            ReceiveAssignedTaskReminders = true
        };

        var task = new TaskItemDto
        {
            Id = Guid.NewGuid(),
            Title = "Verify insurance",
            AssignedUserId = assignedUserId,
            Status = TaskStatus.Assigned
        };

        Assert.True(NotificationRoutingRules.ShouldNotify(assignedUserId, UserRole.FrontDesk, preferences, task, false));
        Assert.False(NotificationRoutingRules.ShouldNotify(otherUserId, UserRole.FrontDesk, preferences, task, false));
    }

    [Fact]
    public void EscalationRules_Triggers_After_Threshold_And_Only_Once()
    {
        var dueAtUtc = DateTime.UtcNow.AddMinutes(-45);

        Assert.True(EscalationRules.ShouldEscalate(dueAtUtc, 30, DateTime.UtcNow, null));
        Assert.False(EscalationRules.ShouldEscalate(dueAtUtc, 30, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public async Task ManagerReportService_Calculates_Summary_And_Per_User_Completion()
    {
        var options = new DbContextOptionsBuilder<TaskReminderDbContext>()
            .UseInMemoryDatabase($"manager-metrics-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new TaskReminderDbContext(options);

        var manager = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Emma Insurance",
            Username = "emma",
            Role = UserRole.Manager,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };

        var frontDesk = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Mia Front Desk",
            Username = "mia",
            Role = UserRole.FrontDesk,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };

        dbContext.Users.AddRange(manager, frontDesk);
        dbContext.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Open overdue task",
                Category = TaskCategory.InsuranceVerification,
                Priority = TaskPriority.High,
                Status = TaskStatus.Overdue,
                AssignedUserId = frontDesk.Id,
                DueAtUtc = DateTime.UtcNow.AddHours(-2),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-4),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Completed task",
                Category = TaskCategory.AppointmentConfirmation,
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Completed,
                AssignedUserId = frontDesk.Id,
                ClaimedByUserId = frontDesk.Id,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
                CompletedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-1)
            });

        await dbContext.SaveChangesAsync();

        var service = new ManagerReportService(dbContext);
        var metrics = await service.GetMetricsAsync(new ManagerMetricsQuery { PresetDays = 7 }, CancellationToken.None);

        Assert.Equal(1, metrics.OverdueTasks);
        Assert.Equal(1, metrics.CompletedInRange);
        Assert.Equal(1, metrics.TotalOpenTasks);
        Assert.Contains(metrics.CompletedPerUser, x => x.UserDisplayName == "Mia Front Desk" && x.CompletedCount == 1);
        Assert.Contains(metrics.TasksByCategory, x => x.Label == TaskCategory.InsuranceVerification.ToString() && x.Count == 1);
    }
}
