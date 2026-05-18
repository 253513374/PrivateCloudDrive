# Backup / Restore Drill Report

- Started: 2026-05-18T19:35:09.8101937+08:00
- Finished: 2026-05-18T19:35:13.5920537+08:00
- Mode: non-destructive backup + restore dry-run
- Backup directory: D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\backups\20260518-193510
- Include Redis: False
- Include MinIO: False
- Secrets copied: no (.env.secret must not exist in this drill)

## Summary

- PASS: 14
- WARN: 0
- FAIL: 0

## Checks

| Status | Check | Message |
|---|---|---|
| PASS | backup-script | backup-local-stack.ps1 found. |
| PASS | restore-script | restore-local-stack.ps1 found. |
| PASS | drill-mode | Running non-destructive drill: backup + restore dry-run only; .env is not copied. |
| PASS | backup-command | Backup command completed. |
| PASS | backup-directory | D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\backups\20260518-193510 |
| PASS | manifest | Found (3425 bytes). |
| PASS | postgres-dump | Found (219678 bytes). |
| PASS | storage-archive | Found (57438383 bytes). |
| PASS | env-secret | No .env.secret copied. |
| PASS | environment-checklist | ENVIRONMENT-REQUIRED.md is present. |
| PASS | backup-manifest | Manifest summary PASS 6 / WARN 1 / FAIL 0. |
| PASS | restore-dry-run | Restore dry-run completed without destructive changes. |
| PASS | report-logs | Wrote drill logs: docs\validation\backup-restore-drill-backup-20260518-193513.log; docs\validation\backup-restore-drill-restore-dry-run-20260518-193513.log |
| PASS | report | Wrote drill report: docs\validation\backup-restore-drill-20260518-193513.md |

## Backup Files

| File | Status | Bytes |
|---|---|---:|
| manifest.json | present | 3425 |
| postgres.dump | present | 219678 |
| storage.tar.gz | present | 57438383 |
| ENVIRONMENT-REQUIRED.md | present | 532 |

## Logs

- Backup command log: docs\validation\backup-restore-drill-backup-20260518-193513.log
- Restore dry-run log: docs\validation\backup-restore-drill-restore-dry-run-20260518-193513.log

## Acceptance Notes

- This report proves that the current local Compose stack can create and validate the minimum backup artifact set: PostgreSQL dump, FileCenter storage archive, manifest, and environment checklist.
- A very small storage.tar.gz means the local storage volume may currently contain no file payloads. In that case, this drill proves the backup/restore control path, not successful recovery of user file bytes; run a destructive restore exercise against a disposable stack with seeded files before production sign-off.
- The restore script was executed without -ConfirmDestructiveRestore, so no target data was overwritten.
- A real disaster recovery exercise must run destructive restore only against a disposable test stack or test machine, then verify login, file list, file preview/download, thumbnails, and sharing behavior.
- .env remains operator-owned sensitive configuration. This drill intentionally writes ENVIRONMENT-REQUIRED.md instead of copying secrets.
