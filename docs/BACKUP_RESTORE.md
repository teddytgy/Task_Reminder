# Backup And Restore

## Backup

Use:

```powershell
.\scripts\backup-database.ps1 -Database task_reminder
```

Notes:

- `pg_dump` must be installed and available on `PATH`, or pass `-PgDumpPath`.
- PostgreSQL will prompt for credentials unless `PGPASSWORD` or a `.pgpass.conf` entry is configured.
- Store backup files somewhere outside the workstation local drive when possible.

## Restore

Use:

```powershell
.\scripts\restore-database.ps1 -Database task_reminder -BackupFile .\artifacts\backups\task_reminder-YYYYMMDD-HHMMSS.backup
```

Notes:

- Restoring will replace the target database contents because the script uses `--clean --if-exists`.
- Validate restore steps on a non-production copy before using them in a live office.

## Recommended office practice

1. Run nightly backups from the API/database host.
2. Copy backups to a second machine or cloud-backed folder.
3. Export operational CSV reports for management snapshots when needed.
4. Test restore procedures at least once before relying on them in production.
