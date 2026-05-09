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

    Write-Host ("[{0}] {1} - {2}" -f $Status, $Name, $Message)
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

function Test-CommandAvailable {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj"

Write-Host "PrivateCloudDrive MAUI sequential build verification"
Write-Host ("Configuration: {0}" -f $Configuration)
Write-Host ""

if (-not (Test-CommandAvailable "dotnet")) {
    Add-CheckResult "FAIL" "dotnet-cli" "dotnet CLI was not found in PATH."
}
else {
    Add-CheckResult "PASS" "dotnet-cli" "dotnet CLI is available."
}

if (-not (Test-Path $projectPath)) {
    Add-CheckResult "FAIL" "maui-project" ("Project file not found: {0}" -f $projectPath)
}
else {
    Add-CheckResult "PASS" "maui-project" ("Project file found: {0}" -f $projectPath)
}

if ($SkipWindows -and $SkipAndroid) {
    Add-CheckResult "WARN" "targets" "Both Windows and Android builds were skipped. No target was built."
}

$commonArgs = @("build", $projectPath, "-c", $Configuration)
if ($NoRestore) {
    $commonArgs += "--no-restore"
}

if ($FailCount -eq 0 -and -not $SkipWindows) {
    $windowsArgs = $commonArgs + @(
        "-p:TargetFrameworks=net10.0-windows10.0.19041.0",
        "-f", "net10.0-windows10.0.19041.0",
        "-p:RuntimeIdentifier=win-x64"
    )

    Invoke-DotNetBuild "maui-windows" $windowsArgs | Out-Null
}
elseif ($SkipWindows) {
    Add-CheckResult "WARN" "maui-windows" "Skipped by parameter."
}

if ($FailCount -eq 0 -and -not $SkipAndroid) {
    $androidArgs = $commonArgs + @(
        "-p:TargetFrameworks=net10.0-android",
        "-f", "net10.0-android"
    )

    Invoke-DotNetBuild "maui-android" $androidArgs | Out-Null
}
elseif ($SkipAndroid) {
    Add-CheckResult "WARN" "maui-android" "Skipped by parameter."
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
