using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Task_Reminder.Wpf.Models;
using Task_Reminder.Wpf.Notifications;
using Task_Reminder.Wpf.Services;
using Task_Reminder.Wpf.ViewModels;
using Task_Reminder.Wpf.Views;

namespace Task_Reminder.Wpf;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<ClientOptions>(context.Configuration.GetSection(ClientOptions.SectionName));
                services.AddHttpClient<ITaskReminderApiClient, TaskReminderApiClient>();
                services.AddSingleton<ISignalRTaskUpdatesClient, SignalRTaskUpdatesClient>();
                services.AddSingleton<IToastNotificationService, ToastNotificationService>();
                services.AddSingleton<SessionState>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<MainWindow>();
                services.AddTransient<TaskEditorWindow>();
                services.AddTransient<AssignTaskWindow>();
                services.AddTransient<SnoozeTaskWindow>();
            })
            .Build();

        await _host.StartAsync();

        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        if (loginWindow.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
