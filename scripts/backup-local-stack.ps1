param(
    [string]$OutputDirectory = "artifacts/backups",
    [switch]$IncludeRedis,
    [switch]$IncludeMinio,
    [switch]$IncludeEnv
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

function Resolve-ComposeVolumeName {
    param(
        [string]$LogicalName,
        [string]$Service,
        [string]$ContainerPath
    )

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

function Copy-DockerFileFromContainer {
    param(
        [string]$ContainerId,
        [string]$ContainerPath,
        [string]$DestinationPath
    )

    Invoke-External "docker" @("cp", ("{0}:{1}" -f $ContainerId, $ContainerPath), $DestinationPath) | Out-Null
    if (-not (Test-Path $DestinationPath)) {
        throw ("Expected backup file was not copied: {0}" -f $DestinationPath)
    }

    $length = (Get-Item $DestinationPath).Length
    if ($length -le 0) {
        throw ("Backup file is empty: {0}" -f $DestinationPath)
    }

    return $length
}

function Backup-NamedVolume {
    param(
        [string]$VolumeName,
        [string]$ArchiveName,
        [string]$BackupDirectory
    )

    $hostBackupDirectory = (Resolve-Path $BackupDirectory).Path
    Invoke-External "docker" @(
        "run",
        "--rm",
        "-v", ("{0}:/volume:ro" -f $VolumeName),
        "-v", ("{0}:/backup" -f $hostBackupDirectory),
        "alpine:3.20",
        "tar",
        "-czf",
        ("/backup/{0}" -f $ArchiveName),
        "-C",
        "/volume",
        "."
    ) | Out-Null

    $archivePath = Join-Path $BackupDirectory $ArchiveName
    if (-not (Test-Path $archivePath)) {
        throw ("Expected volume archive was not created: {0}" -f $archivePath)
    }

    $length = (Get-Item $archivePath).Length
    if ($length -le 0) {
        throw ("Volume archive is empty: {0}" -f $archivePath)
    }

    return $length
}

try {
    if (-not (Test-CommandAvailable "docker")) {
        Add-CheckResult "FAIL" "docker" "Docker CLI is not available."
        throw "Docker CLI is required."
    }

    Add-CheckResult "PASS" "docker" "Docker CLI is available."
    Invoke-External "docker" @("compose", "config", "--quiet") | Out-Null
    Add-CheckResult "PASS" "compose-config" "docker compose config is valid."

    $envValues = Read-DotEnvKeys (Join-Path (Get-Location) ".env")
    $postgresDbDefault = if ($envValues.ContainsKey("POSTGRES_DB") -and -not [string]::IsNullOrWhiteSpace($envValues["POSTGRES_DB"])) { $envValues["POSTGRES_DB"] } else { "PrivateCloudDrive" }
    $postgresUserDefault = if ($envValues.ContainsKey("POSTGRES_USER") -and -not [string]::IsNullOrWhiteSpace($envValues["POSTGRES_USER"])) { $envValues["POSTGRES_USER"] } else { "privateclouddrive" }

    $postgresContainerId = Get-ComposeContainerId "postgres"
    if ([string]::IsNullOrWhiteSpace($postgresContainerId)) {
        Add-CheckResult "FAIL" "postgres" "PostgreSQL service container was not found. Run docker compose up -d first."
        throw "PostgreSQL container is required for backup."
    }

    $postgresState = Get-ContainerState $postgresContainerId
    if ($postgresState -ne "running") {
        Add-CheckResult "FAIL" "postgres" ("PostgreSQL container is not running: {0}" -f $postgresState)
        throw "PostgreSQL container must be running."
    }

    Add-CheckResult "PASS" "postgres" "PostgreSQL container is running."
    $postgresDb = Get-ComposeServiceEnvValue "postgres" "POSTGRES_DB" $postgresDbDefault
    $postgresUser = Get-ComposeServiceEnvValue "postgres" "POSTGRES_USER" $postgresUserDefault

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupPath = Join-Path $OutputDirectory $timestamp
    New-Item -ItemType Directory -Force -Path $backupPath | Out-Null
    $backupPath = (Resolve-Path $backupPath).Path

    $gitCommitResult = Invoke-External "git" @("rev-parse", "--short", "HEAD") -AllowFailure
    $gitCommit = if ($gitCommitResult.ExitCode -eq 0) { $gitCommitResult.Output.Trim() } else { $null }

    $manifest = [ordered]@{
        createdAt = (Get-Date).ToString("o")
        gitCommit = $gitCommit
        composeProject = (Split-Path -Leaf (Get-Location))
        postgres = [ordered]@{
            database = $postgresDb
            user = $postgresUser
            dump = "postgres.dump"
            format = "pg_dump custom"
        }
        files = New-Object System.Collections.Generic.List[object]
        notes = New-Object System.Collections.Generic.List[string]
    }

    $dumpInContainer = "/tmp/privateclouddrive-postgres.dump"
    Invoke-External "docker" @("compose", "exec", "-T", "postgres", "pg_dump", "-U", $postgresUser, "-d", $postgresDb, "--format=custom", "--no-owner", "--no-privileges", "--file", $dumpInContainer) | Out-Null
    $dbDumpPath = Join-Path $backupPath "postgres.dump"
    $dbDumpSize = Copy-DockerFileFromContainer $postgresContainerId $dumpInContainer $dbDumpPath
    Invoke-External "docker" @("compose", "exec", "-T", "postgres", "rm", "-f", $dumpInContainer) -AllowFailure | Out-Null
    $manifest.files.Add([ordered]@{ path = "postgres.dump"; bytes = $dbDumpSize; purpose = "PostgreSQL logical backup" }) | Out-Null
    Add-CheckResult "PASS" "postgres-dump" ("Created PostgreSQL dump ({0} bytes)." -f $dbDumpSize)
    $pgSha256 = (Get-FileHash -Path $dbDumpPath -Algorithm SHA256).Hash.ToLower()
    Add-CheckResult "PASS" "checksum:postgres-dump" ("SHA256 {0}." -f $pgSha256)

    $storageVolumeName = Resolve-ComposeVolumeName "privateclouddrive_stack_storage" "api" "/app/storage"
    $manifest.storage = [ordered]@{
        logicalVolume = "privateclouddrive_stack_storage"
        dockerVolume = $storageVolumeName
        mountPath = "/app/storage"
    }
    $storageArchiveSize = Backup-NamedVolume $storageVolumeName "storage.tar.gz" $backupPath
    $storageSha256 = (Get-FileHash -Path (Join-Path $backupPath "storage.tar.gz") -Algorithm SHA256).Hash.ToLower()
    Add-CheckResult "PASS" "checksum:storage-volume" ("SHA256 {0}." -f $storageSha256)

    $manifest.checksums = [ordered]@{
        "postgres.dump" = $pgSha256
        "storage.tar.gz" = $storageSha256
    }
    $manifest.files.Add([ordered]@{ path = "storage.tar.gz"; bytes = $storageArchiveSize; purpose = "FileCenter local storage, upload temp files, thumbnails, and video covers"; dockerVolume = $storageVolumeName }) | Out-Null
    Add-CheckResult "PASS" "storage-volume" ("Archived storage volume {0} ({1} bytes)." -f $storageVolumeName, $storageArchiveSize)

    if ($IncludeRedis) {
        $redisContainerId = Get-ComposeContainerId "redis"
        if ([string]::IsNullOrWhiteSpace($redisContainerId) -or (Get-ContainerState $redisContainerId) -ne "running") {
            Add-CheckResult "WARN" "redis" "Redis container is not running; skipping Redis RDB backup."
        }
        else {
            Invoke-External "docker" @("compose", "exec", "-T", "redis", "redis-cli", "SAVE") | Out-Null
            $redisDumpPath = Join-Path $backupPath "redis-dump.rdb"
            $redisDumpSize = Copy-DockerFileFromContainer $redisContainerId "/data/dump.rdb" $redisDumpPath
            $manifest.files.Add([ordered]@{ path = "redis-dump.rdb"; bytes = $redisDumpSize; purpose = "Optional Redis cache/rate-limit snapshot" }) | Out-Null
            Add-CheckResult "PASS" "redis-dump" ("Copied Redis RDB snapshot ({0} bytes)." -f $redisDumpSize)
        }
    }
    else {
        $manifest.notes.Add("Redis is not included by default because it stores cache, rate-limit counters, and transient tickets. Pass -IncludeRedis if a point-in-time cache snapshot is required.") | Out-Null
        Add-CheckResult "WARN" "redis-dump" "Skipped. Pass -IncludeRedis to include Redis dump.rdb."
    }

    if ($IncludeMinio) {
        $minioVolumeName = Resolve-ComposeVolumeName "privateclouddrive_stack_minio_data" "minio" "/data"
        $minioArchiveSize = Backup-NamedVolume $minioVolumeName "minio.tar.gz" $backupPath
        $manifest.files.Add([ordered]@{ path = "minio.tar.gz"; bytes = $minioArchiveSize; purpose = "Optional MinIO profile data"; dockerVolume = $minioVolumeName }) | Out-Null
        Add-CheckResult "PASS" "minio-volume" ("Archived MinIO volume {0} ({1} bytes)." -f $minioVolumeName, $minioArchiveSize)
    }

    if ($IncludeEnv) {
        $envPath = Join-Path (Get-Location) ".env"
        if (Test-Path $envPath) {
            Copy-Item $envPath (Join-Path $backupPath ".env.secret") -Force
            $manifest.files.Add([ordered]@{ path = ".env.secret"; purpose = "Sensitive environment file; protect this file" }) | Out-Null
            $manifest.notes.Add(".env.secret contains secrets. Store the backup in encrypted storage and never commit it.") | Out-Null
            Add-CheckResult "WARN" ".env" "Copied .env as .env.secret. Treat the backup directory as sensitive."
        }
        else {
            Add-CheckResult "WARN" ".env" "No .env file found to copy."
        }
    }
    else {
        $envReadme = @"
# Environment file not included

This backup intentionally does not include `.env` because it may contain secrets.

Before restore, recreate `.env` from `.env.example` and set values that match the backed-up deployment, especially:

- POSTGRES_DB
- POSTGRES_USER
- POSTGRES_PASSWORD
- STRING_ENCRYPTION_PASSPHRASE
- PUBLIC_URL
- FILECENTER_STORAGE_PROVIDER and provider-specific keys
- External login provider secrets if enabled

Run the backup command with `-IncludeEnv` only when the backup target is encrypted and access-controlled.
"@
        Set-Content -Path (Join-Path $backupPath "ENVIRONMENT-REQUIRED.md") -Value $envReadme -Encoding UTF8
        $manifest.files.Add([ordered]@{ path = "ENVIRONMENT-REQUIRED.md"; purpose = "Restore-time environment checklist" }) | Out-Null
    }

    $manifest.results = $Results
    $manifest.summary = [ordered]@{
        pass = $PassCount
        warn = $WarnCount
        fail = $FailCount
    }

    $manifestPath = Join-Path $backupPath "manifest.json"
    ($manifest | ConvertTo-Json -Depth 6) | Set-Content -Path $manifestPath -Encoding UTF8
    Add-CheckResult "PASS" "manifest" ("Wrote manifest: {0}" -f $manifestPath)
    $manifest.results = $Results
    $manifest.summary = [ordered]@{
        pass = $PassCount
        warn = $WarnCount
        fail = $FailCount
    }
    ($manifest | ConvertTo-Json -Depth 6) | Set-Content -Path $manifestPath -Encoding UTF8

    Write-Host ""
    Write-Host ("Backup directory: {0}" -f $backupPath)
    Write-Host ("Summary: PASS {0} / WARN {1} / FAIL {2}" -f $PassCount, $WarnCount, $FailCount)

    if ($FailCount -gt 0) {
        exit 1
    }
}
catch {
    Add-CheckResult "FAIL" "backup" $_.Exception.Message
    Write-Host ""
    Write-Host ("Summary: PASS {0} / WARN {1} / FAIL {2}" -f $PassCount, $WarnCount, $FailCount)
    exit 1
}
