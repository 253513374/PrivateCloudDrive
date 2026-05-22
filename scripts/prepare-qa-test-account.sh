#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MIGRATOR_PROJECT="$ROOT_DIR/aspnet-core/src/PrivateCloudDrive.DbMigrator/PrivateCloudDrive.DbMigrator.csproj"

if [[ "${PCD_QA_TEST_ACCOUNT_ENABLED:-}" != "true" && "${PCD_QA_TEST_ACCOUNT_ENABLED:-}" != "1" && "${PCD_QA_TEST_ACCOUNT_ENABLED:-}" != "yes" ]]; then
  export PCD_QA_TEST_ACCOUNT_ENABLED=true
fi

if [[ -z "${PCD_QA_TEST_ACCOUNT_PASSWORD:-}" && -z "${PCD_QA_TEST_ACCOUNT_PASSWORD_FILE:-}" ]]; then
  echo "ERROR: PCD_QA_TEST_ACCOUNT_PASSWORD or PCD_QA_TEST_ACCOUNT_PASSWORD_FILE is required." >&2
  exit 2
fi

if [[ -n "${PCD_QA_TEST_ACCOUNT_PASSWORD_FILE:-}" && ! -f "${PCD_QA_TEST_ACCOUNT_PASSWORD_FILE}" ]]; then
  echo "ERROR: PCD_QA_TEST_ACCOUNT_PASSWORD_FILE points to a missing file." >&2
  exit 2
fi

if [[ "${PCD_QA_TEST_ACCOUNT_SKIP_MIGRATOR:-}" == "true" ]]; then
  echo "QA test account environment validated; migrator skipped. user=qa_user alt_user=qa_user_alt role=QA.Tester force_rotate=${PCD_QA_TEST_ACCOUNT_FORCE_ROTATE:-false}"
  exit 0
fi

echo "Preparing QA test accounts via DbMigrator. user=qa_user alt_user=qa_user_alt role=QA.Tester force_rotate=${PCD_QA_TEST_ACCOUNT_FORCE_ROTATE:-false}"
dotnet run --project "$MIGRATOR_PROJECT" --no-launch-profile
