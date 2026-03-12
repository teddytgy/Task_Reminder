using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class TaskService(
    TaskReminderDbContext dbContext,
    IUserService userService,
    TaskBroadcastService broadcastService) : ITaskService
{
    public async Task<TaskItemDto> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (request.AssignedUserId.HasValue && !await userService.ExistsAsync(request.AssignedUserId.Value, cancellationToken))
        {
            throw new InvalidOperationException("Assigned user was not found.");
        }

        if (request.CreatedByUserId.HasValue && !await userService.ExistsAsync(request.CreatedByUserId.Value, cancellationToken))
        {
            throw new InvalidOperationException("Created by user was not found.");
        }

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category,
            Priority = request.Priority,
            Status = request.AssignedUserId.HasValue ? TaskStatus.Assigned : TaskStatus.New,
            AssignedUserId = request.AssignedUserId,
            CreatedByUserId = request.CreatedByUserId,
            DueAtUtc = request.DueAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PatientReference = request.PatientReference?.Trim(),
            Notes = request.Notes?.Trim(),
            ReminderRepeatMinutes = Math.Clamp(request.ReminderRepeatMinutes ?? 30, 5, 1440)
        };

        dbContext.Tasks.Add(task);
        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Created", null, task.Status, task.CreatedByUserId, $"Task created: {task.Title}", now));
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("created", dto, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyList<TaskItemDto>> ListAsync(TaskQueryParameters query, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var dueNowWindow = now.AddMinutes(15);

        IQueryable<TaskItem> tasks = dbContext.Tasks
            .AsNoTracking()
            .Include(x => x.AssignedUser)
            .Include(x => x.ClaimedByUser)
            .Include(x => x.CreatedByUser);

        if (query.Status.HasValue)
        {
            tasks = tasks.Where(x => x.Status == query.Status.Value);
        }

        if (query.OverdueOnly)
        {
            tasks = tasks.Where(x => x.Status == TaskStatus.Overdue || (x.DueAtUtc.HasValue && x.DueAtUtc < now && x.Status != TaskStatus.Completed && x.Status != TaskStatus.Cancelled));
        }

        if (query.UnassignedOnly)
        {
            tasks = tasks.Where(x => x.AssignedUserId == null);
        }

        if (query.AssignedToMeOnly && query.UserId.HasValue)
        {
            tasks = tasks.Where(x => x.AssignedUserId == query.UserId || x.ClaimedByUserId == query.UserId);
        }

        if (query.CompletedTodayOnly)
        {
            tasks = tasks.Where(x => x.CompletedAtUtc >= todayStart && x.CompletedAtUtc < tomorrowStart);
        }

        if (query.DueTodayOnly)
        {
            tasks = tasks.Where(x => x.DueAtUtc >= todayStart && x.DueAtUtc < tomorrowStart);
        }

        if (query.DueNowOnly)
        {
            tasks = tasks.Where(x =>
                x.Status != TaskStatus.Completed &&
                x.Status != TaskStatus.Cancelled &&
                x.DueAtUtc.HasValue &&
                x.DueAtUtc <= dueNowWindow &&
                (!x.SnoozeUntilUtc.HasValue || x.SnoozeUntilUtc <= now));
        }

        var entities = await tasks
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueAtUtc ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(x => MapTask(x, now)).ToList();
    }

    public async Task<TaskItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .Include(x => x.AssignedUser)
            .Include(x => x.ClaimedByUser)
            .Include(x => x.CreatedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return task is null ? null : MapTask(task, DateTime.UtcNow);
    }

    public async Task<TaskItemDto?> AssignAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        if (!await userService.ExistsAsync(request.AssignedUserId, cancellationToken))
        {
            throw new InvalidOperationException("Assigned user was not found.");
        }

        var oldStatus = task.Status;
        task.AssignedUserId = request.AssignedUserId;
        task.Status = task.Status is TaskStatus.New or TaskStatus.Overdue ? TaskStatus.Assigned : task.Status;
        task.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Assigned", oldStatus, task.Status, request.PerformedByUserId, $"Assigned to user {request.AssignedUserId}.", task.UpdatedAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("assigned", dto, cancellationToken);
        return dto;
    }

    public async Task<TaskItemDto?> ClaimAsync(Guid taskId, ClaimTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        if (!await userService.ExistsAsync(request.UserId, cancellationToken))
        {
            throw new InvalidOperationException("User was not found.");
        }

        if (task.Status is TaskStatus.Completed or TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Completed or cancelled tasks cannot be claimed.");
        }

        var oldStatus = task.Status;
        task.ClaimedByUserId = request.UserId;
        task.Status = TaskStatus.InProgress;
        task.SnoozeUntilUtc = null;
        task.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Claimed", oldStatus, task.Status, request.UserId, $"Claimed by user {request.UserId}.", task.UpdatedAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("claimed", dto, cancellationToken);
        return dto;
    }

    public async Task<TaskItemDto?> SnoozeAsync(Guid taskId, SnoozeTaskRequest request, CancellationToken cancellationToken)
    {
        if (request.SnoozeUntilUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Snooze time must be in the future.");
        }

        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var oldStatus = task.Status;
        task.SnoozeUntilUtc = request.SnoozeUntilUtc;
        task.Status = TaskStatus.Snoozed;
        task.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Snoozed", oldStatus, task.Status, request.UserId, $"Snoozed until {request.SnoozeUntilUtc:O}.", task.UpdatedAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("snoozed", dto, cancellationToken);
        return dto;
    }

    public async Task<TaskItemDto?> CompleteAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var oldStatus = task.Status;
        task.Status = TaskStatus.Completed;
        task.CompletedAtUtc = DateTime.UtcNow;
        task.ClaimedByUserId = request.UserId;
        task.SnoozeUntilUtc = null;
        task.UpdatedAtUtc = task.CompletedAtUtc.Value;

        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Completed", oldStatus, task.Status, request.UserId, "Task completed.", task.UpdatedAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("completed", dto, cancellationToken);
        return dto;
    }

    public async Task<TaskItemDto?> CancelAsync(Guid taskId, CancelTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var oldStatus = task.Status;
        task.Status = TaskStatus.Cancelled;
        task.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Cancelled", oldStatus, task.Status, request.UserId, request.Reason ?? "Task cancelled.", task.UpdatedAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("cancelled", dto, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyList<TaskHistoryDto>> GetHistoryAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await dbContext.TaskHistory
            .AsNoTracking()
            .Where(x => x.TaskItemId == taskId)
            .Include(x => x.PerformedByUser)
            .OrderByDescending(x => x.PerformedAtUtc)
            .Select(x => new TaskHistoryDto
            {
                Id = x.Id,
                TaskItemId = x.TaskItemId,
                ActionType = x.ActionType,
                OldStatus = x.OldStatus,
                NewStatus = x.NewStatus,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByDisplayName = x.PerformedByUser != null ? x.PerformedByUser.DisplayName : null,
                PerformedAtUtc = x.PerformedAtUtc,
                Details = x.Details
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> MarkOverdueTasksAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tasks = await dbContext.Tasks
            .Where(x =>
                x.DueAtUtc.HasValue &&
                x.DueAtUtc < now &&
                x.Status != TaskStatus.Completed &&
                x.Status != TaskStatus.Cancelled &&
                (!x.SnoozeUntilUtc.HasValue || x.SnoozeUntilUtc <= now) &&
                x.Status != TaskStatus.Overdue)
            .ToListAsync(cancellationToken);

        if (tasks.Count == 0)
        {
            return 0;
        }

        foreach (var task in tasks)
        {
            var oldStatus = task.Status;
            task.Status = TaskStatus.Overdue;
            task.UpdatedAtUtc = now;
            dbContext.TaskHistory.Add(CreateHistory(task.Id, "MarkedOverdue", oldStatus, task.Status, null, "Task automatically marked overdue.", now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var task in tasks)
        {
            var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
            await broadcastService.BroadcastTaskChangedAsync("overdue", dto, cancellationToken);
        }

        return tasks.Count;
    }

    private async Task<TaskItemDto> GetRequiredDtoAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .Include(x => x.AssignedUser)
            .Include(x => x.ClaimedByUser)
            .Include(x => x.CreatedByUser)
            .FirstAsync(x => x.Id == taskId, cancellationToken);

        return MapTask(task, DateTime.UtcNow);
    }

    private static TaskHistory CreateHistory(Guid taskItemId, string actionType, TaskStatus? oldStatus, TaskStatus? newStatus, Guid? userId, string? details, DateTime performedAtUtc)
    {
        return new TaskHistory
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskItemId,
            ActionType = actionType,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            PerformedByUserId = userId,
            PerformedAtUtc = performedAtUtc,
            Details = details
        };
    }

    public static TaskItemDto MapTask(TaskItem task, DateTime nowUtc)
    {
        var isOverdue = task.DueAtUtc.HasValue &&
                        task.DueAtUtc.Value < nowUtc &&
                        task.Status != TaskStatus.Completed &&
                        task.Status != TaskStatus.Cancelled &&
                        (!task.SnoozeUntilUtc.HasValue || task.SnoozeUntilUtc <= nowUtc);

        var isDueNow = task.DueAtUtc.HasValue &&
                       task.DueAtUtc.Value <= nowUtc.AddMinutes(15) &&
                       task.Status != TaskStatus.Completed &&
                       task.Status != TaskStatus.Cancelled &&
                       (!task.SnoozeUntilUtc.HasValue || task.SnoozeUntilUtc <= nowUtc);

        return new TaskItemDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Category = task.Category,
            Priority = task.Priority,
            Status = isOverdue ? TaskStatus.Overdue : task.Status,
            AssignedUserId = task.AssignedUserId,
            AssignedUserDisplayName = task.AssignedUser?.DisplayName,
            ClaimedByUserId = task.ClaimedByUserId,
            ClaimedByDisplayName = task.ClaimedByUser?.DisplayName,
            CreatedByUserId = task.CreatedByUserId,
            CreatedByDisplayName = task.CreatedByUser?.DisplayName,
            DueAtUtc = task.DueAtUtc,
            SnoozeUntilUtc = task.SnoozeUntilUtc,
            CompletedAtUtc = task.CompletedAtUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc,
            PatientReference = task.PatientReference,
            Notes = task.Notes,
            ReminderRepeatMinutes = task.ReminderRepeatMinutes,
            IsOverdue = isOverdue,
            IsDueNow = isDueNow
        };
    }
}
