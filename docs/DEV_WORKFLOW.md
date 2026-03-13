# Developer Workflow

## Recommended branch model

- `main`: stable office-ready branch
- `feature/<short-name>`: larger changes
- optional small fixes can be done directly from a short-lived branch and merged back into `main`

## Exact commands to save today's work

Use these commands from the repo root when you are ready to save the current state to GitHub:

```powershell
git status
git add .
git commit -m "Finalize office deployment docs and rollout checklist"
git push origin main
```

If you prefer to use a feature branch first:

```powershell
git checkout -b feature/final-office-deployment-docs
git add .
git commit -m "Finalize office deployment docs and rollout checklist"
git push -u origin feature/final-office-deployment-docs
```

## First clone on a dev machine

```powershell
git clone https://github.com/teddytgy/Task_Reminder.git
cd Task_Reminder
dotnet restore .\Task_Reminder.sln
dotnet build .\Task_Reminder.sln
dotnet test .\tests\Task_Reminder.Tests\Task_Reminder.Tests.csproj
```

Then install the EF Core CLI and apply the database migration:

```powershell
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 8.0.11
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

## Daily work routine across home and office computers

1. Open the repo.
2. Pull latest changes before starting:

```powershell
git checkout main
git pull origin main
```

3. For larger work, create a feature branch:

```powershell
git checkout -b feature/office-ops-improvement
```

4. Build and test before committing:

```powershell
dotnet restore .\Task_Reminder.sln
dotnet build .\Task_Reminder.sln
dotnet test .\tests\Task_Reminder.Tests\Task_Reminder.Tests.csproj
```

5. Commit and push:

```powershell
git add .
git commit -m "Add office ops improvement"
git push -u origin feature/office-ops-improvement
```

6. On the other computer, pull before continuing work:

```powershell
git checkout feature/office-ops-improvement
git pull origin feature/office-ops-improvement
```

## Exact commands to clone and continue on the office dev computer

```powershell
git clone https://github.com/teddytgy/Task_Reminder.git
cd Task_Reminder
git checkout main
git pull origin main
dotnet restore .\Task_Reminder.sln
dotnet build .\Task_Reminder.sln
dotnet test .\tests\Task_Reminder.Tests\Task_Reminder.Tests.csproj
```

If you need the local demo database on that machine too:

```powershell
$env:ConnectionStrings__TaskReminder="Host=localhost;Port=5432;Database=task_reminder_dev;Username=postgres;Password=your-local-password"
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

## Simple branch guidance

- Use `main` for small documentation fixes, configuration-safe cleanup, and already-reviewed deployment changes.
- Use `feature/<name>` for anything that changes code behavior, database schema, WPF screens, API endpoints, or office workflows.
- Merge back into `main` only after `dotnet build` and `dotnet test` are passing.

## Production config separation

- Keep committed `appsettings.json` files on safe placeholders only.
- Use `appsettings.Development.json` for local dev defaults.
- Use environment variables or machine-local config for production secrets.
- Never commit real database passwords, office hostnames, or cert/private key material.

## Safe testing before office deployment

Recommended sequence:

1. `dotnet build`
2. `dotnet test`
3. run local migration update
4. run API
5. run WPF
6. validate key workflows
7. package release
8. deploy to office only after local verification succeeds

## Git commands for common cases

### Check status

```powershell
git status
```

### See what changed

```powershell
git diff
```

### Sync with remote

```powershell
git fetch origin
git pull
```

### Push current branch

```powershell
git push
```

## Secret handling reminders

- do not commit real DB passwords
- do not commit production hostnames if they reveal sensitive internal naming
- do not commit backup archives
- do not commit publish output or local logs unless intentionally needed

## Office production deployment handoff

Before deploying from dev to office production:

- merge or fast-forward the approved code into `main`
- tag or note the release version
- run `git pull origin main`
- run `dotnet build .\Task_Reminder.sln`
- run `dotnet test .\tests\Task_Reminder.Tests\Task_Reminder.Tests.csproj`
- run `scripts/package-release.ps1`
- update the office deployment folder
- keep backup scripts and docs with the release notes
