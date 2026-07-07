# FAQ / 常见问题

本文档收录 PrivateCloudDrive 开发与验收过程中反复出现的问题及其原因、解决方法和预防措施。面向开发者和验收者。

---

## 1. Android APK ANR / 启动崩溃

**现象**

clean install 后 App 闪退、黑屏、ANR 或 `FATAL EXCEPTION`/`AndroidRuntime` 崩溃，logcat 出现 assembly 缺失相关错误。

**原因**

默认 `dotnet build` Debug APK 启用 Fast Deployment，assemblies 留在开发主机而非嵌入 APK。clean install（卸载重装或 adb shell pm clear）后 App 找不到这些 assemblies 直接崩溃。

**解决（.NET 10）**

在 `PrivateCloudDrive.App.csproj` 的 `PropertyGroup` 中硬编码 `EmbedAssembliesIntoApk=true`：

```xml
<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
```

或使用构建脚本 `scripts/build-maui-apk.ps1`：

```bash
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj
  -f net10.0-android
  -c Debug
  -p:EmbedAssembliesIntoApk=true
```

> ⚠️ `AndroidFastDeploymentType=None` 在 .NET 10 中已弃用（XA1037），不再需要。

**验证**

安装后通过 `adb logcat -s AndroidRuntime:F *:S` 确认无崩溃；前台 Activity 应为 `crc644ff135ff239f5ce3.MainActivity`。

**预防**

- 验收/发布 APK 始终使用 `EmbedAssembliesIntoApk=true`。
- 开发调试时优先保持设备连接 VS/`dotnet watch`，避免反复 clean install。

---

## 2. ADB `input text` 在 Android 16 模拟器失效

**现象**

`adb shell input text "my_password"` 执行后无任何输入，App 登录表单字段保持空白。

**原因**

Android 14+ 默认输入法（尤其是模拟器 GMS 版 AVD）不再响应 `input text` 注入的非 IME 输入事件。`input keyevent` 系列操作亦部分受影响。

**临时解决**

1. 切换到非 GMS（Google Play Services）AVD 镜像，如 `system-images;android-36;default;x86_64`，某些模拟器版本仍支持 `input text`。
2. 如果已有 GMS AVD，安装 ADB Keyboard 替代输入法（`adb install -r ADBKeyboard.apk` → `adb shell ime set com.android.adbkeyboard/.AdbIME`）再重试。
3. 绕过输入法，直接通过 App 内置 REST 客户端或 `curl` 调用 `/connect/token` 颁发 token 后注入到 App 本地存储。

**验收注意事项**

验收证据不应依赖 `input text` 执行的登录截图证明功能正常——应优先使用 `/connect/token` 验证登录链路后再执行 App 内操作验收。

---

## 3. Docker Compose 项目名前缀导致备份到空 Volume

**现象**

`backup-local-stack.ps1` 生成的 `storage.tar.gz` 仅几十个字节，不包含真实文件负载。

**原因**

Docker Compose 在创建 volume 时自动添加 `{project_name}_` 前缀（默认 project name 为目录名），而脚本使用未加前缀的 volume 名解析 Docker volume path，导致备份源实际为空 volume。

**发现与修复**

详见 `docs/progress.md`（2026-05-18 条目）。初次演练发现 `storage.tar.gz` 仅 87 bytes；复核 API 容器挂载后确认真实 volume 为 Compose project 前缀版本，修正脚本从运行中 API 容器 `/app/storage` 挂载点动态解析真实 Docker volume 名，并将 `storage.dockerVolume` 写入 `manifest.json`。

**预防**

- 备份脚本必须从运行中容器挂载点反向解析 Docker volume 名，而非写死未加前缀的 volume 名。
- `docker compose ps --format json` 或 `docker inspect` 可作为动态发现容器挂载的备用手段。

---

## 4. 真机验收环境缺失

**现象**

验收过程中无法在真实 Android 设备上安装 APK 或执行触控验收；`adb devices` 返回空列表。

**原因**

- 开发机未连接 Android 设备或未启用 USB 调试。
- 模拟器未启动或 AVD 配置过期。
- CI 执行环境无可用 Android 硬件。

**解决**

1. 启动模拟器：`emulator -avd {AVD_NAME} -no-snapshot-load`（参考已知 AVD 名称：`pixel_9_pro_-_api_36_0`）。
2. 等待模拟器完全启动后执行 `adb wait-for-device`。
3. 安装 APK：`adb install -r artifacts/verify-*/com.companyname.privateclouddrive.app-Signed.apk`。

**验收替代方案**

- 真机不可用时，Android 模拟器（Pixel 9 Pro API 36）属于可接受的验证替代。
- 模拟器无法覆盖的场景（指纹、实际网络切换、微信登录授权）应记录为 Known Limitations 并跟踪。

---

## 5. Docker Compose 构建进度被误判为脚本错误

**现象**

调用 `docker compose up -d --build` 的 PowerShell 验证脚本（如 `verify-local-stack.ps1`）在 `$ErrorActionPreference = "Stop"` 模式下将 Compose 正常构建日志（写入 stderr）误识别为 `NativeCommandError`，提前中止验证流程。

**原因**

Docker Compose 将构建进度输出到 stderr。PowerShell 的 `$ErrorActionPreference = "Stop"` 会把 native stderr 行提升为错误，导致未检查 `$LASTEXITCODE` 即失败。

**解决**

在捕获外部命令输出时临时将 `$ErrorActionPreference` 调整为 `Continue`，用真实退出码决定 PASS/FAIL。

**参考**

`scripts/verify-local-stack.ps1` 中的 `Invoke-External` 函数已实现此修复（2026-05-14）。

---

## 6. 文档敏感内容泄露风险

**现象**

验收报告、日志、Issue 或截图意外包含 token、cookie、AppSecret、连接字符串、私有 URL、服务器绝对路径或真实文件内容。

**常见泄漏点**

| 泄漏点 | 风险等级 | 检查项 |
|---|---|---|
| logcat 输出 | 高 | `adb logcat` 输出复制到 md 文件前必须审查 |
| 截图 | 中 | 截图不应包含地址栏 token、密码明文、分享 URL |
| 配置文件片段 | 高 | `.env`、`appsettings.json` 中的密钥需替换为 `<redacted>` |
| 错误响应 JSON | 高 | API 返回的异常堆栈可能包含路径和 token |

**预防**

- 每次提交前执行 `python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD`。
- 证据文档使用 `<redacted>`、`PLACEHOLDER` 或 `${VAR_NAME}` 代替真实值。
- 默认禁止在证据中输出 `.env` 原文、access_token、refresh_token、`Set-Cookie` 完整值、完整分享 URL 或服务器内部路径。

---

## 7. 后端构建被运行中进程锁定

**现象**

`dotnet build` 失败，错误信息包含文件被另一进程锁定，指向 `bin/` 或 `obj/` 目录下的 `.dll`。

**原因**

默认 `OutDir` 与运行中的 `PrivateCloudDrive.HttpApi.Host` 进程共享，进程退出前文件仍在占用。

**解决**

使用隔离输出目录构建：

```
dotnet build aspnet-core/PrivateCloudDrive.slnx -p:OutDir=D:/path/to/artifacts/verify-build
```

确认无误后再提交或部署。

---
