param(
    [Parameter(Mandatory = $true)]
    [string]$BackupDirectory,
    [switch]$ConfirmDestructiveRestore,
    [switch]$RestoreRedis,
    [switch]$RestoreMinio,
    [switch]$SkipStart,
    [switch]$SkipVerify
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

function Test-CommandAvailable {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-ComposeContainerId {
    param([string]$Service)

    $result = Invoke-External "docker" @("compose", "ps", "-q", $Service) -AllowFailure
    if ($result.ExitCode -ne 0) {
        return $null
    }

    $id = ($result.Output -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($id)) {
        return $null
    }

    return $id.Trim()
}

function Get-ContainerState {
    param([string]$ContainerId)

    if ([string]::IsNullOrWhiteSpace($ContainerId)) {
        return $null
    }

    $result = Invoke-External "docker" @("inspect", "--format", "{{.State.Status}}", $ContainerId) -AllowFailure
    if ($result.ExitCode -ne 0) {
        return $null
    }

    return $result.Output.Trim()
}

function Get-ComposeServiceEnvValue {
    param(
        [string]$Service,
        [string]$Name,
        [string]$DefaultValue
    )

    $result = Invoke-External "docker" @("compose", "exec", "-T", $Service, "printenv", $Name) -AllowFailure
    if ($result.ExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($result.Output)) {
        return $DefaultValue
    }

    return $result.Output.Trim()
}

function Read-DotEnvKeys {
    param([string]$Path)

    $map = @{}
    if (-not (Test-Path $Path)) {
        return $map
    }

    foreach ($line in Get-Content $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) {
            continue
        }

        $index = $trimmed.IndexOf("=")
        if ($index -le 0) {
            continue
        }

        $key = $trimmed.Substring(0, $index).Trim()
        $value = $trimmed.Substring($index + 1).Trim().Trim('"').Trim("'")
        $map[$key] = $value
    }

    return $map
}

function Resolve-ComposeVolumeName {
    param(
        [string]$LogicalName,
        [string]$Service,
        [string]$ContainerPath,
        [object]$ManifestVolume
    )

    if ($null -ne $ManifestVolume -and -not [string]::IsNullOrWhiteSpace($ManifestVolume.dockerVolume)) {
        return $ManifestVolume.dockerVolume
    }

    $containerId = Get-ComposeContainerId $Service
    if (-not [string]::IsNullOrWhiteSpace($containerId)) {
        $mountsResult = Invoke-External "docker" @("inspect", "--format", "{{json .Mounts}}", $containerId) -AllowFailure
        if ($mountsResult.ExitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($mountsResult.Output)) {
            $mounts = $mountsResult.Output | ConvertFrom-Json
            $mount = $mounts | Where-Object { $_.Type -eq "volume" -and $_.Destination -eq $ContainerPath } | Select-Object -First 1
            if ($null -ne $mount -and -not [string]::IsNullOrWhiteSpace($mount.Name)) {
                return $mount.Name
            }
        }
    }

    $configResult = Invoke-External "docker" @("compose", "config", "--format", "json") -AllowFailure
    if ($configResult.ExitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($configResult.Output)) {
        $config = $configResult.Output | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace($config.name)) {
            return ("{0}_{1}" -f $config.name, $LogicalName)
        }
    }

    return $LogicalName
}

function Restore-NamedVolume {
    param(
        [string]$VolumeName,
        [string]$ArchiveName,
        [string]$BackupDirectory
    )

    $hostBackupDirectory = (Resolve-Path $BackupDirectory).Path
    Invoke-External "docker" @(
        "run",
        "--rm",
        "-v", ("{0}:/volume" -f $VolumeName),
        "-v", ("{0}:/backup:ro" -f $hostBackupDirectory),
        "alpine:3.20",
        "sh",
        "-c",
        ("find /volume -mindepth 1 -maxdepth 1 -exec rm -rf {} + && tar -xzf /backup/{0} -C /volume" -f $ArchiveName)
    ) | Out-Null
}

function Require-File {
    param(
        [string]$Path,
        [string]$Name
    )

    if (-not (Test-Path $Path)) {
        Add-CheckResult "FAIL" $Name ("Missing required file: {0}" -f $Path)
        throw ("Missing required file: {0}" -f $Path)
    }

    $length = (Get-Item $Path).Length
    if ($length -le 0) {
        Add-CheckResult "FAIL" $Name ("File is empty: {0}" -f $Path)
        throw ("File is empty: {0}" -f $Path)
    }

    Add-CheckResult "PASS" $Name ("Found ({0} bytes)." -f $length)
}

try {
    if (-not (Test-CommandAvailable "docker")) {
        Add-CheckResult "FAIL" "docker" "Docker CLI is not available."
        throw "Docker CLI is required."
    }

    Add-CheckResult "PASS" "docker" "Docker CLI is available."
    Invoke-External "docker" @("compose", "config", "--quiet") | Out-Null
    Add-CheckResult "PASS" "compose-config" "docker compose config is valid."

    if (-not (Test-Path $BackupDirectory)) {
        Add-CheckResult "FAIL" "backup-directory" ("Not found: {0}" -f $BackupDirectory)
        throw "Backup directory not found."
    }

    $backupPath = (Resolve-Path $BackupDirectory).Path
    $manifestPath = Join-Path $backupPath "manifest.json"
    $postgresDumpPath = Join-Path $backupPath "postgres.dump"
    $storageArchivePath = Join-Path $backupPath "storage.tar.gz"

    Require-File $manifestPath "manifest"
    Require-File $postgresDumpPath "postgres-dump"
    Require-File $storageArchivePath "storage-archive"

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.postgres.database) {
        Add-CheckResult "PASS" "manifest-postgres" ("Backup database: {0}." -f $manifest.postgres.database)
    }
    else {
        Add-CheckResult "WARN" "manifest-postgres" "Manifest does not include PostgreSQL database name; falling back to current Compose environment."
    }

    $storageVolumeName = Resolve-ComposeVolumeName "privateclouddrive_stack_storage" "api" "/app/storage" $manifest.storage
    if (-not $ConfirmDestructiveRestore) {
        Add-CheckResult "PASS" "storage-volume" ("Restore target storage volume: {0}." -f $storageVolumeName)
    }

    if (-not $ConfirmDestructiveRestore) {
        Add-CheckResult "WARN" "dry-run" "No data was changed. Re-run with -ConfirmDestructiveRestore to overwrite target PostgreSQL data and storage volume."
        Write-Host ""
        Write-Host "Restore plan:"
        Write-Host ("  Backup: {0}" -f $backupPath)
        $restoreSteps = New-Object System.Collections.Generic.List[string]
        $restoreSteps.Add("Stop API, media-worker, db-migrator, and MinIO if they are running.") | Out-Null
        $restoreSteps.Add("Start postgres/redis if needed.") | Out-Null
        $restoreSteps.Add("Restore postgres.dump with pg_restore --clean --if-exists.") | Out-Null
        $restoreSteps.Add(("Replace {0} with storage.tar.gz contents." -f $storageVolumeName)) | Out-Null
        if ($RestoreRedis) { $restoreSteps.Add("Restore redis-dump.rdb if present and verify Redis PONG.") | Out-Null }
        if ($RestoreMinio) { $restoreSteps.Add("Replace privateclouddrive_stack_minio_data with minio.tar.gz contents.") | Out-Null }
        if (-not $SkipStart) { $restoreSteps.Add("Start docker compose stack.") | Out-Null }
        if (-not $SkipVerify) { $restoreSteps.Add("Run scripts/verify-local-stack.ps1 -SkipStart.") | Out-Null }

        for ($i = 0; $i -lt $restoreSteps.Count; $i++) {
            Write-Host ("  {0}. {1}" -f ($i + 1), $restoreSteps[$i])
        }

        Write-Host ("Summary: PASS {0} / WARN {1} / FAIL {2}" -f $PassCount, $WarnCount, $FailCount)
        exit 0
    }

    Add-CheckResult "WARN" "destructive-restore" "Confirmed. Target PostgreSQL data and storage volume will be overwritten."

    Invoke-External "docker" @("compose", "stop", "api", "media-worker", "db-migrator", "minio") -AllowFailure | Out-Null
    Add-CheckResult "PASS" "stop-dependent-services" "Stopped API, media-worker, db-migrator, and MinIO if they were running."

    Invoke-External "docker" @("compose", "up", "-d", "postgres", "redis") | Out-Null
    Add-CheckResult "PASS" "compose-up-core" "Started postgres and redis services."

    $postgresContainerId = Get-ComposeContainerId "postgres"
    if ([string]::IsNullOrWhiteSpace($postgresContainerId) -or (Get-ContainerState $postgresContainerId) -ne "running") {
        Add-CheckResult "FAIL" "postgres" "PostgreSQL container is not running after compose up."
        throw "PostgreSQL container is required for restore."
    }

    $envValues = Read-DotEnvKeys (Join-Path (Get-Location) ".env")
    $postgresDbDefault = if ($envValues.ContainsKey("POSTGRES_DB") -and -not [string]::IsNullOrWhiteSpace($envValues["POSTGRES_DB"])) { $envValues["POSTGRES_DB"] } else { "PrivateCloudDrive" }
    $postgresUserDefault = if ($envValues.ContainsKey("POSTGRES_USER") -and -not [string]::IsNullOrWhiteSpace($envValues["POSTGRES_USER"])) { $envValues["POSTGRES_USER"] } else { "privateclouddrive" }
    $postgresDb = Get-ComposeServiceEnvValue "postgres" "POSTGRES_DB" $postgresDbDefault
    $postgresUser = Get-ComposeServiceEnvValue "postgres" "POSTGRES_USER" $postgresUserDefault

    $dumpInContainer = "/tmp/privateclouddrive-restore.dump"
    Invoke-External "docker" @("cp", $postgresDumpPath, ("{0}:{1}" -f $postgresContainerId, $dumpInContainer)) | Out-Null
    Invoke-External "docker" @("compose", "exec", "-T", "postgres", "pg_restore", "-U", $postgresUser, "-d", $postgresDb, "--clean", "--if-exists", "--no-owner", "--no-privileges", $dumpInContainer) | Out-Null
    Invoke-External "docker" @("compose", "exec", "-T", "postgres", "rm", "-f", $dumpInContainer) -AllowFailure | Out-Null
    Add-CheckResult "PASS" "postgres-restore" "Restored PostgreSQL dump."

    Restore-NamedVolume $storageVolumeName "storage.tar.gz" $backupPath
    Add-CheckResult "PASS" "storage-restore" ("Restored storage volume {0} from storage.tar.gz." -f $storageVolumeName)

    if ($RestoreRedis) {
        $redisDumpPath = Join-Path $backupPath "redis-dump.rdb"
        if (Test-Path $redisDumpPath) {
            Require-File $redisDumpPath "redis-dump"
            $redisContainerId = Get-ComposeContainerId "redis"
            if ([string]::IsNullOrWhiteSpace($redisContainerId)) {
                Add-CheckResult "WARN" "redis-restore" "Redis container not found; skipping Redis restore."
            }
            else {
                Invoke-External "docker" @("compose", "stop", "redis") | Out-Null
                $redisState = Get-ContainerState $redisContainerId
                if ($redisState -eq "running") {
                    Add-CheckResult "FAIL" "redis-stop" "Redis is still running after docker compose stop redis."
                    throw "Redis must be stopped before replacing dump.rdb."
                }

                Invoke-External "docker" @("cp", $redisDumpPath, ("{0}:/data/dump.rdb" -f $redisContainerId)) | Out-Null
                Invoke-External "docker" @("compose", "start", "redis") | Out-Null
                $redisPing = Invoke-External "docker" @("compose", "exec", "-T", "redis", "redis-cli", "ping") -AllowFailure
                if ($redisPing.ExitCode -eq 0 -and $redisPing.Output.Trim() -eq "PONG") {
                    Add-CheckResult "PASS" "redis-restore" "Restored redis-dump.rdb and verified Redis PONG."
                }
                else {
                    Add-CheckResult "FAIL" "redis-restore" "Redis did not return PONG after restore."
                    throw "Redis restore verification failed."
                }
            }
        }
        else {
            Add-CheckResult "WARN" "redis-restore" "redis-dump.rdb not found in backup directory."
        }
    }

    if ($RestoreMinio) {
        $minioArchivePath = Join-Path $backupPath "minio.tar.gz"
        if (Test-Path $minioArchivePath) {
            Require-File $minioArchivePath "minio-archive"
            $minioVolumeName = Resolve-ComposeVolumeName "privateclouddrive_stack_minio_data" "minio" "/data" $null
            Restore-NamedVolume $minioVolumeName "minio.tar.gz" $backupPath
            Add-CheckResult "PASS" "minio-restore" ("Restored MinIO volume {0} from minio.tar.gz." -f $minioVolumeName)
        }
        else {
            Add-CheckResult "WARN" "minio-restore" "minio.tar.gz not found in backup directory."
        }
    }

    if (-not $SkipStart) {
        Invoke-External "docker" @("compose", "up", "-d", "--build") | Out-Null
        Add-CheckResult "PASS" "compose-up" "Started full Compose stack."
    }

    if (-not $SkipVerify) {
        $verifyScript = Join-Path (Get-Location) "scripts/verify-local-stack.ps1"
        if (Test-Path $verifyScript) {
            Invoke-External "powershell" @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $verifyScript, "-SkipStart") | Out-Null
            Add-CheckResult "PASS" "verify-local-stack" "verify-local-stack.ps1 -SkipStart passed."
        }
        else {
            Add-CheckResult "WARN" "verify-local-stack" "scripts/verify-local-stack.ps1 not found; skipped verification."
        }
    }

    Write-Host ""
    Write-Host ("Summary: PASS {0} / WARN {1} / FAIL {2}" -f $PassCount, $WarnCount, $FailCount)

    if ($FailCount -gt 0) {
        exit 1
    }
}
catch {
    Add-CheckResult "FAIL" "restore" $_.Exception.Message
    Write-Host ""
    Write-Host ("Summary: PASS {0} / WARN {1} / FAIL {2}" -f $PassCount, $WarnCount, $FailCount)
    exit 1
}
