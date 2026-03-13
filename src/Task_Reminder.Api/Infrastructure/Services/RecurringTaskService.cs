using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class RecurringTaskService(
    TaskReminderDbContext dbContext,
    TaskBroadcastService broadcastService,
    IAuditService auditService,
    ILogger<RecurringTaskService> logger) : IRecurringTaskService
{
    public async Task<IReadOnlyList<RecurringTaskDefinitionDto>> ListAsync(CancellationToken cancellationToken)
    {
        var definitions = await dbContext.RecurringTaskDefinitions
            .AsNoTracking()
            .Include(x => x.AssignedUser)
            .Include(x => x.CreatedByUser)
            .Include(x => x.EscalateToUser)
            .OrderBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return definitions.Select(MapDefinition).ToList();
    }

    public async Task<RecurringTaskDefinitionDto> CreateAsync(CreateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var definition = new RecurringTaskDefinition
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category,
            Priority = request.Priority,
            AssignedUserId = request.AssignedUserId,
            CreatedByUserId = request.CreatedByUserId,
            PatientReference = request.PatientReference?.Trim(),
            Notes = request.Notes?.Trim(),
            ReminderRepeatMinutes = Math.Clamp(request.ReminderRepeatMinutes ?? 30, 5, 1440),
            EscalateAfterMinutes = request.EscalateAfterMinutes,
            EscalateToUserId = request.EscalateToUserId,
            RecurrenceType = request.RecurrenceType,
            RecurrenceInterval = Math.Max(1, request.RecurrenceInterval),
            DaysOfWeek = request.DaysOfWeek,
            DayOfMonth = request.DayOfMonth,
            TimeOfDayLocal = request.TimeOfDayLocal,
            StartDateLocal = request.StartDateLocal,
            EndDateLocal = request.EndDateLocal,
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.RecurringTaskDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("RecurringTaskDefinition", definition.Id, "Created", $"Created recurring task definition {definition.Title}.", definition.Description, definition.CreatedByUserId, cancellationToken);
        logger.LogInformation("Created recurring task definition {DefinitionId}.", definition.Id);
        return await GetRequiredDtoAsync(definition.Id, cancellationToken);
    }

    public async Task<RecurringTaskDefinitionDto?> UpdateAsync(Guid id, UpdateRecurringTaskDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definition = await dbContext.RecurringTaskDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition is null)
        {
            return null;
        }

        definition.Title = request.Title.Trim();
        definition.Description = request.Description?.Trim();
        definition.Category = request.Category;
        definition.Priority = request.Priority;
        definition.AssignedUserId = request.AssignedUserId;
        definition.CreatedByUserId = request.CreatedByUserId;
        definition.PatientReference = request.PatientReference?.Trim();
        definition.Notes = request.Notes?.Trim();
        definition.ReminderRepeatMinutes = Math.Clamp(request.ReminderRepeatMinutes ?? 30, 5, 1440);
        definition.EscalateAfterMinutes = request.EscalateAfterMinutes;
        definition.EscalateToUserId = request.EscalateToUserId;
        definition.RecurrenceType = request.RecurrenceType;
        definition.RecurrenceInterval = Math.Max(1, request.RecurrenceInterval);
        definition.DaysOfWeek = request.DaysOfWeek;
        definition.DayOfMonth = request.DayOfMonth;
        definition.TimeOfDayLocal = request.TimeOfDayLocal;
        definition.StartDateLocal = request.StartDateLocal;
        definition.EndDateLocal = request.EndDateLocal;
        definition.IsActive = request.IsActive;
        definition.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("RecurringTaskDefinition", definition.Id, "Updated", $"Updated recurring task definition {definition.Title}.", definition.Description, definition.CreatedByUserId, cancellationToken);
        logger.LogInformation("Updated recurring task definition {DefinitionId}.", definition.Id);
        return await GetRequiredDtoAsync(definition.Id, cancellationToken);
    }

    public async Task<RecurringTaskDefinitionDto?> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var definition = await dbContext.RecurringTaskDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition is null)
        {
            return null;
        }

        definition.IsActive = isActive;
        definition.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("RecurringTaskDefinition", definition.Id, isActive ? "Enabled" : "Disabled", $"{(isActive ? "Enabled" : "Disabled")} recurring task definition {definition.Title}.", null, definition.CreatedByUserId, cancellationToken);
        logger.LogInformation("{Action} recurring task definition {DefinitionId}.", isActive ? "Enabled" : "Disabled", definition.Id);
        return await GetRequiredDtoAsync(definition.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.RecurringTaskDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition is null)
        {
            return false;
        }

        dbContext.RecurringTaskDefinitions.Remove(definition);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("RecurringTaskDefinition", id, "Deleted", $"Deleted recurring task definition {definition.Title}.", null, definition.CreatedByUserId, cancellationToken);
        logger.LogInformation("Deleted recurring task definition {DefinitionId}.", id);
        return true;
    }

    public async Task<int> GenerateDueTasksAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var nowLocal = DateTime.Now;
        var todayLocal = DateOnly.FromDateTime(nowLocal);
        var generatedCount = 0;

        var definitions = await dbContext.RecurringTaskDefinitions
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var definition in definitions)
        {
            if (!RecurringTaskScheduleRules.ShouldGenerateForDate(
                    definition.RecurrenceType,
                    definition.RecurrenceInterval,
                    definition.DaysOfWeek,
                    definition.DayOfMonth,
                    definition.StartDateLocal,
                    definition.EndDateLocal,
                    todayLocal))
            {
                continue;
            }

            var scheduledLocal = definition.TimeOfDayLocal.HasValue
                ? nowLocal.Date.Add(definition.TimeOfDayLocal.Value)
                : nowLocal.Date;

            if (scheduledLocal > nowLocal)
            {
                continue;
            }

            var alreadyGenerated = await dbContext.Tasks.AnyAsync(
                x => x.GeneratedFromRecurringTaskDefinitionId == definition.Id && x.GeneratedForDateLocal == todayLocal,
                cancellationToken);

            if (alreadyGenerated)
            {
                continue;
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = definition.Title,
                Description = definition.Description,
                Category = definition.Category,
                Priority = definition.Priority,
                Status = definition.AssignedUserId.HasValue ? TaskStatus.Assigned : TaskStatus.New,
                AssignedUserId = definition.AssignedUserId,
                CreatedByUserId = definition.CreatedByUserId,
                DueAtUtc = definition.TimeOfDayLocal.HasValue ? scheduledLocal.ToUniversalTime() : null,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc,
                PatientReference = definition.PatientReference,
                Notes = definition.Notes,
                ReminderRepeatMinutes = definition.ReminderRepeatMinutes,
                EscalateAfterMinutes = definition.EscalateAfterMinutes,
                EscalateToUserId = definition.EscalateToUserId,
                GeneratedFromRecurringTaskDefinitionId = definition.Id,
                GeneratedForDateLocal = todayLocal
            };

            dbContext.Tasks.Add(task);
            dbContext.TaskHistory.Add(new TaskHistory
            {
                Id = Guid.NewGuid(),
                TaskItemId = task.Id,
                ActionType = "GeneratedRecurringTask",
                OldStatus = null,
                NewStatus = task.Status,
                PerformedByUserId = null,
                PerformedAtUtc = nowUtc,
                Details = $"System-generated from recurring definition '{definition.Title}'."
            });

            definition.LastGeneratedAtUtc = nowUtc;
            definition.UpdatedAtUtc = nowUtc;

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync("RecurringTaskDefinition", definition.Id, "GeneratedTask", $"Generated recurring task from {definition.Title}.", $"Generated task {task.Title} for {todayLocal}.", null, cancellationToken);

            var dto = await dbContext.Tasks
                .AsNoTracking()
                .Include(x => x.AssignedUser)
                .Include(x => x.ClaimedByUser)
                .Include(x => x.CreatedByUser)
                .FirstAsync(x => x.Id == task.Id, cancellationToken)
                .ContinueWith(result => TaskService.MapTask(result.Result, DateTime.UtcNow), cancellationToken);

            await broadcastService.BroadcastTaskChangedAsync("recurring-generated", dto, cancellationToken);
            generatedCount++;
        }

        if (generatedCount > 0)
        {
            logger.LogInformation("Generated {GeneratedCount} recurring tasks.", generatedCount);
        }

        return generatedCount;
    }

    private async Task<RecurringTaskDefinitionDto> GetRequiredDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.RecurringTaskDefinitions
            .AsNoTracking()
            .Include(x => x.AssignedUser)
            .Include(x => x.CreatedByUser)
            .Include(x => x.EscalateToUser)
            .FirstAsync(x => x.Id == id, cancellationToken);

        return MapDefinition(definition);
    }

    private static RecurringTaskDefinitionDto MapDefinition(RecurringTaskDefinition definition) =>
        new()
        {
            Id = definition.Id,
            Title = definition.Title,
            Description = definition.Description,
            Category = definition.Category,
            Priority = definition.Priority,
            AssignedUserId = definition.AssignedUserId,
            AssignedUserDisplayName = definition.AssignedUser?.DisplayName,
            CreatedByUserId = definition.CreatedByUserId,
            CreatedByDisplayName = definition.CreatedByUser?.DisplayName,
            PatientReference = definition.PatientReference,
            Notes = definition.Notes,
            ReminderRepeatMinutes = definition.ReminderRepeatMinutes,
            EscalateAfterMinutes = definition.EscalateAfterMinutes,
            EscalateToUserId = definition.EscalateToUserId,
            EscalateToUserDisplayName = definition.EscalateToUser?.DisplayName,
            RecurrenceType = definition.RecurrenceType,
            RecurrenceInterval = definition.RecurrenceInterval,
            DaysOfWeek = definition.DaysOfWeek,
            DayOfMonth = definition.DayOfMonth,
            TimeOfDayLocal = definition.TimeOfDayLocal,
            StartDateLocal = definition.StartDateLocal,
            EndDateLocal = definition.EndDateLocal,
            IsActive = definition.IsActive,
            LastGeneratedAtUtc = definition.LastGeneratedAtUtc,
            CreatedAtUtc = definition.CreatedAtUtc,
            UpdatedAtUtc = definition.UpdatedAtUtc
        };
}
