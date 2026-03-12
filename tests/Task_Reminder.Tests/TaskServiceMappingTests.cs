using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Infrastructure.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Tests;

public sealed class TaskServiceMappingTests
{
    [Fact]
    public void MapTask_Marks_Task_As_Overdue_When_Past_Due_And_Not_Completed()
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Past due task",
            Status = TaskStatus.Assigned,
            Priority = TaskPriority.High,
            Category = TaskCategory.General,
            DueAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };

        var dto = TaskService.MapTask(task, DateTime.UtcNow);

        Assert.True(dto.IsOverdue);
        Assert.Equal(TaskStatus.Overdue, dto.Status);
    }

    [Fact]
    public void MapTask_Does_Not_Mark_Completed_Task_As_Overdue()
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Completed task",
            Status = TaskStatus.Completed,
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            DueAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CompletedAtUtc = DateTime.UtcNow.AddMinutes(-2),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
        };

        var dto = TaskService.MapTask(task, DateTime.UtcNow);

        Assert.False(dto.IsOverdue);
        Assert.Equal(TaskStatus.Completed, dto.Status);
    }
}
