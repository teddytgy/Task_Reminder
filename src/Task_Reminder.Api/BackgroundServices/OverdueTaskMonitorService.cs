using Task_Reminder.Api.Domain.Services;

namespace Task_Reminder.Api.BackgroundServices;

public sealed class OverdueTaskMonitorService(
    IServiceProvider serviceProvider,
    ILogger<OverdueTaskMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Overdue task monitor started.");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
                var updatedCount = await taskService.MarkOverdueTasksAsync(stoppingToken);
                if (updatedCount > 0)
                {
                    logger.LogInformation("Marked {UpdatedCount} tasks as overdue.", updatedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed while processing overdue tasks.");
            }
        }

        logger.LogInformation("Overdue task monitor stopped.");
    }
}
