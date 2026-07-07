<#
.SYNOPSIS
    PrivateCloudDrive deployment health verification script.
    Calls GET /api/health (AllowAnonymous) and prints a readable PASS/WARN/FAIL
    summary for each health check item with colour coding.

.DESCRIPTION
    This script invokes the PrivateCloudDrive /api/health endpoint, parses the
    JSON response, and displays each health check result with colour coding.
    Exits with non-zero code if any check reports FAIL.

.PARAMETER PublicUrl
    Base URL of the deployment (default: http://localhost:8080).
    Overrides the $env:PUBLIC_URL if provided.

.PARAMETER AuthHeader
    Optional extra HTTP header (e.g. "X-API-Key: abc123").
    Overrides $env:HEALTH_AUTH_HEADER if provided.

.PARAMETER Insecure
    Skip TLS certificate verification (PowerShell's -SkipCertificateCheck).

.PARAMETER ShowRaw
    Print raw JSON response before parsed output.

.EXAMPLE
    .\verify-health.ps1

.EXAMPLE
    .\verify-health.ps1 -PublicUrl http://192.168.1.100:8080

.EXAMPLE
    .\verify-health.ps1 -PublicUrl https://my-domain.com -Insecure

.EXAMPLE
    .\verify-health.ps1 -AuthHeader "X-API-Key: abc123" -ShowRaw

.NOTES
    Exit codes: 0 = all PASS, 1 = one or more FAIL
#>

[CmdletBinding()]
param(
    [string]$PublicUrl = "http://localhost:8080",
    [string]$AuthHeader = "",
    [switch]$Insecure,
    [switch]$ShowRaw
)

# Resolve defaults with backward-compatible null-coalescing (PS 5.1+)
if (-not [string]::IsNullOrWhiteSpace($env:PUBLIC_URL)) {
    $PublicUrl = $env:PUBLIC_URL
}
if (-not [string]::IsNullOrWhiteSpace($env:HEALTH_AUTH_HEADER)) {
    $AuthHeader = $env:HEALTH_AUTH_HEADER
}

$ErrorActionPreference = "Stop"
$WarningPreference = "Continue"

# ---- state -------------------------------------------------------------------
$PassCount = 0
$WarnCount = 0
$FailCount = 0

# ---- colour helpers (PowerShell console aware) -------------------------------
function Write-Colour {
    param([string]$Fore, [string]$Text)
    if ($Host.UI.RawUI.ForegroundColor) {
        $old = $Host.UI.RawUI.ForegroundColor
        $Host.UI.RawUI.ForegroundColor = $Fore
        Write-Host $Text -NoNewline
        $Host.UI.RawUI.ForegroundColor = $old
    }
    else {
        Write-Host $Text -NoNewline
    }
}

function Write-Pass {
    param([string]$Name, [string]$Message)
    $PassCount++
    Write-Host " " -NoNewline
    Write-Colour Green "PASS"
    Write-Host "  " -NoNewline
    Write-Host ("{0,-32}" -f $Name) -NoNewline
    Write-Host " $Message"
}

function Write-Warn {
    param([string]$Name, [string]$Message)
    $WarnCount++
    Write-Host " " -NoNewline
    Write-Colour Yellow "WARN"
    Write-Host "  " -NoNewline
    Write-Host ("{0,-32}" -f $Name) -NoNewline
    Write-Host " $Message"
}

function Write-Fail {
    param([string]$Name, [string]$Message)
    $FailCount++
    Write-Host " " -NoNewline
    Write-Colour Red "FAIL"
    Write-Host "  " -NoNewline
    Write-Host ("{0,-32}" -f $Name) -NoNewline
    Write-Host " $Message"
}

# ---- helpers -----------------------------------------------------------------

function Write-Header {
    $cyan = if ($Host.UI.RawUI.ForegroundColor) { "Cyan" } else { "White" }
    Write-Host ""
    Write-Colour $cyan ("=" * 72)
    Write-Host ""
    Write-Colour $cyan "  PrivateCloudDrive - Deployment Health Check"
    Write-Host ""
    Write-Colour $cyan ("=" * 72)
    Write-Host ""
    Write-Host "  URL:     $($PublicUrl.TrimEnd('/'))/api/health"
    Write-Host "  Auth:    $(if ([string]::IsNullOrWhiteSpace($AuthHeader)) { 'none' } else { '<configured>' })"
    Write-Host "  TLS:     $(if ($Insecure) { 'insecure (skip verify)' } else { 'verify' })"
    Write-Host "  Time:    $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))"
    Write-Colour $cyan ("=" * 72)
    Write-Host ""
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

# ---- preflight ---------------------------------------------------------------

Write-Header

# Check for PowerShell version with Invoke-WebRequest
if (-not (Get-Command Invoke-WebRequest -ErrorAction SilentlyContinue)) {
    Write-Fail "preflight" "Invoke-WebRequest is not available on this PowerShell version."
    Write-Summary
    exit 1
}

# Check for PowerShell JSON support
try {
    $null | ConvertTo-Json -Depth 10 | Out-Null
}
catch {
    Write-Fail "preflight" "PowerShell JSON conversion is not available."
    Write-Summary
    exit 1
}

# ---- health check request ----------------------------------------------------

$healthUrl = "$($PublicUrl.TrimEnd('/'))/api/health"

$params = @{
    Uri             = $healthUrl
    Method          = "GET"
    Headers         = @{ "Accept" = "application/json" }
    UseBasicParsing = $true
}

if ($Insecure) {
    # PowerShell 6+ supports -SkipCertificateCheck natively
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $params.SkipCertificateCheck = $true
    }
    else {
        Write-Warn "tls-verify" "PowerShell 5.1: -SkipCertificateCheck not supported. Using basic parsing fallback."
    }
}

if (-not [string]::IsNullOrWhiteSpace($AuthHeader)) {
    $headerParts = $AuthHeader -split ':\s*', 2
    if ($headerParts.Count -eq 2) {
        $params.Headers[$headerParts[0]] = $headerParts[1]
    }
    else {
        Write-Warn "auth-header" "Could not parse auth header '$AuthHeader'. Expected format: 'HeaderName: Value'."
    }
}

try {
    $response = Invoke-WebRequest @params -ErrorAction Stop
}
catch {
    Write-Fail "http-request" "GET $healthUrl failed: $($_.Exception.Message)"
    Write-Summary
    exit 1
}

if ($response.StatusCode -ne 200) {
    Write-Fail "http-status" "GET $healthUrl returned HTTP $($response.StatusCode) (expected 200)."
    Write-Summary
    exit 1
}

# ---- parse JSON --------------------------------------------------------------

try {
    $healthData = $response.Content | ConvertFrom-Json -Depth 10
}
catch {
    Write-Fail "json-parse" "Failed to parse JSON response: $($_.Exception.Message)"
    Write-Summary
    exit 1
}

$overallStatus = $healthData.overallStatus
$generatedAt = $healthData.generatedAt
$checks = $healthData.checks

# Optionally dump raw JSON
if ($ShowRaw) {
    Write-Host ""
    Write-Colour Cyan "--- Raw health response ---"
    Write-Host ""
    Write-Host ($response.Content | ConvertFrom-Json -Depth 10 | ConvertTo-Json -Depth 10)
    Write-Colour Cyan "---------------------------"
    Write-Host ""
    Write-Host ""
}

Write-Host "  Generated at: $generatedAt"
Write-Host ""

# ---- display each check -----------------------------------------------------

if ($null -eq $checks -or $checks.Count -eq 0) {
    Write-Warn "checks" "No health check items returned from the endpoint."
}
else {
    foreach ($check in $checks) {
        $name = $check.name
        $status = [int]$check.status
        $message = $check.message
        $fix = $check.fixSuggestion

        switch ($status) {
            0 {
                Write-Pass $name $message
            }
            1 {
                if (-not [string]::IsNullOrWhiteSpace($fix)) {
                    Write-Warn $name "$message | Fix: $fix"
                }
                else {
                    Write-Warn $name $message
                }
            }
            2 {
                if (-not [string]::IsNullOrWhiteSpace($fix)) {
                    Write-Fail $name "$message | Fix: $fix"
                }
                else {
                    Write-Fail $name $message
                }
            }
            default {
                Write-Warn $name "Unknown status code: $status"
            }
        }
    }
}

Write-Summary

# ---- exit code ---------------------------------------------------------------

if ($overallStatus -eq 2 -or $FailCount -gt 0) {
    Write-Host ""
    Write-Colour Red "[FAIL] Overall health check failed. See details above."
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Colour Green "[PASS] All health checks passed."
Write-Host ""
exit 0
