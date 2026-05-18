param(
    [string]$OutputDirectory = "artifacts/backups",
    [string]$ReportDirectory = "docs/validation",
    [switch]$IncludeRedis,
    [switch]$IncludeMinio
)

$ErrorActionPreference = "Stop"
$PassCount = 0
$WarnCount = 0
$FailCount = 0
$Results = New-Object System.Collections.Generic.List[object]

function Add-CheckResult {
    param(
        [ValidateSet("PASS", "WARN", "FAIL")]
        [string]$Status,
        [string]$Name,
        [string]$Message
    )

    $script:Results.Add([pscustomobject]@{
        Status = $Status
        Name = $Name
        Message = $Message
    }) | Out-Null

    switch ($Status) {
        "PASS" { $script:PassCount++ }
        "WARN" { $script:WarnCount++ }
        "FAIL" { $script:FailCount++ }
    }

    Write-Host ("[{0}] {1} - {2}" -f $Status, $Name, $Message)
}

function Invoke-External {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & $FilePath @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    $result = [pscustomobject]@{
        ExitCode = $exitCode
        Output = ($output -join [Environment]::NewLine)
    }

    if (-not $AllowFailure -and $exitCode -ne 0) {
        throw ("Command failed: {0} {1}`n{2}" -f $FilePath, ($Arguments -join " "), $result.Output)
    }

    return $result
}

function Get-LatestBackupDirectory {
    param([string]$Root)

    if (-not (Test-Path $Root)) {
        return $null
    }

    return Get-ChildItem -Path $Root -Directory |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Assert-BackupFile {
    param(
        [string]$Path,
        [string]$Name
    )

    if (-not (Test-Path $Path)) {
        Add-CheckResult "FAIL" $Name ("Missing required backup file: {0}" -f $Path)
        throw ("Missing required backup file: {0}" -f $Path)
    }

    $length = (Get-Item $Path).Length
    if ($length -le 0) {
        Add-CheckResult "FAIL" $Name ("Backup file is empty: {0}" -f $Path)
        throw ("Backup file is empty: {0}" -f $Path)
    }

    Add-CheckResult "PASS" $Name ("Found ({0} bytes)." -f $length)
}

try {
    $repoRoot = (Get-Location).Path
    $backupScript = Join-Path $repoRoot "scripts/backup-local-stack.ps1"
    $restoreScript = Join-Path $repoRoot "scripts/restore-local-stack.ps1"

    if (-not (Test-Path $backupScript)) {
        Add-CheckResult "FAIL" "backup-script" ("Not found: {0}" -f $backupScript)
        throw "backup-local-stack.ps1 is required."
    }
    Add-CheckResult "PASS" "backup-script" "backup-local-stack.ps1 found."

    if (-not (Test-Path $restoreScript)) {
        Add-CheckResult "FAIL" "restore-script" ("Not found: {0}" -f $restoreScript)
        throw "restore-local-stack.ps1 is required."
    }
    Add-CheckResult "PASS" "restore-script" "restore-local-stack.ps1 found."

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $ReportDirectory | Out-Null

    $startedAt = Get-Date
    $backupArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $backupScript, "-OutputDirectory", $OutputDirectory)
    if ($IncludeRedis) { $backupArgs += "-IncludeRedis" }
    if ($IncludeMinio) { $backupArgs += "-IncludeMinio" }

    Add-CheckResult "PASS" "drill-mode" "Running non-destructive drill: backup + restore dry-run only; .env is not copied."
    $backupResult = Invoke-External "powershell" $backupArgs
    Add-CheckResult "PASS" "backup-command" "Backup command completed."

    $backupDirectoryLine = $backupResult.Output -split "`r?`n" | Where-Object { $_ -match '^Backup directory:\s*(.+)$' } | Select-Object -Last 1
    if ($backupDirectoryLine -match '^Backup directory:\s*(.+)$') {
        $backupPath = $Matches[1].Trim()
    }
    else {
        $latest = Get-LatestBackupDirectory $OutputDirectory
        if ($null -eq $latest) {
            Add-CheckResult "FAIL" "backup-directory" "Backup completed but no backup directory could be found."
            throw "Backup directory not found."
        }
        $backupPath = $latest.FullName
        Add-CheckResult "WARN" "backup-directory" "Backup output did not include directory line; using latest backup directory."
    }

    if (-not (Test-Path $backupPath)) {
        Add-CheckResult "FAIL" "backup-directory" ("Not found: {0}" -f $backupPath)
        throw "Backup directory does not exist."
    }
    Add-CheckResult "PASS" "backup-directory" $backupPath

    $manifestPath = Join-Path $backupPath "manifest.json"
    $postgresDumpPath = Join-Path $backupPath "postgres.dump"
    $storageArchivePath = Join-Path $backupPath "storage.tar.gz"
    $envChecklistPath = Join-Path $backupPath "ENVIRONMENT-REQUIRED.md"

    Assert-BackupFile $manifestPath "manifest"
    Assert-BackupFile $postgresDumpPath "postgres-dump"
    Assert-BackupFile $storageArchivePath "storage-archive"

    if (Test-Path (Join-Path $backupPath ".env.secret")) {
        Add-CheckResult "FAIL" "env-secret" ".env.secret exists in drill backup; this drill must not copy secrets."
        throw "Unexpected .env.secret in non-destructive drill backup."
    }
    Add-CheckResult "PASS" "env-secret" "No .env.secret copied."

    if (Test-Path $envChecklistPath) {
        Add-CheckResult "PASS" "environment-checklist" "ENVIRONMENT-REQUIRED.md is present."
    }
    else {
        Add-CheckResult "WARN" "environment-checklist" "ENVIRONMENT-REQUIRED.md is missing; restore operator must recreate .env manually."
    }

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.summary.fail -gt 0) {
        Add-CheckResult "FAIL" "backup-manifest" ("Manifest reports failures: {0}" -f $manifest.summary.fail)
        throw "Backup manifest contains failures."
    }
    Add-CheckResult "PASS" "backup-manifest" ("Manifest summary PASS {0} / WARN {1} / FAIL {2}." -f $manifest.summary.pass, $manifest.summary.warn, $manifest.summary.fail)

    $restoreArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $restoreScript, "-BackupDirectory", $backupPath)
    if ($IncludeRedis) { $restoreArgs += "-RestoreRedis" }
    if ($IncludeMinio) { $restoreArgs += "-RestoreMinio" }

    $restoreDryRunResult = Invoke-External "powershell" $restoreArgs
    Add-CheckResult "PASS" "restore-dry-run" "Restore dry-run completed without destructive changes."

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $reportPath = Join-Path $ReportDirectory ("backup-restore-drill-{0}.md" -f $timestamp)
    $backupLogPath = Join-Path $ReportDirectory ("backup-restore-drill-backup-{0}.log" -f $timestamp)
    $restoreLogPath = Join-Path $ReportDirectory ("backup-restore-drill-restore-dry-run-{0}.log" -f $timestamp)

    Set-Content -Path $backupLogPath -Value $backupResult.Output -Encoding UTF8
    Set-Content -Path $restoreLogPath -Value $restoreDryRunResult.Output -Encoding UTF8
    Add-CheckResult "PASS" "report-logs" ("Wrote drill logs: {0}; {1}" -f $backupLogPath, $restoreLogPath)
    Add-CheckResult "PASS" "report" ("Wrote drill report: {0}" -f $reportPath)

    $finishedAt = Get-Date
    $resultRows = $Results | ForEach-Object { "| $($_.Status) | $($_.Name) | $($_.Message -replace '\|', '/') |" }
    $files = @(
        @{ Name = "manifest.json"; Path = $manifestPath },
        @{ Name = "postgres.dump"; Path = $postgresDumpPath },
        @{ Name = "storage.tar.gz"; Path = $storageArchivePath },
        @{ Name = "ENVIRONMENT-REQUIRED.md"; Path = $envChecklistPath }
    )
    $fileRows = $files | ForEach-Object {
        if (Test-Path $_.Path) {
            $length = (Get-Item $_.Path).Length
            "| $($_.Name) | present | $length |"
        }
        else {
            "| $($_.Name) | missing | - |"
        }
    }

    $report = @"
# Backup / Restore Drill Report

- Started: $($startedAt.ToString("o"))
- Finished: $($finishedAt.ToString("o"))
- Mode: non-destructive backup + restore dry-run
- Backup directory: $($backupPath)
- Include Redis: $IncludeRedis
- Include MinIO: $IncludeMinio
- Secrets copied: no (.env.secret must not exist in this drill)

## Summary

- PASS: $PassCount
- WARN: $WarnCount
- FAIL: $FailCount

## Checks

| Status | Check | Message |
|---|---|---|
$($resultRows -join [Environment]::NewLine)

## Backup Files

| File | Status | Bytes |
|---|---|---:|
$($fileRows -join [Environment]::NewLine)

## Logs

- Backup command log: $($backupLogPath)
- Restore dry-run log: $($restoreLogPath)

## Acceptance Notes

- This report proves that the current local Compose stack can create and validate the minimum backup artifact set: PostgreSQL dump, FileCenter storage archive, manifest, and environment checklist.
- A very small `storage.tar.gz` means the local storage volume may currently contain no file payloads. In that case, this drill proves the backup/restore control path, not successful recovery of user file bytes; run a destructive restore exercise against a disposable stack with seeded files before production sign-off.
- The restore script was executed without `-ConfirmDestructiveRestore`, so no target data was overwritten.
- A real disaster recovery exercise must run destructive restore only against a disposable test stack or test machine, then verify login, file list, file preview/download, thumbnails, and sharing behavior.
- `.env` remains operator-owned sensitive configuration. This drill intentionally writes `ENVIRONMENT-REQUIRED.md` instead of copying secrets.
"@

    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    Write-Host ""
    Write-Host ("Report: {0}" -f $reportPath)
    Write-Host ("Summary: PASS {0} / WARN {1} / FAIL {2}" -f $PassCount, $WarnCount, $FailCount)

    if ($FailCount -gt 0) {
        exit 1
    }
}
catch {
    Add-CheckResult "FAIL" "drill" $_.Exception.Message
    Write-Host ""
    Write-Host ("Summary: PASS {0} / WARN {1} / FAIL {2}" -f $PassCount, $WarnCount, $FailCount)
    exit 1
}
