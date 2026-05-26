import json
import subprocess
import sys
from pathlib import Path

from scripts.validation_evidence_index import iter_validation_evidence_files, scan_text_for_sensitive_data

REPO_ROOT = Path(__file__).resolve().parents[1]
SCRIPT = REPO_ROOT / "scripts" / "validation_evidence_index.py"
SHARE_URL = "https://" + "cloud.example.com" + "/share/public/abcdef" + "?code=" + "opaque-value"


def run_script(repo: Path, run_id: str = "unit"):
    return subprocess.run(
        [sys.executable, str(SCRIPT), "--repo-root", str(repo), "--date", "20260522", "--run-id", run_id],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )


def init_repo(repo: Path):
    subprocess.run(["git", "init"], cwd=repo, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
    subprocess.run(["git", "config", "user.name", "Test Bot"], cwd=repo, check=True)


def test_pass_status_line_is_not_reported_as_password_secret():
    findings = scan_text_for_sensitive_data("docs/validation/report.md", "- PASS: 14\n- WARN: 0\n- FAIL: 0\n")
    assert findings == []


def test_android_log_transition_token_is_not_reported_as_access_token():
    text = "05-22 WindowManager: token=WCT{RemoteToken{abc123}} transition ok\n"

    findings = scan_text_for_sensitive_data("docs/validation/android-logcat.log", text)

    assert findings == []


def test_hyphenated_auth_token_keys_are_reported_as_access_token():
    text = "access-token: access-value-123\nid-token = id-value-123\n"

    findings = scan_text_for_sensitive_data("docs/validation/auth.md", text)

    assert [finding.rule for finding in findings] == ["access_token", "access_token"]
    serialized = json.dumps([finding.__dict__ for finding in findings], ensure_ascii=False)
    assert "access-value-123" not in serialized
    assert "id-value-123" not in serialized


def test_explicit_secret_keys_are_reported_with_specific_rules_and_redacted():
    text = (
        "password = PassValue123\n"
        "access_token: access-value-123\n"
        "refresh_token: refresh-value-123\n"
        "cookie: sessionid=abcdef\n"
        "client_secret: client-value-123\n"
        f"public link {SHARE_URL}\n"
    )
    findings = scan_text_for_sensitive_data("docs/validation/leak.md", text)
    rules = [finding.rule for finding in findings]
    assert {"password", "access_token", "refresh_token", "cookie", "client_secret", "public_share_url"}.issubset(set(rules))
    serialized = json.dumps([finding.__dict__ for finding in findings], ensure_ascii=False)
    for raw_value in [
        "PassValue123",
        "access-value-123",
        "refresh-value-123",
        "sessionid=abcdef",
        "client-value-123",
        SHARE_URL,
    ]:
        assert raw_value not in serialized


def test_generated_daily_acceptance_outputs_are_not_rescanned(tmp_path):
    validation = tmp_path / "docs" / "validation"
    generated = validation / "daily-acceptance-20260522-old"
    generated.mkdir(parents=True)
    (generated / "sensitive-scan.md").write_text("password = ShouldNotBeScanned\n", encoding="utf-8")
    (validation / "manual-evidence.md").write_text("- PASS: 14\n", encoding="utf-8")

    scanned = [path.name for path in iter_validation_evidence_files(validation)]

    assert scanned == ["manual-evidence.md"]


def test_json_yaml_and_csv_validation_evidence_are_scanned(tmp_path):
    validation = tmp_path / "docs" / "validation"
    validation.mkdir(parents=True)
    for name in ["evidence.json", "evidence.yaml", "evidence.yml", "evidence.csv", "evidence.xml"]:
        (validation / name).write_text("PASS: 1\n", encoding="utf-8")

    scanned = [path.name for path in iter_validation_evidence_files(validation)]

    assert scanned == ["evidence.csv", "evidence.json", "evidence.xml", "evidence.yaml", "evidence.yml"]


def test_missing_validation_directory_fails_closed(tmp_path):
    repo = tmp_path
    init_repo(repo)

    result = run_script(repo)

    assert result.returncode == 1
    assert "Validation evidence directory not found" in result.stderr


def test_generates_daily_acceptance_index_and_json_with_tracked_state_and_metadata(tmp_path):
    repo = tmp_path
    init_repo(repo)
    validation = repo / "docs" / "validation"
    validation.mkdir(parents=True)
    tracked = validation / "backend-tests-2026-05-22.log"
    tracked.write_text("backend PASS\nWARN: slow query\n", encoding="utf-8")
    untracked = validation / "mobile-check-2026-05-22.md"
    untracked.write_text("# mobile\nFAIL: screenshot missing\n", encoding="utf-8")
    subprocess.run(["git", "add", str(tracked.relative_to(repo))], cwd=repo, check=True)
    subprocess.run(["git", "commit", "-m", "seed evidence"], cwd=repo, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    result = run_script(repo)

    assert result.returncode == 0, result.stderr + result.stdout
    output_dir = validation / "daily-acceptance-20260522-unit"
    index_json = json.loads((output_dir / "validation-evidence-index.json").read_text(encoding="utf-8"))
    paths = {item["path"]: item for item in index_json["evidence"]}
    backend = paths["docs/validation/backend-tests-2026-05-22.log"]
    mobile = paths["docs/validation/mobile-check-2026-05-22.md"]
    assert backend["tracked"] is True
    assert backend["status_counts"]["PASS"] == 1
    assert backend["modified_at_utc"]
    assert backend["evidence_domain"] == "backend"
    assert mobile["tracked"] is False
    assert mobile["status_counts"]["FAIL"] == 1
    assert mobile["evidence_domain"] == "mobile"
    assert index_json["totals"]["files"] == 2
    assert (output_dir / "validation-evidence-index.md").exists()
    assert (output_dir / "sensitive-scan.md").exists()


def test_sensitive_scan_json_contains_only_redacted_finding_shape(tmp_path):
    repo = tmp_path
    init_repo(repo)
    validation = repo / "docs" / "validation"
    validation.mkdir(parents=True)
    evidence = validation / "share-and-token.md"
    evidence.write_text(
        "password = PassValue123\n"
        "access_token: abc.def.ghi\n"
        "refresh_token: refresh-value-123\n"
        "cookie: sessionid=abcdef\n"
        "client_secret: client-value-123\n"
        f"public link {SHARE_URL}\n",
        encoding="utf-8",
    )

    result = run_script(repo, "sensitive")

    assert result.returncode == 2
    output_dir = validation / "daily-acceptance-20260522-sensitive"
    scan = json.loads((output_dir / "sensitive-scan.json").read_text(encoding="utf-8"))
    assert scan["finding_count"] >= 6
    serialized = json.dumps(scan, ensure_ascii=False)
    for raw_value in [
        "PassValue123",
        "abc.def.ghi",
        "refresh-value-123",
        "sessionid=abcdef",
        "client-value-123",
        SHARE_URL,
    ]:
        assert raw_value not in serialized
    for finding in scan["findings"]:
        assert set(finding) == {"path", "line", "rule", "redacted_excerpt"}


def test_sensitive_scan_markdown_escapes_backticks(tmp_path):
    repo = tmp_path
    init_repo(repo)
    validation = repo / "docs" / "validation"
    validation.mkdir(parents=True)
    evidence = validation / "backtick-share.md"
    evidence.write_text("password = `PassValue123`\n", encoding="utf-8")

    result = run_script(repo, "backticks")

    assert result.returncode == 2
    output_dir = validation / "daily-acceptance-20260522-backticks"
    markdown = (output_dir / "sensitive-scan.md").read_text(encoding="utf-8")
    assert "`PassValue123`" not in markdown
    assert "[REDACTED_SECRET]" in markdown
