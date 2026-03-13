using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class OperationsBoardService(
    TaskReminderDbContext dbContext,
    IAppointmentService appointmentService,
    IInsuranceWorkService insuranceWorkService,
    IBalanceFollowUpService balanceFollowUpService,
    IManagerReportService managerReportService) : IOperationsBoardService
{
    public async Task<OperationsBoardDto> GetBoardAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);

        return new OperationsBoardDto
        {
            TodayAppointments = await appointmentService.ListAsync(new AppointmentQueryParameters { Filter = "today" }, cancellationToken),
            TomorrowAppointments = await appointmentService.ListAsync(new AppointmentQueryParameters { Filter = "tomorrow" }, cancellationToken),
            UnconfirmedAppointments = await appointmentService.ListAsync(new AppointmentQueryParameters { Filter = "unconfirmed" }, cancellationToken),
            InsurancePendingItems = await insuranceWorkService.ListAsync(new InsuranceQueryParameters { Filter = "verification-pending" }, cancellationToken),
            BalanceDueItems = await balanceFollowUpService.ListAsync(new BalanceQueryParameters { Filter = "balance-due" }, cancellationToken),
            OverdueTasks = await dbContext.Tasks
                .AsNoTracking()
                .Include(x => x.AssignedUser)
                .Include(x => x.ClaimedByUser)
                .Include(x => x.CreatedByUser)
                .Where(x => x.Status == TaskStatus.Overdue)
                .OrderBy(x => x.DueAtUtc)
                .Take(50)
                .Select(x => TaskService.MapTask(x, DateTime.UtcNow))
                .ToListAsync(cancellationToken),
            RecallCandidates = await appointmentService.ListAsync(new AppointmentQueryParameters { Filter = "no-show-cancelled" }, cancellationToken),
            NoShowOrCancelledAppointments = await appointmentService.ListAsync(new AppointmentQueryParameters { Filter = "no-show-cancelled" }, cancellationToken),
            UnresolvedInsuranceIssues = await insuranceWorkService.ListAsync(new InsuranceQueryParameters { Filter = "issue-found" }, cancellationToken),
            ManagerEscalations = await dbContext.Tasks
                .AsNoTracking()
                .Include(x => x.AssignedUser)
                .Include(x => x.ClaimedByUser)
                .Include(x => x.CreatedByUser)
                .Where(x => x.EscalatedAtUtc.HasValue || x.Priority == TaskPriority.Urgent)
                .OrderByDescending(x => x.EscalatedAtUtc ?? x.UpdatedAtUtc)
                .Take(50)
                .Select(x => TaskService.MapTask(x, DateTime.UtcNow))
                .ToListAsync(cancellationToken),
            WorkloadByUser = await GetWorkloadAsync(cancellationToken)
        };
    }

    public async Task<IReadOnlyList<UserWorkloadDto>> GetWorkloadAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
        var tasks = await dbContext.Tasks.AsNoTracking().ToListAsync(cancellationToken);
        var todayUtc = DateTime.UtcNow.Date;
        return users.Select(user =>
        {
            var owned = tasks.Where(x => x.AssignedUserId == user.Id || x.ClaimedByUserId == user.Id).ToList();
            var completed = owned.Where(x => x.CompletedAtUtc.HasValue && x.CompletedAtUtc.Value >= todayUtc).ToList();
            return new UserWorkloadDto
            {
                UserId = user.Id,
                UserDisplayName = user.DisplayName,
                Role = user.Role,
                OpenItems = owned.Count(x => x.Status is not TaskStatus.Completed and not TaskStatus.Cancelled),
                OverdueItems = owned.Count(x => x.Status == TaskStatus.Overdue),
                CompletedToday = completed.Count,
                AverageCompletionMinutes = completed.Count == 0 ? 0 : completed.Average(x => (x.CompletedAtUtc!.Value - x.CreatedAtUtc).TotalMinutes)
            };
        }).OrderByDescending(x => x.OpenItems).ToList();
    }

    public async Task<IReadOnlyList<UserActivityTimelineItemDto>> GetUserActivityAsync(Guid userId, CancellationToken cancellationToken)
    {
        var taskHistory = await dbContext.TaskHistory
            .AsNoTracking()
            .Where(x => x.PerformedByUserId == userId)
            .Select(x => new UserActivityTimelineItemDto
            {
                OccurredAtUtc = x.PerformedAtUtc,
                ActivityType = "Task",
                Summary = $"{x.ActionType}: {x.Details}"
            })
            .ToListAsync(cancellationToken);

        var contactHistory = await dbContext.ContactLogs
            .AsNoTracking()
            .Where(x => x.PerformedByUserId == userId)
            .Select(x => new UserActivityTimelineItemDto
            {
                OccurredAtUtc = x.PerformedAtUtc,
                ActivityType = "Contact",
                Summary = $"{x.ContactType} - {x.Outcome}: {x.Notes}"
            })
            .ToListAsync(cancellationToken);

        return taskHistory.Concat(contactHistory)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(100)
            .ToList();
    }

    public async Task<OperationsKpiDto> GetKpisAsync(ManagerMetricsQuery query, CancellationToken cancellationToken)
    {
        var toUtc = query.ToUtc ?? DateTime.UtcNow;
        var fromUtc = query.FromUtc ?? toUtc.AddDays(-(query.PresetDays ?? 7));
        var appointments = await dbContext.AppointmentWorkItems.AsNoTracking().Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc).ToListAsync(cancellationToken);
        var insurance = await dbContext.InsuranceWorkItems.AsNoTracking().Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc).ToListAsync(cancellationToken);
        var tasks = await dbContext.Tasks.AsNoTracking().Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc).ToListAsync(cancellationToken);
        var contactLogs = await dbContext.ContactLogs.AsNoTracking().Where(x => x.PerformedAtUtc >= fromUtc && x.PerformedAtUtc <= toUtc).ToListAsync(cancellationToken);
        var users = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);

        var totalAppointments = Math.Max(1, appointments.Count);
        var totalInsurance = Math.Max(1, insurance.Count);
        var totalTaskCompletions = tasks.Count(x => x.CompletedAtUtc.HasValue);

        return new OperationsKpiDto
        {
            RangeStartUtc = fromUtc,
            RangeEndUtc = toUtc,
            AppointmentConfirmationRate = 100.0 * appointments.Count(x => x.ConfirmationStatus == AppointmentConfirmationStatus.Confirmed) / totalAppointments,
            NoShowRate = 100.0 * appointments.Count(x => x.Status == AppointmentStatus.NoShow) / totalAppointments,
            CancellationRate = 100.0 * appointments.Count(x => x.Status == AppointmentStatus.Cancelled) / totalAppointments,
            InsuranceVerificationCompletionRate = 100.0 * insurance.Count(x => x.VerificationStatus == InsuranceVerificationStatus.Verified) / totalInsurance,
            InsuranceIssueRateByType = insurance.Where(x => x.IssueType.HasValue)
                .GroupBy(x => x.IssueType!.Value.ToString())
                .Select(g => new MetricBreakdownItemDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            BalanceCollectionProgress = await dbContext.BalanceFollowUpWorkItems
                .AsNoTracking()
                .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc <= toUtc)
                .GroupBy(x => x.Status.ToString())
                .Select(g => new MetricBreakdownItemDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken),
            AverageTaskCompletionTimeMinutes = totalTaskCompletions == 0 ? 0 : tasks.Where(x => x.CompletedAtUtc.HasValue).Average(x => (x.CompletedAtUtc!.Value - x.CreatedAtUtc).TotalMinutes),
            TaskCompletionCountByUser = tasks.Where(x => x.CompletedAtUtc.HasValue)
                .GroupBy(x => x.ClaimedByUserId ?? x.AssignedUserId)
                .Select(g => new UserPerformanceDto
                {
                    UserId = g.Key,
                    UserDisplayName = users.FirstOrDefault(x => x.Id == g.Key)?.DisplayName ?? "Unassigned",
                    CompletedCount = g.Count()
                })
                .OrderByDescending(x => x.CompletedCount)
                .ToList(),
            OverdueRateByCategory = tasks.Where(x => x.Status == TaskStatus.Overdue)
                .GroupBy(x => x.Category.ToString())
                .Select(g => new MetricBreakdownItemDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            ContactOutcomeDistribution = contactLogs
                .GroupBy(x => x.Outcome.ToString())
                .Select(g => new MetricBreakdownItemDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList()
        };
    }

    public async Task<string> ExportOperationsCsvAsync(string exportType, ManagerMetricsQuery query, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        switch (exportType.ToLowerInvariant())
        {
            case "appointments":
                builder.AppendLine("PatientName,PatientReference,AppointmentDate,AppointmentTime,Status,ConfirmationStatus,InsuranceStatus,BalanceStatus");
                foreach (var item in await dbContext.AppointmentWorkItems.AsNoTracking().OrderBy(x => x.AppointmentDateLocal).ToListAsync(cancellationToken))
                {
                    builder.AppendLine(FormattableString.Invariant($"{item.PatientName},{item.PatientReference},{item.AppointmentDateLocal},{item.AppointmentTimeLocal},{item.Status},{item.ConfirmationStatus},{item.InsuranceStatus},{item.BalanceStatus}"));
                }
                break;
            case "insurance":
                builder.AppendLine("PatientName,PatientReference,CarrierName,VerificationStatus,EligibilityStatus,IssueType");
                foreach (var item in await dbContext.InsuranceWorkItems.AsNoTracking().OrderBy(x => x.PatientName).ToListAsync(cancellationToken))
                {
                    builder.AppendLine(FormattableString.Invariant($"{item.PatientName},{item.PatientReference},{item.CarrierName},{item.VerificationStatus},{item.EligibilityStatus},{item.IssueType}"));
                }
                break;
            case "balances":
                builder.AppendLine("PatientName,PatientReference,AmountDue,Status,FollowUpDate");
                foreach (var item in await dbContext.BalanceFollowUpWorkItems.AsNoTracking().OrderByDescending(x => x.AmountDue).ToListAsync(cancellationToken))
                {
                    builder.AppendLine(FormattableString.Invariant($"{item.PatientName},{item.PatientReference},{item.AmountDue.ToString("F2", CultureInfo.InvariantCulture)},{item.Status},{item.FollowUpDateLocal}"));
                }
                break;
            case "contacts":
                builder.AppendLine("PerformedAtUtc,ContactType,Outcome,Notes");
                foreach (var item in await dbContext.ContactLogs.AsNoTracking().OrderByDescending(x => x.PerformedAtUtc).ToListAsync(cancellationToken))
                {
                    builder.AppendLine(FormattableString.Invariant($"{item.PerformedAtUtc:O},{item.ContactType},{item.Outcome},{item.Notes}"));
                }
                break;
            default:
                return await managerReportService.ExportCsvAsync(query, cancellationToken);
        }

        return builder.ToString();
    }
}
