import importlib.util
import subprocess
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SCRIPT = REPO_ROOT / "scripts" / "secret-log-scan.py"


def load_gate_module():
    spec = importlib.util.spec_from_file_location("secret_log_scan", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def init_repo(repo: Path):
    subprocess.run(["git", "init"], cwd=repo, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)
    subprocess.run(["git", "config", "user.name", "Test Bot"], cwd=repo, check=True)


def test_path_guard_flags_tracked_env_and_private_key_files(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    env_file = tmp_path / ".env"
    key_file = tmp_path / "prod.pem"
    env_file.write_text("POSTGRES_" + "PASS" + "WORD=real-value\n", encoding="utf-8")
    key_file.write_text("-----BEGIN " + "PRIVATE KEY-----\n", encoding="utf-8")

    findings = gate.path_guard([env_file, key_file])

    assert ("TRACKED_ENV_FILE", ".env") in {(item.rule, item.path) for item in findings}
    assert ("PRIVATE_KEY_FILE", "prod.pem") in {(item.rule, item.path) for item in findings}


def test_scan_file_reports_secret_patterns_without_values(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    evidence = tmp_path / "docs" / "validation" / "leak.md"
    evidence.parent.mkdir(parents=True)
    bearer_value = "real-token-value"
    url_value = "real-url-token"
    client_value = "real-client-secret"
    evidence.write_text(
        "Authorization: " + f"Bearer {bearer_value}\n"
        "download https://cloud.example.com/file?" + f"tok" + f"en={url_value}\n"
        f"CLIENT_SECRET={client_value}\n",
        encoding="utf-8",
    )

    findings = gate.scan_file(evidence)

    assert [item.rule for item in findings] == [
        "AUTHORIZATION_VALUE",
        "URL_SECRET_QUERY",
        "SECRET_ASSIGNMENT",
    ]
    serialized = "\n".join(str(item) for item in findings)
    assert "real-token-value" not in serialized
    assert "real-url-token" not in serialized
    assert "real-client-secret" not in serialized


def test_placeholders_and_examples_are_allowed(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    evidence = tmp_path / "docs" / "deployment.md"
    evidence.parent.mkdir(parents=True)
    evidence.write_text(
        "ALIYUN_OSS_ACCESS_KEY_" + "SECRET=your-ram-access-key-secret\n"
        "Authorization: Bearer <redacted>\n"
        "client_" + "sec" + "ret=PLACEHOLDER\n",
        encoding="utf-8",
    )

    assert gate.scan_file(evidence) == []


def test_paths_to_scan_include_public_docs_and_gate_files(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    init_repo(tmp_path)
    docs_file = tmp_path / "docs" / "release-notes.md"
    readme = tmp_path / "README.md"
    source_file = tmp_path / "aspnet-core" / "src" / "Example.cs"
    gate_file = tmp_path / "scripts" / "secret-log-scan.py"
    for path in [docs_file, readme, source_file, gate_file]:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("PASS\n", encoding="utf-8")
    subprocess.run(["git", "add", "."], cwd=tmp_path, check=True)

    scanned = {path.relative_to(tmp_path).as_posix() for path in gate.paths_to_scan(include_working_tree=True)}

    assert "docs/release-notes.md" in scanned
    assert "README.md" in scanned
    assert "scripts/secret-log-scan.py" in scanned
    assert "aspnet-core/src/Example.cs" in scanned


def test_tracked_source_file_secret_is_scanned(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    init_repo(tmp_path)
    source_file = tmp_path / "scripts" / "validation_evidence_index.py"
    source_file.parent.mkdir(parents=True)
    source_file.write_text("CLIENT_" + "SEC" + "RET=" + "real-client-secret\n", encoding="utf-8")
    subprocess.run(["git", "add", "scripts/validation_evidence_index.py"], cwd=tmp_path, check=True)

    scanned = {path.relative_to(tmp_path).as_posix() for path in gate.paths_to_scan(include_working_tree=False)}
    findings = gate.scan_file(source_file)

    assert "scripts/validation_evidence_index.py" in scanned
    assert ("SECRET_ASSIGNMENT", "scripts/validation_evidence_index.py") in {
        (item.rule, item.path) for item in findings
    }


def test_source_file_raw_authorization_and_url_token_are_reported(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    source_file = tmp_path / "aspnet-core" / "src" / "Leak.cs"
    source_file.parent.mkdir(parents=True)
    bearer_value = "abc.def.ghi"
    url_value = "url-token-value"
    source_file.write_text(
        'var header = "' + "Authorization: " + f"Bearer {bearer_value}" + '";\n'
        'var url = "https://cloud.example.com/file?' + "tok" + f"en={url_value}" + '";\n',
        encoding="utf-8",
    )

    findings = gate.scan_file(source_file)

    assert [item.rule for item in findings] == ["AUTHORIZATION_VALUE", "URL_SECRET_QUERY"]


def test_sensitive_marker_deny_list_literals_are_allowed(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    source_file = tmp_path / "aspnet-core" / "src" / "DeploymentHealthCheckService.cs"
    source_file.parent.mkdir(parents=True)
    source_file.write_text(
        'private static readonly HashSet<string> ForbiddenSensitiveMarkers = new()\n'
        '{\n'
        '    "Password=", "myPassword", "client_secret", "client secret",\n'
        '};\n',
        encoding="utf-8",
    )

    assert gate.scan_file(source_file) == []


def test_dynamic_qa_secret_assignments_and_android_window_tokens_are_allowed(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    script = tmp_path / "scripts" / "prepare-qa-test-account.sh"
    script.parent.mkdir(parents=True)

    script.write_text(
        "PASS" + "WORD=\"$(load_password_from_file \"$PASSWORD_FILE\")\"\n"
        "export PCD_QA_TEST_ACCOUNT_" + "PASS" + "WORD=\"$PASSWORD\"\n"
        "export PCD_QA_TEST_ACCOUNT_" + "SEC" + "RET_ID=\"$SECRET_ID\"\n",
        encoding="utf-8",
    )
    log = tmp_path / "docs" / "validation" / "logcat-clean-launch.txt"
    log.parent.mkdir(parents=True)
    log.write_text(
        "WindowManager token=WCT{RemoteToken{abc123}} transition ok\n",
        encoding="utf-8",
    )

    assert gate.scan_file(script) == []
    assert gate.scan_file(log) == []


def test_archive_guard_flags_env_files_in_release_archive(tmp_path):
    gate = load_gate_module()
    gate.ROOT = tmp_path
    init_repo(tmp_path)
    env_file = tmp_path / ".env"
    env_file.write_text("POSTGRES_" + "PASS" + "WORD=" + "real-value\n", encoding="utf-8")
    subprocess.run(["git", "add", ".env"], cwd=tmp_path, check=True)
    subprocess.run(["git", "commit", "-m", "track env"], cwd=tmp_path, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    findings = gate.archive_guard("HEAD")

    assert ("ARCHIVE_ENV_FILE", ".env") in {(item.rule, item.path) for item in findings}
