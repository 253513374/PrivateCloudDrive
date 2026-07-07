<#
.SYNOPSIS
    Sequentially build the PrivateCloudDrive MAUI app for Windows and Android,
    with clear PASS/FAIL output and artifact verification.

.DESCRIPTION
    Builds Windows first, then Android. Stops on first platform failure.
    Uses separate TargetFramework overrides to avoid multi-target restore conflicts.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Debug.

.PARAMETER SkipWindows
    Skip the Windows platform build.

.PARAMETER SkipAndroid
    Skip the Android platform build.

.PARAMETER NoRestore
    Skip dotnet restore step (useful in CI where restore was already run).

.EXAMPLE
    .\scripts\verify-maui-build.ps1

.EXAMPLE
    .\scripts\verify-maui-build.ps1 -Configuration Release

.EXAMPLE
    .\scripts\verify-maui-build.ps1 -SkipAndroid
#>

param(
    [string]$Configuration = "Debug",
    [switch]$SkipWindows,
    [switch]$SkipAndroid,
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"
$PassCount = 0
$WarnCount = 0
$FailCount = 0

function Add-CheckResult {
    param(
        [ValidateSet("PASS", "WARN", "FAIL")]
        [string]$Status,
        [string]$Name,
        [string]$Message
    )

    switch ($Status) {
        "PASS" { $script:PassCount++ }
        "WARN" { $script:WarnCount++ }
        "FAIL" { $script:FailCount++ }
    }

    Write-Host ("[{0}] {1} - {2}" -f $Status.PadRight(4), $Name.PadRight(18), $Message)
}

function Invoke-DotNetBuild {
    param(
        [string]$TargetName,
        [string[]]$Arguments
    )

    Write-Host ""
    Write-Host ("==> Building {0}" -f $TargetName)
    Write-Host ("dotnet {0}" -f ($Arguments -join " "))

    & dotnet @Arguments
    if ($LASTEXITCODE -eq 0) {
        Add-CheckResult "PASS" $TargetName "Build completed."
        return $true
    }

    Add-CheckResult "FAIL" $TargetName ("Build failed with exit code {0}." -f $LASTEXITCODE)
    return $false
}

function Test-ArtifactExists {
    param(
        [string]$Name,
        [string]$Pattern
    )

    $matches = Get-ChildItem -Path $repoRoot -Recurse -Filter $Pattern -ErrorAction SilentlyContinue |
               Where-Object { $_.FullName -match "bin\\$Configuration\\" } |
               Sort-Object LastWriteTime -Descending |
               Select-Object -First 1

    if ($matches) {
        $size = "{0:N2} MB" -f ($matches.Length / 1MB)
        Add-CheckResult "PASS" ("{0}-artifact" -f $Name) ("Found: {0} ({1})" -f $matches.FullName, $size)
        return $true
    }

    Add-CheckResult "WARN" ("{0}-artifact" -f $Name) "No matching artifact found in bin/$Configuration/. Verify build output location."
    return $false
}

function Test-CommandAvailable {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Test-WorkloadInstalled {
    param([string]$WorkloadName)
    $workloads = dotnet workload list 2>$null
    return $workloads -match [regex]::Escape($WorkloadName)
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj"

Write-Host "================================================================"
Write-Host "  PrivateCloudDrive MAUI Sequential Build Verification"
Write-Host "================================================================"
Write-Host "  Project:  $projectPath"
Write-Host "  Config:   $Configuration"
Write-Host "  Windows:  $(-not $SkipWindows)"
Write-Host "  Android:  $(-not $SkipAndroid)"
Write-Host "  Restore:  $(-not $NoRestore)"
Write-Host "================================================================"
Write-Host ""

# --- Preflight checks ---

if (-not (Test-CommandAvailable "dotnet")) {
    Add-CheckResult "FAIL" "dotnet-cli" "dotnet CLI was not found in PATH."
}
else {
    Add-CheckResult "PASS" "dotnet-cli" ("dotnet CLI is available ({0})." -f (& dotnet --version))
}

if (-not (Test-Path $projectPath)) {
    Add-CheckResult "FAIL" "maui-project" ("Project file not found: {0}" -f $projectPath)
}
else {
    Add-CheckResult "PASS" "maui-project" "Project file found."
}

if (-not $SkipWindows -and -not (Test-WorkloadInstalled "maui-windows")) {
    Add-CheckResult "WARN" "maui-windows-wl" "maui-windows workload not detected. Build may fail."
}
elseif (-not $SkipWindows) {
    Add-CheckResult "PASS" "maui-windows-wl" "maui-windows workload detected."
}

if (-not $SkipAndroid -and -not (Test-WorkloadInstalled "android")) {
    Add-CheckResult "WARN" "android-wl" "android workload not detected. Build may fail."
}
elseif (-not $SkipAndroid) {
    Add-CheckResult "PASS" "android-wl" "android workload detected."
}

if ($SkipWindows -and $SkipAndroid) {
    Add-CheckResult "WARN" "targets" "Both Windows and Android builds were skipped. No target was built."
}

$commonArgs = @("build", $projectPath, "-c", $Configuration)
if ($NoRestore) {
    $commonArgs += "--no-restore"
}

# --- Windows build ---

if ($FailCount -eq 0 -and -not $SkipWindows) {
    $windowsArgs = $commonArgs + @(
        "-p:TargetFrameworks=net10.0-windows10.0.19041.0",
        "-f", "net10.0-windows10.0.19041.0",
        "-p:RuntimeIdentifier=win-x64"
    )

    if (Invoke-DotNetBuild "maui-windows" $windowsArgs) {
        Test-ArtifactExists "maui-windows" "*.exe"
    }
}
elseif ($SkipWindows) {
    Add-CheckResult "WARN" "maui-windows" "Skipped by parameter."
}

# --- Android build (only if Windows passed) ---

if ($FailCount -eq 0 -and -not $SkipAndroid) {
    $androidArgs = $commonArgs + @(
        "-p:TargetFrameworks=net10.0-android",
        "-f", "net10.0-android"
    )

    if (Invoke-DotNetBuild "maui-android" $androidArgs) {
        Test-ArtifactExists "maui-android" "*.apk"
    }
}
elseif ($SkipAndroid) {
    Add-CheckResult "WARN" "maui-android" "Skipped by parameter."
}

# --- Summary ---

Write-Host ""
Write-Host "================================================================"
Write-Host "  Summary"
Write-Host "================================================================"
Write-Host ("  PASS: {0}" -f $PassCount)
Write-Host ("  WARN: {0}" -f $WarnCount)
Write-Host ("  FAIL: {0}" -f $FailCount)
Write-Host "================================================================"

if ($FailCount -gt 0) {
    Write-Host ""
    Write-Host "[FAIL] One or more checks failed. See details above."
    exit 1
}

Write-Host ""
Write-Host "[PASS] All MAUI build checks passed."
exit 0
