#!/usr/bin/env python3
"""Redacted smoke probe for a restored PrivateCloudDrive Compose stack.

The script validates the post-restore trust path without printing passwords,
access tokens, refresh tokens, cookies, or full public share URLs.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
import tempfile
from pathlib import Path

import requests


def fail(message: str) -> None:
    print(f"[FAIL] {message}")
    raise SystemExit(1)


def ok(message: str) -> None:
    print(f"[PASS] {message}")


def warn(message: str) -> None:
    print(f"[WARN] {message}")


def request_json(session: requests.Session, method: str, url: str, **kwargs):
    response = session.request(method, url, timeout=30, **kwargs)
    if response.status_code >= 400:
        fail(f"{method} {url} returned HTTP {response.status_code}: {response.text[:300]}")
    if not response.content:
        return None
    return response.json()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", default=os.environ.get("PCD_BASE_URL", "http://localhost:8080"))
    parser.add_argument("--username", default=os.environ.get("PCD_SMOKE_USERNAME", "admin"))
    parser.add_argument("--password", default=os.environ.get("PCD_SMOKE_PASSWORD"))
    parser.add_argument("--password-file", default=os.environ.get("PCD_SMOKE_PASSWORD_FILE"))
    parser.add_argument("--sample-name", default=os.environ.get("PCD_SMOKE_SAMPLE_NAME", "dr-restored-smoke.txt"))
    args = parser.parse_args()

    password = args.password
    if not password and args.password_file:
        password = Path(args.password_file).read_text(encoding="utf-8").strip()
    if not password:
        fail("Set PCD_SMOKE_PASSWORD or --password-file. The password is never printed.")

    base_url = args.base_url.rstrip("/")
    session = requests.Session()

    token_response = session.post(
        f"{base_url}/connect/token",
        data={
            "grant_type": "password",
            "client_id": "PrivateCloudDrive_App",
            "username": args.username,
            "password": password,
            "scope": "offline_access PrivateCloudDrive",
        },
        timeout=30,
    )
    if token_response.status_code != 200:
        fail(f"login returned HTTP {token_response.status_code}: {token_response.text[:300]}")
    token_payload = token_response.json()
    access_token = token_payload.get("access_token")
    if not access_token:
        fail("login response did not include an access token")
    session.headers.update({"Authorization": f"Bearer {access_token}"})
    ok("login succeeded; token value redacted")

    root = request_json(session, "GET", f"{base_url}/api/app/file-center-folders", params={"SkipCount": 0, "MaxResultCount": 50})
    root_items = root.get("items", []) if isinstance(root, dict) else []
    ok(f"file list loaded; root items={len(root_items)}")

    sample_text = "PrivateCloudDrive DR restored-stack smoke file\n"
    sample_hash = hashlib.sha256(sample_text.encode("utf-8")).hexdigest()
    files = {"file": (args.sample_name, sample_text.encode("utf-8"), "text/plain")}
    upload_response = session.post(f"{base_url}/api/file-center/files/upload-small", files=files, timeout=30)
    if upload_response.status_code >= 400:
        fail(f"upload returned HTTP {upload_response.status_code}: {upload_response.text[:300]}")
    node = upload_response.json()
    node_id = node.get("id")
    if not node_id:
        fail("upload response did not include file node id")
    ok(f"uploaded smoke file; node id suffix={node_id[-8:]}")

    download_response = session.get(f"{base_url}/api/file-center/files/{node_id}/download", headers={"Range": "bytes=0-7"}, timeout=30)
    if download_response.status_code not in (200, 206):
        fail(f"download/range returned HTTP {download_response.status_code}: {download_response.text[:300]}")
    ok(f"download/range succeeded; HTTP {download_response.status_code}; bytes={len(download_response.content)}")

    content_response = session.get(f"{base_url}/api/file-center/files/{node_id}/content", timeout=30)
    if content_response.status_code != 200:
        fail(f"content preview returned HTTP {content_response.status_code}: {content_response.text[:300]}")
    restored_hash = hashlib.sha256(content_response.content).hexdigest()
    if restored_hash != sample_hash:
        fail("downloaded content hash does not match uploaded smoke file")
    ok(f"content preview hash matched; sha256={restored_hash[:12]}…")

    share = request_json(session, "POST", f"{base_url}/api/file-center/shares", json={"fileNodeId": node_id, "allowDownload": True})
    token = share.get("token") if isinstance(share, dict) else None
    if not token:
        fail("share create response did not include token")
    ok(f"share created; token suffix={token[-6:]}")

    public = request_json(requests.Session(), "GET", f"{base_url}/api/public/shares/{token}")
    if not isinstance(public, dict) or not public.get("fileName"):
        fail("public share response did not include file name")
    ok("public share opened; full token redacted")

    delete_response = session.delete(f"{base_url}/api/file-center/files/{node_id}", timeout=30)
    if delete_response.status_code not in (200, 204):
        fail(f"delete-to-trash returned HTTP {delete_response.status_code}: {delete_response.text[:300]}")
    ok("delete-to-trash succeeded")

    trash = request_json(session, "GET", f"{base_url}/api/file-center/trash", params={"SkipCount": 0, "MaxResultCount": 20})
    trash_items = trash.get("items", []) if isinstance(trash, dict) else []
    if not any(item.get("id") == node_id for item in trash_items):
        fail("trash list did not include deleted smoke file")
    ok("trash list contains deleted smoke file")

    restored = request_json(session, "POST", f"{base_url}/api/file-center/nodes/{node_id}/restore")
    if not isinstance(restored, dict) or restored.get("id") != node_id:
        fail("restore response did not include restored node")
    ok("trash restore succeeded")

    op_logs = session.get(f"{base_url}/api/operation-logs", params={"SkipCount": 0, "MaxResultCount": 10}, timeout=30)
    if op_logs.status_code == 200:
        body = op_logs.text.lower()
        forbidden = ["access_token", "refresh_token", password.lower()]
        if any(secret and secret in body for secret in forbidden):
            fail("operation log response exposed a token or password")
        ok("operation/audit sample loaded without token or password leakage")
    else:
        warn(f"operation log sample skipped: HTTP {op_logs.status_code}")

    # Cleanup test share and file so the restored test stack remains tidy.
    share_id = share.get("id") if isinstance(share, dict) else None
    if share_id:
        session.delete(f"{base_url}/api/file-center/shares/{share_id}", timeout=30)
    session.delete(f"{base_url}/api/file-center/files/{node_id}", timeout=30)
    session.delete(f"{base_url}/api/file-center/nodes/{node_id}/permanent", timeout=30)
    ok("smoke artifacts cleaned up")

    print("Summary: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
