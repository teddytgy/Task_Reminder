namespace Task_Reminder.Shared;

public static class RecurringTaskScheduleRules
{
    public static bool ShouldGenerateForDate(
        RecurrenceType recurrenceType,
        int recurrenceInterval,
        string? daysOfWeek,
        int? dayOfMonth,
        DateOnly startDateLocal,
        DateOnly? endDateLocal,
        DateOnly targetDateLocal)
    {
        if (targetDateLocal < startDateLocal || (endDateLocal.HasValue && targetDateLocal > endDateLocal.Value))
        {
            return false;
        }

        var interval = Math.Max(1, recurrenceInterval);
        var daysSinceStart = targetDateLocal.DayNumber - startDateLocal.DayNumber;

        return recurrenceType switch
        {
            RecurrenceType.Daily or RecurrenceType.Custom => daysSinceStart % interval == 0,
            RecurrenceType.Weekdays => targetDateLocal.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
            RecurrenceType.Weekly => daysSinceStart >= 0 &&
                                     (daysSinceStart / 7) % interval == 0 &&
                                     MatchesDaysOfWeek(daysOfWeek, targetDateLocal.DayOfWeek),
            RecurrenceType.Monthly => MatchesMonthlyRule(startDateLocal, dayOfMonth, targetDateLocal, interval),
            _ => false
        };
    }

    private static bool MatchesDaysOfWeek(string? daysOfWeek, DayOfWeek dayOfWeek)
    {
        if (string.IsNullOrWhiteSpace(daysOfWeek))
        {
            return true;
        }

        var values = daysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return values.Any(value => value.Equals(dayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesMonthlyRule(DateOnly startDateLocal, int? dayOfMonth, DateOnly targetDateLocal, int interval)
    {
        var scheduledDay = dayOfMonth ?? startDateLocal.Day;
        if (targetDateLocal.Day != Math.Min(scheduledDay, DateTime.DaysInMonth(targetDateLocal.Year, targetDateLocal.Month)))
        {
            return false;
        }

        var monthDistance = (targetDateLocal.Year - startDateLocal.Year) * 12 + targetDateLocal.Month - startDateLocal.Month;
        return monthDistance >= 0 && monthDistance % interval == 0;
    }
}

public static class EscalationRules
{
    public static bool ShouldEscalate(DateTime dueAtUtc, int? escalateAfterMinutes, DateTime nowUtc, DateTime? escalatedAtUtc)
    {
        if (!escalateAfterMinutes.HasValue || escalateAfterMinutes.Value <= 0 || escalatedAtUtc.HasValue)
        {
            return false;
        }

        return dueAtUtc.AddMinutes(escalateAfterMinutes.Value) <= nowUtc;
    }
}

public static class NotificationRoutingRules
{
    public static bool ShouldNotify(
        Guid currentUserId,
        UserRole currentUserRole,
        UserNotificationPreferencesDto preferences,
        TaskItemDto task,
        bool isRecurringGenerationAlert)
    {
        if (isRecurringGenerationAlert && !preferences.ReceiveRecurringTaskGenerationAlerts)
        {
            return false;
        }

        if (task.ClaimedByUserId.HasValue && task.ClaimedByUserId != currentUserId)
        {
            return false;
        }

        if (task.AssignedUserId.HasValue)
        {
            if (task.AssignedUserId == currentUserId)
            {
                return preferences.ReceiveAssignedTaskReminders;
            }

            return task.EscalatedAtUtc.HasValue &&
                   preferences.ReceiveOverdueEscalationAlerts &&
                   currentUserRole is UserRole.Manager or UserRole.Admin;
        }

        return preferences.ReceiveUnassignedTaskReminders &&
               currentUserRole == UserRole.FrontDesk;
    }
}
