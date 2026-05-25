#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MIGRATOR_PROJECT="$ROOT_DIR/aspnet-core/src/PrivateCloudDrive.DbMigrator/PrivateCloudDrive.DbMigrator.csproj"
MIN_PASSWORD_LENGTH=8

normalize_true() {
  local value="${1:-}"
  [[ "$value" == "true" || "$value" == "1" || "$value" == "yes" ]]
}

load_secret() {
  if [[ -n "${PCD_QA_TEST_ACCOUNT_PASSWORD:-}" ]]; then
    printf '%s' "$PCD_QA_TEST_ACCOUNT_PASSWORD"
    return 0
  fi

  if [[ -n "${PCD_QA_TEST_ACCOUNT_PASSWORD_FILE:-}" ]]; then
    tr -d '\r' < "$PCD_QA_TEST_ACCOUNT_PASSWORD_FILE" | sed ':a;N;$!ba;s/\n$//'
    return 0
  fi

  return 1
}

if [[ -z "${PCD_QA_TEST_ACCOUNT_ENABLED:-}" ]]; then
  export PCD_QA_TEST_ACCOUNT_ENABLED=true
elif ! normalize_true "${PCD_QA_TEST_ACCOUNT_ENABLED}"; then
  echo "QA test account seed disabled by ${PCD_QA_TEST_ACCOUNT_ENABLED:+explicit }${PCD_QA_TEST_ACCOUNT_ENABLED}. Nothing to do."
  exit 0
fi

if [[ -z "${PCD_QA_TEST_ACCOUNT_PASSWORD:-}" && -z "${PCD_QA_TEST_ACCOUNT_PASSWORD_FILE:-}" ]]; then
  echo "ERROR: PCD_QA_TEST_ACCOUNT_PASSWORD or PCD_QA_TEST_ACCOUNT_PASSWORD_FILE is required." >&2
  exit 2
fi

if [[ -n "${PCD_QA_TEST_ACCOUNT_PASSWORD_FILE:-}" && ! -f "${PCD_QA_TEST_ACCOUNT_PASSWORD_FILE}" ]]; then
  echo "ERROR: PCD_QA_TEST_ACCOUNT_PASSWORD_FILE points to a missing file." >&2
  exit 2
fi

SECRET_VALUE="$(load_secret)"
if [[ ${#SECRET_VALUE} -lt ${MIN_PASSWORD_LENGTH} ]]; then
  echo "ERROR: QA test account password must be at least ${MIN_PASSWORD_LENGTH} characters." >&2
  exit 2
fi
unset SECRET_VALUE

if [[ "${PCD_QA_TEST_ACCOUNT_SKIP_MIGRATOR:-}" == "true" ]]; then
  echo "QA test account environment validated; migrator skipped. user=qa_user alt_user=qa_user_alt role=QA.Tester force_rotate=${PCD_QA_TEST_ACCOUNT_FORCE_ROTATE:-false}"
  exit 0
fi

echo "Preparing QA test accounts via DbMigrator. user=qa_user alt_user=qa_user_alt role=QA.Tester force_rotate=${PCD_QA_TEST_ACCOUNT_FORCE_ROTATE:-false}"
dotnet run --project "$MIGRATOR_PROJECT" --no-launch-profile >/dev/null 2>&1
