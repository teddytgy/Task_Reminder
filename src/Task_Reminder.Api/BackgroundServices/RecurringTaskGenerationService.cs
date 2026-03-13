using Task_Reminder.Api.Domain.Services;

namespace Task_Reminder.Api.BackgroundServices;

public sealed class RecurringTaskGenerationService(
    IServiceProvider serviceProvider,
    ILogger<RecurringTaskGenerationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Recurring task generation service started.");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var recurringTaskService = scope.ServiceProvider.GetRequiredService<IRecurringTaskService>();
                var generated = await recurringTaskService.GenerateDueTasksAsync(stoppingToken);
                if (generated > 0)
                {
                    logger.LogInformation("Generated {GeneratedCount} recurring tasks.", generated);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Recurring task generation failed.");
            }
        }

        logger.LogInformation("Recurring task generation service stopped.");
    }
}
