# t_ea50cac5 Android 可见隐私脱敏修复验证

## 结论
PASS_WITH_WARN。Settings、StorageUsage、Uploads、Login 的用户可见错误与服务器状态已改为安全摘要/脱敏展示；原始异常仅写入本机 Debug 诊断或备份错误本机日志，不进入 UI/上传队列失败文案。当前隔离工作区已重新完成 Android APK 构建；截图/logcat 证据沿用同一代码补丁在可构建 clean clone 的安装验证产物，并已复制到本工作区。

## 修复范围
- 新增 `UserVisibleErrorSanitizer`，统一提供 Settings/Storage/SystemHealth/Backup/SignIn 安全文案、URL/Token/路径脱敏和服务器状态占位文案。
- Settings：后端地址、会话读取、容量读取、system-health、微信/Google/GitHub 绑定/解绑异常不再显示 `exception.Message`；服务器状态不再展示完整 URL。
- StorageUsage：容量/system-health 异常、diagnostics、存储位置、恢复边界、隐私边界通过脱敏后展示。
- Uploads/Backup 队列：上传失败默认分支不再回退显示 raw exception.Message。
- Login：当前连接和输入框 placeholder 不展示默认 private URL；登录/第三方登录异常改为安全摘要。

## 验证记录
- Android APK 构建：PASS_WITH_WARN
  - 命令：`dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -r android-arm64 -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None -m:1 -v:minimal`
  - 当前隔离工作区结果：0 errors，17 warnings（既有 AndroidX NU1608 / XA1037 警告）。
- 静态扫描：PASS
  - `SettingsPage.xaml.cs`、`StorageUsagePage.xaml.cs`、`BackupTransferService.cs`、`LoginPage.xaml.cs` 中目标 `exception.Message` 扫描为 0。
  - Views 中默认 private URL 可见扫描为 0。
- Android 安装/启动截图：PASS
  - `docs/validation/artifacts/pcd-mobile-sanitization-login-android.png`
  - 登录页显示“默认私有服务器 · 完整地址已隐藏”，未见完整 private URL、token、cookie、路径、bucket/object key 或 raw exception。
- logcat 简单敏感词扫描：PASS_WITH_NOISE
  - `docs/validation/artifacts/pcd-mobile-sanitization-logcat-raw.txt`
  - 命中主要为 Android 系统 `/apex`、`/data/app`、包路径和 GMS `token` 字样；未见 App private URL、业务 token/cookie、AccessKey、连接串、bucket/object key。

## 遗留风险
- 当前运行环境 `adb devices` 未发现已连接 emulator/device，因此本轮无法重新安装并重截当前隔离工作区 APK；安装/启动截图与 logcat 证据来自同一代码补丁在 `D:/Devs/Projects/Personal/PrivateCloudDrive_clean_clone_20260522_144026` 的既有验证产物，已复制到本工作区。
- 本次只覆盖 QA 指定 Android Settings/StorageUsage/Uploads/Login 可见脱敏路径；全 App 其他页面历史 raw exception 展示建议另立全局 hardening 任务。
