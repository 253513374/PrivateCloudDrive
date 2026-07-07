#!/usr/bin/env bash
# shellcheck disable=SC2317
#===============================================================================
#  verify-health.sh
#
#  PrivateCloudDrive deployment health verification script.
#  Calls GET /api/health (AllowAnonymous) and prints a readable PASS/WARN/FAIL
#  summary for each health check item with colour coding.
#
#  Usage:
#    bash scripts/verify-health.sh
#    bash scripts/verify-health.sh --url http://192.168.1.100:8080
#    bash scripts/verify-health.sh --auth-header "X-API-Key: abc123"
#    bash scripts/verify-health.sh --url https://my-domain.com --insecure
#    bash scripts/verify-health.sh --verbose
#
#  Options:
#    --url <URL>             Base URL (default: http://localhost:8080)
#    --auth-header <Header>  Optional extra HTTP header
#    --insecure / -k         Skip TLS certificate verification (curl -k)
#    --verbose / -v          Print raw JSON response before parsed output
#    --help                  Show this usage
#
#  Environment variables:
#    PUBLIC_URL              Base URL (overridden by --url)
#    HEALTH_AUTH_HEADER      Auth header (overridden by --auth-header)
#
#  Exit codes:
#    0   All checks PASS (optional WARN is acceptable)
#    1   One or more checks FAIL
#===============================================================================

set -Eeuo pipefail
IFS=$'\n\t'

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# ---- defaults ----------------------------------------------------------------
PUBLIC_URL="${PUBLIC_URL:-http://localhost:8080}"
AUTH_HEADER="${HEALTH_AUTH_HEADER:-}"
INSECURE=false
VERBOSE=false

declare -i PASS_COUNT=0
declare -i WARN_COUNT=0
declare -i FAIL_COUNT=0

# ---- colour helpers ----------------------------------------------------------
if [[ -t 1 ]]; then
  GREEN='\033[0;32m'
  YELLOW='\033[1;33m'
  RED='\033[0;31m'
  CYAN='\033[0;36m'
  BOLD='\033[1m'
  NC='\033[0m'  # No Colour
else
  GREEN='' YELLOW='' RED='' CYAN='' BOLD='' NC=''
fi

# ---- helpers -----------------------------------------------------------------

die() {
  echo -e "${RED}[FAIL]${NC} $*" >&2
  exit 1
}

pass() {
  local name="$1" message="$2"
  PASS_COUNT+=1
  echo -e " ${GREEN}PASS${NC}  ${BOLD}$(printf '%-32s' "$name")${NC} ${message}"
}

warn() {
  local name="$1" message="$2"
  WARN_COUNT+=1
  echo -e " ${YELLOW}WARN${NC}  ${BOLD}$(printf '%-32s' "$name")${NC} ${message}"
}

fail() {
  local name="$1" message="$2"
  FAIL_COUNT+=1
  echo -e " ${RED}FAIL${NC}  ${BOLD}$(printf '%-32s' "$name")${NC} ${message}"
}

print_header() {
  echo ""
  echo -e "${CYAN}================================================================${NC}"
  echo -e "${CYAN}  PrivateCloudDrive - Deployment Health Check${NC}"
  echo -e "${CYAN}================================================================${NC}"
  echo -e "  URL:     ${PUBLIC_URL%/}/api/health"
  echo -e "  Auth:    $([[ -n "$AUTH_HEADER" ]] && echo '<configured>' || echo 'none')"
  echo -e "  TLS:     $([[ $INSECURE == true ]] && echo 'insecure (skip verify)' || echo 'verify')"
  echo -e "  Time:    $(date -u '+%Y-%m-%dT%H:%M:%SZ')"
  echo -e "${CYAN}================================================================${NC}"
  echo ""
}

print_summary() {
  echo ""
  echo -e "${CYAN}================================================================${NC}"
  echo -e "${CYAN}  Summary${NC}"
  echo -e "${CYAN}================================================================${NC}"
  echo -e "  ${GREEN}PASS${NC}:  ${PASS_COUNT}"
  echo -e "  ${YELLOW}WARN${NC}:  ${WARN_COUNT}"
  echo -e "  ${RED}FAIL${NC}:  ${FAIL_COUNT}"
  echo -e "${CYAN}================================================================${NC}"
}

usage() {
  sed -n '2,22p' "${BASH_SOURCE[0]}" | sed 's/^#//; s/^  //'
  exit 0
}

# ---- arg parsing ------------------------------------------------------------

while [[ $# -gt 0 ]]; do
  case "$1" in
    --url)           PUBLIC_URL="$2";       shift 2 ;;
    --auth-header)   AUTH_HEADER="$2";       shift 2 ;;
    --insecure|-k)   INSECURE=true;         shift ;;
    --verbose|-v)    VERBOSE=true;          shift ;;
    --help)          usage ;;
    *)               echo "Unknown option: $1" >&2; usage ;;
  esac
done

# ---- preflight ---------------------------------------------------------------

print_header

if ! command -v curl &>/dev/null; then
  die "curl is required but not found in PATH."
fi

# Prefer jq; fall back to python3, then python
JSON_PARSER=""
if command -v jq &>/dev/null; then
  JSON_PARSER="jq"
elif command -v python3 &>/dev/null; then
  JSON_PARSER="python3"
elif command -v python &>/dev/null; then
  JSON_PARSER="python"
else
  die "One of jq, python3, or python is required for JSON parsing."
fi

# ---- health check request ---------------------------------------------------

HEALTH_URL="${PUBLIC_URL%/}/api/health"
CURL_OPTS=("-sS")
if [[ $INSECURE == true ]]; then
  CURL_OPTS+=("-k")
fi

# Build curl headers
CURL_HEADERS=("-H" "Accept: application/json")
if [[ -n "$AUTH_HEADER" ]]; then
  CURL_HEADERS+=("-H" "$AUTH_HEADER")
fi

RESPONSE_FILE=$(mktemp -t pcd-health-XXXXXX.json 2>/dev/null || mktemp /tmp/pcd-health-XXXXXX.json)
trap 'rm -f "$RESPONSE_FILE"' EXIT

HTTP_CODE="$(curl "${CURL_OPTS[@]}" "${CURL_HEADERS[@]}" -o "$RESPONSE_FILE" -w '%{http_code}' "$HEALTH_URL" 2>/dev/null || true)"

if [[ "$HTTP_CODE" != "200" ]]; then
  echo -e " ${RED}FAIL${NC}  ${BOLD}http-status${NC}              GET ${HEALTH_URL} returned HTTP ${HTTP_CODE} (expected 200)."
  if [[ -s "$RESPONSE_FILE" ]]; then
    echo -e "       Response body (first 500 chars):"
    head -c 500 "$RESPONSE_FILE" | sed 's/^/       /'
  fi
  FAIL_COUNT+=1
  print_summary
  exit 1
fi

# Optionally dump raw JSON
if [[ $VERBOSE == true ]]; then
  echo ""
  echo -e "${CYAN}--- Raw health response ---${NC}"
  if [[ "$JSON_PARSER" == "jq" ]]; then
    jq '.' "$RESPONSE_FILE" 2>/dev/null || cat "$RESPONSE_FILE"
  else
    ${JSON_PARSER} -m json.tool "$RESPONSE_FILE" 2>/dev/null || cat "$RESPONSE_FILE"
  fi
  echo -e "${CYAN}---------------------------${NC}"
  echo ""
fi

# ---- parse and display ------------------------------------------------------

parse_with_jq() {
  local file="$1"

  local overall_status
  overall_status=$(jq -r '.overallStatus // -1' "$file" 2>/dev/null)
  local generated_at
  generated_at=$(jq -r '.generatedAt // "unknown"' "$file" 2>/dev/null)

  echo -e "  Generated at: ${generated_at}"
  echo ""

  # Print each check
  local count
  count=$(jq '.checks | length' "$file" 2>/dev/null || echo 0)
  for (( i=0; i<count; i++ )); do
    local name status message fix
    name=$(jq -r ".checks[$i].name // \"unknown-$i\"" "$file")
    status=$(jq -r ".checks[$i].status // -1" "$file")
    message=$(jq -r ".checks[$i].message // \"\"" "$file")
    fix=$(jq -r ".checks[$i].fixSuggestion // \"\"" "$file")

    case "$status" in
      0) pass "$name" "$message" ;;
      1)
        if [[ -n "$fix" ]]; then
          warn "$name" "${message} | Fix: ${fix}"
        else
          warn "$name" "$message"
        fi
        ;;
      2)
        if [[ -n "$fix" ]]; then
          fail "$name" "${message} | Fix: ${fix}"
        else
          fail "$name" "$message"
        fi
        ;;
      *)
        warn "$name" "Unknown status code: ${status}"
        ;;
    esac
  done
}

parse_with_python() {
  local file="$1"
  local py="${JSON_PARSER:-python}"
  $py -c "
import json, sys
with open('$file', encoding='utf-8') as f:
    data = json.load(f)
overall = data.get('overallStatus', -1)
generated = data.get('generatedAt', 'unknown')
print(f'  Generated at: {generated}\n')
checks = data.get('checks', [])
for c in checks:
    name = c.get('name', '?')
    status = c.get('status', -1)
    message = c.get('message', '')
    fix = c.get('fixSuggestion', '') or ''
    if status == 0:
        print(f'pass|{name}|{message}')
    elif status == 1:
        if fix:
            message = f'{message} | Fix: {fix}'
        print(f'warn|{name}|{message}')
    elif status == 2:
        if fix:
            message = f'{message} | Fix: {fix}'
        print(f'fail|{name}|{message}')
    else:
        print(f'warn|{name}|Unknown status code: {status}')
"
}

case "$JSON_PARSER" in
  jq)
    overall_status=$(jq -r '.overallStatus // -1' "$RESPONSE_FILE")
    parse_with_jq "$RESPONSE_FILE"
    ;;
  python3|python)
    overall_status=$($JSON_PARSER -c "import json; d=json.load(open('$RESPONSE_FILE')); print(d.get('overallStatus', -1))" 2>/dev/null || echo -1)
    while IFS='|' read -r tag name message; do
      case "$tag" in
        pass) pass "$name" "$message" ;;
        warn) warn "$name" "$message" ;;
        fail) fail "$name" "$message" ;;
      esac
    done <<< "$(parse_with_python "$RESPONSE_FILE")"
    ;;
esac

print_summary

# ---- exit code ---------------------------------------------------------------

if [[ "$overall_status" == "2" ]] || [[ $FAIL_COUNT -gt 0 ]]; then
  echo ""
  echo -e " ${RED}[FAIL]${NC} Overall health check failed. See details above."
  exit 1
fi

echo ""
echo -e " ${GREEN}[PASS]${NC} All health checks passed."
exit 0
