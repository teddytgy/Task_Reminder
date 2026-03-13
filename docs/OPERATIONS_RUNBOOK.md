# Operations Runbook

## Daily checks

- Verify the API health endpoint at `/health`.
- Review WPF and API logs in `%LOCALAPPDATA%\Task_Reminder\Logs\`.
- Check the Admin Operations screen for audit entries and integration status.

## Common issues

### API unreachable

- Confirm the API process is running.
- Verify workstation `Client:ApiBaseUrl`.
- Check firewall and certificate trust for the office host.

### Database unavailable

- Check PostgreSQL service status.
- Verify the API connection string.
- Review API startup logs and `/health`.

### Version mismatch

- The WPF client will warn if its version is below the API minimum supported version.
- Re-copy the current publish output to the workstation.

### Permission denied

- Confirm the selected office user has the correct role.
- `FrontDesk`: day-to-day workflow actions
- `Manager`: reporting, boards, audit viewing
- `Admin`: settings, imports, integrations, full audit/admin tooling

## Audit review

- Use the Admin Operations screen.
- Filter deeper via `GET /api/audit` if support staff need a targeted investigation.

## Integration scaffolding

- All integration providers are disabled by default.
- Use the Admin Operations screen to review last run status.
- Current providers are stubs only and are meant to be replaced by real implementations later.
