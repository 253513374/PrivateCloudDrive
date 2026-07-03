# 环境坑 / Environment Pitfalls

本文档收录 PrivateCloudDrive 开发与验收中已验证的环境陷阱及其复现条件、根因和规避方式。供开发者和验收者快速排查。

---

## 1. MAUI Debug APK — EmbedAssembliesIntoApk

| 项目 | 值 |
|---|---|
| **触发条件** | clean install Debug APK（卸载重装 / `adb shell pm clear`） |
| **现象** | App 启动后立即崩溃或 ANR；logcat 出现 `FATAL EXCEPTION`，assembly 缺失 |
| **根因** | MAUI/Android 默认 Debug 构建依赖 Fast Deployment（assemblies 留在开发主机） |
| **修复** | 构建时加入 `-p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None` |
| **验证** | `adb logcat -s AndroidRuntime:F *:S` 确认无崩溃 |

---

## 2. Android 14+ ADB `input text` 失效

| 项目 | 值 |
|---|---|
| **触发条件** | GMS（Google Play Services）版 AVD，Android 14 / 15 / 16 模拟器 |
| **现象** | `adb shell input text "xxx"` 执行后无任何字符输入到 App 表单 |
| **根因** | Android 默认输入法不再响应 `input text` 注入的非 IME 输入事件 |
| **规避** | (1) 使用非 GMS 镜像如 `system-images;android-36;default;x86_64`；(2) 安装 ADB Keyboard（`adb install ADBKeyboard.apk` → `adb shell ime set com.android.adbkeyboard/.AdbIME`）；(3) 绕过输入法，用 `/connect/token` 验证登录链路 |
| **替代方案** | `curl -X POST http://localhost:8080/connect/token -d "grant_type=password&username=...&password=..."` 验证登录链路正常后再进入 App 验收 |
| **影响** | 自动化测试脚本中依赖 `input text` 的步骤在 GMS 模拟器上无法工作 |

---

## 3. Docker Desktop — WSL2 磁盘镜像膨胀

| 项目 | 值 |
|---|---|
| **触发条件** | 频繁重建镜像（多轮 `docker compose up -d --build`）、大量构建缓存 |
| **现象** | Docker Desktop 虚拟磁盘（`ext4.vhdx`）持续增长至数十 GB，即使清理容器和镜像后也不自动收缩 |
| **根因** | WSL2 的 ext4 文件系统只增不减，Docker Desktop 不自动执行 `discard` / shrink |
| **解决** | 手动压缩：`wsl --shutdown` → `diskpart` → `select vdisk file="%LOCALAPPDATA%\\Docker\\wsl\\data\\ext4.vhdx"` → `compact vdisk` → `detach vdisk` |
| **预防** | 定期执行 `docker system prune -a --volumes` 清理未使用的镜像、容器、网络和卷；在 CI 环境使用一次性 Agent，避免镜像堆积 |

---

## 4. 模拟器 ScrollView 坐标偏移

| 项目 | 值 |
|---|---|
| **触发条件** | Android 模拟器中 ScrollView / RecyclerView 内元素通过 `adb shell input tap` 坐标点击 |
| **现象** | 点击坐标（如 y=800）实际触发元素偏移（如 y=900 位置的元素），导致误点击或点击无效 |
| **根因** | 模拟器窗口缩放（DPI / 窗口尺寸）造成坐标变换偏差；部分 AVD 配置中 `hw.lcd.density` 与窗口缩放比不一致 |
| **规避** | (1) 模拟器窗口保持 100% 缩放；(2) 使用 `uiautomator dump` 获取精确元素 bounds 后计算点击坐标；(3) 优先通过 App 内导航而非绝对坐标操作 |
| **影响** | 自动化验收脚本中依赖 `adb shell input tap x y` 的步骤需频繁调整坐标 |

---

## 5. PowerShell `ErrorActionPreference = "Stop"` + Docker Compose stderr

| 项目 | 值 |
|---|---|
| **触发条件** | PowerShell 脚本中 `$ErrorActionPreference = "Stop"` 且调用 `docker compose up -d --build` |
| **现象** | 脚本提前退出，误报错误 |
| **根因** | Docker Compose 将构建进度输出到 stderr，PowerShell 将其提升为 `NativeCommandError`，未检查 `$LASTEXITCODE` 即失败 |
| **修复** | 捕获外部命令时临时调整 `$ErrorActionPreference = "Continue"`，用退出码判断 P/FAIL |
| **参考** | `scripts/verify-local-stack.ps1` 的 `Invoke-External` 函数（2026-05-14 修复） |
| **测试** | `docker compose config --quiet` 不触发此问题（只检查配置合法性，不启动步骤） |

---

## 6. Docker Compose volume 名前缀污染

| 项目 | 值 |
|---|---|
| **触发条件** | 使用 `docker volume inspect {volume_name}` 且 volume_name 未带 Compose project 前缀 |
| **现象** | 备份脚本备份到空 volume；`storage.tar.gz` 仅几十字节 |
| **根因** | Docker Compose 自动为 volume 添加 `{project_name}_` 前缀（默认 project name 为包含 compose.yaml 的目录名） |
| **修复** | 从运行中容器 `/app/storage` 挂载点动态解析真实 Docker volume 名，而不是写死未加前缀的名称 |
| **参考** | `scripts/backup-local-stack.ps1`、`scripts/restore-local-stack.ps1`；初始发现记录在 `docs/progress.md` 2026-05-18 |
| **测试** | 在 Docker Compose 运行状态下执行 `docker inspect {api_container_id} --format '{{range .Mounts}}{{.Name}}:{{.Source}}{{end}}'` 确认 volume 名包含 project 前缀 |

---

## 7. `dotnet build` 被运行中进程锁定

| 项目 | 值 |
|---|---|
| **触发条件** | `dotnet build` 使用默认 OutDir，且 `PrivateCloudDrive.HttpApi.Host` 正在运行 |
| **现象** | 构建失败，提示文件被另一进程锁定 |
| **根因** | 默认输出目录 `bin/` 与运行中进程共享；进程退出前文件一直占用 |
| **解决** | 使用隔离输出目录：`dotnet build ... -p:OutDir={isolated_absolute_path}` |
| **验证** | 构建后检查输出文件存在于指定 `OutDir` 而非 `bin/` |

---

## 8. Android 模拟器 Fast Boot / Quick Boot 快照损坏

| 项目 | 值 |
|---|---|
| **触发条件** | 多次启动/关闭 AVD，模拟器使用 Quick Boot 恢复快照 |
| **现象** | App 安装后启动失败；logcat 无明确错误；`adb install -r` 正常但 App 不可见 |
| **根因** | Quick Boot 快照陈旧，系统状态与 Emulator 内核不一致 |
| **修复** | 使用 `-no-snapshot-load` 参数启动以绕过快照，获得干净的模拟器实例 |
| **命令** | `emulator -avd pixel_9_pro_-_api_36_0 -no-snapshot-load` |
| **预防** | 长期开发周期的模拟器在关键验收前应以 `-no-snapshot-load` 冷启动一次 |

---

## 9. GitHub Actions — Public repo quality gate 下载 actions/setup-dotnet 失败

| 项目 | 值 |
|---|---|
| **触发条件** | Public repository CI workflow 执行，尤其在 fork PR 或仓库权限变更后 |
| **现象** | `actions/setup-dotnet@v5` 步骤卡住或 403；workflow 整体失败 |
| **根因** | GitHub Actions 的 setup 缓存/授权问题；非文档内容错误 |
| **处理** | (1) 说明此失败不影响文档内容的合规性；(2) 手动执行本地构建验证作为替代证据；(3) 确认 actions/setup-dotnet 的基础设施恢复后 rerun |
| **影响** | Public repository 的 CI 绿灯不可作为唯一发布依据——须保留本地验证证据 |

---
