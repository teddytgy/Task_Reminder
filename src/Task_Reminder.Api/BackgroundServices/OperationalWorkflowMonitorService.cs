using Task_Reminder.Api.Domain.Services;

namespace Task_Reminder.Api.BackgroundServices;

public sealed class OperationalWorkflowMonitorService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OperationalWorkflowMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Operational workflow monitor started.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var automationService = scope.ServiceProvider.GetRequiredService<IWorkflowAutomationService>();
                await automationService.EnsureOperationalCoverageAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operational workflow monitor encountered an error.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
