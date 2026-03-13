# Deployment

## API

Use `.\scripts\publish-api.ps1` for a repeatable API publish.

- Framework-dependent publish is the default for office use.
- Use `-SelfContained` when the host machine will not have the .NET 8 runtime installed.
- Configure connection strings through `appsettings.json` or environment variables.

Suggested office model:

1. Publish the API to a dedicated front desk server or always-on office PC.
2. Keep PostgreSQL on the same host or another stable office server.
3. Run the API as a scheduled startup app or a Windows Service wrapper.
4. Leave `App:RunMigrationsOnStartup=true` for small-office deployments unless migrations are managed separately.

## WPF

Use `.\scripts\publish-wpf.ps1` for the desktop client.

- Publish once per release.
- Copy the output to each workstation or a shared deployment folder.
- Update `appsettings.json` so `Client:ApiBaseUrl` and `Client:SignalRHubUrl` point to the office API host.

## Multi-PC rollout

Recommended pattern:

1. Build a release with `.\scripts\package-release.ps1`.
2. Store the `artifacts\release\wpf` output in a shared network folder.
3. Copy that folder to each workstation during rollout.
4. Launch the client and confirm the desktop version is not below the API minimum-supported version.

## Version compatibility

- API exposes `/api/system/version`.
- WPF compares its own version to the API minimum supported desktop version on startup.
- If a workstation is too old, the user gets an update warning instead of silent drift.
