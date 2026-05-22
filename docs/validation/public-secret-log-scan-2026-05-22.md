# Public repo secret/log scan evidence

Date: 2026-05-22
Task: t_ba4be5c2
Related GitHub issue: #5

## Summary

The public repository now has a blocking secret/log gate for tracked files, non-ignored working-tree additions and release archives. The scanner reports only path, line and rule identifiers; it never prints matched secret values.

## Gate commands

```bash
python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD
git ls-files -- .env .env.secret
git archive --format=tar HEAD | tar -tf - | grep -Ei '(^|/)\.env(\.|$)|\.(pem|key|pfx)$' | grep -Ev '(^|/)\.env\.example$'
```

## Expected public result

- Secret/log scan: PASS, 0 findings.
- Tracked local env files: none.
- Release archive env/private-key entries: none.
- Previously tracked raw Android logcat evidence: replaced with minimized PASS evidence.

## Notes

Allowed examples/placeholders such as `<redacted>`, `${ENV_VAR}` and `.env.example` are not treated as leaks. Real private key blocks, bearer/basic authorization values, URL secret parameters, tracked `.env` files and non-placeholder password/token/secret assignments in public validation evidence fail the gate.
