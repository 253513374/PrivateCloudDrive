#!/usr/bin/env python3
"""Build a sanitized validation evidence index for daily acceptance artifacts.

Exit code: 0 means no sensitive findings; 2 means reports were generated but
one or more token/cookie/password/share URL findings need remediation.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

TEXT_SUFFIXES = {".md", ".txt", ".json", ".log", ".yml", ".yaml", ".csv", ".xml"}
STATUS_WORDS = ("PASS", "WARN", "FAIL")
SENSITIVE_RULES: tuple[tuple[str, re.Pattern[str], str], ...] = (
    ("refresh_token", re.compile(r"(?i)\brefresh[_-]?token\b\s*[:=]\s*[^\s,;]+"), "[REDACTED_TOKEN]"),
    ("access_token", re.compile(r"(?i)\b(?:access[_-]?token|id[_-]?token)\b\s*[:=]\s*[^\s,;]+"), "[REDACTED_TOKEN]"),
    ("cookie", re.compile(r"(?i)\b(?:cookie|session[_-]?cookie|set-cookie)\b\s*[:=]\s*[^\s,;]+"), "[REDACTED_COOKIE]"),
    ("public_share_url", re.compile(r"(?i)(?:public[_-]?share[_-]?url\s*[:=]\s*)?https?://[^\s)\]}>\"']*(?:/(?:s|share|public)/|share/public/)[^\s)\]}>\"']+"), "[REDACTED_SHARE_URL]"),
    ("client_secret", re.compile(r"(?i)\bclient_secret\b\s*[:=]\s*[^\s,;]+"), "[REDACTED_SECRET]"),
    ("password", re.compile(r"(?i)\b(?:password|passwd|pwd)\b\s*[:=]\s*[^\s,;]+"), "[REDACTED_SECRET]"),
)

@dataclass(frozen=True)
class Finding:
    path: str
    line: int
    rule: str
    redacted_excerpt: str


def sanitize_slug(value: str) -> str:
    slug = re.sub(r"[^A-Za-z0-9]+", "-", value.strip()).strip("-").lower()
    return slug or "manual-run"


def redact(text: str) -> str:
    redacted = text
    for _rule, pattern, replacement in SENSITIVE_RULES:
        redacted = pattern.sub(replacement, redacted)
    return redacted


def is_generated_daily_acceptance(path: Path, validation_dir: Path) -> bool:
    try:
        parts = path.resolve().relative_to(validation_dir.resolve()).parts
    except ValueError:
        return False
    return any(part.startswith("daily-acceptance-") for part in parts)


def iter_text_files(validation_dir: Path) -> Iterable[Path]:
    if not validation_dir.exists():
        return []
    files: list[Path] = []
    for path in validation_dir.rglob("*"):
        if path.is_file() and path.suffix.lower() in TEXT_SUFFIXES and not is_generated_daily_acceptance(path, validation_dir):
            files.append(path)
    return sorted(files)

# Compatibility name used by earlier regression tests.
def iter_validation_evidence_files(validation_root: Path) -> Iterable[Path]:
    return iter_text_files(validation_root)


def repo_relative(repo_root: Path, path: Path) -> str:
    return path.resolve().relative_to(repo_root.resolve()).as_posix()


def git_tracked_paths(repo_root: Path) -> set[str]:
    try:
        result = subprocess.run(["git", "ls-files"], cwd=repo_root, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    except OSError:
        return set()
    if result.returncode != 0:
        return set()
    return {line.strip().replace("\\", "/") for line in result.stdout.splitlines() if line.strip()}


def status_counts(text: str) -> dict[str, int]:
    return {word: len(re.findall(rf"\b{word}\b", text, flags=re.IGNORECASE)) for word in STATUS_WORDS}


def scan_text(text: str, relative_path: str) -> list[Finding]:
    findings: list[Finding] = []
    for line_no, line in enumerate(text.splitlines(), start=1):
        seen: set[str] = set()
        for rule, pattern, _replacement in SENSITIVE_RULES:
            if rule not in seen and pattern.search(line):
                seen.add(rule)
                findings.append(Finding(relative_path, line_no, rule, redact(line.strip())[:240]))
    return findings

# Compatibility name used by earlier regression tests.
def scan_text_for_sensitive_data(path: str, text: str) -> list[Finding]:
    return scan_text(text, path)


def scan_sensitive_file(path: Path) -> list[dict]:
    text = path.read_text(encoding="utf-8", errors="replace")
    return [finding.__dict__ for finding in scan_text(text, path.as_posix())]


def classify_evidence(path: Path) -> str:
    name = path.name.lower()
    if "daily-acceptance" in name:
        return "daily-acceptance"
    if "logcat" in name or path.suffix.lower() == ".log":
        return "log-summary"
    if path.suffix.lower() == ".json":
        return "json-summary"
    if path.suffix.lower() == ".md":
        return "markdown-report"
    return "text-evidence"


def evidence_domain(path: Path) -> str:
    relative = path.as_posix().lower()
    name = path.name.lower()
    if "backend" in name or "/backend" in relative or "aspnet" in relative:
        return "backend"
    if "mobile" in name or "maui" in relative or "android" in name or "logcat" in name:
        return "mobile"
    if "backup" in name or "restore" in name:
        return "backup-restore"
    if "secret" in name or "scan" in name:
        return "security"
    return "validation"


def build_outputs(repo_root: Path, run_id: str, date: str, validation_root: Path | None = None) -> tuple[dict, dict, str]:
    validation_dir = (validation_root or repo_root / "docs" / "validation").resolve()
    safe_run_id = sanitize_slug(run_id)
    output_name = f"daily-acceptance-{date}-{safe_run_id}"
    tracked_paths = git_tracked_paths(repo_root)
    evidence: list[dict] = []
    findings: list[Finding] = []
    for path in iter_text_files(validation_dir):
        relative = repo_relative(repo_root, path) if path.resolve().is_relative_to(repo_root.resolve()) else path.as_posix()
        try:
            text = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            text = ""
        stat = path.stat()
        counts = status_counts(text)
        evidence.append({
            "path": relative,
            "type": classify_evidence(path),
            "evidence_domain": evidence_domain(path),
            "tracked": relative in tracked_paths,
            "size_bytes": stat.st_size,
            "modified_at_utc": datetime.fromtimestamp(stat.st_mtime, timezone.utc).replace(microsecond=0).isoformat(),
            "status_counts": counts,
            "status_summary": ", ".join(f"{word}={counts[word]}" for word in STATUS_WORDS if counts[word]) or "no PASS/WARN/FAIL markers",
        })
        findings.extend(scan_text(text, relative))
    scan = {"schema_version": 1, "status": "FAIL" if findings else "PASS", "finding_count": len(findings), "findings": [finding.__dict__ for finding in findings]}
    totals = {
        "files": len(evidence),
        "tracked": sum(1 for item in evidence if item["tracked"]),
        "untracked": sum(1 for item in evidence if not item["tracked"]),
        "PASS": sum(item["status_counts"]["PASS"] for item in evidence),
        "WARN": sum(item["status_counts"]["WARN"] for item in evidence),
        "FAIL": sum(item["status_counts"]["FAIL"] for item in evidence),
        "sensitive_findings": len(findings),
    }
    index = {"schema_version": 1, "generated_at_utc": datetime.now(timezone.utc).replace(microsecond=0).isoformat(), "run_id": run_id, "safe_run_id": safe_run_id, "date": date, "status": "FAIL" if findings else "PASS", "finding_count": len(findings), "output_dir": output_name, "evidence_count": len(evidence), "totals": totals, "evidence": evidence, "entries": evidence, "sensitive_scan": scan}
    return index, scan, output_name


def write_outputs(index: dict, scan: dict, output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    (output_dir / "validation-evidence-index.json").write_text(json.dumps(index, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (output_dir / "sensitive-scan.json").write_text(json.dumps(scan, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    index_lines = ["# Validation Evidence Index", "", f"- Run ID: `{index['run_id']}`", f"- Date: `{index['date']}`", f"- Daily acceptance artifact: `{index['output_dir']}`", f"- Status: **{index['status']}**", f"- Evidence files: {index['evidence_count']}", f"- Sensitive findings: {index['finding_count']}", "", "## Evidence", "", "| Path | Type | Tracked | PASS | WARN | FAIL | Size |", "| --- | --- | --- | ---: | ---: | ---: | ---: |"]
    for item in index["evidence"]:
        counts = item["status_counts"]
        index_lines.append(f"| `{item['path']}` | {item['type']} | {item['tracked']} | {counts['PASS']} | {counts['WARN']} | {counts['FAIL']} | {item['size_bytes']} |")
    (output_dir / "validation-evidence-index.md").write_text("\n".join(index_lines) + "\n", encoding="utf-8")
    scan_lines = ["# Sensitive Scan Summary", "", f"- Status: {scan['status']}", f"- Finding count: {scan['finding_count']}", ""]
    if scan["findings"]:
        scan_lines.extend(["| Rule | Path | Line | Redacted excerpt |", "| --- | --- | ---: | --- |"])
        for finding in scan["findings"]:
            excerpt = finding["redacted_excerpt"].replace("|", "\\|")
            scan_lines.append(f"| {finding['rule']} | `{finding['path']}` | {finding['line']} | `{excerpt}` |")
    else:
        scan_lines.append("No token, cookie, password/client secret, or complete share URL findings.")
    (output_dir / "sensitive-scan.md").write_text("\n".join(scan_lines) + "\n", encoding="utf-8")


def build_validation_evidence_index(repo_root: Path, run_id: str, date: str | None = None, output_root: Path | None = None, validation_root: Path | None = None) -> dict:
    repo_root = repo_root.resolve()
    run_date = date or datetime.now(timezone.utc).strftime("%Y%m%d")
    index, scan, output_name = build_outputs(repo_root, run_id, run_date, validation_root)
    base = output_root.resolve() if output_root else (validation_root.resolve() if validation_root else repo_root / "docs" / "validation")
    write_outputs(index, scan, base / output_name)
    return index


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", default=".")
    parser.add_argument("--validation-root", default=None)
    parser.add_argument("--output-root", default=None)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--date", default=None)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    repo_root = Path(args.repo_root).resolve()
    index = build_validation_evidence_index(
        repo_root,
        args.run_id,
        date=args.date,
        output_root=Path(args.output_root) if args.output_root else None,
        validation_root=Path(args.validation_root) if args.validation_root else None,
    )
    print(f"validation_evidence_index output={index['output_dir']} status={index['status']} evidence_count={index['evidence_count']} finding_count={index['sensitive_scan']['finding_count']}")
    return 2 if index["sensitive_scan"]["finding_count"] else 0

if __name__ == "__main__":
    raise SystemExit(main())
