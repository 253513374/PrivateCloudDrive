#!/usr/bin/env bash
# shellcheck disable=SC2317
#===============================================================================
#  verify-maui-build.sh
#
#  PrivateCloudDrive MAUI sequential build verification.
#  Builds Windows first, then Android, stopping on first failure.
#
#  Usage:
#    bash scripts/verify-maui-build.sh
#    bash scripts/verify-maui-build.sh --configuration Release
#    bash scripts/verify-maui-build.sh --skip-android
#    bash scripts/verify-maui-build.sh --skip-windows --no-restore
#
#  Options:
#    --configuration <Config>    Build configuration (Debug|Release). Default: Debug
#    --skip-windows              Skip Windows platform build
#    --skip-android              Skip Android platform build
#    --no-restore                Skip dotnet restore (CI use)
#    --help                      Show this usage
#===============================================================================

set -Eeuo pipefail
IFS=$'\n\t'

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_PATH="${ROOT_DIR}/maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj"

# Defaults
CONFIGURATION="Debug"
SKIP_WINDOWS=false
SKIP_ANDROID=false
NO_RESTORE=false

declare -i PASS_COUNT=0
declare -i WARN_COUNT=0
declare -i FAIL_COUNT=0

# ---- helpers ----------------------------------------------------------------

die() {
  echo "[FAIL] $*" >&2
  exit 1
}

pass() {
  local name="$1" message="$2"
  PASS_COUNT+=1
  printf "[PASS] %-18s %s\n" "$name" "$message"
}

warn() {
  local name="$1" message="$2"
  WARN_COUNT+=1
  printf "[WARN] %-18s %s\n" "$name" "$message"
}

fail() {
  local name="$1" message="$2"
  FAIL_COUNT+=1
  printf "[FAIL] %-18s %s\n" "$name" "$message"
}

print_header() {
  echo "================================================================"
  echo "  PrivateCloudDrive MAUI Sequential Build Verification"
  echo "================================================================"
  echo "  Project:  ${PROJECT_PATH}"
  echo "  Config:   ${CONFIGURATION}"
  echo "  Windows:  $([[ $SKIP_WINDOWS == false ]] && echo yes || echo no)"
  echo "  Android:  $([[ $SKIP_ANDROID == false ]] && echo yes || echo no)"
  echo "  Restore:  $([[ $NO_RESTORE == false ]] && echo yes || echo no)"
  echo "================================================================"
  echo ""
}

print_summary() {
  echo ""
  echo "================================================================"
  echo "  Summary"
  echo "================================================================"
  echo "  PASS: ${PASS_COUNT}"
  echo "  WARN: ${WARN_COUNT}"
  echo "  FAIL: ${FAIL_COUNT}"
  echo "================================================================"
}

dotnet_build() {
  local target_name="$1"
  shift

  echo ""
  echo "==> Building ${target_name}"
  echo "    dotnet build $*"

  if dotnet build "$@" 2>&1; then
    pass "${target_name}" "Build completed."
    return 0
  fi

  local rc=$?
  fail "${target_name}" "Build failed with exit code ${rc}."
  return 1
}

artifact_exists() {
  local name="$1" pattern="$2"

  # Find the most recent build artifact matching the pattern
  local artifact
  artifact=$(find "${ROOT_DIR}/maui" -path "*/bin/${CONFIGURATION}/*" \
    -name "${pattern}" -type f 2>/dev/null \
    | sort | tail -1)

  if [[ -n "$artifact" ]]; then
    local size
    size=$(du -h "$artifact" 2>/dev/null | cut -f1)
    pass "${name}-artifact" "Found: ${artifact} (${size})"
    return 0
  fi

  warn "${name}-artifact" "No matching artifact found in bin/${CONFIGURATION}/."
  return 1
}

usage() {
  sed -n '2,20p' "${BASH_SOURCE[0]}" | sed 's/^#//; s/^  //'
  exit 0
}

# ---- arg parsing -----------------------------------------------------------

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration) CONFIGURATION="$2"; shift 2 ;;
    --skip-windows)  SKIP_WINDOWS=true;  shift ;;
    --skip-android)  SKIP_ANDROID=true;  shift ;;
    --no-restore)    NO_RESTORE=true;    shift ;;
    --help)          usage ;;
    *)               echo "Unknown option: $1" >&2; usage ;;
  esac
done

# ---- main -------------------------------------------------------------------

print_header

# Preflight: dotnet CLI
if ! command -v dotnet &>/dev/null; then
  fail "dotnet-cli" "dotnet CLI was not found in PATH."
else
  pass "dotnet-cli" "dotnet CLI is available ($(dotnet --version))."
fi

# Preflight: project file
if [[ ! -f "$PROJECT_PATH" ]]; then
  fail "maui-project" "Project file not found: ${PROJECT_PATH}"
else
  pass "maui-project" "Project file found."
fi

# Preflight: workloads
if [[ $SKIP_WINDOWS == false ]]; then
  if dotnet workload list 2>/dev/null | grep -qi "maui-windows"; then
    pass "maui-windows-wl" "maui-windows workload detected."
  else
    warn "maui-windows-wl" "maui-windows workload not detected. Build may fail."
  fi
fi

if [[ $SKIP_ANDROID == false ]]; then
  if dotnet workload list 2>/dev/null | grep -qi "^android"; then
    pass "android-wl" "android workload detected."
  else
    warn "android-wl" "android workload not detected. Build may fail."
  fi
fi

if [[ $SKIP_WINDOWS == true && $SKIP_ANDROID == true ]]; then
  warn "targets" "Both Windows and Android builds were skipped. No target was built."
fi

# Build args
COMMON_ARGS=("build" "${PROJECT_PATH}" "-c" "${CONFIGURATION}")
if [[ $NO_RESTORE == true ]]; then
  COMMON_ARGS+=("--no-restore")
fi

# ---- Windows build (sequential) --------------------------------------------

if [[ $FAIL_COUNT -eq 0 && $SKIP_WINDOWS == false ]]; then
  WINDOWS_ARGS=("${COMMON_ARGS[@]}"
    "-p:TargetFrameworks=net10.0-windows10.0.19041.0"
    "-f" "net10.0-windows10.0.19041.0"
    "-p:RuntimeIdentifier=win-x64"
  )

  if dotnet_build "maui-windows" "${WINDOWS_ARGS[@]}"; then
    artifact_exists "maui-windows" "*.exe" || true
  fi
elif [[ $SKIP_WINDOWS == true ]]; then
  warn "maui-windows" "Skipped by parameter."
fi

# ---- Android build (only if Windows passed) --------------------------------

if [[ $FAIL_COUNT -eq 0 && $SKIP_ANDROID == false ]]; then
  ANDROID_ARGS=("${COMMON_ARGS[@]}"
    "-p:TargetFrameworks=net10.0-android"
    "-f" "net10.0-android"
  )

  if dotnet_build "maui-android" "${ANDROID_ARGS[@]}"; then
    artifact_exists "maui-android" "*.apk" || true
  fi
elif [[ $SKIP_ANDROID == true ]]; then
  warn "maui-android" "Skipped by parameter."
fi

# ---- summary ----------------------------------------------------------------

print_summary

if [[ $FAIL_COUNT -gt 0 ]]; then
  echo ""
  echo "[FAIL] One or more checks failed. See details above."
  exit 1
fi

echo ""
echo "[PASS] All MAUI build checks passed."
exit 0
