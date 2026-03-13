# Task_Reminder

Internal office task reminder system for a dental front desk team. The solution uses PostgreSQL, an ASP.NET Core Web API, SignalR for real-time updates, and a WPF desktop client for Windows.

Current office-ready features include shared tasks, recurring task templates, manager reporting, role-aware reminders, comments, escalation support, audit history, and multi-PC real-time updates.

The solution now also supports appointment workflows, insurance work queues, contact logging, balance follow-up, office settings, operational boards, and sample data import for future scheduling/insurance integration work.

The final production-readiness layer now adds release packaging, desktop/API version compatibility checks, API-side permission enforcement, audit review, integration scaffolding, backup/restore scripts, and admin operations tooling for office deployment.

The current operational phase makes the recommended shared-office deployment model explicit, adds a realistic fresh-database demo seed, includes a reset/reseed script, and adds front desk/manager training materials for rollout.

## Solution Overview

- `src/Task_Reminder.Api`: ASP.NET Core Web API, EF Core data access, SignalR hub, business rules, startup migration logic, and overdue monitoring.
- `src/Task_Reminder.Wpf`: Windows WPF client using MVVM, REST + SignalR integration, recurring template management, manager dashboard, toast reminders, and desktop logging.
- `src/Task_Reminder.Shared`: DTOs, request models, and shared Enums.
- `tests/Task_Reminder.Tests`: unit tests for task mapping, recurring rules, notification routing, escalation, and manager metrics.

## Feature Overview

### Appointment workflow

- First-class appointment records now support:
  - patient name/reference
  - local appointment date and time
  - provider and appointment type
  - appointment status
  - confirmation status
  - insurance status
  - balance status
  - source system and source reference
- Appointment quick actions in the desktop app include:
  - confirm
  - voicemail left
  - no answer
  - text sent
  - email sent
  - cancel
  - reschedule
  - check in
  - complete
  - create follow-up task
- Appointment automation can generate linked tasks for confirmations, insurance verification, balances, no-shows, and reschedule follow-up.

### Insurance workflow

- Structured insurance work items now support:
  - carrier / plan / member / group / payer fields
  - verification status
  - eligibility status
  - verification method
  - structured benefits fields
  - issue type
  - notes
  - appointment link
  - source system and source reference
- Insurance queue quick actions include:
  - mark started
  - mark verified
  - mark failed
  - mark needs manual review
  - mark inactive coverage
  - create patient follow-up task
  - create manager escalation task

### Balance and contact workflow

- Balance follow-up work items track:
  - amount due
  - due reason
  - follow-up date
  - collection status
  - linked appointment and task references
- Contact logs can be attached to:
  - tasks
  - appointments
  - insurance work items
  - balance follow-up items
- Contact log types:
  - call
  - voicemail
  - text
  - email
  - in-person

### Front desk boards

- The desktop app now includes an operations boards window with:
  - Today Board
  - Tomorrow Prep
  - Collections Board
  - Recall / Follow-Up
  - Manager Queue
- Boards summarize appointments, insurance items, balances, overdue tasks, escalations, and workload by user.

### Reporting and KPI expansion

- Managers can review operational KPIs for:
  - appointment confirmation rate
  - no-show rate
  - cancellation rate
  - insurance verification completion rate
  - insurance issue rate
  - balance collection progress
  - average task completion time
  - task completion count by user
  - overdue rate by category
  - contact outcome distribution
- CSV export endpoints support manager-friendly exports for:
  - appointments
  - insurance queue
  - task queue
  - balance follow-up
  - contact outcomes

### Office settings

- Manager/admin users can edit office settings for:
  - office name
  - business hours summary
  - confirmation lead hours
  - insurance verification lead days
  - overdue escalation defaults
  - no-show follow-up delay
  - default reminder interval
  - time zone
  - board/module enable flags

### Office deployment and training support

- The recommended production model is now documented as:
  - one shared PostgreSQL database
  - one always-on office API host
  - multiple WPF client PCs pointing at the same API
  - separate GitHub-based development on office/home dev machines
- New office rollout and training assets include:
  - `docs/OFFICE_DEPLOYMENT_MODEL.md`
  - `docs/DEV_WORKFLOW.md`
  - `docs/USER_TRAINING_MANUAL.md`
  - `docs/USER_TRAINING_MANUAL.html`
  - `docs/screenshots/SCREENSHOT_CAPTURE_CHECKLIST.md`
  - `scripts/reset-demo-data.ps1`

### Import readiness

- The API now includes import endpoints for appointments and insurance work items.
- Supported import formats:
  - JSON
  - CSV
- Sample files are included:
  - `sample-data/appointments-sample.json`
  - `sample-data/insurance-sample.json`
- These imports preserve `SourceSystem` and `SourceReference` so future Open Dental / PatientXpress / clearinghouse adapters can map cleanly into the same workflow tables.
- Import processing also performs duplicate prevention where a source reference or matching operational key is already present.

### Recurring tasks

- Managers can open `Recurring Tasks` from the desktop dashboard.
- Recurring templates support:
  - daily
  - weekdays
  - weekly
  - monthly
  - custom interval
- Each template can define:
  - category
  - priority
  - assigned user
  - escalation target
  - reminder repeat minutes
  - notes or checklist-style instructions
  - start/end dates
  - local time of day
- The API background service generates real `TaskItem` records from active templates and avoids duplicate generation for the same local occurrence date.

### Manager dashboard

- Managers and admins can open `Manager Dashboard` from the desktop app.
- Dashboard metrics include:
  - total open tasks
  - overdue tasks
  - completed in range
  - unassigned tasks
  - tasks by category
  - tasks by priority
  - tasks completed per user
  - average completion time
- Range filters:
  - today
  - last 7 days
  - last 30 days
  - custom range
- CSV export is available from the manager dashboard window.

### Roles

Current lightweight roles:

- `FrontDesk`
- `Manager`
- `Admin`

Behavior:

- manager dashboard is intended for `Manager` and `Admin`
- recurring template management is intended for `Manager` and `Admin`
- unassigned reminder toasts are routed to front desk users
- overdue escalation alerts can route to manager or admin users based on preferences

### Notifications and comments

- `My Alerts` in the desktop app lets each user control reminder preferences.
- Assigned tasks notify only the assigned user.
- Unassigned tasks notify front desk users.
- Claimed tasks stop reminding other users.
- Escalated overdue tasks can notify manager or admin users.
- Staff can add comments such as:
  - called patient, no answer
  - insurance portal unavailable
  - left voicemail
- Comments are stored in task history so accountability remains visible.

## Verified Local Run Steps

### 1. Prerequisites

- Windows 10 or Windows 11
- .NET 8 SDK
- PostgreSQL 15 or newer
- Visual Studio 2022 or newer with `.NET desktop development` and `ASP.NET and web development`

### 2. Restore, build, and test

```powershell
dotnet restore .\Task_Reminder.sln
dotnet build .\Task_Reminder.sln
dotnet test .\tests\Task_Reminder.Tests\Task_Reminder.Tests.csproj
```

### 3. Configure local development settings

Committed config files now use example values only.

API local development defaults are in:

- `src/Task_Reminder.Api/appsettings.Development.json`

WPF local development defaults are in:

- `src/Task_Reminder.Wpf/appsettings.Development.json`

If your local PostgreSQL password differs, override it with an environment variable instead of editing committed files:

```powershell
$env:ConnectionStrings__TaskReminder="Host=localhost;Port=5432;Database=task_reminder_dev;Username=postgres;Password=your-local-password"
```

### 4. Install the correct EF CLI

```powershell
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 8.0.11
```

If `dotnet ef` is not on `PATH` yet, use:

```powershell
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe --version
```

### 5. Apply the database migration

```powershell
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

### 5a. Reset and reseed demo data

For a clean local demo or training environment:

```powershell
$env:ConnectionStrings__TaskReminder="Host=localhost;Port=5432;Database=task_reminder_dev;Username=postgres;Password=your-local-password"
.\scripts\reset-demo-data.ps1
```

This will drop the local demo database, reapply migrations, start the API briefly, and let startup seeding repopulate the office demo dataset.

### 6. Run the API

```powershell
dotnet run --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

Verified local endpoints:

- Swagger: `https://localhost:7087/swagger`
- Health: `https://localhost:7087/health`
- SignalR hub: `https://localhost:7087/hubs/tasks`

### 7. Run the WPF client

In a second terminal:

```powershell
dotnet run --project .\src\Task_Reminder.Wpf\Task_Reminder.Wpf.csproj
```

The WPF project uses `DOTNET_ENVIRONMENT=Development` through launch settings when started with `dotnet run`, so it loads `appsettings.Development.json` locally and connects to `https://localhost:7087/`.

## Configuration Model

### API config

- `src/Task_Reminder.Api/appsettings.json`
  - office or shared deployment defaults
  - no real secrets
  - demo seeding disabled by default
- `src/Task_Reminder.Api/appsettings.Development.json`
  - local development defaults
  - safe sample password only
  - demo seeding enabled for local development

Relevant API settings:

- `ConnectionStrings:TaskReminder`
- `App:RunMigrationsOnStartup`
- `App:SeedDemoDataOnStartup`
- `Logging:File:*`

### WPF config

- `src/Task_Reminder.Wpf/appsettings.json`
  - office deployment placeholder API URL
- `src/Task_Reminder.Wpf/appsettings.Development.json`
  - local development URL pointing to `https://localhost:7087/`

Relevant WPF settings:

- `Client:ApiBaseUrl`
- `Client:SignalRHubUrl`
- `Client:ReminderPollingSeconds`
- `Client:DefaultRepeatMinutes`
- `Client:AllowInvalidLocalCertificatesInDevelopment`
- `Logging:File:*`

## HTTPS and Certificate Handling

The WPF client now allows certificate bypass only when all of the following are true:

- the app is running in `Development`
- `Client:AllowInvalidLocalCertificatesInDevelopment` is `true`
- the request is going to a loopback address only:
  - `localhost`
  - `127.0.0.1`
  - `::1`

No broad certificate bypass is allowed for non-local office or production addresses.

For a cleaner local browser experience, you can still trust the ASP.NET developer certificate:

```powershell
dotnet dev-certs https --trust
```

## Health and Diagnostics

### Health endpoint

- `GET /health`
- returns a simple JSON payload and checks database connectivity

### Log locations

By default logs are written to:

- API: `%LOCALAPPDATA%\Task_Reminder\Logs\api\`
- WPF: `%LOCALAPPDATA%\Task_Reminder\Logs\wpf\`

The exact file paths are configurable through `Logging:File:Path`.
Daily rolling log files are enabled by default and keep the most recent 14 files.

### What is logged

API:

- startup and environment
- migration check / apply status
- demo seeding enabled or skipped
- task create / assign / claim / snooze / complete / cancel
- overdue monitor lifecycle and errors
- SignalR broadcast failures
- health check database failures

WPF:

- startup and shutdown
- API base address configuration
- API connection failures
- SignalR reconnecting / reconnected / closed
- toast notification failures
- unhandled desktop exceptions

## Publishing

### Publish the API

```powershell
.\scripts\publish-api.ps1
```

Default output:

- `.\artifacts\publish\api`

### Publish the WPF client

```powershell
.\scripts\publish-wpf.ps1
```

Default output:

- `.\artifacts\publish\wpf`

### Package a release

```powershell
.\scripts\package-release.ps1
```

Release output:

- `.\artifacts\release\api`
- `.\artifacts\release\wpf`
- `.\artifacts\release\release-manifest.json`

## Office Deployment Guide

The primary production model for this solution is documented in:

- `docs/OFFICE_DEPLOYMENT_MODEL.md`

That runbook covers:

- which machine hosts PostgreSQL
- which machine hosts the API
- which machines run WPF clients
- recommended folders and URLs
- nightly backup placement
- multi-PC rollout and update flow

### API deployment

1. Publish the API on the office PC or server that will host the shared task service.
2. Configure `appsettings.json` or environment variables with the real office PostgreSQL connection string.
3. Leave `App:RunMigrationsOnStartup` enabled unless your deployment process handles migrations separately.
4. Leave `App:SeedDemoDataOnStartup` disabled for office deployment.
5. Make sure the office firewall allows access to the API port you publish on.

### WPF deployment

1. Publish the WPF client.
2. On each front desk PC, update `appsettings.json` with the office API base URL and SignalR hub URL.
3. Distribute the published WPF folder to each workstation.
4. Launch the client and verify the login screen loads users from the shared API.

### Multi-PC setup

Recommended office topology:

1. Host PostgreSQL on one reachable machine or database server.
2. Run the API on one office PC or server.
3. Point every WPF client at that same API URL.
4. Keep `SignalRHubUrl` aligned with the API host and `/hubs/tasks`.

Example WPF office config:

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

### Multi-PC update flow

1. Build a release with `.\scripts\package-release.ps1`.
2. Copy `artifacts\release\wpf` to a shared deployment folder or directly to each workstation.
3. Replace the workstation client folder during updates.
4. Verify the desktop build is not below the API minimum supported version.

Version compatibility support now includes:

- `GET /api/system/version`
- version display in the WPF main window
- WPF startup check against the API minimum supported desktop version

## EF Migration Commands

Create a new migration:

```powershell
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe migrations add AddYourChangeName --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --output-dir Migrations
```

Apply migrations:

```powershell
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

Latest verified migration:

- `20260312234852_OfficeWorkflowExpansion`
- `20260313003855_OperationsPlatformExpansion`
- `20260313014119_FinalProductionReadiness`

## New API Modules

Key workflow endpoints now include:

- `GET/POST/PUT /api/appointments`
- `POST /api/appointments/{id}/{actionName}`
- `POST /api/appointments/{id}/follow-up-task`
- `GET/POST/PUT /api/insurance`
- `POST /api/insurance/{id}/status`
- `POST /api/insurance/{id}/follow-up-task`
- `GET/POST/PUT /api/balances`
- `POST /api/balances/{id}/status`
- `POST /api/balances/{id}/follow-up-task`
- `GET/POST /api/contact-logs`
- `GET/PUT /api/office-settings`
- `POST /api/imports/appointments`
- `POST /api/imports/insurance`
- `GET /api/operations/board`
- `GET /api/operations/workload`
- `GET /api/operations/activity/{userId}`
- `GET /api/operations/kpis`
- `GET /api/operations/export/{exportType}`
- `GET /api/system/version`
- `GET /api/system/summary`
- `GET /api/audit`
- `GET /api/integrations`
- `PUT /api/integrations/{id}`
- `POST /api/integrations/{id}/run`

## Desktop Modules

New desktop windows and workflows include:

- Appointments
- Insurance Queue
- Front Desk Boards
- Office Settings
- Import Data
- Contact Log dialog
- Admin Operations

These are reachable from the main WPF dashboard toolbar.

## Audit And Permissions

The API now enforces lightweight office permissions through request user context headers sent by the WPF client after user selection.

Role intent:

- `FrontDesk`: daily workflow actions
- `Manager`: dashboards, reports, operational boards, audit review
- `Admin`: office settings changes, imports, recurring administration, integration management, full audit/admin tooling

Audit records now capture:

- who
- when
- entity type
- entity id
- action
- summary/details

Important write operations now log to the audit store, including tasks, recurring task definitions, appointments, insurance items, balance items, user preferences, office settings, imports, and integration runs.

## Backup And Restore

Scripts:

- `.\scripts\backup-database.ps1`
- `.\scripts\restore-database.ps1`

Examples:

```powershell
.\scripts\backup-database.ps1 -Database task_reminder
.\scripts\restore-database.ps1 -Database task_reminder -BackupFile .\artifacts\backups\task_reminder-YYYYMMDD-HHMMSS.backup
```

Use `pg_dump` / `pg_restore`, avoid hardcoded secrets, and validate restore steps on a non-production copy before relying on them in a live office.

## Integration Scaffolding

Disabled-by-default scaffolding now exists for:

- Open Dental appointment sync
- PatientXpress insurance sync
- CSV/manual import provider
- patient communication provider

These provider runs are currently placeholders only, but the API, data model, and admin tooling are now ready for real implementations later.

## Sample Usage Flows

### Front desk morning workflow

1. Open `Boards` and review the Today Board.
2. Open `Appointments` to work unconfirmed visits.
3. Add contact logs while calling or texting patients.
4. Open `Insurance Queue` to resolve pending verifications.
5. Review balance-due items on the Collections Board.

### Manager workflow

1. Open `Manager Dashboard` for KPI and task reporting.
2. Open `Boards` and review the Manager Queue for escalations and user workload.
3. Open `Office Settings` to tune lead times and escalation defaults.
4. Export appointment, insurance, balance, task, or contact CSV reports as needed.

## Git Workflow Across PCs

The full office/home developer workflow guide is documented in:

- `docs/DEV_WORKFLOW.md`

### Clone on another PC

```powershell
git clone https://github.com/teddytgy/Task_Reminder.git
cd Task_Reminder
dotnet restore .\Task_Reminder.sln
dotnet build .\Task_Reminder.sln
dotnet test .\tests\Task_Reminder.Tests\Task_Reminder.Tests.csproj
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 8.0.11
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

Then configure:

- `src/Task_Reminder.Api/appsettings.json` or environment variables for office DB settings
- `src/Task_Reminder.Wpf/appsettings.json` for the office API URL

## Recommended Next Enhancements

- Add authenticated API access and real authorization around manager/admin-only endpoints
- Add a proper installer or MSIX packaging for WPF deployment
- Add API request correlation IDs
- Add retry/backoff around selected WPF API operations
- Add operational dashboards or centralized log shipping
- Add real provider implementations for Open Dental, insurance verification, and patient communication workflows
- Add a workstation install script for shared-folder client rollout

## Operational Docs

Additional runbook and deployment documentation is included in:

- `docs/DEPLOYMENT.md`
- `docs/BACKUP_RESTORE.md`
- `docs/OPERATIONS_RUNBOOK.md`
- `docs/INTEGRATIONS.md`
- `docs/OFFICE_DEPLOYMENT_MODEL.md`
- `docs/OFFICE_DEPLOYMENT_CHECKLIST.md`
- `docs/RELEASE_CHECKLIST.md`
- `docs/DEV_WORKFLOW.md`
- `docs/USER_TRAINING_MANUAL.md`
- `docs/USER_TRAINING_MANUAL.html`
- `docs/screenshots/SCREENSHOT_CAPTURE_CHECKLIST.md`

## Demo Seed Overview

Fresh local demo seeding now creates a practical office dataset with approximately:

- 4 users
- 12 recurring task definitions
- 60 appointments
- 36 insurance workflow items
- 22 balance follow-up items
- 40+ manually seeded tasks plus additional generated workflow/recurring tasks
- 60+ contact logs
- audit history, comments, integration configs, and integration run examples

Seeded example users:

- `mia` (`FrontDesk`)
- `noah` (`FrontDesk`)
- `emma` (`Manager`)
- `ava` (`Admin`)

Seeded office name:

- `Bright Smile Dental Center`

## Training Manual

Staff-facing training material is now included in:

- `docs/USER_TRAINING_MANUAL.md`
- `docs/USER_TRAINING_MANUAL.html`

If screenshots need to be captured or refreshed later, use:

- `docs/screenshots/SCREENSHOT_CAPTURE_CHECKLIST.md`

## Notes

- The MVP remains end-to-end functional with the current architecture.
- Local development continues to work with `dotnet run`.
- Committed config files now contain placeholder or example values only.
