# Public repository P1 security gate review — 2026-05-22

Issue: GitHub #5
Kanban parent: `t_11afb7fa`
Downstream fix task: `t_ba4be5c2`

## Conclusion

SEC-P1-001 and SEC-P1-003 are addressed for public repository gatekeeping:

- local env files remain ignored and are now explicitly blocked from Git and release archives;
- public validation logcat evidence has been minimized to a sanitized PASS summary;
- CI has a dedicated redacted secret/log scan that fails on high-risk paths or raw credential-like values without printing any matched value.

## Evidence added

- `.github/workflows/security-gate.yml`: runs on push, pull request, and manual workflow dispatch.
- `scripts/secret-log-scan.py`: redacted scanner for public docs/scripts/workflows plus Git/archive path guardrails.
- `docs/validation/public-secret-log-scan-2026-05-22.md`: non-secret scan summary and operator verification commands.
- `docs/validation/android-logcat-storage-trust-boundary-2026-05-18.log`: already sanitized/minimized in the current workspace.

## Validation checklist

| Check | Expected result |
| --- | --- |
| `python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD` | `SECRET/LOG SCAN PASS: 0 findings`, values redacted by design |
| `git ls-files -- .env .env.secret` | no tracked local env files |
| release archive blocked-path grep | no `.env`, private key, pfx/p12/keystore path except explicit templates |
| `git diff --check` on gate/evidence files | pass |
| `python -m py_compile scripts/secret-log-scan.py` | pass |
| GitHub Issue #5 comment | include auditable evidence summary without secret plaintext |

## Residual risk and reviewer notes

- The workflow is intentionally blocking. A reviewer should approve the rule scope before merging.
- The scanner is tuned for public evidence surfaces (`docs/`, `.github/`, `scripts/`) and path-level Git/archive guardrails. Application source identifiers such as `PasswordHash` or `ShareToken` should remain covered by code review and backend tests rather than this public evidence leak gate.
- If future examples need credential-shaped placeholders, use obvious placeholder values such as `<redacted>`, `${PLACEHOLDER}`, `YOUR_TOKEN`, or `CHANGEME`; do not disable the gate.

## Rollback

Revert the security gate workflow, scanner, and public scan report together if a false positive blocks release. Do not reintroduce raw logcat or local `.env` files into Git; keep those artifacts local/ignored.
