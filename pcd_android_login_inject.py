#!/usr/bin/env python3
"""
pcd_android_login_inject.py — PrivateCloudDrive Android 登录 Token 预处理与 ADB 注入脚本

工作流程：
1. 检查 ADB 连接（检测已连接的设备/模拟器）
2. 检查后端健康（验证 /connect/token 可达）
3. 通过 OpenIddict password grant 获取 access/refresh token
4. 停止 App 并清空数据（可选 --skip-clear）
5. 启动 App（使 BroadcastReceiver 注册）
6. 通过 ADB broadcast 注入 token JSON 到 App
7. 停止并重启 App（使 StartupPage 消费 token 并迁移到 SecureStorage）
8. 验证 App 已跳过登录页、进入登录后页面（文件页）

使用方式：
  python pcd_android_login_inject.py
  python pcd_android_login_inject.py --backend http://localhost:8081
  # python pcd_android_login_inject.py --username admin --password <redacted>
  python pcd_android_login_inject.py --emulator-udid emulator-5554
  python pcd_android_login_inject.py --skip-clear          # 保留已有数据
  python pcd_android_login_inject.py --skip-health-check

依赖:
  - Python 3.8+（标准库，无第三方依赖）
  - ADB (Android Debug Bridge) 在 PATH 中
  - 模拟器或设备已连接

安全约束:
  - 日志/文档不会输出完整 token（仅显示前20字符和长度）
  - 该脚本仅供 Debug/TestAutomation 构建使用
  - Release 构建不包含 BroadcastReceiver

V1.1 — Hermes Test Automation Engineer
"""
import json
import os
import re
import subprocess
import sys
import time
import urllib.request
import urllib.parse
import urllib.error

# ─── 默认配置 ────────────────────────────────────────────────────────────────
DEFAULT_BACKEND = "http://localhost:8081"
DEFAULT_USERNAME = "admin"
DEFAULT_PASSWORD = os.environ.get("PCD_QA_PASSWORD", "<redacted>")
PACKAGE_NAME = "com.companyname.privateclouddrive.app"
INJECT_ACTION = "com.companyname.privateclouddrive.app.action.INJECT_TOKEN"
EMULATOR_UDID = "emulator-5554"

# ─── 安全日志 ────────────────────────────────────────────────────────────────
def safe_token_summary(token: str | None) -> str:
    """仅展示 token 长度和前20字符，避免完整泄露。"""
    if not token:
        return "NONE"
    return f"YES ({len(token)} chars) ...{token[:20]}..."


def safe_token_mask(token: str | None) -> str:
    """用于日志输出 token 时用 [MASKED] 代替完整值。"""
    if not token:
        return "[NONE]"
    if len(token) <= 12:
        return "[TOO_SHORT]"
    return f"{token[:6]}...{token[-4:]}"


def log_step(step: str, status: str, detail: str = "") -> None:
    """统一日志格式。"""
    icon = {"PASS": "✓", "FAIL": "✗", "WARN": "!", "INFO": "→"}.get(status, "?")
    line = f"  [{icon}] {step:<40} {detail}"
    print(line)


def log_header(title: str) -> None:
    print("")
    print("=" * 65)
    print(f"  {title}")
    print("=" * 65)


# ─── ADB 辅助 ────────────────────────────────────────────────────────────────
def adb(args: list[str], timeout: int = 30, udid: str | None = None) -> subprocess.CompletedProcess:
    """执行 ADB 命令，自动添加 -s UDID。"""
    cmd = ["adb"]
    if udid:
        cmd += ["-s", udid]
    cmd += args
    try:
        return subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout,
        )
    except FileNotFoundError:
        print("[FAIL] ADB 未找到。请确保 adb 在 PATH 中（Android SDK platform-tools）。")
        sys.exit(1)
    except subprocess.TimeoutExpired:
        print(f"[FAIL] ADB 命令超时: {' '.join(cmd)}")
        sys.exit(1)


def check_adb() -> str | None:
    """检查 ADB 可用性和模拟器连接。返回第一个已连接设备/模拟器的 UDID 或 None。"""
    result = adb(["devices"], udid=None)
    if result.returncode != 0:
        print(f"[FAIL] adb devices 失败: {result.stderr.strip()}")
        return None

    for line in result.stdout.splitlines():
        parts = line.strip().split("\t")
        if len(parts) == 2 and parts[1] == "device":
            return parts[0]
    return None


def adb_shell(cmd: str, udid: str, timeout: int = 30) -> subprocess.CompletedProcess:
    """简化 ADB shell 命令调用。"""
    return adb(["shell", cmd], udid=udid, timeout=timeout)


# ─── 后端健康检查 ────────────────────────────────────────────────────────────
def check_backend_health(backend_url: str) -> bool:
    """检查后端是否可达：尝试 GET /connect/token（预期返回 405 或 400 而非连接拒绝）。"""
    url = f"{backend_url.rstrip('/')}/connect/token"
    log_step("后端健康检查", "INFO", f"GET {url}")
    try:
        req = urllib.request.Request(url, method="GET")
        with urllib.request.urlopen(req, timeout=10) as resp:
            # OpenIddict 预期返回 405 Method Not Allowed
            if resp.status in (405, 400):
                log_step("后端健康检查", "PASS", f"HTTP {resp.status} (预期 OIDC 端点响应)")
                return True
            log_step("后端健康检查", "WARN", f"HTTP {resp.status} (可能不是预期 OIDC 端点)")
            return True
    except urllib.error.HTTPError as e:
        if e.code in (405, 400):
            log_step("后端健康检查", "PASS", f"HTTP {e.code} (OIDC 端点可达)")
            return True
        log_step("后端健康检查", "WARN", f"HTTP {e.code}: {e.reason}")
        return True
    except Exception as e:
        log_step("后端健康检查", "FAIL", f"后端不可达: {e}")
        return False


# ─── Token 获取 ──────────────────────────────────────────────────────────────
def get_token(
    backend_url: str,
    username: str,
    password: str,
) -> tuple[str | None, str | None, int | None]:
    """通过 OpenIddict password grant 获取 token。"""
    url = f"{backend_url.rstrip('/')}/connect/token"
    data = urllib.parse.urlencode({
        "grant_type": "password",
        "username": username,
        "password": password,
        "client_id": "PrivateCloudDrive_App",
        "scope": "openid profile email roles offline_access PrivateCloudDrive",
    }).encode("utf-8")

    log_step("获取 Token", "INFO", f"POST {url}")
    req = urllib.request.Request(
        url,
        data=data,
        headers={"Content-Type": "application/x-www-form-urlencoded"},
    )

    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            body = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        error_body = e.read().decode("utf-8", errors="replace")
        log_step("获取 Token", "FAIL", f"HTTP {e.code}: {error_body[:200]}")
        return None, None, None
    except Exception as e:
        log_step("获取 Token", "FAIL", str(e))
        return None, None, None

    access_token = body.get("access_token")
    refresh_token = body.get("refresh_token")
    expires_in = body.get("expires_in", 3600)

    if not access_token:
        log_step("获取 Token", "FAIL", "响应中没有 access_token")
        return None, None, None

    log_step("获取 Token", "PASS", safe_token_summary(access_token))
    log_step("Token 有效期", "INFO", f"{expires_in} 秒 ({expires_in // 60} 分钟)")
    if refresh_token:
        log_step("Refresh Token", "INFO", safe_token_summary(refresh_token))

    return access_token, refresh_token, expires_in


# ─── Token JSON 构建 ─────────────────────────────────────────────────────────
def build_token_json(access_token: str, refresh_token: str | None, expires_in: int) -> str:
    """构建与 OpenIddictAuthService.StoredTokenSet 兼容的 JSON。

    时间戳以 ISO 8601 格式输出（兼容 C# DateTimeOffset 序列化）。
    """
    from datetime import datetime, timezone
    expires_iso = datetime.fromtimestamp(time.time() + expires_in, tz=timezone.utc).isoformat()

    token_set = {
        "AccessToken": access_token,
        "RefreshToken": refresh_token or "",
        "TokenType": "Bearer",
        "ExpiresAt": expires_iso,
    }
    return json.dumps(token_set, ensure_ascii=False)


# ─── ADB 操作 ────────────────────────────────────────────────────────────────
def adb_clear_app_data(udid: str) -> bool:
    """清空 App 数据（pm clear）。"""
    result = adb_shell(f"pm clear {PACKAGE_NAME}", udid=udid)
    if result.returncode != 0:
        log_step("清空 App 数据", "FAIL", result.stderr.strip())
        return False
    if "Success" in result.stdout:
        log_step("清空 App 数据", "PASS", "")
        return True
    log_step("清空 App 数据", "WARN", f"输出: {result.stdout.strip()[:100]}")
    return True


def adb_force_stop(udid: str) -> bool:
    """强制停止 App。"""
    result = adb_shell(f"am force-stop {PACKAGE_NAME}", udid=udid)
    if result.returncode != 0:
        log_step("停止 App", "WARN", result.stderr.strip())
        return False
    log_step("停止 App", "PASS", "")
    return True


def detect_main_activity(udid: str) -> str | None:
    """从已安装的包中检测 MAUI MainActivity 类名（CRC 哈希可能变化）。"""
    result = adb_shell(f"dumpsys package {PACKAGE_NAME}", udid=udid, timeout=15)
    if result.returncode != 0:
        return None
    for line in result.stdout.splitlines():
        m = re.search(rf'{re.escape(PACKAGE_NAME)}/([\w.]+MainActivity)', line)
        if m:
            return m.group(1)
    return None


def adb_launch_app(udid: str) -> bool:
    """启动 App 主 Activity。自动检测 MainActivity 类名。"""
    activity = detect_main_activity(udid)
    if not activity:
        log_step("启动 App", "FAIL", "无法从设备检测 MainActivity 类名")
        return False

    result = adb([
        "shell",
        "am", "start",
        "-n", f"{PACKAGE_NAME}/{activity}",
        "-a", "android.intent.action.MAIN",
        "-c", "android.intent.category.LAUNCHER",
    ], udid=udid)

    if result.returncode != 0:
        log_step("启动 App", "FAIL", result.stderr.strip())
        return False
    log_step("启动 App", "PASS", f"Intent 已发送: {activity}")
    return True


def adb_inject_token(udid: str, token_json: str) -> bool:
    """通过 ADB broadcast 向 App 注入 token。

    先将 JSON 写入设备临时文件，再通过 shell 变量读取传递，
    避免 JSON 中的空格/引号在 --es 参数中被 shell 展开破坏。
    """
    # 1. 写入设备临时文件
    escaped = token_json.replace("'", "'\\''")
    write_cmd = f"echo '{escaped}' > /data/local/tmp/pcd_token_inject.json"
    result = adb_shell(write_cmd, udid=udid, timeout=10)
    if result.returncode != 0:
        log_step("ADB 注入 Token", "FAIL", f"写入临时文件失败: {result.stderr.strip()}")
        return False

    # 2. 通过 shell 变量读取文件内容并广播（"$TOKEN" 保留 JSON 中的空格和引号）
    result = adb([
        "shell",
        f"TOKEN=$(cat /data/local/tmp/pcd_token_inject.json) && "
        f"am broadcast -a {INJECT_ACTION} -f 0x01000000 "
        f'--es token_json "$TOKEN"',
    ], udid=udid, timeout=15)

    if result.returncode != 0:
        log_step("ADB 注入 Token", "FAIL", result.stderr.strip())
        return False

    # 解析 broadcast 结果确认
    if "Broadcast completed: result=" in result.stdout:
        log_step("ADB 注入 Token", "PASS", "Broadcast 已完成")
    else:
        log_step("ADB 注入 Token", "PASS", "Broadcast 已发送")
        # 记录广播接收情况供调试
        log_step("Broadcast 输出", "INFO", result.stdout.strip()[:200])

    # 3. 清理设备临时文件（静默）
    adb_shell("rm -f /data/local/tmp/pcd_token_inject.json", udid=udid, timeout=5)
    return True


# ─── 验证 ────────────────────────────────────────────────────────────────────
def verify_app_signed_in(udid: str, max_wait: int = 15) -> bool:
    """
    验证 App 是否已登录：检查当前顶层 Activity 是否为文件页或主 TabBar 页面。
    登录成功 → 导航到 //files → Activity 为 FilesPage 或 Shell 主页面。
    未登录 → 保持在 LoginPage。

    最多轮询 max_wait 秒。
    """
    log_step("验证登录状态", "INFO", f"等待 {max_wait} 秒检测 Activity...")

    login_patterns = [
        "login", "LoginPage", "signin", "SignIn",
    ]
    signed_in_patterns = [
        "files", "FilesPage", "main", "TabBar", "Shell",
    ]

    for i in range(max_wait):
        time.sleep(1)

        # 方法1: 通过 dumpsys window 获取当前焦点 Activity
        result = adb_shell("dumpsys window windows", udid=udid, timeout=10)
        if result.returncode != 0:
            continue

        output = result.stdout

        # 尝试 mFocusedApp
        focus_match = re.search(r'mFocusedApp=.*?([\w.]+/[\w.]+)', output)
        current_activity = focus_match.group(1) if focus_match else ""

        # 回退：mCurrentFocus
        if not current_activity:
            for line in output.splitlines():
                if "mCurrentFocus" in line:
                    current_activity = line.strip()
                    break

        # 判断当前 Activity 属于哪个页面
        lower = current_activity.lower()
        if any(p in lower for p in login_patterns):
            log_step("验证登录状态", "WARN", f"仍在登录页: {current_activity}")
            return False
        elif any(p in lower for p in signed_in_patterns):
            log_step("验证登录状态", "PASS", f"已进入登录后页面: {current_activity}")
            return True

        # 最后几秒尝试 uiautomator 检查登录按钮
        if i >= max_wait - 3:
            ui_result = adb_shell("uiautomator dump /dev/tty", udid=udid, timeout=10)
            if ui_result.returncode == 0 and ui_result.stdout:
                if "登录" in ui_result.stdout or "sign in" in ui_result.stdout.lower():
                    log_step("验证登录状态", "WARN", "UI 树包含登录按钮，仍停留在登录页")
                    return False

        if i == max_wait - 1:
            log_step("验证登录状态", "FAIL", f"超时({max_wait}s)，无法确定 Activity")
            activity = detect_main_activity(udid)
            if activity:
                log_step("提示", "INFO", f"检测到的 Activity: {activity}")
            return False

    return False


def verify_token_consumed(udid: str) -> bool:
    """
    回退验证：检查 test_automation_prefs 中 injected_token_json 是否已被消费。

    若已被 StartupPage 迁移到 SecureStorage，则证明注入成功。
    适用于无头模拟器（屏幕关闭）环境或 UI 验证失败的回退。
    """
    prefs_path = f"/data/data/{PACKAGE_NAME}/shared_prefs/test_automation_prefs.xml"
    result = adb_shell(f"cat {prefs_path}", udid=udid, timeout=10)
    if result.returncode != 0 or not result.stdout.strip():
        log_step("验证 Token 消费", "WARN", "无法读取 SharedPreferences（可能文件不存在）")
        return False

    # 如果文件没有 injected_token_json key，说明已被消费
    if "injected_token_json" in result.stdout:
        log_step("验证 Token 消费", "FAIL", "injected_token_json 仍存在，未被消费")
        return False

    # 检查是否有 injected_at_ms（说明 Receiver 曾触发过）
    if "injected_at_ms" in result.stdout:
        log_step("验证 Token 消费", "PASS", "Token 已被 StartupPage 迁移到 SecureStorage")
        return True

    log_step("验证 Token 消费", "WARN", "SharedPreferences 中无 injected_at_ms，Receiver 可能未触发")
    return False


# ─── 主流程 ───────────────────────────────────────────────────────────────────
def main():
    import argparse

    parser = argparse.ArgumentParser(
        description="PrivateCloudDrive Android 登录 Token 预处理与 ADB 注入",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=r"""
示例:
  %(prog)s
  %(prog)s --backend http://192.168.1.100:8080 --skip-clear
  %(prog)s --username qa_user --password test123 --emulator-udid emulator-5556

失败回退:
  若注入失败或页面验证失败：
    1. adb -s <UDID> shell pm clear com.companyname.privateclouddrive.app && adb shell am force-stop com.companyname.privateclouddrive.app
    2. 重试本脚本（使用 --skip-clear 避免二次清除）
    3. 或手动登录 App
        """,
    )
    parser.add_argument("--backend", default=DEFAULT_BACKEND, help=f"后端地址 (默认: {DEFAULT_BACKEND})")
    parser.add_argument("--username", default=DEFAULT_USERNAME, help=f"登录用户名 (默认: {DEFAULT_USERNAME})")
    parser.add_argument("--password", default=DEFAULT_PASSWORD, help=f"登录密码 (默认: {DEFAULT_PASSWORD})")
    parser.add_argument("--emulator-udid", default=None, help=f"模拟器 UDID (默认: 自动检测)")
    parser.add_argument("--skip-clear", action="store_true", help="跳过清空 App 数据")
    parser.add_argument("--skip-health-check", action="store_true", help="跳过后端健康检查")
    parser.add_argument("--verify-timeout", type=int, default=15, help="验证超时秒数 (默认: 15)")

    args = parser.parse_args()

    # ── Step 1: 检查 ADB ─────────────────────────────────────────────────────
    log_header("步骤 1/8: 检查 ADB 与模拟器连接")
    detected_udid = check_adb()
    udid = args.emulator_udid or detected_udid
    if not udid:
        log_step("ADB 连接", "FAIL", "未检测到已连接的设备/模拟器。请连接模拟器后重试。")
        sys.exit(1)
    log_step("ADB 连接", "PASS", f"已连接: {udid}")

    # ── Step 2: 后端健康检查 ─────────────────────────────────────────────────
    log_header("步骤 2/8: 后端健康检查")
    if args.skip_health_check:
        log_step("后端健康检查", "INFO", "已跳过 (--skip-health-check)")
    elif not check_backend_health(args.backend):
        log_step("后端健康检查", "FAIL", f"后端 {args.backend} 不可达，请先启动后端。")
        sys.exit(1)
    else:
        pass  # 日志已在函数中输出

    # ── Step 3: 获取 Token ──────────────────────────────────────────────────
    log_header("步骤 3/8: 获取登录 Token")
    access_token, refresh_token, expires_in = get_token(args.backend, args.username, args.password)
    if not access_token:
        log_step("获取 Token", "FAIL", "获取失败。请检查用户名/密码及后端状态。")
        sys.exit(1)
    token_json = build_token_json(access_token, refresh_token, expires_in)
    log_step("Token JSON", "INFO", f"{len(token_json)} 字符")

    # ── Step 4: 准备 App（停止 + 清数据）────────────────────────────────────
    log_header("步骤 4/8: 准备 App")
    adb_force_stop(udid)
    time.sleep(1)
    if args.skip_clear:
        log_step("清空 App 数据", "INFO", "已跳过 (--skip-clear)")
    else:
        if not adb_clear_app_data(udid):
            log_step("清空 App 数据", "FAIL", "无法清空 App 数据，尝试继续...")
    time.sleep(1)

    # ── Step 5: 首次启动 App（使 BroadcastReceiver 注册）────────────────────
    log_header("步骤 5/8: 启动 App（注册 BroadcastReceiver）")
    if not adb_launch_app(udid):
        log_step("启动 App", "FAIL", "启动失败")
        sys.exit(1)
    log_step("等待初始化", "INFO", "等待 4 秒使 BroadcastReceiver 完成注册...")
    time.sleep(4)

    # ── Step 6: ADB 注入 Token ─────────────────────────────────────────────
    log_header("步骤 6/8: 注入 Token")
    if not adb_inject_token(udid, token_json):
        log_step("ADB 注入 Token", "FAIL", "注入失败（尝试继续）")

    log_step("等待处理", "INFO", "等待 2 秒使 BroadcastReceiver 处理完 token...")
    time.sleep(2)

    # ── Step 6b: 重启 App（使 StartupPage 消费新注入的 Token）───────────────
    log_header("步骤 7/8: 重启 App 以消费 Token")
    log_step("重启 App", "INFO", "停止并重新启动使 StartupPage 消费 Token...")
    adb_force_stop(udid)
    time.sleep(1)
    if not adb_launch_app(udid):
        log_step("启动 App", "FAIL", "启动失败")
        sys.exit(1)

    # ── Step 7: 验证登录状态 ────────────────────────────────────────────────
    log_header("步骤 8/8: 验证登录状态")
    is_signed_in = verify_app_signed_in(udid, max_wait=args.verify_timeout)

    # 若 UI 无法检测（无头模式等），回退检查 Token 是否已被消费
    if not is_signed_in:
        log_step("UI 登录验证", "INFO", "尝试回退方案：验证 Token 消费状态...")
        is_signed_in = verify_token_consumed(udid)

    # ── 输出最终结果 ──────────────────────────────────────────────────────────
    print("")
    print("=" * 65)
    if is_signed_in:
        print("  ✓  全部完成：Token 注入成功，App 已跳过登录页！")
        print("=" * 65)
        sys.exit(0)
    else:
        print("  ✗  注入完成但未能验证登录状态。")
        print("")
        print("  排查建议：")
        print(f"    1. 检查 logcat 是否有 BroadcastReceiver 日志：")
        print(f"       adb -s {udid} logcat -s TestAutomation *:I")
        print(f"    2. 确认 MAUI App 已使用 Debug 构建安装：")
        print(f"       cd maui/PrivateCloudDrive.App")
        print(f"       dotnet build -f net10.0-android -c Debug")
        print(f"    3. 手动重试注入：")
        detected_activity = detect_main_activity(udid) or "crcXXXXXX.MainActivity"
        print(f"       adb -s {udid} shell am broadcast -a {INJECT_ACTION} --es token_json '<token>'")
        print(f"       adb -s {udid} shell am start -n {PACKAGE_NAME}/{detected_activity}")
        print("    4. 若问题持续，尝试重新构建并安装 MAUI Debug APK。")
        print(f"       adb -s {udid} uninstall {PACKAGE_NAME}")
        print(f"       dotnet build -f net10.0-android -c Debug")
        print("=" * 65)
        sys.exit(1)


if __name__ == "__main__":
    main()
