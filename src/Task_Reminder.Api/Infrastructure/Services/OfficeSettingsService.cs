using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class OfficeSettingsService(
    TaskReminderDbContext dbContext,
    IAuditService auditService) : IOfficeSettingsService
{
    public async Task<OfficeSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<OfficeSettingsDto> UpdateAsync(UpdateOfficeSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        settings.OfficeName = request.OfficeName.Trim();
        settings.BusinessHoursSummary = request.BusinessHoursSummary.Trim();
        settings.ConfirmationLeadHours = Math.Max(1, request.ConfirmationLeadHours);
        settings.InsuranceVerificationLeadDays = Math.Max(0, request.InsuranceVerificationLeadDays);
        settings.OverdueEscalationMinutes = Math.Max(5, request.OverdueEscalationMinutes);
        settings.NoShowFollowUpDelayHours = Math.Max(1, request.NoShowFollowUpDelayHours);
        settings.ManagerEscalationUserId = request.ManagerEscalationUserId;
        settings.DefaultReminderIntervalMinutes = Math.Max(5, request.DefaultReminderIntervalMinutes);
        settings.TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId) ? settings.TimeZoneId : request.TimeZoneId.Trim();
        settings.EnableTodayBoard = request.EnableTodayBoard;
        settings.EnableTomorrowPrepBoard = request.EnableTomorrowPrepBoard;
        settings.EnableCollectionsBoard = request.EnableCollectionsBoard;
        settings.EnableRecallBoard = request.EnableRecallBoard;
        settings.EnableManagerQueue = request.EnableManagerQueue;
        settings.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("OfficeSettings", settings.Id, "Updated", $"Updated office settings for {settings.OfficeName}.", settings.BusinessHoursSummary, null, cancellationToken);
        return Map(settings);
    }

    private async Task<OfficeSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.OfficeSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new OfficeSettings
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.OfficeSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync("OfficeSettings", settings.Id, "Created", "Created office settings record.", settings.OfficeName, null, cancellationToken);
        return settings;
    }

    public static OfficeSettingsDto Map(OfficeSettings settings) => new()
    {
        Id = settings.Id,
        OfficeName = settings.OfficeName,
        BusinessHoursSummary = settings.BusinessHoursSummary,
        ConfirmationLeadHours = settings.ConfirmationLeadHours,
        InsuranceVerificationLeadDays = settings.InsuranceVerificationLeadDays,
        OverdueEscalationMinutes = settings.OverdueEscalationMinutes,
        NoShowFollowUpDelayHours = settings.NoShowFollowUpDelayHours,
        ManagerEscalationUserId = settings.ManagerEscalationUserId,
        DefaultReminderIntervalMinutes = settings.DefaultReminderIntervalMinutes,
        TimeZoneId = settings.TimeZoneId,
        EnableTodayBoard = settings.EnableTodayBoard,
        EnableTomorrowPrepBoard = settings.EnableTomorrowPrepBoard,
        EnableCollectionsBoard = settings.EnableCollectionsBoard,
        EnableRecallBoard = settings.EnableRecallBoard,
        EnableManagerQueue = settings.EnableManagerQueue,
        UpdatedAtUtc = settings.UpdatedAtUtc
    };
}
