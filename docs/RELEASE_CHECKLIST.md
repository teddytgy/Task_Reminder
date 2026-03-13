# Release Checklist

Use this checklist before deploying a new office release.

## 1. Sync the repo

```powershell
git checkout main
git pull origin main
```

## 2. Verify the code

```powershell
dotnet restore .\Task_Reminder.sln
dotnet build .\Task_Reminder.sln
dotnet test .\tests\Task_Reminder.Tests\Task_Reminder.Tests.csproj
```

## 3. Back up the office database

```powershell
.\scripts\backup-database.ps1 -Database task_reminder
```

## 4. Publish the API

```powershell
.\scripts\publish-api.ps1
```

## 5. Publish the WPF client

```powershell
.\scripts\publish-wpf.ps1
```

## 6. Package the release

```powershell
.\scripts\package-release.ps1
```

## 7. Apply migrations for the target office database

```powershell
$env:ConnectionStrings__TaskReminder="Host=postgres.office.local;Port=5432;Database=task_reminder;Username=task_reminder_app;Password=your-real-password"
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe database update --project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj --startup-project .\src\Task_Reminder.Api\Task_Reminder.Api.csproj
```

## 8. Deploy the API

1. Stop the running office API.
2. Copy `artifacts\release\api` to the API host.
3. Start the API.
4. Verify:
   - `/health`
   - `/swagger`
   - `/api/system/version`

## 9. Deploy the WPF client

1. Copy `artifacts\release\wpf` to each workstation or shared deployment folder.
2. Confirm workstation config points to the shared office API.
3. Launch the app.
4. Confirm the login screen loads users and no version mismatch warning appears.

## 10. Smoke test production

1. Log in as a front desk user.
2. Log in as a manager or admin user.
3. Confirm tasks, appointments, insurance, balances, and boards load.
4. Confirm audit entries still appear.
5. Confirm SignalR live updates still work.
