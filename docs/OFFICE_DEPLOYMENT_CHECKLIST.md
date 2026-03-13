# Office Deployment Checklist

This checklist is for the recommended production model:

- one shared PostgreSQL database
- one shared office API host
- multiple WPF client PCs
- separate GitHub-based development workflow

## A. Pre-deployment preparation

1. Choose the PostgreSQL host machine.
   Recommended: a stable office server or always-on office PC.
2. Choose the API host machine.
   Recommended: a separate always-on office PC or mini server on the same network.
3. Confirm the WPF client PCs that will use the system.
4. Decide the office API hostname, URL, and port.
   Example: `https://frontdesk-api.office.local:7087/`
5. Decide the PostgreSQL hostname and port.
   Example: `postgres.office.local:5432`
6. Install PostgreSQL on the database host if it is not already installed.
7. Install PostgreSQL client tools where backups will run.
   Required tools: `pg_dump`, `pg_restore`
8. Install the .NET 8 runtime on the API host if you will use a framework-dependent API publish.
9. Decide the office deployment folders.
   Suggested:
   - API host: `C:\OfficeApps\Task_Reminder\Api`
   - WPF client: `C:\OfficeApps\Task_Reminder\Client`
   - backups: `D:\Task_Reminder\Backups`
10. Decide how the API will stay running.
    Recommended: scheduled task at startup, service wrapper, or another controlled always-on host method.

## B. Database setup

1. Create the production database.
   Example database name: `task_reminder`
2. Create or confirm the database login the API will use.
3. Set the API connection string.
   Example:
   `Host=postgres.office.local;Port=5432;Database=task_reminder;Username=task_reminder_app;Password=SET_IN_ENVIRONMENT`
4. From the deployment machine, apply migrations:

```powershell
$env:ConnectionStrings__TaskReminder="Host=postgres.office.local;Port=5432;Database=task_reminder;Username=task_reminder_app;Password=your-real-password"
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

5. Decide whether to seed demo data.
   - Use demo seed only for training/demo environments.
   - Keep `App:SeedDemoDataOnStartup=false` for real production unless you intentionally want starter data.
6. If using a training environment, verify the seeded users:
   - `mia`
   - `noah`
   - `emma`
   - `ava`
7. Start the API and test:
   - `https://frontdesk-api.office.local:7087/health`
8. Confirm health returns:
   - `status: Healthy`
   - `database: Healthy`

## C. API deployment

1. On the deployment/dev machine, publish the API:

```powershell
.\scripts\publish-api.ps1
```

2. Copy the publish output to the API host.
   Default source:
   - `.\artifacts\publish\api`
3. Place the files in the office API folder.
   Suggested target:
   - `C:\OfficeApps\Task_Reminder\Api`
4. Configure the production connection string and app settings.
   Required values:
   - `ConnectionStrings:TaskReminder`
   - `App:RunMigrationsOnStartup`
   - `App:SeedDemoDataOnStartup=false`
5. Start the API on the host.
6. Verify:
   - `/health`
   - `/swagger`
   - `/api/system/version`
7. Confirm that logs are being written.
8. Configure the API to stay running after reboot.
   Recommended:
   - scheduled task at startup under a service account
   - or a Windows Service wrapper if you already use one in the office

## D. WPF deployment to multiple PCs

1. On the deployment/dev machine, publish the WPF client:

```powershell
.\scripts\publish-wpf.ps1
```

2. Or prepare the combined release package:

```powershell
.\scripts\package-release.ps1
```

3. Copy the WPF output to each workstation or to a shared deployment folder.
   Default release folder:
   - `.\artifacts\release\wpf`
4. On each workstation, configure:
   - `Client:ApiBaseUrl`
   - `Client:SignalRHubUrl`
5. Example workstation values:
   - `https://frontdesk-api.office.local:7087/`
   - `https://frontdesk-api.office.local:7087/hubs/tasks`
6. Launch the WPF app on each PC.
7. Verify:
   - login screen loads users
   - main dashboard opens
   - SignalR connects
   - notifications still work
8. Confirm the workstation does not show a version mismatch warning.

## E. First-day office validation

1. Log in as a `FrontDesk` user.
2. Confirm the user can see tasks, appointments, insurance, and balances.
3. Confirm front desk actions work:
   - claim
   - assign if allowed by role
   - snooze
   - complete
   - add comment
   - add contact log
4. Log in as a `Manager` user.
5. Confirm the manager can open:
   - Manager Dashboard
   - Boards
   - reports and CSV export
6. Log in as an `Admin` user.
7. Confirm the admin can open:
   - Admin Tools
   - Office Settings
   - Imports
   - audit review
8. Confirm role-based permission behavior is correct.
9. Confirm audit entries are being created after write actions.
10. Run a backup once and confirm the file is produced.
11. Confirm the WPF app shows current version information and no unsupported-version warning.

## F. Ongoing operations

1. Schedule nightly backups with `scripts\backup-database.ps1`.
2. Store backups off-machine or in a protected synced location.
3. Before every production update:
   - run a fresh backup
   - build and test on the dev machine
   - package the release
4. To roll out a new WPF version:
   - copy the new `wpf` publish folder to each workstation
   - replace the previous client folder
   - relaunch the client
5. To update the API safely:
   - back up the database
   - stop the running API
   - copy in the new API publish output
   - start the API
   - verify `/health`
6. Use the Admin Tools screen and log folders during troubleshooting.
7. Review API and WPF logs in `%LOCALAPPDATA%\Task_Reminder\Logs\`
8. For developer changes, use the Git workflow in `docs/DEV_WORKFLOW.md`.

## Common values to customize before go-live

- PostgreSQL host name
- PostgreSQL database name
- PostgreSQL username/password
- office API host name
- office API HTTPS certificate/trust setup
- WPF `Client:ApiBaseUrl`
- WPF `Client:SignalRHubUrl`
- backup destination folder
- which real office users should be `FrontDesk`, `Manager`, and `Admin`
