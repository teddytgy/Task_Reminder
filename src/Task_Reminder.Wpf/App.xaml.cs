using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task_Reminder.Wpf.Logging;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Notifications;
using Task_Reminder.Wpf.Services;
using Task_Reminder.Wpf.ViewModels;
using Task_Reminder.Wpf.Views;

namespace Task_Reminder.Wpf;

public partial class App : Application
{
    private IHost? _host;
    private ILogger<App>? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<ClientOptions>(context.Configuration.GetSection(ClientOptions.SectionName));
                    services.Configure<FileLoggingOptions>(context.Configuration.GetSection("Logging:File"));
                    services.AddSingleton<LocalCertificateValidator>();
                    services.AddHttpClient<ITaskReminderApiClient, TaskReminderApiClient>()
                        .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                        {
                            var validator = serviceProvider.GetRequiredService<LocalCertificateValidator>();
                            return CreateHttpMessageHandler(validator);
                        });
                    services.AddSingleton<ISignalRTaskUpdatesClient, SignalRTaskUpdatesClient>();
                    services.AddSingleton<IToastNotificationService, ToastNotificationService>();
                    services.AddSingleton<SessionState>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<NotificationSettingsViewModel>();
                    services.AddTransient<RecurringTasksViewModel>();
                    services.AddTransient<ManagerDashboardViewModel>();
                    services.AddTransient<TaskCommentViewModel>();
                    services.AddTransient<ContactLogViewModel>();
                    services.AddTransient<AppointmentBoardViewModel>();
                    services.AddTransient<InsuranceQueueViewModel>();
                    services.AddTransient<OperationsBoardsViewModel>();
                    services.AddTransient<OfficeSettingsViewModel>();
                    services.AddTransient<ImportDataViewModel>();
                    services.AddTransient<AdminOperationsViewModel>();
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<MainWindow>();
                    services.AddTransient<TaskEditorWindow>();
                    services.AddTransient<AssignTaskWindow>();
                    services.AddTransient<SnoozeTaskWindow>();
                    services.AddTransient<NotificationSettingsWindow>();
                    services.AddTransient<RecurringTasksWindow>();
                    services.AddTransient<ManagerDashboardWindow>();
                    services.AddTransient<TaskCommentWindow>();
                    services.AddTransient<ContactLogWindow>();
                    services.AddTransient<AppointmentBoardWindow>();
                    services.AddTransient<InsuranceQueueWindow>();
                    services.AddTransient<OperationsBoardsWindow>();
                    services.AddTransient<OfficeSettingsWindow>();
                    services.AddTransient<ImportDataWindow>();
                    services.AddTransient<AdminOperationsWindow>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddProvider(new FileLoggerProvider(new Microsoft.Extensions.Options.OptionsWrapper<FileLoggingOptions>(
                        context.Configuration.GetSection("Logging:File").Get<FileLoggingOptions>() ?? new FileLoggingOptions())));
                })
                .Build();

            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            RegisterExceptionHandlers();

            _logger.LogInformation("Starting WPF client in {EnvironmentName}.", _host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
            await _host.StartAsync();

            var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
            if (loginWindow.ShowDialog() != true)
            {
                _logger.LogInformation("Login was cancelled before main window opened.");
                Shutdown();
                return;
            }

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "WPF client failed during startup.");
            MessageBox.Show(
                "The Task Reminder desktop app could not start. Check that the API is available and review the WPF log file for details.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _logger?.LogInformation("Stopping WPF client.");
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            _logger?.LogError(args.Exception, "Unhandled dispatcher exception.");
            MessageBox.Show(
                "An unexpected desktop app error occurred. Review the WPF log file for details.",
                "Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                _logger?.LogCritical(exception, "Unhandled AppDomain exception.");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger?.LogError(args.Exception, "Unobserved task exception.");
            args.SetObserved();
        };
    }

    private static HttpMessageHandler CreateHttpMessageHandler(LocalCertificateValidator validator)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = validator.Validate;
        return handler;
    }
}
