using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Task_Reminder.Api.BackgroundServices;
using Task_Reminder.Api.Configuration;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Hubs;
using Task_Reminder.Api.Infrastructure.Seed;
using Task_Reminder.Api.Infrastructure.Services;
using Task_Reminder.Api.Logging;
using Task_Reminder.Api.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiStartupOptions>(builder.Configuration.GetSection(ApiStartupOptions.SectionName));
builder.Services.Configure<DeploymentOptions>(builder.Configuration.GetSection(DeploymentOptions.SectionName));
builder.Services.Configure<FileLoggingOptions>(builder.Configuration.GetSection("Logging:File"));
builder.Logging.AddProvider(new FileLoggerProvider(new OptionsWrapper<FileLoggingOptions>(
    builder.Configuration.GetSection("Logging:File").Get<FileLoggingOptions>() ?? new FileLoggingOptions())));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddDbContext<TaskReminderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TaskReminder")));
builder.Services.AddScoped<IRequestUserContextAccessor, RequestUserContextAccessor>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IRecurringTaskService, RecurringTaskService>();
builder.Services.AddScoped<IManagerReportService, ManagerReportService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IInsuranceWorkService, InsuranceWorkService>();
builder.Services.AddScoped<IBalanceFollowUpService, BalanceFollowUpService>();
builder.Services.AddScoped<IContactLogService, ContactLogService>();
builder.Services.AddScoped<IOfficeSettingsService, OfficeSettingsService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IOperationsBoardService, OperationsBoardService>();
builder.Services.AddScoped<IWorkflowAutomationService, WorkflowAutomationService>();
builder.Services.AddScoped<IExternalIntegrationService, ExternalIntegrationService>();
builder.Services.AddScoped<ISystemInfoService, SystemInfoService>();
builder.Services.AddSingleton<IExternalAppointmentSyncProvider, DisabledExternalAppointmentSyncProvider>();
builder.Services.AddSingleton<IExternalInsuranceVerificationProvider, DisabledExternalInsuranceVerificationProvider>();
builder.Services.AddSingleton<IExternalPatientCommunicationProvider, DisabledExternalPatientCommunicationProvider>();
builder.Services.AddScoped<TaskBroadcastService>();
builder.Services.AddHostedService<OverdueTaskMonitorService>();
builder.Services.AddHostedService<RecurringTaskGenerationService>();
builder.Services.AddHostedService<OperationalWorkflowMonitorService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DesktopClient", policy =>
    {
        policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("DesktopClient");
app.UseHttpsRedirection();
app.UseMiddleware<RequestUserContextMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TaskUpdatesHub>(TaskUpdatesHub.HubPath);
app.MapGet("/health", async (TaskReminderDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "Healthy", database = "Healthy", timestampUtc = DateTime.UtcNow })
            : Results.Json(new { status = "Unhealthy", database = "Unavailable", timestampUtc = DateTime.UtcNow }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Health check failed while verifying database connectivity.");
        return Results.Json(new { status = "Unhealthy", database = "Unavailable", timestampUtc = DateTime.UtcNow }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskReminderDbContext>();
    var options = scope.ServiceProvider.GetRequiredService<IOptions<ApiStartupOptions>>().Value;

    startupLogger.LogInformation("Starting API in {EnvironmentName}.", app.Environment.EnvironmentName);
    startupLogger.LogInformation(
        "Database initialization settings: RunMigrationsOnStartup={RunMigrationsOnStartup}, SeedDemoDataOnStartup={SeedDemoDataOnStartup}.",
        options.RunMigrationsOnStartup,
        options.SeedDemoDataOnStartup);

    if (options.RunMigrationsOnStartup)
    {
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
        if (pendingMigrations.Count == 0)
        {
            startupLogger.LogInformation("No pending database migrations were found.");
        }
        else
        {
            startupLogger.LogInformation("Applying {PendingMigrationCount} pending migrations: {PendingMigrations}.", pendingMigrations.Count, pendingMigrations);
        }

        await dbContext.Database.MigrateAsync();
        startupLogger.LogInformation("Database migration check completed.");
    }
    else
    {
        startupLogger.LogWarning("Database migrations were skipped because App:RunMigrationsOnStartup is disabled.");
    }

    if (options.SeedDemoDataOnStartup)
    {
        var seeded = await DemoDataSeeder.SeedAsync(dbContext, CancellationToken.None);
        startupLogger.LogInformation(seeded ? "Demo data was seeded." : "Demo data seeding was skipped because users already exist.");
    }
    else
    {
        startupLogger.LogInformation("Demo data seeding is disabled.");
    }
}
catch (Exception ex)
{
    startupLogger.LogCritical(ex, "API startup failed during database initialization.");
    throw;
}

startupLogger.LogInformation("API startup complete. Swagger is available at /swagger and health checks at /health.");
app.Run();

public partial class Program;
