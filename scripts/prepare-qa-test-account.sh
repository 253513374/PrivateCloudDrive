#!/usr/bin/env bash
# Prepare the low-privilege QA test accounts by injecting a password secret into DbMigrator.
# Stdout/stderr must never print the password, access tokens, or refresh tokens.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MIGRATOR_PROJECT="$ROOT_DIR/aspnet-core/src/PrivateCloudDrive.DbMigrator/PrivateCloudDrive.DbMigrator.csproj"
USER_NAME="${PCD_QA_TEST_ACCOUNT_USER_NAME:-qa_user}"
ALT_USER_NAME="${PCD_QA_TEST_ACCOUNT_ALT_USER_NAME:-qa_user_alt}"
ROLE_NAME="${PCD_QA_TEST_ACCOUNT_ROLE:-QA.Tester}"
ENABLED_RAW="${PCD_QA_TEST_ACCOUNT_ENABLED:-true}"
SECRET_ID="${PCD_QA_TEST_ACCOUNT_SECRET_ID:-}"
ROTATED_AT="${PCD_QA_TEST_ACCOUNT_ROTATED_AT:-}"
PASSWORD="${PCD_QA_TEST_ACCOUNT_PASSWORD:-}"
PASSWORD_FILE="${PCD_QA_TEST_ACCOUNT_PASSWORD_FILE:-}"
MIN_PASSWORD_LENGTH=12

log_field() {
  printf '%s=%s\n' "$1" "$2"
}

fail() {
  printf 'ERROR: %s\n' "$1" >&2
  exit 1
}

normalize_bool() {
  case "${1,,}" in
    1|true|yes|on) printf 'true' ;;
    0|false|no|off|'') printf 'false' ;;
    *) fail "PCD_QA_TEST_ACCOUNT_ENABLED must be true/false" ;;
  esac
}

load_password_from_file() {
  tr -d '\r\n' < "$1"
}

ENABLED="$(normalize_bool "$ENABLED_RAW")"

if [ "$ENABLED" = "true" ] && [ -z "$PASSWORD" ] && [ -n "$PASSWORD_FILE" ]; then
  if [ ! -f "$PASSWORD_FILE" ]; then
    fail "PCD_QA_TEST_ACCOUNT_PASSWORD_FILE does not exist"
  fi

  PASSWORD="$(load_password_from_file "$PASSWORD_FILE")"
  if [ -z "$SECRET_ID" ]; then
    SECRET_ID="$PASSWORD_FILE"
  fi
fi

if [ "$ENABLED" = "true" ] && [ -z "$PASSWORD" ]; then
  fail "Set PCD_QA_TEST_ACCOUNT_PASSWORD or PCD_QA_TEST_ACCOUNT_PASSWORD_FILE before preparing QA test accounts"
fi

if [ "$ENABLED" = "true" ] && [ ${#PASSWORD} -lt ${MIN_PASSWORD_LENGTH} ]; then
  fail "PCD_QA_TEST_ACCOUNT_PASSWORD must be at least ${MIN_PASSWORD_LENGTH} characters"
fi

if [ -z "$SECRET_ID" ]; then
  SECRET_ID="env:PCD_QA_TEST_ACCOUNT_PASSWORD"
fi

if [ -z "$ROTATED_AT" ]; then
  ROTATED_AT="unknown"
fi

export PCD_QA_TEST_ACCOUNT_ENABLED="$ENABLED"
export PCD_QA_TEST_ACCOUNT_USER_NAME="$USER_NAME"
export PCD_QA_TEST_ACCOUNT_ALT_USER_NAME="$ALT_USER_NAME"
export PCD_QA_TEST_ACCOUNT_ROLE="$ROLE_NAME"
if [ "$ENABLED" = "true" ]; then
  export PCD_QA_TEST_ACCOUNT_PASSWORD="$PASSWORD"
else
  unset PCD_QA_TEST_ACCOUNT_PASSWORD || true
fi
export PCD_QA_TEST_ACCOUNT_SECRET_ID="$SECRET_ID"
export PCD_QA_TEST_ACCOUNT_ROTATED_AT="$ROTATED_AT"

log_field "qa_test_account_prepare" "start"
log_field "enabled" "$ENABLED"
log_field "user_name" "$USER_NAME"
log_field "alt_user_name" "$ALT_USER_NAME"
log_field "role" "$ROLE_NAME"
log_field "secret_id" "$SECRET_ID"
log_field "rotated_at" "$ROTATED_AT"
log_field "sanitized" "true"

if [ "${PCD_QA_TEST_ACCOUNT_SKIP_MIGRATOR:-false}" = "true" ]; then
  log_field "db_migrator" "skipped"
  log_field "qa_test_account_prepare" "complete"
  exit 0
fi

cd "$ROOT_DIR"
if [ -f "$MIGRATOR_PROJECT" ]; then
  dotnet run --project "$MIGRATOR_PROJECT" --no-launch-profile >/dev/null 2>&1
else
  fail "DbMigrator project not found"
fi

log_field "db_migrator" "completed"
log_field "qa_test_account_prepare" "complete"
