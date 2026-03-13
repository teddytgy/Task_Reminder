# Office Deployment Model

## Recommended production layout

This solution is designed for one shared office backend and multiple front desk workstations:

1. PostgreSQL runs on one stable office machine or server.
2. The API runs on one always-on office PC or server.
3. Every WPF client points to that one shared API.
4. Development continues separately on one or more GitHub-connected dev machines.

## Machine roles

### PostgreSQL host

- Runs the shared `task_reminder` database.
- Should be backed up nightly.
- Needs port `5432` open only to the API host or trusted office network.

### API host

- Runs the published `Task_Reminder.Api`.
- Connects to the shared PostgreSQL database.
- Hosts Swagger, health checks, and SignalR.
- Recommended HTTPS port: `7087`

### WPF client PCs

- Run only the published `Task_Reminder.Wpf` client.
- Do not need local PostgreSQL.
- Point `Client:ApiBaseUrl` and `Client:SignalRHubUrl` to the office API host.

## Recommended office folder structure

### PostgreSQL host

- `D:\Task_Reminder\PostgresData`
- `D:\Task_Reminder\Backups`

### API host

- `C:\OfficeApps\Task_Reminder\Api`
- `C:\OfficeApps\Task_Reminder\Api\logs`
- `C:\OfficeApps\Task_Reminder\Api\backups`

### WPF workstation

- `C:\OfficeApps\Task_Reminder\Client`
- `C:\OfficeApps\Task_Reminder\Client\logs`

## Required URLs and ports

- API HTTPS base URL example: `https://frontdesk-api.office.local:7087/`
- SignalR hub: `https://frontdesk-api.office.local:7087/hubs/tasks`
- Health check: `https://frontdesk-api.office.local:7087/health`
- PostgreSQL: `server-name-or-ip:5432`

## Config examples

### API host appsettings

```json
{
  "ConnectionStrings": {
    "TaskReminder": "Host=postgres.office.local;Port=5432;Database=task_reminder;Username=task_reminder_app;Password=SET_IN_ENVIRONMENT"
  },
  "App": {
    "RunMigrationsOnStartup": true,
    "SeedDemoDataOnStartup": false
  }
}
```

### WPF workstation appsettings

```json
{
  "Client": {
    "ApiBaseUrl": "https://frontdesk-api.office.local:7087/",
    "SignalRHubUrl": "https://frontdesk-api.office.local:7087/hubs/tasks",
    "ReminderPollingSeconds": 60,
    "DefaultRepeatMinutes": 30,
    "AllowInvalidLocalCertificatesInDevelopment": false
  }
}
```

## Startup recommendation for the API host

Recommended options:

1. Dedicated always-on office PC or mini server.
2. Publish the API with `scripts/publish-api.ps1`.
3. Run it at Windows startup with a service wrapper, scheduled task, or controlled startup shortcut.
4. Monitor `/health` daily.

## Nightly backup fit

Recommended daily flow:

1. Run `scripts/backup-database.ps1` on the PostgreSQL host or API host.
2. Save the `.backup` file into `D:\Task_Reminder\Backups` or another secure backup location.
3. Copy backups off-machine or into a cloud-synced protected folder.
4. Keep at least several days of rolling backups.

## Multi-PC client rollout

1. Run `scripts/package-release.ps1` on the dev/deployment machine.
2. Copy `artifacts\release\wpf` to a shared deployment folder.
3. Replace each workstation client folder from that release.
4. Launch the client and confirm the login screen loads users from the shared API.

## Office go-live checklist

- PostgreSQL reachable from API host
- API `/health` returns healthy
- WPF clients point to the office API URL
- one Admin user available
- nightly backup scheduled
- logs folder location confirmed
