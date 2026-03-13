using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class ExternalIntegrationService(
    TaskReminderDbContext dbContext,
    IAuditService auditService,
    IExternalAppointmentSyncProvider appointmentProvider,
    IExternalInsuranceVerificationProvider insuranceProvider,
    IExternalPatientCommunicationProvider patientCommunicationProvider,
    ILogger<ExternalIntegrationService> logger) : IExternalIntegrationService
{
    public async Task<IReadOnlyList<ExternalIntegrationProviderStatusDto>> ListAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var providers = await dbContext.ExternalIntegrationProviderConfigs
            .AsNoTracking()
            .Include(x => x.Runs.OrderByDescending(r => r.StartedAtUtc).Take(1))
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return providers.Select(Map).ToList();
    }

    public async Task<ExternalIntegrationProviderStatusDto?> UpdateAsync(Guid id, UpdateExternalIntegrationProviderRequest request, CancellationToken cancellationToken)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var provider = await dbContext.ExternalIntegrationProviderConfigs
            .Include(x => x.Runs.OrderByDescending(r => r.StartedAtUtc).Take(1))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (provider is null)
        {
            return null;
        }

        provider.IsEnabled = request.IsEnabled;
        provider.BaseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? null : request.BaseUrl.Trim();
        provider.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        provider.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ExternalIntegrationProvider",
            provider.Id,
            "Updated",
            $"Updated integration provider {provider.DisplayName}.",
            provider.Notes,
            null,
            cancellationToken);

        return Map(provider);
    }

    public async Task<ExternalIntegrationProviderStatusDto?> RunAsync(Guid id, RunExternalIntegrationRequest request, CancellationToken cancellationToken)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var provider = await dbContext.ExternalIntegrationProviderConfigs
            .Include(x => x.Runs.OrderByDescending(r => r.StartedAtUtc).Take(1))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (provider is null)
        {
            return null;
        }

        var run = new ExternalIntegrationRun
        {
            Id = Guid.NewGuid(),
            ProviderConfigId = provider.Id,
            Status = provider.IsEnabled ? ExternalIntegrationRunStatus.Pending : ExternalIntegrationRunStatus.Disabled,
            StartedAtUtc = DateTime.UtcNow,
            Message = request.Notes
        };

        dbContext.ExternalIntegrationRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!provider.IsEnabled)
        {
            run.CompletedAtUtc = DateTime.UtcNow;
            run.Message = "Integration is disabled.";
            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync("ExternalIntegrationRun", run.Id, "RunSkipped", $"Skipped disabled integration {provider.DisplayName}.", run.Message, null, cancellationToken);
            provider.Runs = [run];
            return Map(provider);
        }

        try
        {
            run.Message = await ExecuteProviderAsync(provider.ProviderType, cancellationToken);
            run.Status = ExternalIntegrationRunStatus.Success;
            run.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Executed external integration provider {ProviderType}.", provider.ProviderType);
            await auditService.WriteAsync("ExternalIntegrationRun", run.Id, "RunSucceeded", $"Executed integration {provider.DisplayName}.", run.Message, null, cancellationToken);
        }
        catch (Exception ex)
        {
            run.Status = ExternalIntegrationRunStatus.Failed;
            run.Message = ex.Message;
            run.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogError(ex, "External integration provider {ProviderType} failed.", provider.ProviderType);
            await auditService.WriteAsync("ExternalIntegrationRun", run.Id, "RunFailed", $"Integration {provider.DisplayName} failed.", ex.Message, null, cancellationToken);
        }

        provider = await dbContext.ExternalIntegrationProviderConfigs
            .AsNoTracking()
            .Include(x => x.Runs.OrderByDescending(r => r.StartedAtUtc).Take(1))
            .FirstAsync(x => x.Id == id, cancellationToken);
        return Map(provider);
    }

    private async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.ExternalIntegrationProviderConfigs.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        dbContext.ExternalIntegrationProviderConfigs.AddRange(
            new ExternalIntegrationProviderConfig
            {
                Id = Guid.NewGuid(),
                ProviderType = ExternalIntegrationProviderType.OpenDentalAppointments,
                DisplayName = "Open Dental Appointment Sync",
                IsEnabled = false,
                Notes = "Scaffolded placeholder for future appointment sync.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new ExternalIntegrationProviderConfig
            {
                Id = Guid.NewGuid(),
                ProviderType = ExternalIntegrationProviderType.PatientXpressInsurance,
                DisplayName = "PatientXpress Insurance Sync",
                IsEnabled = false,
                Notes = "Scaffolded placeholder for future insurance verification sync.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new ExternalIntegrationProviderConfig
            {
                Id = Guid.NewGuid(),
                ProviderType = ExternalIntegrationProviderType.CsvManualImport,
                DisplayName = "CSV / Manual Import Provider",
                IsEnabled = false,
                Notes = "Placeholder for scheduled import workflows.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new ExternalIntegrationProviderConfig
            {
                Id = Guid.NewGuid(),
                ProviderType = ExternalIntegrationProviderType.PatientCommunication,
                DisplayName = "Patient Communication Provider",
                IsEnabled = false,
                Notes = "Scaffolded placeholder for SMS or email delivery integrations.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ExecuteProviderAsync(ExternalIntegrationProviderType providerType, CancellationToken cancellationToken) =>
        providerType switch
        {
            ExternalIntegrationProviderType.OpenDentalAppointments => await appointmentProvider.RunAsync(cancellationToken),
            ExternalIntegrationProviderType.PatientXpressInsurance => await insuranceProvider.RunAsync(cancellationToken),
            ExternalIntegrationProviderType.PatientCommunication => await patientCommunicationProvider.RunAsync(cancellationToken),
            ExternalIntegrationProviderType.CsvManualImport => "CSV/manual provider scaffold is active. Use import endpoints or scheduled file drops in a future pass.",
            _ => "No integration provider implementation is registered."
        };

    private static ExternalIntegrationProviderStatusDto Map(ExternalIntegrationProviderConfig provider)
    {
        var run = provider.Runs.OrderByDescending(x => x.StartedAtUtc).FirstOrDefault();
        return new ExternalIntegrationProviderStatusDto
        {
            Id = provider.Id,
            ProviderType = provider.ProviderType,
            DisplayName = provider.DisplayName,
            IsEnabled = provider.IsEnabled,
            BaseUrl = provider.BaseUrl,
            Notes = provider.Notes,
            LastRunStartedAtUtc = run?.StartedAtUtc,
            LastRunCompletedAtUtc = run?.CompletedAtUtc,
            LastRunStatus = run?.Status,
            LastRunMessage = run?.Message
        };
    }
}
