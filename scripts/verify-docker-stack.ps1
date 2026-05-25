param(
    [switch]$PreflightOnly,
    [switch]$BuildImages,
    [switch]$StrictImageCheck,
    [int]$TimeoutSeconds = 300,
    [string]$PublicUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message"
}

function Invoke-Docker {
    param([string[]]$Arguments)

    & docker @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-ComposeContainerId {
    param([string]$Service)

    $output = & docker compose ps -a -q $Service
    if ($LASTEXITCODE -ne 0 -or $null -eq $output) {
        return $null
    }

    $id = ($output | Select-Object -First 1).ToString().Trim()
    if ([string]::IsNullOrWhiteSpace($id)) {
        return $null
    }

    return $id
}

function Wait-Condition {
    param(
        [string]$Name,
        [scriptblock]$Condition,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) {
            Write-Host "$Name is ready."
            return
        }

        Start-Sleep -Seconds 3
    }

    throw "Timed out waiting for $Name."
}

function Test-ImageExists {
    param([string]$Image)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & docker image inspect $Image > $null 2> $null
        return $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

Write-Step "Checking Docker CLI"
Invoke-Docker @("version")
Invoke-Docker @("compose", "version")

Write-Step "Validating Compose configuration"
Invoke-Docker @("compose", "config")

Write-Step "Checking local Docker images"
$requiredImages = @(
    "postgres:17-alpine",
    "redis:7-alpine",
    "mcr.microsoft.com/dotnet/sdk:10.0",
    "mcr.microsoft.com/dotnet/aspnet:10.0"
)

$missingImages = @()
foreach ($image in $requiredImages) {
    if (Test-ImageExists $image) {
        Write-Host "found   $image"
    }
    else {
        Write-Host "missing $image"
        $missingImages += $image
    }
}

if ($missingImages.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing images can be pulled with:"
    foreach ($image in $missingImages) {
        Write-Host "docker pull $image"
    }

    if ($StrictImageCheck) {
        throw "Missing required Docker images: $($missingImages -join ', ')"
    }
}

if ($PreflightOnly) {
    Write-Host ""
    Write-Host "Preflight completed."
    exit 0
}

Write-Step "Starting full Docker Compose stack"
$composeArguments = @("compose", "up", "-d")
if ($BuildImages) {
    $composeArguments += "--build"
}
Invoke-Docker $composeArguments

Write-Step "Waiting for dependency health checks"
Wait-Condition "postgres" {
    $id = Get-ComposeContainerId "postgres"
    if ($null -eq $id) { return $false }
    $status = (& docker inspect --format "{{.State.Health.Status}}" $id).Trim()
    return $status -eq "healthy"
} $TimeoutSeconds

Wait-Condition "redis" {
    $id = Get-ComposeContainerId "redis"
    if ($null -eq $id) { return $false }
    $status = (& docker inspect --format "{{.State.Health.Status}}" $id).Trim()
    return $status -eq "healthy"
} $TimeoutSeconds

Write-Step "Waiting for database migrator"
Wait-Condition "db-migrator" {
    $id = Get-ComposeContainerId "db-migrator"
    if ($null -eq $id) { return $false }
    $state = (& docker inspect --format "{{.State.Status}} {{.State.ExitCode}}" $id).Trim()
    if ($state -eq "exited 0") { return $true }
    if ($state -match "^exited ") { throw "db-migrator finished unsuccessfully: $state" }
    return $false
} $TimeoutSeconds

Write-Step "Waiting for API and media worker"
Wait-Condition "api" {
    $id = Get-ComposeContainerId "api"
    if ($null -eq $id) { return $false }
    $state = (& docker inspect --format "{{.State.Status}}" $id).Trim()
    return $state -eq "running"
} $TimeoutSeconds

Wait-Condition "media-worker" {
    $id = Get-ComposeContainerId "media-worker"
    if ($null -eq $id) { return $false }
    $state = (& docker inspect --format "{{.State.Status}}" $id).Trim()
    return $state -eq "running"
} $TimeoutSeconds

Write-Step "Checking Swagger endpoint"
$swaggerUrl = "$($PublicUrl.TrimEnd('/'))/swagger/index.html"
Wait-Condition "swagger" {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $swaggerUrl -TimeoutSec 30
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
} $TimeoutSeconds

Write-Host "Swagger is available at $swaggerUrl."
Write-Host "Docker stack verification completed."
