using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class ImportService(
    TaskReminderDbContext dbContext,
    IWorkflowAutomationService workflowAutomationService,
    IAuditService auditService,
    ILogger<ImportService> logger) : IImportService
{
    public async Task<ImportResultDto> ImportAppointmentsAsync(ImportAppointmentsRequest request, CancellationToken cancellationToken)
    {
        var created = 0;
        var skipped = 0;
        var messages = new List<string>();
        var items = ParseAppointments(request);

        foreach (var item in items)
        {
            var sourceSystem = item.SourceSystem ?? request.SourceSystem;
            var existing = await dbContext.AppointmentWorkItems.FirstOrDefaultAsync(x =>
                (!string.IsNullOrWhiteSpace(sourceSystem) && !string.IsNullOrWhiteSpace(item.SourceReference) && x.SourceSystem == sourceSystem && x.SourceReference == item.SourceReference) ||
                (x.PatientReference == item.PatientReference && x.AppointmentDateLocal == item.AppointmentDateLocal && x.AppointmentTimeLocal == item.AppointmentTimeLocal),
                cancellationToken);

            if (existing is not null)
            {
                skipped++;
                continue;
            }

            var entity = new AppointmentWorkItem
            {
                Id = Guid.NewGuid(),
                PatientName = item.PatientName.Trim(),
                PatientReference = item.PatientReference.Trim(),
                AppointmentDateLocal = item.AppointmentDateLocal,
                AppointmentTimeLocal = item.AppointmentTimeLocal,
                ProviderName = item.ProviderName?.Trim(),
                AppointmentType = item.AppointmentType.Trim(),
                Status = item.Status,
                ConfirmationStatus = item.ConfirmationStatus,
                InsuranceStatus = item.InsuranceStatus,
                BalanceStatus = item.BalanceStatus,
                Notes = item.Notes?.Trim(),
                SourceSystem = sourceSystem,
                SourceReference = item.SourceReference?.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            dbContext.AppointmentWorkItems.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            await workflowAutomationService.EnsureAppointmentTasksAsync(entity.Id, cancellationToken);
            created++;
        }

        messages.Add($"Created {created} appointment records.");
        messages.Add($"Skipped {skipped} duplicate appointment records.");
        logger.LogInformation("Imported {CreatedCount} appointments and skipped {SkippedCount}.", created, skipped);
        await auditService.WriteAsync("AppointmentImport", null, "Imported", "Imported appointment workflow data.", string.Join(" ", messages), null, cancellationToken);
        return new ImportResultDto { CreatedCount = created, SkippedCount = skipped, Messages = messages };
    }

    public async Task<ImportResultDto> ImportInsuranceAsync(ImportInsuranceWorkItemsRequest request, CancellationToken cancellationToken)
    {
        var created = 0;
        var skipped = 0;
        var messages = new List<string>();
        var items = ParseInsurance(request);

        foreach (var item in items)
        {
            var sourceSystem = item.SourceSystem ?? request.SourceSystem;
            var existing = await dbContext.InsuranceWorkItems.FirstOrDefaultAsync(x =>
                (!string.IsNullOrWhiteSpace(sourceSystem) && !string.IsNullOrWhiteSpace(item.SourceReference) && x.SourceSystem == sourceSystem && x.SourceReference == item.SourceReference) ||
                (x.PatientReference == item.PatientReference && x.AppointmentDateLocal == item.AppointmentDateLocal && x.CarrierName == item.CarrierName),
                cancellationToken);

            if (existing is not null)
            {
                skipped++;
                continue;
            }

            var entity = new InsuranceWorkItem
            {
                Id = Guid.NewGuid(),
                PatientName = item.PatientName.Trim(),
                PatientReference = item.PatientReference.Trim(),
                CarrierName = item.CarrierName?.Trim(),
                PlanName = item.PlanName?.Trim(),
                MemberId = item.MemberId?.Trim(),
                GroupNumber = item.GroupNumber?.Trim(),
                PayerId = item.PayerId?.Trim(),
                AppointmentDateLocal = item.AppointmentDateLocal,
                VerificationStatus = item.VerificationStatus,
                EligibilityStatus = item.EligibilityStatus,
                VerificationMethod = item.VerificationMethod,
                CopayAmount = item.CopayAmount,
                DeductibleAmount = item.DeductibleAmount,
                AnnualMaximum = item.AnnualMaximum,
                RemainingMaximum = item.RemainingMaximum,
                FrequencyNotes = item.FrequencyNotes?.Trim(),
                WaitingPeriodNotes = item.WaitingPeriodNotes?.Trim(),
                MissingInfoNotes = item.MissingInfoNotes?.Trim(),
                IssueType = item.IssueType,
                Notes = item.Notes?.Trim(),
                SourceSystem = sourceSystem,
                SourceReference = item.SourceReference?.Trim(),
                AppointmentWorkItemId = item.AppointmentWorkItemId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            dbContext.InsuranceWorkItems.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            await workflowAutomationService.EnsureInsuranceTasksAsync(entity.Id, cancellationToken);
            created++;
        }

        messages.Add($"Created {created} insurance work records.");
        messages.Add($"Skipped {skipped} duplicate insurance records.");
        logger.LogInformation("Imported {CreatedCount} insurance work items and skipped {SkippedCount}.", created, skipped);
        await auditService.WriteAsync("InsuranceImport", null, "Imported", "Imported insurance workflow data.", string.Join(" ", messages), null, cancellationToken);
        return new ImportResultDto { CreatedCount = created, SkippedCount = skipped, Messages = messages };
    }

    private static IReadOnlyList<CreateAppointmentWorkItemRequest> ParseAppointments(ImportAppointmentsRequest request)
    {
        return request.Format switch
        {
            ImportFormat.Json => JsonSerializer.Deserialize<List<CreateAppointmentWorkItemRequest>>(request.Content, JsonOptions()) ?? [],
            ImportFormat.Csv => ParseAppointmentsCsv(request.Content),
            _ => []
        };
    }

    private static IReadOnlyList<CreateInsuranceWorkItemRequest> ParseInsurance(ImportInsuranceWorkItemsRequest request)
    {
        return request.Format switch
        {
            ImportFormat.Json => JsonSerializer.Deserialize<List<CreateInsuranceWorkItemRequest>>(request.Content, JsonOptions()) ?? [],
            ImportFormat.Csv => ParseInsuranceCsv(request.Content),
            _ => []
        };
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private static IReadOnlyList<CreateAppointmentWorkItemRequest> ParseAppointmentsCsv(string content)
    {
        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return [];
        }

        var results = new List<CreateAppointmentWorkItemRequest>();
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',');
            if (parts.Length < 6)
            {
                continue;
            }

            results.Add(new CreateAppointmentWorkItemRequest
            {
                PatientName = parts[0],
                PatientReference = parts[1],
                AppointmentDateLocal = DateOnly.Parse(parts[2], CultureInfo.InvariantCulture),
                AppointmentTimeLocal = TimeSpan.Parse(parts[3], CultureInfo.InvariantCulture),
                ProviderName = parts[4],
                AppointmentType = parts[5],
                Notes = parts.Length > 6 ? parts[6] : null,
                SourceReference = parts.Length > 7 ? parts[7] : null
            });
        }

        return results;
    }

    private static IReadOnlyList<CreateInsuranceWorkItemRequest> ParseInsuranceCsv(string content)
    {
        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return [];
        }

        var results = new List<CreateInsuranceWorkItemRequest>();
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',');
            if (parts.Length < 4)
            {
                continue;
            }

            results.Add(new CreateInsuranceWorkItemRequest
            {
                PatientName = parts[0],
                PatientReference = parts[1],
                CarrierName = parts[2],
                PlanName = parts[3],
                AppointmentDateLocal = parts.Length > 4 && DateOnly.TryParse(parts[4], CultureInfo.InvariantCulture, out var date) ? date : null,
                MemberId = parts.Length > 5 ? parts[5] : null,
                GroupNumber = parts.Length > 6 ? parts[6] : null,
                SourceReference = parts.Length > 7 ? parts[7] : null
            });
        }

        return results;
    }
}
