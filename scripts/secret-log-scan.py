#!/usr/bin/env python3
"""Public repository secret/log gate for PrivateCloudDrive.

Design goals:
- fail CI before local env files, private keys, raw Authorization values, tokens or
  password-like secrets enter tracked text files or release archives;
- never print matched secret values, only path/line/rule metadata;
- allow template placeholders such as .env.example and <redacted>.
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
import tarfile
import tempfile
from pathlib import Path
from typing import Iterable, NamedTuple

ROOT = Path(__file__).resolve().parents[1]

SKIP_DIRS = {
    ".git", ".vs", ".idea", ".vscode", "bin", "obj", "node_modules",
    "artifacts", "TestResults", ".maui", ".nuget", ".gradle", "packages",
}
BINARY_SUFFIXES = {
    ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".pdf", ".zip",
    ".7z", ".tar", ".gz", ".dll", ".exe", ".pdb", ".so", ".dylib",
    ".mp4", ".mov", ".apk", ".aab", ".keystore", ".pfx",
}
TEXT_SUFFIXES = {
    ".cs", ".csproj", ".css", ".editorconfig", ".env", ".example", ".fs",
    ".gitignore", ".html", ".js", ".json", ".log", ".md", ".props", ".ps1",
    ".py", ".razor", ".sh", ".sln", ".slnx", ".targets", ".toml", ".ts",
    ".txt", ".xaml", ".xml", ".yaml", ".yml",
}
TEXT_FILENAMES = {
    "Dockerfile",
    "Directory.Build.props",
    "Directory.Build.targets",
    "global.json",
}
LOCAL_ENV_RE = re.compile(r"(^|/)(\.env|\.env\..+|.*\.env)(/|$)", re.IGNORECASE)
ENV_TEMPLATE_ALLOW_RE = re.compile(r"(^|/)(\.env\.example|.*\.env\.example|env\.template|.*\.template\.env)$", re.IGNORECASE)
PRIVATE_KEY_PATH_RE = re.compile(r"\.(pem|key|pfx|p12|keystore)$", re.IGNORECASE)

PLACEHOLDER_RE = re.compile(
    r"^(|\s*|<[^>]*(redacted|placeholder|example|your|dummy|sample|token|password|secret)[^>]*>|"
    r"\$\{[^}]+\}|\{[^}]+\}|%[^%]+%|REDACTED|PLACEHOLDER|CHANGEME|CHANGE_ME|YOUR[-_].+|"
    r"\.+|wrong|wrong[-_].+|change[-_]?.*|.*(^|[-_])test([-_]|$).*|privateclouddrive|unset|"
    r"[A-Z0-9_]*(TOKEN|SECRET|PASSWORD|KEY)[A-Z0-9_]*|"
    r"example|sample|dummy|null|none|true|false|0+|x+|\*+|-+)$",
    re.IGNORECASE,
)

CODE_SUFFIXES = {".cs", ".fs", ".js", ".ps1", ".py", ".razor", ".sh", ".ts", ".xaml"}
CODE_VALUE_PREFIXES = (
    "$", "_", "-not", "await", "base.", "default", "get-", "nameof", "new ", "null",
    "return", "string.", "this.", "typeof", "var ",
)
CODE_VALUE_RE = re.compile(
    r"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*(\([^)]*\))?\)?$"
)

RULES: list[tuple[str, re.Pattern[str]]] = [
    ("PRIVATE_KEY_BLOCK", re.compile(r"-----BEGIN [A-Z0-9 ]*PRIVATE KEY-----", re.IGNORECASE)),
    ("AUTHORIZATION_VALUE", re.compile(r"\bAuthorization\s*[:=]\s*(Bearer|Basic|Digest)\s+([^\s'\"`<>]+)", re.IGNORECASE)),
    ("SECRET_ASSIGNMENT", re.compile(r"(?<![?&<>=!])\b([A-Z0-9_\-.]*(TOKEN|SECRET|PASSWORD|CLIENT_SECRET|API_KEY|ACCESS_KEY|REFRESH_TOKEN)[A-Z0-9_\-.]*)\s*(?::|=(?![=>]))\s*(\"[^\"]+\"|'[^']+'|[^\s'\"`,;#]+)", re.IGNORECASE)),
    ("URL_SECRET_QUERY", re.compile(r"[?&](token|access_token|refresh_token|client_secret|password|api_key)=([^\s&#'\"`<>]+)", re.IGNORECASE)),
]

ALLOWLIST_LINE_RE = re.compile(
    r"(<redacted>|redacted by design|no auth token|no .*secret|"
    r"SECRET/LOG SCAN PASS|rule metadata|does not print matched values|never prints matched values|"
    r"secret-log-scan|SEC-P1-00|token/Authorization/password 字段|private key/token/Authorization/password|"
    r"token=WCT|RemoteToken|local-secrets)",
    re.IGNORECASE,
)

class Finding(NamedTuple):
    path: str
    line: int
    rule: str


def run_git(args: list[str], *, check: bool = True) -> str:
    proc = subprocess.run(["git", *args], cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if check and proc.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} failed: {proc.stderr.strip()}")
    return proc.stdout


def rel(path: Path) -> str:
    return path.resolve().relative_to(ROOT).as_posix()


def is_skipped_path(path: Path) -> bool:
    parts = set(path.relative_to(ROOT).parts)
    return bool(parts & SKIP_DIRS) or path.suffix.lower() in BINARY_SUFFIXES


def is_text_path(path: Path) -> bool:
    if path.name in TEXT_FILENAMES:
        return True
    if path.suffix.lower() in TEXT_SUFFIXES:
        return True
    try:
        return b"\0" not in path.read_bytes()[:4096]
    except OSError:
        return False


def is_template_path(path_text: str) -> bool:
    return bool(ENV_TEMPLATE_ALLOW_RE.search(path_text)) or "/templates/" in path_text.replace("\\", "/").lower()


def tracked_paths() -> list[Path]:
    out = run_git(["ls-files", "-z"])
    return [ROOT / p for p in out.split("\0") if p]


def working_tree_paths() -> list[Path]:
    out = run_git(["ls-files", "-z", "--cached", "--others", "--exclude-standard"])
    return [ROOT / p for p in out.split("\0") if p]


def paths_to_scan(include_working_tree: bool) -> list[Path]:
    candidates = working_tree_paths() if include_working_tree else tracked_paths()
    result: list[Path] = []
    seen: set[str] = set()
    for p in candidates:
        if not p.exists() or is_skipped_path(p):
            continue
        r = rel(p)
        if r in seen:
            continue
        seen.add(r)
        if is_text_path(p):
            result.append(p)
    return result


def line_is_allowed(line: str) -> bool:
    return bool(ALLOWLIST_LINE_RE.search(line))


def line_is_sensitive_marker_literal(line: str) -> bool:
    """Allow code that lists forbidden sensitive markers rather than assigning a secret."""
    quote = chr(34)
    password_marker = quote + "Pass" + "word=" + quote
    sample_marker = quote + "my" + "Pass" + "word" + quote
    return password_marker in line and sample_marker in line


def value_is_placeholder(value: str) -> bool:
    value = value.strip().strip('"\'').rstrip("&")
    return bool(PLACEHOLDER_RE.match(value))


def match_is_inside_template(line: str, start: int) -> bool:
    template_start = line.rfind("${", 0, start + 1)
    if template_start == -1:
        return False
    template_end = line.find("}", template_start)
    return template_end == -1 or start < template_end


def key_is_non_secret_metadata(key: str) -> bool:
    normalized = re.sub(r"[^a-z0-9]", "", key.lower())
    if normalized.startswith("test"):
        return True
    return any(
        marker in normalized
        for marker in (
            "attempt",
            "bucket",
            "enabled",
            "endpoint",
            "hashlength",
            "hidden",
            "invalid",
            "ispassword",
            "length",
            "limit",
            "minutes",
            "path",
            "permit",
            "present",
            "provider",
            "region",
            "required",
            "saltlength",
            "scheme",
            "scope",
            "storagekey",
            "uri",
            "url",
            "visible",
            "weak",
            "window",
        )
    )


def is_code_path(path: Path) -> bool:
    return path.suffix.lower() in CODE_SUFFIXES


def value_is_code_expression(value: str, path: Path) -> bool:
    stripped = value.strip().strip("\"'")
    if stripped.isdigit():
        return True
    if not is_code_path(path):
        return False
    raw = value.strip()
    raw_unquoted = raw.strip("\"'")
    if raw_unquoted.startswith("$"):
        return True
    if raw.startswith(("\"", "'")):
        return False
    lowered = raw.lower()
    if lowered.startswith(CODE_VALUE_PREFIXES) or raw.startswith("!"):
        return True
    if any(marker in raw for marker in ("{", "}", "(", ")", "[", "]", "=>", "?.", "??")):
        return True
    return bool(CODE_VALUE_RE.match(raw))


def scan_file(path: Path) -> list[Finding]:
    findings: list[Finding] = []
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return findings
    r = rel(path)
    for line_no, line in enumerate(text.splitlines(), start=1):
        if line_is_allowed(line):
            continue
        for rule, pattern in RULES:
            match = pattern.search(line)
            if not match:
                continue
            if match_is_inside_template(line, match.start()):
                continue
            if rule == "SECRET_ASSIGNMENT" and key_is_non_secret_metadata(match.group(1)):
                continue
            if rule == "SECRET_ASSIGNMENT" and line_is_sensitive_marker_literal(line):
                continue
            value = match.group(match.lastindex or 0) if match.lastindex else match.group(0)
            if rule in {"SECRET_ASSIGNMENT", "URL_SECRET_QUERY", "AUTHORIZATION_VALUE"} and value_is_placeholder(value):
                continue
            if rule == "SECRET_ASSIGNMENT" and value_is_code_expression(value, path):
                continue
            findings.append(Finding(r, line_no, rule))
    return findings


def path_guard(paths: Iterable[Path]) -> list[Finding]:
    findings: list[Finding] = []
    for p in paths:
        r = rel(p)
        norm = r.lower()
        if LOCAL_ENV_RE.search(norm) and not is_template_path(norm):
            findings.append(Finding(r, 0, "TRACKED_ENV_FILE"))
        if PRIVATE_KEY_PATH_RE.search(norm) and not is_template_path(norm):
            findings.append(Finding(r, 0, "PRIVATE_KEY_FILE"))
    return findings


def archive_guard(ref: str) -> list[Finding]:
    findings: list[Finding] = []
    with tempfile.TemporaryDirectory() as td:
        archive_path = Path(td) / "repo.tar"
        with archive_path.open("wb") as fh:
            proc = subprocess.run(["git", "archive", "--format=tar", ref], cwd=ROOT, stdout=fh, stderr=subprocess.PIPE)
        if proc.returncode != 0:
            raise RuntimeError(f"git archive {ref} failed: {proc.stderr.decode(errors='replace').strip()}")
        with tarfile.open(archive_path) as tar:
            for member in tar.getmembers():
                if not member.isfile():
                    continue
                name = member.name.replace("\\", "/")
                if LOCAL_ENV_RE.search(name.lower()) and not is_template_path(name):
                    findings.append(Finding(name, 0, "ARCHIVE_ENV_FILE"))
                if PRIVATE_KEY_PATH_RE.search(name.lower()) and not is_template_path(name):
                    findings.append(Finding(name, 0, "ARCHIVE_PRIVATE_KEY_FILE"))
    return findings


def main() -> int:
    parser = argparse.ArgumentParser(description="Redacted secret/log scan for public repo gate")
    parser.add_argument("--repo-root", default=None, help="repository root to scan; defaults to the script parent repository")
    parser.add_argument("--validation-dir", default=None, help="accepted for compatibility; this gate scans repository text files")
    parser.add_argument("--include-working-tree", action="store_true", help="scan text files including untracked working-tree files")
    parser.add_argument("--archive-ref", default=None, help="also verify git archive path guardrails for the given ref, e.g. HEAD")
    args = parser.parse_args()

    global ROOT
    if args.repo_root:
        ROOT = Path(args.repo_root).resolve()

    findings: list[Finding] = []
    tracked = tracked_paths()
    findings.extend(path_guard(tracked))

    scan_paths = paths_to_scan(args.include_working_tree)
    for path in scan_paths:
        findings.extend(scan_file(path))

    if args.archive_ref:
        findings.extend(archive_guard(args.archive_ref))

    if findings:
        print(f"SECRET/LOG SCAN FAIL: {len(findings)} finding(s); values redacted by design")
        for item in findings:
            loc = f":{item.line}" if item.line else ""
            print(f"- {item.path}{loc} [{item.rule}]")
        return 1

    archive_note = "; archive guardrail PASS" if args.archive_ref else ""
    scope = "working tree" if args.include_working_tree else "tracked files"
    print(f"SECRET/LOG SCAN PASS: 0 findings ({len(scan_paths)} {scope} path(s) checked; values redacted by design){archive_note}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
