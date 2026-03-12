# Task_Reminder

Internal office task reminder system for a dental front desk team. This solution uses a shared PostgreSQL database, an ASP.NET Core Web API, SignalR for real-time updates, and a WPF desktop client for Windows.

## Architecture Overview

The solution is split into four projects:

- `src/Task_Reminder.Api`: ASP.NET Core Web API, EF Core data access, SignalR hub, business rules, seeding, and overdue background processing.
- `src/Task_Reminder.Wpf`: Windows WPF client using MVVM, REST + SignalR integration, filters, task actions, and toast reminders.
- `src/Task_Reminder.Shared`: DTOs, request models, and shared enums used by both API and client.
- `tests/Task_Reminder.Tests`: Unit tests covering core task status mapping logic.

High-level flow:

1. The WPF app signs a staff member into the shared dashboard.
2. The WPF client calls the API for reads and writes.
3. The API stores data in PostgreSQL and writes audit history for every change.
4. The API broadcasts task changes over SignalR.
5. All open WPF clients refresh immediately and show the updated assignee, claimant, snooze state, completion state, or overdue state.

## Folder Structure

```text
Task_Reminder
|-- Task_Reminder.sln
|-- Directory.Build.props
|-- README.md
|-- .gitignore
|-- src
|   |-- Task_Reminder.Api
|   |-- Task_Reminder.Shared
|   `-- Task_Reminder.Wpf
`-- tests
    `-- Task_Reminder.Tests
```

## Features Included

- Shared task list across multiple Windows PCs
- Multi-user front desk selection screen
- Assign, claim, snooze, complete, and cancel task APIs
- Audit trail for every task change
- Due now, due today, overdue, assigned to me, unassigned, and completed today filters
- Real-time updates with SignalR
- PostgreSQL + EF Core + migration files
- Demo seed users and seed tasks
- Background service that marks tasks overdue
- Windows toast notifications for due-now and overdue tasks
- Clean, extendable layered structure for future appointment and insurance workflow integrations

## Local Setup

### 1. Install prerequisites

- Windows 10 or Windows 11
- .NET 8 SDK
- PostgreSQL 15 or newer
- Visual Studio 2022 or newer with `.NET desktop development` and `ASP.NET and web development`

### 2. Clone the repository

```powershell
git clone https://github.com/teddytgy/Task_Reminder.git
cd Task_Reminder
```

### 3. Configure PostgreSQL

Create a database:

```sql
CREATE DATABASE task_reminder_dev;
```

Update the API connection string in:

- `src/Task_Reminder.Api/appsettings.Development.json`
- or user secrets / environment variables for real environments

Recommended environment-variable override:

```powershell
$env:ConnectionStrings__TaskReminder="Host=localhost;Port=5432;Database=task_reminder_dev;Username=postgres;Password=local-dev-password"
```

### 4. Apply the database migration

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

### 5. Run the API

```powershell
dotnet run --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

Swagger will open at:

- `https://localhost:7087/swagger`

SignalR hub:

- `https://localhost:7087/hubs/tasks`

### 6. Run the WPF app

In a new terminal:

```powershell
dotnet run --project .\src\Task_Reminder.Wpf\Task_Reminder.Wpf.csproj
```

The login window will show seeded front desk users once the API has started and the seed has run.

## Build Commands

```powershell
dotnet restore .\Task_Reminder.sln
dotnet build .\Task_Reminder.sln
dotnet test .\tests\Task_Reminder.Tests\Task_Reminder.Tests.csproj
```

## EF Migration Commands

Create a new migration:

```powershell
dotnet ef migrations add AddYourChangeName --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --output-dir Migrations
```

Apply migrations:

```powershell
dotnet ef database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

## Working From Multiple PCs

The shared database is what keeps every office workstation on the same task list. GitHub keeps the code in sync across developer machines.

Recommended setup:

1. Host PostgreSQL on one reachable machine or server inside the office network.
2. Run the API on a Windows PC or server that every front desk workstation can reach.
3. Point every WPF client to the same API URL and SignalR hub URL in `src/Task_Reminder.Wpf/appsettings.json`.
4. Point the API on every environment to the same PostgreSQL database or the correct environment database.

## SignalR Notes

- The hub path is `/hubs/tasks`.
- The API broadcasts `TaskChanged` events after create, assign, claim, snooze, complete, cancel, and overdue updates.
- The WPF app listens for those events and refreshes the grid automatically.

## GitHub Workflow

### Push this generated project to GitHub

```powershell
git init
git branch -M main
git remote add origin https://github.com/teddytgy/Task_Reminder.git
git add .
git commit -m "Initial production-ready task reminder MVP"
git push -u origin main
```

### Continue development from another PC

```powershell
git clone https://github.com/teddytgy/Task_Reminder.git
cd Task_Reminder
dotnet restore .\Task_Reminder.sln
dotnet build .\Task_Reminder.sln
```

Then update local config:

- `src/Task_Reminder.Api/appsettings.Development.json`
- `src/Task_Reminder.Wpf/appsettings.json`

## PostgreSQL Setup Notes

- Use PostgreSQL only. Do not switch to SQL Server for this solution.
- For a small office MVP, one central PostgreSQL instance is enough.
- Create a dedicated database user for the app in shared environments.
- Use strong passwords and keep production secrets out of source control.
- Make sure the API host can connect to PostgreSQL through the local firewall and network rules.

## Future Enhancement Ideas

- Add authentication and role-based permissions
- Add task comments and attachments
- Add patient search and appointment integration
- Add insurance workflow templates
- Add recurring task templates and escalation rules
- Add desktop auto-start and tray support
- Add richer toast actions such as claim or snooze directly from the notification

## Notes

- `appsettings.json` files in this repository only contain placeholders or local sample values.
- Seed data is intended for development and demo use.
- The first production hardening step should be secret management plus authenticated API access.
