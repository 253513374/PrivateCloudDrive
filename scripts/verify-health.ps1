<#
.SYNOPSIS
    PrivateCloudDrive deployment health verification script.
    Calls GET /api/health (AllowAnonymous) and, in admin mode, also checks
    authenticated admin-level endpoints for deeper insight.

.DESCRIPTION
    This script invokes the PrivateCloudDrive health endpoints, parses the
    JSON response, and displays each health check result with colour coding.

    Normal mode: checks the anonymous /api/health endpoint.
    Admin mode (-AdminMode): additionally authenticates with a Bearer token,
    checks the system health API and admin endpoints for availability.

    Exits with non-zero code if any check reports FAIL.

.PARAMETER PublicUrl
    Base URL of the deployment (default: http://localhost:8080).
    Overrides the $env:PUBLIC_URL if provided.

.PARAMETER AuthHeader
    Optional extra HTTP header (e.g. "X-API-Key: ***").
    Overrides $env:HEALTH_AUTH_HEADER if provided.

.PARAMETER Insecure
    Skip TLS certificate verification (PowerShell's -SkipCertificateCheck).

.PARAMETER ShowRaw
    Print raw JSON response before parsed output.

.PARAMETER AdminMode
    Enable admin-level health checks. Requires -AdminToken or $env:ADMIN_TOKEN
    to authenticate as an admin user.

.PARAMETER AdminToken
    Bearer token for admin-mode authenticated requests. Overrides $env:ADMIN_TOKEN.

.EXAMPLE
    .\verify-health.ps1                              # Basic anonymous health check

.EXAMPLE
    .\verify-health.ps1 -PublicUrl http://192.168.1.100:8080

.EXAMPLE
    .\verify-health.ps1 -AdminMode                   # With $env:ADMIN_TOKEN set

.EXAMPLE
    .\verify-health.ps1 -AdminMode -AdminToken "eyJ..."

.EXAMPLE
    .\verify-health.ps1 -AdminMode -ShowRaw          # Admin mode with raw JSON dump

.NOTES
    Exit codes: 0 = all PASS, 1 = one or more FAIL
    Version: V1.3 - enhanced with admin-level health checks.
#>

[CmdletBinding()]
param(
    [string]$PublicUrl = "http://localhost:8080",
    [string]$AuthHeader = "",
    [switch]$Insecure,
    [switch]$ShowRaw,
    [switch]$AdminMode,
    [string]$AdminToken = ""
)

# Resolve defaults
if (-not [string]::IsNullOrWhiteSpace($env:PUBLIC_URL)) {
    $PublicUrl = $env:PUBLIC_URL
}
if (-not [string]::IsNullOrWhiteSpace($env:HEALTH_AUTH_HEADER)) {
    $AuthHeader = $env:HEALTH_AUTH_HEADER
}
if (-not [string]::IsNullOrWhiteSpace($AdminToken)) {
    $env:ADMIN_TOKEN = $AdminToken
}
if ($AdminMode -and [string]::IsNullOrWhiteSpace($env:ADMIN_TOKEN)) {
    Write-Warning "AdminMode requires -AdminToken parameter or `$env:ADMIN_TOKEN to be set."
    $AdminMode = $false
}

$ErrorActionPreference = "Stop"
$WarningPreference = "Continue"
$PassCount = 0
$WarnCount = 0
$FailCount = 0

function Write-Colour {
    param([string]$Fore, [string]$Text)
    if ($Host.UI.RawUI.ForegroundColor) {
        $old = $Host.UI.RawUI.ForegroundColor
        $Host.UI.RawUI.ForegroundColor = $Fore
        Write-Host $Text -NoNewline
        $Host.UI.RawUI.ForegroundColor = $old
    } else {
        Write-Host $Text -NoNewline
    }
}

function Write-Pass {
    param([string]$Name, [string]$Message)
    $script:PassCount++
    Write-Host " " -NoNewline
    Write-Colour Green "PASS"
    Write-Host "  " -NoNewline
    Write-Host ("{0,-32}" -f $Name) -NoNewline
    Write-Host " $Message"
}

function Write-Warn {
    param([string]$Name, [string]$Message)
    $script:WarnCount++
    Write-Host " " -NoNewline
    Write-Colour Yellow "WARN"
    Write-Host "  " -NoNewline
    Write-Host ("{0,-32}" -f $Name) -NoNewline
    Write-Host " $Message"
}

function Write-Fail {
    param([string]$Name, [string]$Message)
    $script:FailCount++
    Write-Host " " -NoNewline
    Write-Colour Red "FAIL"
    Write-Host "  " -NoNewline
    Write-Host ("{0,-32}" -f $Name) -NoNewline
    Write-Host " $Message"
}

function Write-Header {
    Write-Host ""
    Write-Colour Cyan ("=" * 72)
    Write-Host ""
    Write-Colour Cyan "  PrivateCloudDrive - Deployment Health Check"
    Write-Host ""
    Write-Colour Cyan ("=" * 72)
    Write-Host ""
    Write-Host "  URL:       $($PublicUrl.TrimEnd('/'))"
    Write-Host "  Auth:      $(if ([string]::IsNullOrWhiteSpace($AuthHeader)) { if ($AdminMode) { 'bearer (admin)' } else { 'none' } } else { '<configured>' })"
    Write-Host "  TLS:       $(if ($Insecure) { 'insecure (skip verify)' } else { 'verify' })"
    Write-Host "  Mode:      $(if ($AdminMode) { 'standard + admin' } else { 'standard' })"
    Write-Host "  Time:      $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))"
    Write-Colour Cyan ("=" * 72)
    Write-Host ""
}

function Write-Summary {
    Write-Host ""
    Write-Colour Cyan ("=" * 72)
    Write-Host ""
    Write-Colour Cyan "  Summary"
    Write-Host ""
    Write-Colour Cyan ("=" * 72)
    Write-Host ""
    Write-Host "  PASS:  $PassCount"
    Write-Host "  WARN:  $WarnCount"
    Write-Host "  FAIL:  $FailCount"
    Write-Colour Cyan ("=" * 72)
    Write-Host ""
}

function Invoke-HealthApi {
    param([string]$Uri, [bool]$AddBearerAuth = $false)
    $params = @{
        Uri = $Uri
        Method = "GET"
        Headers = @{ "Accept" = "application/json" }
        UseBasicParsing = $true
    }
    if ($Insecure -and $PSVersionTable.PSVersion.Major -ge 6) {
        $params.SkipCertificateCheck = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($AuthHeader)) {
        $parts = $AuthHeader -split ':\s*', 2
        if ($parts.Count -eq 2) {
            $params.Headers[$parts[0]] = $parts[1]
        }
    }
    if ($AddBearerAuth -and (-not [string]::IsNullOrWhiteSpace($env:ADMIN_TOKEN))) {
        $params.Headers["Authorization"] = "Bearer $($env:ADMIN_TOKEN.Trim())"
    }
    try {
        $response = Invoke-WebRequest @params -ErrorAction Stop
        return [pscustomobject]@{ Success = $true; StatusCode = $response.StatusCode; Content = $response.Content }
    } catch {
        $sc = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { 0 }
        return [pscustomobject]@{ Success = $false; StatusCode = $sc; Error = $_.Exception.Message }
    }
}

function Format-Bytes {
    param([long]$Bytes)
    if ($Bytes -ge 1TB) { return "{0:N2} TB" -f ($Bytes / 1TB) }
    if ($Bytes -ge 1GB) { return "{0:N2} GB" -f ($Bytes / 1GB) }
    if ($Bytes -ge 1MB) { return "{0:N2} MB" -f ($Bytes / 1MB) }
    if ($Bytes -ge 1KB) { return "{0:N2} KB" -f ($Bytes / 1KB) }
    return "$Bytes B"
}

# ---- preflight ----
Write-Header
if (-not (Get-Command Invoke-WebRequest -ErrorAction SilentlyContinue)) {
    Write-Fail "preflight" "Invoke-WebRequest is not available."
    Write-Summary; exit 1
}
try { $null | ConvertTo-Json -Depth 10 | Out-Null } catch {
    Write-Fail "preflight" "JSON conversion not available."
    Write-Summary; exit 1
}

# =====================================================================
# PART 1 - Anonymous health check (/api/health)
# =====================================================================
$healthUrl = "$($PublicUrl.TrimEnd('/'))/api/health"
$result = Invoke-HealthApi -Uri $healthUrl

if (-not $result.Success) {
    Write-Fail "http-request" "GET $healthUrl failed: $($result.Error)"
} elseif ($result.StatusCode -ne 200) {
    Write-Fail "http-status" "GET $healthUrl returned HTTP $($result.StatusCode) (expected 200)."
} else {
    try { $healthData = $result.Content | ConvertFrom-Json -Depth 10 } catch {
        Write-Fail "json-parse" "Failed to parse JSON response."
        Write-Summary; exit 1
    }
    $overallStatus = $healthData.overallStatus
    $generatedAt = $healthData.generatedAt
    $checks = $healthData.checks

    if ($ShowRaw) {
        Write-Host ""; Write-Colour Cyan "--- Raw health response (/api/health) ---"; Write-Host ""
        Write-Host ($result.Content | ConvertFrom-Json -Depth 10 | ConvertTo-Json -Depth 10)
        Write-Colour Cyan "------------------------------------------"; Write-Host ""
    }
    Write-Host "  [Anonymous Health: /api/health]"
    Write-Host "  Generated at: $generatedAt"
    Write-Host ""

    if ($null -eq $checks -or $checks.Count -eq 0) {
        Write-Warn "checks" "No health check items returned."
    } else {
        foreach ($check in $checks) {
            $s = [int]$check.status
            $m = $check.message
            $f = $check.fixSuggestion
            if (-not [string]::IsNullOrWhiteSpace($f)) { $m = "$m | Fix: $f" }
            switch ($s) {
                0 { Write-Pass $check.name $m }
                1 { Write-Warn $check.name $m }
                2 { Write-Fail $check.name $m }
                default { Write-Warn $check.name "Unknown status: $s" }
            }
        }
    }
    switch ([int]$overallStatus) {
        0 { Write-Pass "overall" "Anonymous health check: PASS" }
        1 { Write-Warn "overall" "Anonymous health check: WARN" }
        2 { Write-Fail "overall" "Anonymous health check: FAIL" }
    }
}

# =====================================================================
# PART 2 - Admin-level health checks
# =====================================================================
if ($AdminMode) {
    Write-Host ""; Write-Colour Cyan ("-" * 72); Write-Host ""
    Write-Colour Cyan "  Admin-Level Health Checks"
    Write-Host ""; Write-Colour Cyan ("-" * 72); Write-Host ""

    # 2a. System health (requires auth)
    $sysUrl = "$($PublicUrl.TrimEnd('/'))/api/file-center/system-health"
    $sysRes = Invoke-HealthApi -Uri $sysUrl -AddBearerAuth $true

    if (-not $sysRes.Success) {
        Write-Warn "system-health" "GET system-health failed: $($sysRes.Error)"
    } elseif ($sysRes.StatusCode -eq 200) {
        try {
            $sys = $sysRes.Content | ConvertFrom-Json -Depth 10
        } catch {
            Write-Warn "system-health-parse" "Parse failed: $($_.Exception.Message)"
        }
        if ($ShowRaw) {
            Write-Host ""; Write-Colour Cyan "--- Raw system-health response ---"; Write-Host ""
            Write-Host ($sysRes.Content | ConvertFrom-Json -Depth 10 | ConvertTo-Json -Depth 10)
            Write-Colour Cyan "--------------------------------------"; Write-Host ""
        }
        Write-Host "  [Authenticated Health: /api/file-center/system-health]"
        Write-Host ""

        $statusLabels = @{ 0 = "Healthy"; 1 = "Degraded"; 2 = "Unhealthy" }
        $items = @(
            @{N="api-status"; V=$sys.apiStatus; D="API"},
            @{N="database-status"; V=$sys.databaseStatus; D="Database"},
            @{N="redis-status"; V=$sys.redisStatus; D="Redis"},
            @{N="storage-status"; V=$sys.storageStatus; D="Storage"},
            @{N="ffmpeg-status"; V=$sys.ffmpegStatus; D="FFmpeg"},
            @{N="ffprobe-status"; V=$sys.ffprobeStatus; D="FFprobe"}
        )
        foreach ($item in $items) {
            $sv = [int]$item.V
            $label = $statusLabels[$sv]
            switch ($sv) {
                0 { Write-Pass $item.N "$($item.D): $label" }
                1 { Write-Warn $item.N "$($item.D): $label" }
                2 { Write-Fail $item.N "$($item.D): $label" }
            }
        }
        Write-Pass "storage-provider" "Provider: $($sys.storageProvider)"
        Write-Pass "storage-location" "$($sys.storageLocationDescription)"
        if ($sys.storageDiskTotalBytes -gt 0) {
            $a = Format-Bytes $sys.storageDiskAvailableBytes
            $t = Format-Bytes $sys.storageDiskTotalBytes
            Write-Pass "disk-space" "Available: $a / $t"
        }
        if ($null -ne $sys.diagnostics -and $sys.diagnostics.Count -gt 0) {
            foreach ($d in $sys.diagnostics) { Write-Warn "diagnostic" $d }
        }
        switch ([int]$sys.overallStatus) {
            0 { Write-Pass "sys-overall" "System health: Healthy" }
            1 { Write-Warn "sys-overall" "System health: Degraded" }
            2 { Write-Fail "sys-overall" "System health: Unhealthy" }
        }
    } elseif ($sysRes.StatusCode -eq 401 -or $sysRes.StatusCode -eq 403) {
        Write-Fail "system-health-auth" "HTTP $($sysRes.StatusCode) - invalid token or insufficient permissions."
    } else {
        Write-Warn "system-health" "HTTP $($sysRes.StatusCode) (expected 200)."
    }

    # 2b. Admin API endpoint probes
    $adminEps = @(
        @{N="admin-users"; U="$($PublicUrl.TrimEnd('/'))/api/identity/users"; D="Admin user management API"},
        @{N="storage-config"; U="$($PublicUrl.TrimEnd('/'))/api/file-center/storage/config"; D="Storage config API"},
        @{N="operation-logs"; U="$($PublicUrl.TrimEnd('/'))/api/operation-logs"; D="Operation logs API"}
    )
    foreach ($ep in $adminEps) {
        $epRes = Invoke-HealthApi -Uri $ep.U -AddBearerAuth $true
        if (-not $epRes.Success) {
            $sc = if ($null -ne $epRes.StatusCode -and $epRes.StatusCode -gt 0) { $epRes.StatusCode } else { "?" }
            if ($sc -eq 401 -or $sc -eq 403) {
                Write-Fail $ep.N "$($ep.D): HTTP $sc - insufficient permissions"
            } elseif ($sc -eq 404) {
                Write-Warn $ep.N "$($ep.D): HTTP 404 - endpoint not yet deployed"
            } else {
                Write-Warn $ep.N "$($ep.D): HTTP $sc - $($epRes.Error)"
            }
        } else {
            Write-Pass $ep.N "$($ep.D): HTTP $($epRes.StatusCode) - accessible"
        }
    }

    # 2c. Token summary
    if (-not [string]::IsNullOrWhiteSpace($env:ADMIN_TOKEN)) {
        $preview = $env:ADMIN_TOKEN.Substring(0, [Math]::Min(20, $env:ADMIN_TOKEN.Length))
        Write-Pass "admin-token" "Bearer token configured ($preview...)"
    }
}

# =====================================================================
# Summary & exit
# =====================================================================
Write-Summary
if ($FailCount -gt 0) {
    Write-Host ""; Write-Colour Red "[FAIL] Health check completed with failures."; Write-Host ""
    exit 1
}
Write-Host ""; Write-Colour Green "[PASS] All health checks passed."; Write-Host ""
exit 0
