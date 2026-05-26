#!/usr/bin/env bash
set -Eeuo pipefail

# PrivateCloudDrive local/CI smoke gate.
# Guards against Swagger JSON 500 regressions and mobile OAuth invalid_client regressions
# without printing access tokens, refresh tokens, passwords, cookies, or full secrets.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-pcd_smoke_local}"
API_HTTP_PORT="${API_HTTP_PORT:-18080}"
POSTGRES_PORT="${POSTGRES_PORT:-15432}"
REDIS_PORT="${REDIS_PORT:-16379}"
PUBLIC_URL="${PUBLIC_URL:-http://localhost:${API_HTTP_PORT}}"
MOBILE_APP_CLIENT_ID="${MOBILE_APP_CLIENT_ID:-PrivateCloudDrive_App}"
QA_USERNAME="${QA_USERNAME:-admin}"
QA_PASSWORD="${QA_PASSWORD:-1q2w3E*}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-300}"
BUILD_IMAGES="${BUILD_IMAGES:-1}"
KEEP_STACK_ON_FAILURE="${KEEP_STACK_ON_FAILURE:-0}"

export COMPOSE_PROJECT_NAME API_HTTP_PORT POSTGRES_PORT REDIS_PORT PUBLIC_URL MOBILE_APP_CLIENT_ID
export SWAGGER_ENABLED="${SWAGGER_ENABLED:-true}"
export AUTH_SERVER_REQUIRE_HTTPS_METADATA="${AUTH_SERVER_REQUIRE_HTTPS_METADATA:-false}"
export ALLOW_INSECURE_LOCAL_VALIDATION="${ALLOW_INSECURE_LOCAL_VALIDATION:-true}"

declare -a PASSED_CHECKS=()

die() {
  echo "[FAIL] $*" >&2
  exit 1
}

pass() {
  PASSED_CHECKS+=("$1")
  echo "[PASS] $1"
}

run_compose() {
  docker compose "$@"
}

cleanup() {
  local exit_code=$?
  if [[ "$exit_code" -eq 0 || "$KEEP_STACK_ON_FAILURE" != "1" ]]; then
    docker compose down -v --remove-orphans >/dev/null 2>&1 || true
  else
    echo "[WARN] Smoke failed; keeping Compose stack for inspection because KEEP_STACK_ON_FAILURE=1." >&2
    echo "[WARN] Cleanup manually with: COMPOSE_PROJECT_NAME=${COMPOSE_PROJECT_NAME} docker compose down -v --remove-orphans" >&2
  fi
}
trap cleanup EXIT

require_command() {
  command -v "$1" >/dev/null 2>&1 || die "Required command not found: $1"
}

wait_for_http_200() {
  local name="$1"
  local url="$2"
  local deadline=$((SECONDS + TIMEOUT_SECONDS))
  local status="000"

  while (( SECONDS < deadline )); do
    status="$(curl -k -sS -o /tmp/pcd-smoke-response.txt -w '%{http_code}' "$url" || true)"
    if [[ "$status" == "200" ]]; then
      pass "$name HTTP 200"
      return 0
    fi
    sleep 3
  done

  echo "[FAIL] $name did not return HTTP 200 within ${TIMEOUT_SECONDS}s; last status=${status}" >&2
  if [[ -s /tmp/pcd-smoke-response.txt ]]; then
    echo "[FAIL] Last response excerpt (sanitized, first 500 chars):" >&2
    python - <<'PY' >&2
from pathlib import Path
text = Path('/tmp/pcd-smoke-response.txt').read_text(errors='replace')[:500]
for marker in ('access_token', 'refresh_token', 'password', 'client_secret'):
    text = text.replace(marker, f'{marker[:3]}***')
print(text)
PY
  fi
  return 1
}

request_mobile_token() {
  local token_url="${PUBLIC_URL%/}/connect/token"
  local response_file="/tmp/pcd-smoke-token-response.json"
  local http_code

  http_code="$(curl -k -sS -o "$response_file" -w '%{http_code}' \
    -H 'Content-Type: application/x-www-form-urlencoded' \
    --data-urlencode 'grant_type=password' \
    --data-urlencode "client_id=${MOBILE_APP_CLIENT_ID}" \
    --data-urlencode "username=${QA_USERNAME}" \
    --data-urlencode "password=${QA_PASSWORD}" \
    --data-urlencode 'scope=openid offline_access PrivateCloudDrive profile email roles' \
    "$token_url" || true)"

  python - "$http_code" "$response_file" <<'PY'
import json
import sys
from pathlib import Path

http_code = sys.argv[1]
response_path = Path(sys.argv[2])
body = response_path.read_text(encoding='utf-8', errors='replace') if response_path.exists() else ''
try:
    payload = json.loads(body) if body.strip() else {}
except json.JSONDecodeError:
    payload = {}

if http_code != '200':
    error = payload.get('error') or 'non_200_token_response'
    description = payload.get('error_description') or ''
    if 'invalid_client' in error or 'invalid_client' in description:
        print('[FAIL] Android login smoke failed: invalid_client for client_id=PrivateCloudDrive_App', file=sys.stderr)
    else:
        print(f'[FAIL] Android login smoke failed: token endpoint HTTP {http_code}, error={error}', file=sys.stderr)
    # Do not print raw response; it may contain sensitive fields.
    sys.exit(1)

if not payload.get('access_token'):
    print('[FAIL] Android login smoke failed: access_token missing from HTTP 200 response', file=sys.stderr)
    sys.exit(1)

print('[PASS] token-client PASS (token value not printed)')
PY
}

require_command docker
require_command curl
require_command python

echo "PrivateCloudDrive Compose smoke gate"
echo "Project: ${COMPOSE_PROJECT_NAME}"
echo "Public URL: ${PUBLIC_URL}"
echo "Mobile client_id: ${MOBILE_APP_CLIENT_ID}"
echo "QA username: ${QA_USERNAME} (password hidden)"

docker --version
run_compose version
run_compose config --quiet
pass "compose-config"

# Always remove a same-name disposable stack first so stale volumes cannot hide seed regressions.
run_compose down -v --remove-orphans >/dev/null 2>&1 || true

if [[ "$BUILD_IMAGES" == "1" ]]; then
  run_compose up -d --build
else
  run_compose up -d
fi
pass "compose-up"

for service in postgres redis; do
  deadline=$((SECONDS + TIMEOUT_SECONDS))
  until [[ "$(run_compose ps --status running --format '{{.Service}}' | grep -x "$service" || true)" == "$service" ]]; do
    (( SECONDS < deadline )) || die "Service did not reach running state: $service"
    sleep 3
  done
  pass "$service running"
done

deadline=$((SECONDS + TIMEOUT_SECONDS))
until run_compose ps --status exited --format '{{.Service}} {{.ExitCode}}' | grep -q '^db-migrator 0$'; do
  if run_compose ps --status exited --format '{{.Service}} {{.ExitCode}}' | grep -q '^db-migrator '; then
    run_compose logs --no-color --tail=120 db-migrator >&2 || true
    die "db-migrator exited unsuccessfully"
  fi
  (( SECONDS < deadline )) || die "db-migrator did not complete within ${TIMEOUT_SECONDS}s"
  sleep 3
done
pass "db-migrator completed"

for service in api media-worker; do
  deadline=$((SECONDS + TIMEOUT_SECONDS))
  until [[ "$(run_compose ps --status running --format '{{.Service}}' | grep -x "$service" || true)" == "$service" ]]; do
    (( SECONDS < deadline )) || die "Service did not reach running state: $service"
    sleep 3
  done
  pass "$service running"
done

wait_for_http_200 "swagger-json" "${PUBLIC_URL%/}/swagger/v1/swagger.json"
request_mobile_token

# Catch invalid_client regressions even if a token request path changes later. Duplicate-key
# ABP seed noise is reported as WARN after Swagger JSON and mobile token both pass because
# this gate's release-blocking contract is Swagger 200 + Android client login viability.
if run_compose logs --no-color api db-migrator 2>/dev/null | grep -Ei 'invalid_client' >/tmp/pcd-smoke-invalid-client.txt; then
  echo "[FAIL] Found invalid_client markers in startup/token logs (values omitted):" >&2
  sed -E 's/(access_token|refresh_token|password|client_secret)=[^ ]+/\1=***REDACTED***/Ig' /tmp/pcd-smoke-invalid-client.txt >&2
  exit 1
fi
pass "no-invalid-client-log-markers"

if run_compose logs --no-color api db-migrator 2>/dev/null | grep -Ei 'duplicate key|unique constraint' >/tmp/pcd-smoke-unique-warnings.txt; then
  echo "[WARN] Found ABP duplicate-key/unique-constraint startup log markers after smoke passed; sanitized excerpt:" >&2
  sed -E 's/(access_token|refresh_token|password|client_secret)=[^ ]+/\1=***REDACTED***/Ig' /tmp/pcd-smoke-unique-warnings.txt >&2
fi

echo "Summary: ${#PASSED_CHECKS[@]} checks passed"
