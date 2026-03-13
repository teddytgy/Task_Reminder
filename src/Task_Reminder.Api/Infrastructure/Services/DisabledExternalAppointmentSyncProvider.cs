using Task_Reminder.Api.Domain.Services;

namespace Task_Reminder.Api.Infrastructure.Services;

public sealed class DisabledExternalAppointmentSyncProvider : IExternalAppointmentSyncProvider
{
    public string ProviderName => "Open Dental Appointment Stub";

    public Task<string> RunAsync(CancellationToken cancellationToken) =>
        Task.FromResult("Open Dental appointment sync is scaffolded but disabled. Use imports or add a real provider implementation.");
}
