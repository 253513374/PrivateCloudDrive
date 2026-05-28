# Public secret/log scan evidence — 2026-05-22

Scope: PrivateCloudDrive public repository release gate for SEC-P1-001 and SEC-P1-003.

## Gate coverage

- `scripts/secret-log-scan.py` blocks tracked local env files such as `.env`, `.env.secret`, private-key file paths, raw private key blocks, raw `Authorization` header values, URL secret query values, and non-placeholder token/password/secret assignments in public evidence surfaces.
- `.github/workflows/security-gate.yml` runs on `push`, `pull_request`, and `workflow_dispatch`.
- Scanner output is redacted by design: it reports only `path:line [rule]`, never matched secret values.
- `git archive --format=tar HEAD` is checked so release archives cannot include local env/private-key artifacts.

## Sanitized logcat status

`docs/validation/android-logcat-storage-trust-boundary-2026-05-18.log` has been reduced to a minimal public PASS summary. Raw device/system chatter, package inventory, tokens, cookies, account secrets, OAuth client keys, and complete share URLs are not retained in Git.

## Verification commands

Run from the isolated repository workspace:

```bash
python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD
git ls-files -- .env .env.secret
git archive --format=tar HEAD | tar -tf - | grep -Ei '(^|/)\.env(\.|$)|\.(pem|key|pfx|p12|keystore)$' | grep -Ev '(^|/)\.env\.example$'
git diff --check -- .github/workflows/security-gate.yml scripts/secret-log-scan.py docs/validation/public-secret-log-scan-2026-05-22.md docs/security-review-public-repo-p1-2026-05-22.md
python -m py_compile scripts/secret-log-scan.py
```

Expected result: secret/log scan reports `PASS: 0 findings`; `git ls-files` emits no tracked local env files; archive grep emits no blocked paths; diff check and Python compile pass.

## Rollback path

Revert these gate/evidence files together if the workflow blocks valid release work unexpectedly:

- `.github/workflows/security-gate.yml`
- `scripts/secret-log-scan.py`
- `docs/validation/public-secret-log-scan-2026-05-22.md`
- `docs/security-review-public-repo-p1-2026-05-22.md`

Do not restore raw logcat into public tracking. Keep raw logs as ignored local/CI artifacts only.
