# Backup / Restore Destructive Test Stack Report

- Started: 2026-05-21T21:50:20+08:00
- Mode: destructive restore into disposable Compose project
- Compose project: `pcd_drill_test`
- Backup directory: `artifacts/backups/20260518-193510`
- Restore log: local ignored artifact `docs/validation/backup-restore-destructive-test-stack-20260521-215020.log`
- Smoke log: local ignored artifact `docs/validation/backup-restore-destructive-test-stack-smoke-20260521-215020.log`
- Secrets copied: no
- Tokens/passwords printed: no

## Summary

| Area | Result | Evidence |
|---|---:|---|
| Destructive restore into disposable stack | PASS | Restore summary PASS 14 / WARN 1 / FAIL 0 |
| Source volume isolation | PASS | `-UseCurrentComposeProjectVolumes` restored into `pcd_drill_test_privateclouddrive_stack_storage`, not the manifest source volume |
| PostgreSQL restore | PASS | `pg_restore --clean --if-exists --no-owner --no-privileges` completed after waiting for PostgreSQL readiness |
| Storage volume restore | PASS | `storage.tar.gz` restored into disposable stack volume |
| Stack verification | PASS | `verify-local-stack.ps1 -SkipStart` passed after restore |
| Login | PASS | `/connect/token` succeeded; access token redacted |
| File list | PASS | root list loaded; restored root item count observed |
| Upload/download/preview | PASS | smoke file uploaded; Range download returned HTTP 206; content hash matched |
| Share link | PASS | share created and public share opened; only token suffix recorded in smoke log |
| Trash restore | PASS | file deleted to trash, appeared in trash list, and restored successfully |
| Audit/security sample | PASS | operation log sample did not contain password/access token/refresh token |

## Checks

| Status | Check | Message |
|---|---|---|
| PASS | docker | Docker CLI is available. |
| PASS | compose-config | docker compose config is valid. |
| PASS | manifest | Found `manifest.json` from source backup. |
| PASS | postgres-dump | Found `postgres.dump`. |
| PASS | storage-archive | Found `storage.tar.gz`. |
| PASS | volume-resolution-mode | Ignored source manifest Docker volume names and restored into current Compose project volumes. |
| WARN | destructive-restore | Explicit `-ConfirmDestructiveRestore` was used against disposable project only. |
| PASS | postgres-ready | Restore script waited until PostgreSQL was running and healthy before `pg_restore`. |
| PASS | postgres-restore | PostgreSQL dump restored. |
| PASS | storage-restore | FileCenter storage volume restored. |
| PASS | compose-up | Full Compose stack started. |
| PASS | verify-local-stack | post-restore verification passed. |
| PASS | smoke | login, file list, upload, download/preview, share, trash restore and audit sample passed. |

## Notes

- The disposable stack used a local-only `COMPOSE_PROJECT_NAME=pcd_drill_test` so the destructive restore could not overwrite the original Compose project's runtime volumes.
- The restored backup did not contain an operator-known admin password for automated verification. For this disposable stack only, the local admin password was reset after restore to run the redacted smoke probe. The temporary password value is intentionally not recorded in this report or logs.
- The smoke probe prints only node ID/token suffixes and truncated hashes; it does not print access tokens, refresh tokens, cookies, full share URLs, or passwords.
- Older failed local attempt logs from this session are intentionally not referenced as acceptance evidence; the accepted evidence is the PASS restore log and PASS smoke log listed above.
