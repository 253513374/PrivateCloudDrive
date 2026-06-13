# Android 备份闭环发布证据包（D7 Release Gate 输入）

适用对象：芮发布（Release Manager / release-manager）、QA 复核、最终验收说明。

结论：PASS with WARN。当前 `docs/validation/` 已具备一份可公开引用的 Android 模拟器端到端统一证据包，覆盖备份入口、照片/相册入口可见、本机文件备份完成、批量/队列状态、失败重试、容量/健康、恢复说明和隐私边界。下载/预览、删除/恢复由 Android 入口可见性和后端恢复烟测共同补强。证据包可作为 D7 发布闸门输入；真机相册权限、后台长任务、弱网与 OEM 差异仍列为发布后增强或真机补证项，不得宣传为已完成真机全量验收。

## 1. Release Manager 可直接引用摘要

| 闸门项 | 当前结论 | 可引用证据 | 发布说明建议 |
|---|---|---|---|
| Android 构建 | PASS，Debug APK 构建成功；存在 AndroidX 版本约束 warning 和 Fast Deployment 弃用 warning，未阻断构建 | `docs/validation/maui-android-build-2026-05-18.log` | 可写为“Android Debug 构建通过，已记录非阻断依赖 warning”。 |
| Android 启动/登录/存储信任边界 | PASS，已用模拟器截图和裁剪 logcat 摘要证明启动、登录、存储页、我的页可见 | `docs/validation/android-logcat-storage-trust-boundary-2026-05-18.log`；`docs/validation/storage-trust-boundary-*.png` | 可写为“模拟器完成启动、登录与存储信任边界可见性验收”。 |
| 备份入口与来源选择 | PASS，底部导航和备份弹层可见，支持备份照片、从相册选择备份、备份本机文件等入口 | `docs/validation/screenshots/private-backup-slice4-start-backup-page.png`；`private-backup-slice2-backup-queue.png` | 可写为“Android 端已提供手机照片与本机文件备份入口”。 |
| 照片/视频媒体备份 | WARN，照片/相册选择入口可见；当前公开证据未单独证明大体积视频文件真机备份 | `docs/validation/screenshots/private-backup-slice4-start-backup-page.png`；第 5 节真机待增强 | 发布说明应避免宣称“大视频真机已验收”，可写为“媒体备份入口已就绪，视频大文件真机验收待增强”。 |
| 真实文件备份与上传结果 | PASS，使用公开小文件完成上传结果可见性验证 | `docs/validation/screenshots/private-backup-slice4-upload-result.png`；`docs/validation/pcd-real-backup-slice4.txt` | 可写为“文件备份路径已在模拟器完成可见验收”。 |
| 备份队列与批量状态 | PASS，队列页展示等待、上传、失败和完成状态汇总 | `docs/validation/screenshots/private-backup-slice5-server-down-failed-queue.png`；`private-backup-slice2-backup-queue.png` | 可写为“备份队列状态可见，失败任务保留并可重试”。 |
| 失败重试 | PASS，服务不可用时出现失败提示；服务恢复后可触发重试并看到成功结果 | `docs/validation/screenshots/private-backup-slice5-server-down-failed-queue.png`；`private-backup-slice5-retry-success.png`；`docs/validation/pcd-retry-slice5.txt` | 可写为“失败任务不会丢失，用户可在网络/服务恢复后手动重试”。 |
| 下载/预览 | PASS with WARN，Android 系统下载列表可见；后端恢复烟测覆盖 Range 下载和内容预览读取；MAUI 内完整下载/预览页仍建议补证 | `docs/validation/screenshots/private-backup-slice4-downloads-list.png`；`docs/validation/backup-restore-destructive-test-stack-20260521-215020.md` | 可写为“下载列表可见，恢复烟测已覆盖下载/预览控制路径；MAUI 内完整预览继续补证”。 |
| 删除/恢复 | PASS with WARN，后端灾备烟测覆盖删除到回收站、回收站列表和恢复；Android 设置页存在回收站入口；MAUI 内完整删除/恢复截图仍建议补证 | `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md`；`docs/validation/screenshots/private-backup-slice3-settings-health.png` | 可写为“恢复能力已由后端灾备烟测证明，Android 入口可见；真机端删除/恢复全链路建议继续补证”。 |
| 容量/健康 | PASS，Android 我的页展示在线状态、容量百分比、已用/剩余额度和存储用量入口 | `docs/validation/screenshots/private-backup-slice3-settings-health.png`；`private-backup-slice3-storage-usage.png` | 可写为“容量和私有云状态在 Android 端可见”。 |
| 恢复说明/隐私边界 | PASS，灾备文档和证据策略明确恢复、敏感信息和公开证据边界 | `docs/disaster-recovery.md`；`docs/validation/README.md`；`backup-restore-*.md` | 可写为“恢复说明与证据脱敏边界已公开文档化”。 |
| 敏感信息扫描 | PASS，D7 daily acceptance evidence index 均显示 `Sensitive findings: 0` | `docs/validation/daily-acceptance-20260523-review-7/validation-evidence-index.md`；`review-23`；`review-24` | 可写为“发布证据包未发现 token/cookie/password/完整私有 URL”。 |

## 2. 证据索引（按 Android 闭环场景）

### 2.1 环境与前提

| 项目 | 内容 |
|---|---|
| 客户端 | .NET MAUI Android App，包名见 logcat 摘要 `com.companyname.privateclouddrive.app`。 |
| 运行形态 | Android 模拟器可见验收；后端为本地 PrivateCloudDrive 服务栈。 |
| Android API 地址 | 模拟器访问本机后端时使用 `http://10.0.2.2:8080`，真实手机需改为局域网可访问地址。 |
| 构建命令 | `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None` |
| 证据目录 | `docs/validation/`；截图集中在 `docs/validation/screenshots/`。 |
| 公开边界 | 只提交裁剪日志、摘要报告和已确认无敏感信息的截图；原始 logcat、全量设备日志和完整扫描导出不进入 Git。 |

### 2.2 启动、登录与信任边界

| 页面/场景 | 证据文件 | 证明点 |
|---|---|---|
| App 启动 | `docs/validation/app-startup-2026-05-17.png`；`open-design-app-startup.png`；`storage-trust-boundary-startup-2026-05-18.png` | App 可启动，启动页/首屏可见。 |
| 登录配置与登录前状态 | `docs/validation/screenshots/private-backup-slice1-login-server-config.png`；`storage-trust-boundary-login-enter-2026-05-18.png` | 登录入口和服务器配置流程可见。 |
| 登录后文件页 | `docs/validation/screenshots/private-backup-slice2-files-after-login.png`；`storage-trust-boundary-after-login-2026-05-18.png` | 登录后文件页/根目录可见。 |
| 存储信任边界 | `docs/validation/storage-trust-boundary-storage-page-2026-05-18.png`；`storage-trust-boundary-my-page-2026-05-18.png` | 存储页、个人空间、容量与信任提示可见。 |
| 裁剪 logcat 摘要 | `docs/validation/android-logcat-storage-trust-boundary-2026-05-18.log` | 仅保留启动/登录/存储验收摘要，不含原始 token/cookie/password。 |

### 2.3 备份入口、照片/文件/批量状态

| 页面/场景 | 证据文件 | 证明点 |
|---|---|---|
| 备份页和队列入口 | `docs/validation/screenshots/private-backup-slice2-backup-queue.png` | 底部“备份”导航可见，队列页可查看当前任务状态。 |
| 新建/选择操作 | `docs/validation/screenshots/private-backup-slice2-create-action.png` | 用户可从 Android UI 发起新增/备份操作。 |
| 开始备份弹层 | `docs/validation/screenshots/private-backup-slice4-start-backup-page.png` | 弹层展示“备份照片”“从相册选择备份”“备份本机文件”“扫描文档（即将推出）”“新建文件夹”“从链接导入（探索）”。 |
| 文件备份上传结果 | `docs/validation/screenshots/private-backup-slice4-upload-result.png`；`docs/validation/pcd-real-backup-slice4.txt` | 使用公开小文件验证上传结果可见；测试文件内容不含敏感信息。 |
| 下载列表 | `docs/validation/screenshots/private-backup-slice4-downloads-list.png` | 下载/传输列表入口可见。 |
| 重复/认证异常路径 | `docs/validation/screenshots/private-backup-slice4-duplicate-failure-retry.png`；`private-backup-slice4-auth-expired-patched-result.png` | 用户可见异常状态和修复后的结果，支撑非理想路径验收。 |

### 2.4 失败重试闭环

| 页面/场景 | 证据文件 | 证明点 |
|---|---|---|
| 失败前基线 | `docs/validation/screenshots/private-backup-slice5-before-failure.png`；`private-backup-slice5-login-before-failure.png` | 失败演练前已登录并可进入备份相关页面。 |
| 服务不可用时选择文件 | `docs/validation/screenshots/private-backup-slice5-picker-after-api-stop.png` | 后端不可用期间仍可选择待备份文件。 |
| 失败队列 | `docs/validation/screenshots/private-backup-slice5-server-down-failed-queue.png`；`docs/validation/pcd-retry-slice5.txt` | 队列显示 `0 个备份中，0 个等待，1 个失败，0 个已完成`，任务保留，提示网络/服务恢复后点击“重试备份”。 |
| 重试成功 | `docs/validation/screenshots/private-backup-slice5-retry-success.png` | 服务恢复后失败任务可重试成功。 |

### 2.5 容量、健康、回收站和分享入口

| 页面/场景 | 证据文件 | 证明点 |
|---|---|---|
| 我的页健康状态 | `docs/validation/screenshots/private-backup-slice3-settings-health.png` | 展示“在线”、真实数据、容量百分比、已用/剩余额度、存储用量入口、回收站、我的分享和操作日志入口。 |
| 存储用量 | `docs/validation/screenshots/private-backup-slice3-storage-usage.png` | 容量分类/存储用量页面可见。 |
| 回收站入口 | `docs/validation/screenshots/private-backup-slice3-settings-health.png` | Android 端存在回收站入口；删除/恢复深链路由后端灾备烟测补强。 |
| 分享入口 | `docs/validation/screenshots/private-backup-slice3-settings-health.png` | Android “我的分享”入口可见；完整分享 URL 不进入截图或文档。 |

### 2.6 后端恢复、下载/预览、删除/恢复补强证据

| 能力 | 证据文件 | 证明点 |
|---|---|---|
| 非破坏性备份/恢复演练 | `docs/validation/backup-restore-drill-20260518-193513.md` | 备份、manifest、PostgreSQL dump、storage archive 和恢复 dry-run 控制路径通过。 |
| 隔离栈破坏性恢复 | `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md` | 在一次性 Compose 项目中恢复，避免覆盖源运行卷。 |
| 下载/预览 | `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md` | 烟测覆盖上传、Range 下载 HTTP 206、内容 hash 匹配和预览/读取控制路径。 |
| 删除/恢复 | `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md` | 烟测覆盖删除到回收站、回收站列表和恢复成功。 |
| 审计/隐私 | `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md` | 操作日志样本未包含 password/access token/refresh token；报告只记录脱敏摘要。 |

## 3. D7 daily acceptance 入口

| 入口 | 状态 | 说明 |
|---|---|---|
| `docs/validation/daily-acceptance-20260523-review-7/validation-evidence-index.md` | PASS，Sensitive findings 0 | 包含 9 个证据文件，其中含公开敏感扫描摘要。 |
| `docs/validation/daily-acceptance-20260523-review-23/validation-evidence-index.md` | PASS，Sensitive findings 0 | 包含 9 个核心证据文件。 |
| `docs/validation/daily-acceptance-20260523-review-24/validation-evidence-index.md` | PASS，Sensitive findings 0 | 包含 9 个核心证据文件。 |
| `docs/validation/daily-acceptance-20260523-review-*/sensitive-scan.md` | PASS | 每个 D7 复核目录均记录敏感扫描摘要。 |

说明：部分底层报告中保留历史 WARN/FAIL 计数字样用于说明旧限制或早期演练缺口，不等同于 D7 证据包当前敏感信息闸门失败。发布引用时以 daily acceptance index 顶部的 `Status: PASS` 和 `Sensitive findings: 0` 为闸门摘要，不把表格中的历史 FAIL 计数字样解读为当前发布阻断；同时在“已知限制”章节列出未覆盖项。

### 3.1 2026-05-28 本地可复跑证据链

| 验证项 | 命令 | 结果 |
|---|---|---|
| 证据索引与 secret scan 单元测试 | `python -B -m pytest -q tests -p no:cacheprovider` | PASS，19 passed |
| Secret/log scan | `python -B scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD` | PASS，0 findings，598 个 tracked/未忽略 working-tree 文本路径已检查，archive guardrail PASS |
| Validation evidence index dry-run | `python -B scripts/validation_evidence_index.py --run-id codex-d7-gate-local-r2 --date 20260528 --output-root artifacts/validation-evidence-codex-d7-gate-r2` | PASS，evidence_count 17，finding_count 0 |
| 本地栈脚本语法 | PowerShell Parser 解析 `scripts/verify-local-stack.ps1` | PASS |
| 空白检查 | `git diff --check` | PASS，仅有 LF/CRLF replacement 工作区提示 |

说明：本地 dry-run 输出位于 `artifacts/validation-evidence-codex-d7-gate-r2/`，该目录作为本机临时验证产物，不作为发布证据直接提交。`--include-working-tree` 覆盖 tracked 与未忽略的 untracked 文本文件；`--archive-ref HEAD` 仅验证当前已提交 HEAD 的 archive path guardrail，正式发布时必须在目标提交上重新运行。

## 4. 脱敏与公开发布规则

1. 不引用、不提交 token、cookie、password、OAuth client secret、完整私有分享 URL。
2. Android 原始 logcat、设备 dump、服务端全量日志只允许作为本地 ignored artifact 或 CI artifact；Git 中只保留裁剪摘要。
3. 截图只作为 UI 验收证据，发布前需确认不含真实个人照片、真实联系人、完整分享链接或账户密钥。
4. 测试文件 `pcd-real-backup-slice4.txt`、`pcd-retry-slice5.txt` 是公开小文件，可用于复核上传/重试路径。
5. 灾备报告中只允许记录 token suffix、截断 hash、PASS/WARN/FAIL 摘要，不记录完整认证值。
6. 发布前复核命令：

```bash
git diff --check
python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD
python scripts/validation_evidence_index.py --run-id <run-id> --date <yyyymmdd>
```

如 secret/log scan 命中，应人工确认是否为规则说明文字；命中真实敏感值时 release gate 必须改为 FAIL，并先移除/轮换。

## 5. 已知限制与未覆盖项

### 模拟器已验收

- Android App 启动、登录、文件页、备份页、我的页、容量/健康、存储信任边界可见。
- 备份入口、文件选择、上传结果、队列状态、失败保留、手动重试成功可见。
- 后端恢复烟测覆盖上传、下载/预览、分享打开、删除到回收站、回收站恢复和审计样本。
- 证据包公开文本的 D7 敏感扫描结果为 0。

### 真机待增强

- Android 真机相册权限、媒体库大批量照片/视频选择、后台长任务和系统杀进程后的续传未形成公开证据。
- 不同 Android 厂商/OEM、省电策略、蜂窝网络和弱网切换场景未形成公开证据。
- 真实手机访问局域网后端、HTTPS 证书、局域网 DNS/防火墙策略仍需部署前按环境复核。
- Android 端“删除文件到回收站并恢复”的完整 UI 链路仍建议补充真机截图；当前由后端灾备烟测和 Android 入口可见性共同支撑。
- “扫描文档（即将推出）”在 UI 中明确为未推出，不应作为 D7 已交付能力宣传。

## 6. 复核清单

| 检查项 | 命令/路径 | 通过标准 |
|---|---|---|
| 文档路径存在 | `test -f docs/validation/android-backup-release-evidence.md` | 文件存在且可打开。 |
| Android 构建摘要存在 | `test -f docs/validation/maui-android-build-2026-05-18.log` | 构建日志存在且含“已成功生成”。 |
| logcat 摘要存在 | `test -f docs/validation/android-logcat-storage-trust-boundary-2026-05-18.log` | 摘要存在且说明 raw logcat 未进入 Git。 |
| 备份截图存在 | `test -f docs/validation/screenshots/private-backup-slice4-start-backup-page.png` | 截图存在。 |
| 重试截图存在 | `test -f docs/validation/screenshots/private-backup-slice5-retry-success.png` | 截图存在。 |
| D7 敏感扫描 | `grep -R "Sensitive findings: 0" docs/validation/daily-acceptance-20260523-review-*/validation-evidence-index.md` | 每个 D7 index 均有 0 命中摘要。 |
| Markdown 空白检查 | `git diff --check -- docs/validation/android-backup-release-evidence.md docs/validation/README.md` | 无 trailing whitespace 或冲突标记。 |

## 7. Handoff 给芮发布（release-manager）

芮发布在 D7 发布闸门中建议直接引用：

1. 本文第 1 节“Release Manager 可直接引用摘要”作为发布说明和闸门结论来源。
2. 本文第 2 节作为证据链接索引，按能力引用对应截图/日志/报告。
3. 本文第 3 节作为 D7 daily acceptance 和敏感扫描入口。
4. 本文第 5 节作为“已知限制/真机待增强”发布风险说明。

建议发布摘要：

> Android 私有备份 MVP 已在模拟器完成备份入口、文件备份、队列状态、失败重试、容量/健康和恢复说明可见性验收；后端隔离恢复烟测补强下载/预览、删除/恢复和审计边界。D7 证据索引显示敏感发现为 0。真机相册/视频大批量、后台续传、弱网和 OEM 差异仍列为后续增强验证。
