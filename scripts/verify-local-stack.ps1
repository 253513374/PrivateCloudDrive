param(
    [switch]$PreflightOnly,
    [switch]$SkipStart,
    [int]$TimeoutSeconds = 300,
    [string]$PublicUrl = "http://localhost:8080"
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
        [string[]]$Arguments
    )

    $output = & $FilePath @Arguments 2>&1
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = ($output -join [Environment]::NewLine)
    }
}

function Test-CommandAvailable {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-ComposeServices {
    $result = Invoke-External "docker" @("compose", "config", "--services")
    if ($result.ExitCode -ne 0) {
        Add-CheckResult "FAIL" "compose-services" "Unable to list Compose services. Run docker compose config for details."
        return @()
    }

    return @($result.Output -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
}

function Get-ComposeContainerId {
    param([string]$Service)

    $output = & docker compose ps -a -q $Service 2>$null
    if ($LASTEXITCODE -ne 0 -or $null -eq $output) {
        return $null
    }

    $id = ($output | Select-Object -First 1).ToString().Trim()
    if ([string]::IsNullOrWhiteSpace($id)) {
        return $null
    }

    return $id
}

function Get-ContainerInspectValue {
    param(
        [string]$ContainerId,
        [string]$Format
    )

    $value = & docker inspect --format $Format $ContainerId 2>$null
    if ($LASTEXITCODE -ne 0 -or $null -eq $value) {
        return $null
    }

    return ($value | Select-Object -First 1).ToString().Trim()
}

function Wait-Condition {
    param(
        [string]$Name,
        [scriptblock]$Condition,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            if (& $Condition) {
                Add-CheckResult "PASS" $Name "Ready."
                return $true
            }
        }
        catch {
            Add-CheckResult "FAIL" $Name $_.Exception.Message
            return $false
        }

        Start-Sleep -Seconds 3
    }

    Add-CheckResult "FAIL" $Name ("Timed out after {0} seconds." -f $TimeoutSeconds)
    return $false
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

function Test-EnvConfiguration {
    $envPath = Join-Path (Get-Location) ".env"
    if (-not (Test-Path $envPath)) {
        Add-CheckResult "WARN" ".env" "Missing .env. Copy .env.example to .env before an RC deployment and replace template secrets."
        return
    }

    Add-CheckResult "PASS" ".env" "Found .env. Secret values are not printed."
    $envValues = Read-DotEnvKeys $envPath

    $criticalKeys = @("STRING_ENCRYPTION_PASSPHRASE", "POSTGRES_PASSWORD", "PUBLIC_URL")
    foreach ($key in $criticalKeys) {
        if (-not $envValues.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($envValues[$key])) {
            Add-CheckResult "WARN" "env:$key" "Missing or empty. Set this before RC deployment."
            continue
        }

        $value = $envValues[$key]
        if ($key -eq "STRING_ENCRYPTION_PASSPHRASE" -and $value -match "change-this|secret|template") {
            Add-CheckResult "WARN" "env:$key" "Looks like a template value. Replace it before RC deployment."
        }
        elseif ($key -eq "POSTGRES_PASSWORD" -and $value -match "^(privateclouddrive|postgres|password|changeme)$") {
            Add-CheckResult "WARN" "env:$key" "Looks like a default password. Replace it before RC deployment."
        }
        elseif ($key -eq "PUBLIC_URL" -and $value -match "localhost|127\.0\.0\.1") {
            Add-CheckResult "WARN" "env:$key" "Uses a local URL. OK for local validation; set a device-reachable URL for mobile RC testing."
        }
        else {
            Add-CheckResult "PASS" "env:$key" "Configured. Value hidden."
        }
    }

    $storageProvider = "FileSystem"
    if ($envValues.ContainsKey("FILECENTER_STORAGE_PROVIDER") -and
        -not [string]::IsNullOrWhiteSpace($envValues["FILECENTER_STORAGE_PROVIDER"])) {
        $storageProvider = $envValues["FILECENTER_STORAGE_PROVIDER"].Trim()
    }

    if ($storageProvider -notin @("FileSystem", "AliyunOss")) {
        Add-CheckResult "FAIL" "env:FILECENTER_STORAGE_PROVIDER" "Unsupported value. Use FileSystem or AliyunOss."
        return
    }

    Add-CheckResult "PASS" "env:FILECENTER_STORAGE_PROVIDER" ("Using {0}." -f $storageProvider)

    if ($storageProvider -eq "AliyunOss") {
        $ossRequiredKeys = @(
            "ALIYUN_OSS_ACCESS_KEY_ID",
            "ALIYUN_OSS_ACCESS_KEY_SECRET",
            "ALIYUN_OSS_ENDPOINT",
            "ALIYUN_OSS_REGION_ID",
            "ALIYUN_OSS_BUCKET"
        )

        foreach ($key in $ossRequiredKeys) {
            if (-not $envValues.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($envValues[$key])) {
                Add-CheckResult "FAIL" "env:$key" "Required when FILECENTER_STORAGE_PROVIDER=AliyunOss."
            }
            else {
                Add-CheckResult "PASS" "env:$key" "Configured. Value hidden."
            }
        }
    }
}

function Test-ContainerHealthy {
    param([string]$Service)

    return Wait-Condition $Service {
        $id = Get-ComposeContainerId $Service
        if ($null -eq $id) { return $false }
        $status = Get-ContainerInspectValue $id "{{.State.Health.Status}}"
        return $status -eq "healthy"
    } $TimeoutSeconds
}

function Test-ContainerRunning {
    param([string]$Service)

    return Wait-Condition $Service {
        $id = Get-ComposeContainerId $Service
        if ($null -eq $id) { return $false }
        $status = Get-ContainerInspectValue $id "{{.State.Status}}"
        return $status -eq "running"
    } $TimeoutSeconds
}

function Test-DbMigratorCompleted {
    return Wait-Condition "db-migrator" {
        $id = Get-ComposeContainerId "db-migrator"
        if ($null -eq $id) { return $false }
        $state = Get-ContainerInspectValue $id "{{.State.Status}} {{.State.ExitCode}}"
        if ($state -eq "exited 0") { return $true }
        if ($state -match "^exited ") { throw "db-migrator finished unsuccessfully: $state" }
        return $false
    } $TimeoutSeconds
}

function Test-ContainerCommand {
    param(
        [string]$Service,
        [string]$CheckName,
        [string[]]$Command
    )

    $id = Get-ComposeContainerId $Service
    if ($null -eq $id) {
        Add-CheckResult "FAIL" $CheckName ("Container for service {0} was not found." -f $Service)
        return $false
    }

    $result = Invoke-External "docker" (@("exec", $id) + $Command)
    if ($result.ExitCode -eq 0) {
        Add-CheckResult "PASS" $CheckName "Available."
        return $true
    }

    Add-CheckResult "FAIL" $CheckName ("Command failed inside {0}." -f $Service)
    return $false
}

Write-Host "PrivateCloudDrive V1.0 RC local stack verification"
Write-Host "Mode: $($(if ($PreflightOnly) { 'PreflightOnly' } elseif ($SkipStart) { 'Full, SkipStart' } else { 'Full, StartStack' }))"
Write-Host ""

if (Test-CommandAvailable "docker") {
    $dockerVersion = Invoke-External "docker" @("--version")
    if ($dockerVersion.ExitCode -eq 0) {
        Add-CheckResult "PASS" "docker-cli" "Docker CLI is available."
    }
    else {
        Add-CheckResult "FAIL" "docker-cli" "Docker CLI command failed."
    }
}
else {
    Add-CheckResult "FAIL" "docker-cli" "Docker CLI was not found in PATH."
}

if ($FailCount -eq 0) {
    $composeVersion = Invoke-External "docker" @("compose", "version")
    if ($composeVersion.ExitCode -eq 0) {
        Add-CheckResult "PASS" "docker-compose" "Docker Compose is available."
    }
    else {
        Add-CheckResult "FAIL" "docker-compose" "docker compose version failed."
    }
}

if ($FailCount -eq 0) {
    $config = Invoke-External "docker" @("compose", "config")
    if ($config.ExitCode -eq 0) {
        Add-CheckResult "PASS" "compose-config" "Compose configuration is valid."
    }
    else {
        Add-CheckResult "FAIL" "compose-config" "Compose configuration is invalid. Run docker compose config for details."
    }
}

if ($FailCount -eq 0) {
    $services = Get-ComposeServices
    $requiredServices = @("postgres", "redis", "db-migrator", "api", "media-worker")
    foreach ($service in $requiredServices) {
        if ($services -contains $service) {
            Add-CheckResult "PASS" "service:$service" "Service is defined."
        }
        else {
            Add-CheckResult "FAIL" "service:$service" "Required Compose service is missing."
        }
    }
}

Test-EnvConfiguration

if (-not $PreflightOnly -and $FailCount -eq 0) {
    if (-not $SkipStart) {
        $up = Invoke-External "docker" @("compose", "up", "-d", "--build")
        if ($up.ExitCode -eq 0) {
            Add-CheckResult "PASS" "compose-up" "Stack started or updated."
        }
        else {
            Add-CheckResult "FAIL" "compose-up" "Failed to start stack. Run docker compose logs for details."
        }
    }
    else {
        Add-CheckResult "WARN" "compose-up" "Skipped stack startup; checking current containers only."
    }
}

if (-not $PreflightOnly -and $FailCount -eq 0) {
    Test-ContainerHealthy "postgres" | Out-Null
    Test-ContainerHealthy "redis" | Out-Null
    Test-DbMigratorCompleted | Out-Null
    Test-ContainerRunning "api" | Out-Null
    Test-ContainerRunning "media-worker" | Out-Null
}

if (-not $PreflightOnly -and $FailCount -eq 0) {
    $swaggerUrl = "$($PublicUrl.TrimEnd('/'))/swagger/index.html"
    Wait-Condition "swagger" {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $swaggerUrl -TimeoutSec 30
            return $response.StatusCode -eq 200
        }
        catch {
            return $false
        }
    } $TimeoutSeconds | Out-Null

    Test-ContainerCommand "api" "storage:/app/storage" @("sh", "-c", "test -d /app/storage -a -w /app/storage") | Out-Null
    Test-ContainerCommand "api" "ffmpeg" @("sh", "-c", "command -v ffmpeg >/dev/null 2>&1") | Out-Null
    Test-ContainerCommand "api" "ffprobe" @("sh", "-c", "command -v ffprobe >/dev/null 2>&1") | Out-Null
}

Write-Host ""
Write-Host "Summary"
Write-Host ("PASS: {0}" -f $PassCount)
Write-Host ("WARN: {0}" -f $WarnCount)
Write-Host ("FAIL: {0}" -f $FailCount)

if ($FailCount -gt 0) {
    exit 1
}

exit 0
