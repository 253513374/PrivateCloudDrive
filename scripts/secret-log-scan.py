#!/usr/bin/env python3
"""Scan validation evidence and working-tree text for accidentally logged secrets.

The scanner is intentionally conservative for evidence artifacts: it flags explicit
password/token/cookie assignments and bearer tokens while allowing documented secret
variable names such as PCD_QA_TEST_ACCOUNT_PASSWORD when no value is present.
"""
from __future__ import annotations

import argparse
import re
import subprocess
from pathlib import Path

SECRET_PATTERNS = [
    re.compile(r"(?i)\b(access[_-]?token|refresh[_-]?token|id[_-]?token|bearer)\b\s*[:=]\s*['\"]?[A-Za-z0-9._~+/=-]{16,}"),
    re.compile(r"(?i)\b(password|passwd|pwd|client[_-]?secret|app[_-]?secret|cookie|set-cookie)\b\s*[:=]\s*['\"]?[^\s'\"]{8,}"),
    re.compile(r"(?i)Authorization:\s*Bearer\s+[A-Za-z0-9._~+/=-]{16,}"),
]
ALLOWLIST = [
    re.compile(r"(?i)<replace|change-this|change-me|example|placeholder|secret_id|value hidden|sanitized=true|\$\{|Configuration|PassPhrase|ConnectionStrings|OpenIddict|privateclouddrive|PCD_QA_TEST_ACCOUNT_PASSWORD(_FILE)?\s*=\s*$"),
    re.compile(r"(?i)PCD_QA_TEST_ACCOUNT_PASSWORD(_FILE)?"),
]
TEXT_EXTENSIONS = {
    ".cs", ".ps1", ".sh", ".py", ".md", ".txt", ".yml", ".yaml", ".json", ".example", ".env"
}
SKIP_DIRS = {".git", "bin", "obj", "node_modules", "artifacts", "TestResults", "coverage", ".secrets"}


def is_text_candidate(path: Path) -> bool:
    if any(part in SKIP_DIRS for part in path.parts):
        return False
    if path.suffix in TEXT_EXTENSIONS:
        return True
    return path.name in {".gitignore", ".env.example", ".secrets.example", "docker-compose.yml"}


def iter_files(root: Path, validation_dir: Path | None, include_working_tree: bool):
    roots = [validation_dir] if validation_dir else []
    if include_working_tree:
        roots.append(root)
    seen: set[Path] = set()
    for base in roots:
        if not base or not base.exists():
            continue
        for path in base.rglob("*"):
            if path.is_file() and is_text_candidate(path):
                resolved = path.resolve()
                if resolved not in seen:
                    seen.add(resolved)
                    yield path


def git_changed_and_untracked(root: Path):
    paths: set[Path] = set()
    commands = [
        ["git", "-C", str(root), "diff", "--name-only", "--diff-filter=ACMRT"],
        ["git", "-C", str(root), "ls-files", "--others", "--exclude-standard"],
    ]
    for command in commands:
        try:
            out = subprocess.check_output(command, text=True)
        except Exception:
            continue
        for line in out.splitlines():
            if line.strip():
                paths.add((root / line.strip()).resolve())
    return paths


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=".")
    parser.add_argument("--validation-dir", default="docs/validation")
    parser.add_argument("--include-working-tree", action="store_true")
    args = parser.parse_args()

    root = Path(args.repo_root).resolve()
    validation_dir = (root / args.validation_dir).resolve() if args.validation_dir else None
    allowed_files = git_changed_and_untracked(root) if args.include_working_tree else set()
    findings = []

    for path in iter_files(root, validation_dir, args.include_working_tree):
        if args.include_working_tree and allowed_files and path.resolve() not in allowed_files and not (validation_dir and validation_dir in path.resolve().parents):
            continue
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            continue
        for line_no, line in enumerate(text.splitlines(), 1):
            if any(a.search(line) for a in ALLOWLIST):
                continue
            for pattern in SECRET_PATTERNS:
                if pattern.search(line):
                    rel = path.resolve().relative_to(root)
                    findings.append(f"{rel}:{line_no}: possible secret material (line redacted)")
                    break

    print(f"finding_count={len(findings)}")
    for finding in findings:
        print(finding)
    return 1 if findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
