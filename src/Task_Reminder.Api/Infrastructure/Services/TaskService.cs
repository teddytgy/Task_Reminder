using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class TaskService(
    TaskReminderDbContext dbContext,
    IUserService userService,
    TaskBroadcastService broadcastService,
    IAuditService auditService,
    ILogger<TaskService> logger) : ITaskService
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
            ReminderRepeatMinutes = Math.Clamp(request.ReminderRepeatMinutes ?? 30, 5, 1440),
            EscalateAfterMinutes = request.EscalateAfterMinutes,
            EscalateToUserId = request.EscalateToUserId,
            AppointmentWorkItemId = request.AppointmentWorkItemId,
            InsuranceWorkItemId = request.InsuranceWorkItemId,
            BalanceFollowUpWorkItemId = request.BalanceFollowUpWorkItemId
        };

        dbContext.Tasks.Add(task);
        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Created", null, task.Status, task.CreatedByUserId, $"Task created: {task.Title}", now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("Task", task.Id, "Created", $"Created task {task.Title}.", task.Description, task.CreatedByUserId, cancellationToken);
        logger.LogInformation("Created task {TaskId} with status {TaskStatus} and assignee {AssignedUserId}.", task.Id, task.Status, task.AssignedUserId);

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
            logger.LogWarning("Assign requested for missing task {TaskId}.", taskId);
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
        await auditService.WriteAsync("Task", task.Id, "Assigned", $"Assigned task {task.Title}.", $"Assigned to user {request.AssignedUserId}.", request.PerformedByUserId, cancellationToken);
        logger.LogInformation("Assigned task {TaskId} to user {AssignedUserId}.", task.Id, request.AssignedUserId);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("assigned", dto, cancellationToken);
        return dto;
    }

    public async Task<TaskItemDto?> ClaimAsync(Guid taskId, ClaimTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            logger.LogWarning("Claim requested for missing task {TaskId}.", taskId);
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
        await auditService.WriteAsync("Task", task.Id, "Claimed", $"Claimed task {task.Title}.", $"Claimed by user {request.UserId}.", request.UserId, cancellationToken);
        logger.LogInformation("Claimed task {TaskId} by user {UserId}.", task.Id, request.UserId);

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
            logger.LogWarning("Snooze requested for missing task {TaskId}.", taskId);
            return null;
        }

        var oldStatus = task.Status;
        task.SnoozeUntilUtc = request.SnoozeUntilUtc;
        task.Status = TaskStatus.Snoozed;
        task.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Snoozed", oldStatus, task.Status, request.UserId, $"Snoozed until {request.SnoozeUntilUtc:O}.", task.UpdatedAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("Task", task.Id, "Snoozed", $"Snoozed task {task.Title}.", $"Snoozed until {request.SnoozeUntilUtc:O}.", request.UserId, cancellationToken);
        logger.LogInformation("Snoozed task {TaskId} until {SnoozeUntilUtc}.", task.Id, request.SnoozeUntilUtc);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("snoozed", dto, cancellationToken);
        return dto;
    }

    public async Task<TaskItemDto?> CompleteAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            logger.LogWarning("Complete requested for missing task {TaskId}.", taskId);
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
        await auditService.WriteAsync("Task", task.Id, "Completed", $"Completed task {task.Title}.", "Task completed.", request.UserId, cancellationToken);
        logger.LogInformation("Completed task {TaskId} by user {UserId}.", task.Id, request.UserId);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("completed", dto, cancellationToken);
        return dto;
    }

    public async Task<TaskItemDto?> CancelAsync(Guid taskId, CancelTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            logger.LogWarning("Cancel requested for missing task {TaskId}.", taskId);
            return null;
        }

        var oldStatus = task.Status;
        task.Status = TaskStatus.Cancelled;
        task.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.TaskHistory.Add(CreateHistory(task.Id, "Cancelled", oldStatus, task.Status, request.UserId, request.Reason ?? "Task cancelled.", task.UpdatedAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("Task", task.Id, "Cancelled", $"Cancelled task {task.Title}.", request.Reason ?? "Task cancelled.", request.UserId, cancellationToken);
        logger.LogInformation("Cancelled task {TaskId} by user {UserId}.", task.Id, request.UserId);

        var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
        await broadcastService.BroadcastTaskChangedAsync("cancelled", dto, cancellationToken);
        return dto;
    }

    public async Task<TaskHistoryDto?> AddCommentAsync(Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            logger.LogWarning("Comment requested for missing task {TaskId}.", taskId);
            return null;
        }

        var historyEntry = new TaskHistory
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId,
            ActionType = "Comment",
            OldStatus = null,
            NewStatus = null,
            PerformedByUserId = request.UserId,
            PerformedAtUtc = DateTime.UtcNow,
            Details = request.Comment.Trim()
        };

        dbContext.TaskHistory.Add(historyEntry);
        task.UpdatedAtUtc = historyEntry.PerformedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("Task", taskId, "Commented", $"Added comment to task {task.Title}.", request.Comment.Trim(), request.UserId, cancellationToken);
        logger.LogInformation("Added comment to task {TaskId} by user {UserId}.", taskId, request.UserId);

        var performedBy = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        return new TaskHistoryDto
        {
            Id = historyEntry.Id,
            TaskItemId = historyEntry.TaskItemId,
            ActionType = historyEntry.ActionType,
            OldStatus = historyEntry.OldStatus,
            NewStatus = historyEntry.NewStatus,
            PerformedByUserId = historyEntry.PerformedByUserId,
            PerformedByDisplayName = performedBy?.DisplayName,
            PerformedAtUtc = historyEntry.PerformedAtUtc,
            Details = historyEntry.Details
        };
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
            await auditService.WriteAsync("Task", task.Id, "MarkedOverdue", $"Marked task {task.Title} overdue.", "Task automatically marked overdue.", null, cancellationToken);
        }
        logger.LogInformation("Marked {TaskCount} tasks as overdue.", tasks.Count);

        foreach (var task in tasks)
        {
            var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
            await broadcastService.BroadcastTaskChangedAsync("overdue", dto, cancellationToken);
        }

        var escalationCandidates = await dbContext.Tasks
            .Where(x =>
                x.DueAtUtc.HasValue &&
                x.Status == TaskStatus.Overdue &&
                x.EscalateToUserId.HasValue)
            .ToListAsync(cancellationToken);

        var escalatedTasks = escalationCandidates
            .Where(x => Task_Reminder.Shared.EscalationRules.ShouldEscalate(x.DueAtUtc!.Value, x.EscalateAfterMinutes, now, x.EscalatedAtUtc))
            .ToList();

        foreach (var task in escalatedTasks)
        {
            task.EscalatedAtUtc = now;
            task.UpdatedAtUtc = now;
            dbContext.TaskHistory.Add(CreateHistory(task.Id, "Escalated", task.Status, task.Status, task.EscalateToUserId, $"Task escalated to user {task.EscalateToUserId}.", now));
        }

        if (escalatedTasks.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            foreach (var task in escalatedTasks)
            {
                await auditService.WriteAsync("Task", task.Id, "Escalated", $"Escalated task {task.Title}.", $"Task escalated to user {task.EscalateToUserId}.", task.EscalateToUserId, cancellationToken);
            }
            logger.LogInformation("Escalated {TaskCount} overdue tasks.", escalatedTasks.Count);

            foreach (var task in escalatedTasks)
            {
                var dto = await GetRequiredDtoAsync(task.Id, cancellationToken);
                await broadcastService.BroadcastTaskChangedAsync("escalated", dto, cancellationToken);
            }
        }

        return tasks.Count + escalatedTasks.Count;
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
            EscalateAfterMinutes = task.EscalateAfterMinutes,
            EscalateToUserId = task.EscalateToUserId,
            EscalatedAtUtc = task.EscalatedAtUtc,
            GeneratedFromRecurringTaskDefinitionId = task.GeneratedFromRecurringTaskDefinitionId,
            GeneratedForDateLocal = task.GeneratedForDateLocal,
            AppointmentWorkItemId = task.AppointmentWorkItemId,
            InsuranceWorkItemId = task.InsuranceWorkItemId,
            BalanceFollowUpWorkItemId = task.BalanceFollowUpWorkItemId,
            IsOverdue = isOverdue,
            IsDueNow = isDueNow
        };
    }
}
