using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class ManagerReportService(TaskReminderDbContext dbContext) : IManagerReportService
{
    public async Task<ManagerMetricsDto> GetMetricsAsync(ManagerMetricsQuery query, CancellationToken cancellationToken)
    {
        var range = ResolveRange(query);
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Include(x => x.AssignedUser)
            .Where(x => x.CreatedAtUtc >= range.fromUtc && x.CreatedAtUtc <= range.toUtc || (x.CompletedAtUtc.HasValue && x.CompletedAtUtc >= range.fromUtc && x.CompletedAtUtc <= range.toUtc))
            .ToListAsync(cancellationToken);

        var users = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
        var completedTasks = tasks.Where(x => x.CompletedAtUtc.HasValue && x.CompletedAtUtc.Value >= range.fromUtc && x.CompletedAtUtc.Value <= range.toUtc).ToList();

        return new ManagerMetricsDto
        {
            RangeStartUtc = range.fromUtc,
            RangeEndUtc = range.toUtc,
            TotalOpenTasks = tasks.Count(x => x.Status is not TaskStatus.Completed and not TaskStatus.Cancelled),
            OverdueTasks = tasks.Count(x => x.Status == TaskStatus.Overdue),
            CompletedInRange = completedTasks.Count,
            UnassignedTasks = tasks.Count(x => !x.AssignedUserId.HasValue && x.Status is not TaskStatus.Completed and not TaskStatus.Cancelled),
            AverageCompletionMinutes = completedTasks.Count == 0
                ? 0
                : completedTasks.Average(x => (x.CompletedAtUtc!.Value - x.CreatedAtUtc).TotalMinutes),
            TasksByCategory = tasks.GroupBy(x => x.Category)
                .Select(g => new MetricBreakdownItemDto { Label = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            TasksByPriority = tasks.GroupBy(x => x.Priority)
                .Select(g => new MetricBreakdownItemDto { Label = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            CompletedPerUser = completedTasks
                .GroupBy(x => x.ClaimedByUserId ?? x.AssignedUserId)
                .Select(g =>
                {
                    var user = users.FirstOrDefault(x => x.Id == g.Key);
                    return new UserPerformanceDto
                    {
                        UserId = g.Key,
                        UserDisplayName = user?.DisplayName ?? "Unassigned",
                        CompletedCount = g.Count()
                    };
                })
                .OrderByDescending(x => x.CompletedCount)
                .ToList()
        };
    }

    public async Task<string> ExportCsvAsync(ManagerMetricsQuery query, CancellationToken cancellationToken)
    {
        var metrics = await GetMetricsAsync(query, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("Section,Label,Count");
        foreach (var item in metrics.TasksByCategory)
        {
            builder.AppendLine(FormattableString.Invariant($"Category,{item.Label},{item.Count}"));
        }

        foreach (var item in metrics.TasksByPriority)
        {
            builder.AppendLine(FormattableString.Invariant($"Priority,{item.Label},{item.Count}"));
        }

        foreach (var item in metrics.CompletedPerUser)
        {
            builder.AppendLine(FormattableString.Invariant($"CompletedPerUser,{item.UserDisplayName},{item.CompletedCount}"));
        }

        builder.AppendLine(FormattableString.Invariant($"Summary,TotalOpenTasks,{metrics.TotalOpenTasks}"));
        builder.AppendLine(FormattableString.Invariant($"Summary,OverdueTasks,{metrics.OverdueTasks}"));
        builder.AppendLine(FormattableString.Invariant($"Summary,CompletedInRange,{metrics.CompletedInRange}"));
        builder.AppendLine(FormattableString.Invariant($"Summary,UnassignedTasks,{metrics.UnassignedTasks}"));
        builder.AppendLine(FormattableString.Invariant($"Summary,AverageCompletionMinutes,{metrics.AverageCompletionMinutes.ToString("F2", CultureInfo.InvariantCulture)}"));
        return builder.ToString();
    }

    private static (DateTime fromUtc, DateTime toUtc) ResolveRange(ManagerMetricsQuery query)
    {
        var toUtc = query.ToUtc ?? DateTime.UtcNow;
        if (query.FromUtc.HasValue)
        {
            return (query.FromUtc.Value, toUtc);
        }

        var presetDays = query.PresetDays.GetValueOrDefault(7);
        return (toUtc.AddDays(-Math.Max(1, presetDays)), toUtc);
    }
}
