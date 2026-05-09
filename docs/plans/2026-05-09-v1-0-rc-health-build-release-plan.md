# V1.0 RC Health, Build, and Release Documentation Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Add the first V1.0 RC delivery assets: local stack health verification, MAUI sequential build verification, and release acceptance documentation.

**Architecture:** Keep this as a low-risk release-engineering pass. Do not add new business features or change ABP runtime behavior. Scripts live under `scripts/`, documentation lives under `docs/`, and all checks must avoid printing passwords, tokens, OAuth codes, client secrets, or provider access tokens.

**Tech Stack:** PowerShell 7+/Windows PowerShell compatible scripts, Docker Compose, .NET 10, .NET MAUI, Markdown documentation.

---

## Task 1: Add a V1.0 RC health verification script

**Objective:** Create a script that can run in preflight mode or full-stack mode and report PASS/WARN/FAIL for Docker, Compose services, Swagger, PostgreSQL, Redis, storage volume, ffmpeg/ffprobe, and relevant configuration boundaries.

**Files:**
- Create: `scripts/verify-local-stack.ps1`
- Reference: `scripts/verify-docker-stack.ps1`
- Reference: `docker-compose.yml`
- Reference: `.env.example`

**Requirements:**
- Parameters:
  - `-PreflightOnly`
  - `-SkipStart`
  - `-TimeoutSeconds` default 300
  - `-PublicUrl` default `http://localhost:8080`
- Preflight must check:
  - Docker CLI available.
  - Docker Compose available.
  - `docker compose config` succeeds.
  - Required services exist: `postgres`, `redis`, `db-migrator`, `api`, `media-worker`.
  - `.env` exists; if missing, WARN with instruction to copy `.env.example`.
  - `.env` should not keep template/empty critical values for production-like RC: `STRING_ENCRYPTION_PASSPHRASE`, `POSTGRES_PASSWORD`, `PUBLIC_URL`.
- Full mode must optionally start stack unless `-SkipStart` is passed.
- Full mode must check:
  - `postgres` healthy.
  - `redis` healthy.
  - `db-migrator` exited 0.
  - `api` running.
  - `media-worker` running.
  - Swagger URL returns 200.
  - API container can see storage path, default `/app/storage`.
  - API or media-worker has `ffmpeg` and `ffprobe` available.
- Output must use PASS/WARN/FAIL, maintain counters, and exit non-zero if any FAIL exists.
- Never print secret values; only print variable names and remediation hints.

**Verification:**

```powershell
.\scripts\verify-local-stack.ps1 -PreflightOnly
```

Expected: returns PASS/WARN/FAIL summary. WARN is acceptable if `.env` is intentionally missing in dev; FAIL only for missing Docker/invalid Compose/missing service definitions.

---

## Task 2: Add a sequential MAUI build verification script

**Objective:** Create a script that builds MAUI Windows and Android targets sequentially to avoid multi-target restore/build collisions.

**Files:**
- Create: `scripts/verify-maui-build.ps1`
- Reference: `maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj`
- Reference: `README.md`
- Reference: `docs/testing.md`

**Requirements:**
- Parameters:
  - `-Configuration` default `Debug`
  - `-SkipWindows`
  - `-SkipAndroid`
  - `-NoRestore`
- Build Windows target:
  - `net10.0-windows10.0.19041.0`
  - `RuntimeIdentifier=win-x64`
  - `TargetFrameworks=net10.0-windows10.0.19041.0`
- Build Android target:
  - `net10.0-android`
  - `TargetFrameworks=net10.0-android`
- Run targets sequentially, never in parallel.
- Print PASS/WARN/FAIL summary and exact failed target.
- Exit non-zero if a target fails.

**Verification:**

```powershell
.\scripts\verify-maui-build.ps1 -SkipAndroid
```

Expected: Windows build succeeds on Windows MAUI environment. Android can be skipped when workload/device environment is unavailable.

---

## Task 3: Add V1.0 RC release acceptance documentation

**Objective:** Create a single release acceptance checklist and release notes draft for V1.0 RC.

**Files:**
- Create: `docs/release-notes-v1.0-rc.md`
- Modify: `docs/testing.md`
- Modify: `docs/deployment.md`
- Modify: `README.md`

**Requirements for `docs/release-notes-v1.0-rc.md`:**
- Include product positioning.
- Include included features.
- Include excluded/non-goal features.
- Include deployment prerequisites.
- Include release acceptance checklist:
  - backend build/test
  - Docker config
  - local stack health script
  - MAUI build script
  - Android real-device main-flow acceptance
  - WeChat/Google/GitHub optional-provider boundary
  - backup/restore readiness
- Include known limitations:
  - WeChat true E2E requires real credentials/device.
  - iOS WeChat implementation pending.
  - No desktop sync/NAS protocols/AI album.
- Include security notes: no secrets in logs, provider secrets backend-only, storage volume must be backed up.

**Requirements for docs updates:**
- `docs/testing.md` should reference `scripts/verify-local-stack.ps1` and `scripts/verify-maui-build.ps1`.
- `docs/deployment.md` should explain when to run preflight vs full stack verification.
- `README.md` validation section should reference the new V1.0 RC verification scripts.

**Verification:**

```powershell
git diff --check
```

Expected: no whitespace errors.

---

## Task 4: Run validation

**Objective:** Verify the new scripts and docs are usable without leaking secrets.

**Commands:**

```powershell
.\scripts\verify-local-stack.ps1 -PreflightOnly
.\scripts\verify-maui-build.ps1 -SkipAndroid
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-local-stack.ps1 -PreflightOnly
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-maui-build.ps1 -SkipAndroid
git diff --check
docker compose config
```

**Expected:**
- Preflight script runs and reports summary.
- MAUI script runs at least Windows build unless the environment lacks MAUI workload; if build fails due environment, report as environment blocker.
- `git diff --check` passes.
- `docker compose config` passes.

---

## Out of scope

- Do not add new API endpoints.
- Do not change business logic.
- Do not commit changes automatically.
- Do not print `.env` values.
- Do not enable WeChat/Google/GitHub by default.
