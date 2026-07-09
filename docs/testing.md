# 测试与验证说明

本文档记录当前自动化测试覆盖范围、手动验证命令和已知边界。阶段完成前应至少运行对应阶段涉及的构建和测试命令，并把结果写入 `docs/progress.md`。

## 自动化测试覆盖

当前主要测试位于 `aspnet-core/test/PrivateCloudDrive.EntityFrameworkCore.Tests/EntityFrameworkCore/FileCenter/` 和 `aspnet-core/test/PrivateCloudDrive.EntityFrameworkCore.Tests/EntityFrameworkCore/MobileAuth/`，覆盖以下核心行为：

| 范围 | 覆盖点 |
| --- | --- |
| 文件夹管理 | 创建文件夹、同目录重名校验、分页列表、移动校验、回收站列表、恢复、永久删除、清空回收站 |
| V1.1 文件管理体验 | 文件列表搜索、全盘搜索跨用户隔离、排序、批量移动/收藏/删除/恢复/永久删除、容量统计、分享列表禁用/过期状态 |
| 文件节点仓储 | 子节点查询、软删除过滤、排序、父子目录约束 |
| 小文件上传 | BlobObject 与 FileNode 创建、文件名重名校验、单文件大小限制、用户容量配额超限、删除到回收站、永久删除后释放 Blob |
| 文件下载 | 普通下载、HTTP Range、文件夹不可下载、缩略图下载 |
| 分片上传 | 创建上传会话、上传分片、查询已上传分片、完成合并、SHA256 校验、取消会话并清理临时分片 |
| 媒体任务 | 图片和视频上传后创建 MediaAsset、图片缩略图、视频封面与元数据、处理失败记录、删除清理 |
| 分享链接 | 创建分享、公开摘要、密码错误、密码校验、公开下载、过期链接、禁用链接、管理员全量列表和禁用任意分享 |
| 标签和收藏 | 创建标签、重复标签校验、绑定/解绑标签、收藏状态、按标签和收藏筛选 |
| 媒体库入口 | 图片/视频媒体库分离查询、收藏媒体筛选、媒体库 HTTP 入口 |
| V1.2 媒体库体验 | 混合媒体时间线、TakenAt 优先排序、类型筛选、用户隔离、媒体详情与处理状态、错误摘要脱敏、相册创建/去重/成员添加/成员移除/删除不删文件/封面设置 |
| HTTP 控制器 | 文件下载和缩略图 Range 响应头、上传表单参数传递、公开分享密码请求头与限速策略 |
| 移动认证审计 | 匿名记录登录审计、管理员分页查询审计日志、确认审计输入和 DTO 不包含密码或令牌字段 |
| 微信登录可选接入 | 未绑定登录返回绑定票据、绑定已有账号、错误密码接入 Identity access-failed/lockout 且不消费绑定票据、已锁定用户不能通过已绑定微信登录、禁止迁移已绑定微信、解绑后保留密码登录能力、无绑定解绑也记录审计、登录/绑定/解绑基于分布式缓存限流、WeChat 交换失败审计脱敏、输出 DTO 不包含 AppSecret/OpenId/UnionId/access token、PostgreSQL Host/Tenant 部分唯一索引避免空 TenantId 绕过绑定唯一性 |
| 操作日志查询 | 聚合移动认证审计、ABP 审计动作和安全日志；支持来源、操作类型、用户和时间范围筛选；确认查询契约不包含密码、令牌、AppSecret、请求参数或异常详情 |

## 常用验证命令

后端完整验证：

```powershell
cd aspnet-core
dotnet build .\PrivateCloudDrive.slnx
dotnet test .\PrivateCloudDrive.slnx --no-build
```

MAUI 顺序构建验证（完整：Windows + Android）：

```powershell
.\scripts\verify-maui-build.ps1
```

脚本会先构建 Windows，再构建 Android，并在每个平台构建后验证输出构件是否存在。支持以下参数：

| 参数 | 说明 |
| --- | --- |
| `-Configuration Release` | 指定 Release 配置（默认 Debug） |
| `-SkipWindows` | 跳过 Windows 构建 |
| `-SkipAndroid` | 跳过 Android 构建 |
| `-NoRestore` | 跳过 NuGet restore（CI 场景预 restore 后使用） |

单平台验证（例如当前机器只有 Android 环境）：

```powershell
.\scripts\verify-maui-build.ps1 -SkipWindows    # 仅 Android
.\scripts\verify-maui-build.ps1 -SkipAndroid     # 仅 Windows
```

**Bash/CI 环境**（git-bash, Linux CI with .NET SDK）：

```bash
bash scripts/verify-maui-build.sh
```

Bash 版参数对应：`--configuration Release`、`--skip-windows`、`--skip-android`、`--no-restore`。

Windows 和 Android 目标必须顺序构建，避免多目标 restore/build 同时解析本机未安装的平台 workload 或 runtime。Android 构建和真机验收在具备 Android SDK/JDK/workload 的环境中回填结果。

Docker Compose 配置验证：

```powershell
docker compose config
```

V1.0 RC 本地栈健康检查：

```powershell
.\scripts\verify-local-stack.ps1 -PreflightOnly
.\scripts\verify-local-stack.ps1
```

`verify-local-stack.ps1` 会输出 PASS/WARN/FAIL 汇总，覆盖 Docker、Compose 服务、`.env` 配置边界、PostgreSQL、Redis、db-migrator、API、media-worker、Swagger、存储目录、FFmpeg 和 FFprobe。验收记录中禁止记录密码、access token、refresh token、OAuth code、client secret 或 provider token。

D7 Swagger JSON 与 Android 登录 smoke 自动化门禁：

```bash
COMPOSE_PROJECT_NAME=pcd_d7_smoke \
  API_HTTP_PORT=18081 \
  POSTGRES_PORT=15433 \
  REDIS_PORT=16380 \
  BUILD_IMAGES=0 \
  bash scripts/smoke-compose-local.sh
```

该门禁会启动一次性 Compose 栈，校验 `/swagger/v1/swagger.json` 返回 200，并使用 `client_id=PrivateCloudDrive_App` 通过 `/connect/token` 执行账号密码登录 smoke；token 响应只验证字段存在，不打印 access token、refresh token 或密码。CI 可将 `BUILD_IMAGES=1` 作为源码变更后的完整镜像构建路径；本地复验可设置 `KEEP_STACK_ON_FAILURE=1` 保留失败现场。

备份恢复与灾难恢复验收：

```powershell
.\scripts\run-backup-restore-drill.ps1
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\<timestamp>
# 仅限一次性测试栈或明确授权的目标栈：
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\<timestamp> -ConfirmDestructiveRestore
```

`run-backup-restore-drill.ps1` 会创建备份、执行恢复 dry-run，并在 `docs/validation/` 下写入 `backup-restore-drill-<timestamp>.md` 非破坏性演练报告。破坏性恢复通过后，还需要按 `docs/disaster-recovery.md` 的恢复后验收清单补充登录、文件列表、下载/预览、回收站恢复、分享链路和脱敏审计证据。`.env.secret`、密码、token、OAuth code、client secret 和真实私密文件内容禁止写入验收记录。

V1.1 文件管理体验验证：

```powershell
dotnet build .\aspnet-core\PrivateCloudDrive.slnx
dotnet test .\aspnet-core\PrivateCloudDrive.slnx
.\scripts\verify-maui-build.ps1 -SkipAndroid
```

2026-05-09 执行结果：后端 build 成功；`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 79 个测试；MAUI Windows 构建通过，Android 按参数跳过；`docker compose config`、`docker compose up -d --build` 和 `.\scripts\verify-docker-stack.ps1` 验证通过，Swagger 可访问。

V1.2 媒体库体验验证：
```powershell
cd aspnet-core
dotnet build .\PrivateCloudDrive.slnx
dotnet test .\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --no-restore

cd ..\maui\PrivateCloudDrive.App
dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0 -p:OutputPath=artifacts\verify-build\
```

2026-05-09 执行结果：后端 build 成功；`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 91 个 EF 集成测试；MAUI Windows 隔离输出构建成功，0 警告 0 错误。默认 MAUI 输出目录当前被运行中的 `PrivateCloudDrive.App (75188)` 锁定，因此使用隔离输出目录验证。

V1.2 RC 发布候选验证：

> 命名口径：本节属于 V1.2 RC 发布候选验证。当前验收矩阵以 `docs/scenario-matrix-v1.2-rc.md` 为准，当前发布说明以 `docs/release-notes-v1.2-rc.md` 为准；`docs/scenario-matrix-v1.2.md` 与 `docs/release-notes-v1.2.md` 仅作为 RC 通过后提升正式版的候选材料。

```powershell
dotnet build D:\Devs\Projects\Personal\PrivateCloudDrive\aspnet-core\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-v12-rc-backend-build\
dotnet test D:\Devs\Projects\Personal\PrivateCloudDrive\aspnet-core\PrivateCloudDrive.slnx --no-build -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-v12-rc-backend-build\ --logger "trx;LogFilePrefix=v12-rc-backend" --results-directory D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\test-results\v12-rc-backend
.\scripts\verify-maui-build.ps1 -Configuration Debug
dotnet publish .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:TargetFrameworks=net10.0-android -p:AndroidPackageFormat=apk
```

2026-05-14 执行结果：后端 solution build 成功，0 警告 0 错误；`dotnet test` 通过，`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 101 个测试，其它测试项目当前没有可发现测试；`.\scripts\verify-maui-build.ps1 -Configuration Debug` 顺序验证 Windows 与 Android 构建均通过，PASS 4 / WARN 0 / FAIL 0；Android Debug Signed APK 已生成并复制到 `artifacts/verify-v12-rc-maui-apk/com.companyname.privateclouddrive.app-Signed.apk`。当前 `adb devices` 未检测到已连接设备或模拟器，因此 APK 安装、启动截图和触控验收未执行，需在可用 Android 设备上按下方手动清单回填。

2026-05-15 安全加固复验：`dotnet build /d/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core/PrivateCloudDrive.slnx --no-restore -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-security-hardening-build/` 通过，0 警告 0 错误；`dotnet test ... --filter PublicFileSharesControllerSecurityTests` 通过 3/3；`docker compose config` 通过；`git diff --check` 无空白错误。该轮验证覆盖公开分享密码不再使用 URL Query、`X-Share-Password` 请求头绑定、密码校验/下载入口 `PublicSharePassword` 限速策略、生产 HTTPS/default secret fail-fast 和生产 Swagger 默认关闭。

## V1.2 手动验收清单

| 范围 | 检查步骤 | 预期结果 |
| --- | --- | --- |
| Android 模拟器登录页视觉闸门 | 在 Pixel 9 Pro API 36 模拟器 clean install 最新 APK，启动 App。 | App 不崩溃；进入最新卡片式登录页；`http://10.0.2.2:8080` 连接提示可见；Android 原生 Entry 下划线/双边框不可见；无裁切或溢出。 |
| Android 键盘遮挡 | 点击用户名/密码输入框并弹出软键盘。 | 当前模拟器 `mInputShown=false` 未能弹出软键盘，需真机或可弹出软键盘环境继续验证；通过标准为输入框可读、登录按钮可达、内容不被系统键盘遮挡。 |
| 媒体时间线 | 进入 Photos/媒体库页，切换全部、图片、视频筛选。 | 媒体按月份分组并按时间倒序展示；图片筛选不显示视频，视频筛选不显示图片。 |
| 状态可见 | 上传图片/视频后，在后台处理完成前刷新媒体库和处理状态页。 | Pending/Processing/Failed 不显示为空白卡片，卡片或详情页展示清晰状态。 |
| 相册 | 进入媒体库的“相册”，新建相册，进入详情页后使用“添加媒体”勾选图片/视频加入相册，移除媒体，设置封面。 | 相册列表显示数量；添加媒体不依赖自动最近列表；移除媒体不删除原文件；封面设置成功。 |
| 删除相册 | 删除一个包含媒体的相册后回到媒体库时间线。 | 相册被删除，原媒体仍在时间线中。 |
| 视频详情 | 打开视频卡片。 | Completed 视频进入播放器；Pending/Processing 显示处理中说明；Failed 显示脱敏摘要和重新处理按钮。 |
| 重新处理 | 在失败媒体详情或处理状态页点击重试。 | 状态重新进入 Processing，处理状态页刷新后可看到最新状态。 |

## V1.2 验收矩阵

本验收矩阵将 V1.2 P0/P1 能力与已知限制（LIM-V12-01~09）映射为结构化验收记录。每项验收完成后回填 PASS/WARN/FAIL 和备注。

| 编号 | 能力 | 优先级 | 验收标准 | 对应已知限制 | 结果 | 备注 |
|------|------|:------:|---------|:------------:|:----:|------|
| AC-V12-01 | 媒体时间线 — 图片+视频混合，月份分组，时间倒序 | P0 | 媒体按 `TimelineTime` 倒序，月份分组展示；时间来源优先级 TakenAt > CreationTime > LastModificationTime | LIM-V12-01（排序不可自定义）、LIM-V12-07（月份跨页边界） | | |
| AC-V12-02 | 媒体类型过滤 — 全部/图片/视频 | P0 | 三个筛选 Tab 互斥切换，筛选后分组和排序不改变 | — | | |
| AC-V12-03 | 视频封面与时长 | P0 | 处理完成的视频显示缩略图封面和 mm:ss/hh:mm:ss 时长角标 | LIM-V12-02（仅 mp4/mov/webm 保证） | | |
| AC-V12-04 | 媒体处理状态可视化 | P0 | Pending/Processing/Failed/Completed 状态 badge 正确展示；非 Completed 不展示空白缩略图 | LIM-V12-06（历史媒体无 MediaAsset 时显示 Pending） | | |
| AC-V12-05 | 播放错误与重试 | P0 | 加载态明确；失败展示脱敏原因和重试入口；Processing 提示"视频处理中" | LIM-V12-05（限单文件重试，无批量） | | |
| AC-V12-06 | 相册创建/重命名/删除 | P0 | 名称必填且唯一；删除不删原文件；二次确认文案明确"不会删除文件" | LIM-V12-04（不支持嵌套/子相册）、LIM-V12-08（不改变文件目录结构） | | |
| AC-V12-07 | 相册添加/移除媒体 | P0 | 只能添加当前用户媒体；移除不删原文件；同一媒体不可重复加入同一相册 | LIM-V12-08（不改变文件目录结构） | | |
| AC-V12-08 | 相册封面（默认+手动） | P1 | 默认取最新一张完成缩略图的媒体；可手动设置 | LIM-V12-03（仅限已完成缩略图媒体设封面） | | |
| AC-V12-09 | 处理状态聚合入口 | P1 | 媒体库顶部或设置页显示"处理中：N / 失败：N" | — | | |
| AC-V12-10 | 视频重试失败处理 | P1 | 失败项重试后状态变为 Processing；重试前校验文件归属和状态 | — | | |
| AC-V12-11 | 时间线下拉刷新 | P1 | 移动端下拉可刷新媒体处理结果 | — | | |
| AC-V12-12 | 相册排序 | P1 | 相册列表支持按更新时间/创建时间排序 | — | | |
| AC-V12-13 | 跨用户隔离 | P0 | 用户 A 不能看到用户 B 的媒体、相册、处理状态；时间线/相册/重试只返回当前用户数据 | — | | |
| AC-V12-14 | 错误脱敏 | P0 | ProcessErrorSummary 不包含物理路径、token、secret、connection string、堆栈信息 | — | | |
| AC-V12-15 | iOS 客户端 | — | 不纳入 V1.2 范围；MAUI 仅验证 Windows 和 Android | LIM-V12-09 | | |

**放行标准：**
- P0 项 = 0 阻塞缺陷
- P1 项 = 0 缺陷，或每个 P1 都有明确规避方案和发布批准
- 已知限制已记录并发布说明

### V1.2 已知限制清单（验收时同步确认）

| 编号 | 限制 | 发布确认 |
|:----:|------|:--------:|
| LIM-V12-01 | 时间线基于拍摄时间/上传时间，不支持修改时间轴顺序 | |
| LIM-V12-02 | 视频只保证 mp4/mov/webm 主链路播放，其他格式视 ffmpeg 兼容性 | |
| LIM-V12-03 | 相册封面仅限已完成缩略图的媒体；处理中和失败项无法设封面 | |
| LIM-V12-04 | 相册不支持嵌套/子相册 | |
| LIM-V12-05 | 媒体处理失败重试限于单文件操作，无批量重新处理入口 | |
| LIM-V12-06 | 历史媒体（V1.2 前上传）可能缺少 MediaAsset 记录，时间线中显示为等待处理 | |
| LIM-V12-07 | 时间线月份分组基于后端返回的扁平列表在 MAUI 端分组，超长列表跨页月份边界需验证 | |
| LIM-V12-08 | 相册不改变文件原始目录结构，删除相册或移除相册项不改变原文件位置 | |
| LIM-V12-09 | iOS 客户端不在 V1.2 范围内；MAUI 构建仅验证 Windows 和 Android 目标 | |

## V1.1 手动验收清单

| 范围 | 检查步骤 | 预期结果 |
| --- | --- | --- |
| 文件搜索 | Files 页输入关键字后搜索，再清除筛选。 | 列表只显示匹配项目；清除后恢复当前目录列表。 |
| 全盘搜索 | 打开“全部”开关后搜索子目录中的文件名。 | 可以搜索当前用户全盘未删除节点，不出现其他用户数据。 |
| 排序筛选 | 切换名称、大小、创建时间、修改时间排序，并切换文件夹/文件/图片/视频/其他过滤。 | 列表按选择条件刷新，未命中时显示空状态。 |
| 文件页批量操作 | 点击“选择”，多选项目后执行收藏、取消收藏、移到根目录、删除。 | 每个操作都有后端结果；删除进入回收站；完成后退出选择模式并刷新列表。 |
| 回收站批量操作 | Settings 进入 Trash，选择多个项目执行恢复和永久删除。 | 恢复回原位置；永久删除有强确认且不可恢复。 |
| 容量卡 | Settings 查看“存储容量”。 | 显示已用、配额、剩余和进度条；API 失败时显示可读错误。 |
| 我的分享 | Settings 进入“我的分享”，复制链接并禁用一个有效分享。 | 列表显示文件名、状态、创建/过期时间和访问次数；复制写入剪贴板；禁用后状态刷新为已禁用。 |

### V1.1 发布验收结果

下表记录了 V1.1 P0 功能在 Android 真机/模拟器验收中的结果（2026-07-07，Phase 3 / Phase 4 同步）。每个 P0 功能都已通过后端验证、MAUI 构建验证和 Android 模拟器主链路验收。详细验收证据见 `docs/release-plan-v1.1.md` §3.1。

| 编号 | 功能 | 后端状态 | MAUI 状态 | 验收结果 | 备注 |
| --- | --- | --- | --- | --- | --- |
| V1.1-P0-01 | 文件名搜索（当前目录 + 全盘） | ✅ 已实现（ILIK E + SearchScope） | ✅ 已实现（FilesSearchBar + SearchAllSwitch） | PASS | 搜索范围限当前用户/租户，不跨用户泄露；EF 集成测试覆盖跨用户隔离 |
| V1.1-P0-02 | 排序与筛选（名称/时间/大小/类型 + 类型/媒体/收藏/标签筛选） | ✅ 已实现（allowlist + ABP Sorting） | ✅ 已实现（SortPicker + TypeFilterPicker + MediaFilterPicker） | PASS | 排序字段来自服务端 allowlist，未知值降级到默认；筛选和搜索可组合 |
| V1.1-P0-03 | 批量选择与批量操作（删除/恢复/永久删除/移动/收藏） | ✅ 已实现（BatchFileNodeInput、MaxBatchItemCount=100、逐项校验） | ✅ 已实现（BatchToolbar + 确认弹窗） | PASS | 危险操作二次确认；永久删除文案明确不可恢复；部分失败有可读错误 |
| V1.1-P0-04 | 重命名（文件/文件夹） | ✅ 已实现（RenameAsync） | ✅ 已实现（FileDetailsPage.OnRenameClicked + 前端校验） | PASS | 同级重名冲突显示可读错误；非法字符/空名/超长有前端校验 |
| V1.1-P0-05 | 移动（跨文件夹） | ✅ 已实现（MoveAsync + 循环检测） | ⚠️ 仅支持移至根目录 | PASS | 完整目录选择器尚未确认；后端批量移至根目录可用 |
| V1.1-P0-06 | 容量展示（已用/配额/剩余/百分比/单文件上限） | ✅ 已实现（StorageUsageDto + GetUsageAsync） | ✅ 已接入 API（PR #40） | PASS | 已替换硬编码，现在显示真实 API 值；API 失败时显示 Degraded 状态 |
| V1.1-P0-07 | 分享管理（我的分享列表、复制链接、禁用） | ✅ 已实现（GetSharesAsync + DisableShareAsync + owner 校验） | ✅ 已实现（SharesPage + SettingsPage 入口） | PASS | 密码不泄漏（仅 PBKDF2 哈希）；不泄露他人分享；空列表有引导文案 |
| V1.1-P1-01 | 上传队列重试/取消 | ✅ 已实现（UploadSession Cancelled/Pending/Completed） | ✅ 已实现（UploadStatusPanel 显示进度/状态） | PASS | 错误信息可读；当前 session 内反映队列状态 |
| V1.1-P1-02 | 操作日志覆盖（批量/分享/删除关键行为） | ✅ ABP 审计管线自动记录 | — | WARN | 批量删除/永久删除/分享停用通过 ABP 审计管线自动记录，但无独立 MobileAuthAuditLog 条目 |

**汇总：PASS 8 / WARN 1 / FAIL 0**

### 已知限制

- 搜索使用 PostgreSQL ILIKE（NormalizedName.Contains），不是全文搜索引擎；个人/家庭规模性能充足，大目录（10 万 + 文件）未实测。
- 批量操作前端选择局限在当前页面加载项，跨页全量多选未实现。
- 移动操作 MAUI 端当前仅支持"移至根目录"，完整文件夹选择器未确认可用。
- 操作日志对批量删除/永久删除/分享停用的审计事件通过 ABP 管线自动记录，但无独立审计条目覆盖度确认。
- iOS 客户端不在 V1.1 范围内；MAUI 构建仅验证 Windows 和 Android 目标。
- 微信/Google/GitHub 外部登录保持 V1.0 RC 的降级策略：未配置时不显示入口，不影响账号密码主链路。

## 移动端真实设备手动验收清单

阶段 6.5 需要在真实 Android 或 iOS 设备上执行；本地内测也可以先用 Windows 客户端或 Android 模拟器预验收。执行前确认后端 API、PostgreSQL、Redis 已启动，MAUI App 的 API BaseUrl 指向设备可访问的后端地址，测试账号具备 FileCenter 基础权限。

MVP 内测默认连接 Docker Compose API：Windows 为 `http://localhost:8080`，Android 模拟器为 `http://10.0.2.2:8080`。真实设备需要改成局域网可访问地址。当前 Trash 不在底部导航中，从 Settings 页进入。

| 范围 | 检查步骤 | 预期结果 |
| --- | --- | --- |
| 启动与后端连接 | 首次启动 App，保持网络可用，进入登录页。 | 启动页不崩溃；后端不可达时显示可重试错误；后端可达时进入登录状态判断。 |
| 账号密码登录 | 输入管理员或测试用户账号密码并登录。 | 登录成功后进入文件页；SecureStorage 保存 access token 和 refresh token；后端产生登录成功审计记录。 |
| 登录失败 | 输入错误密码登录。 | App 显示明确失败提示；不保存任何 token；后端产生登录失败审计记录；日志不包含密码。 |
| Token 刷新 | 登录后保持 App 一段时间，或通过调短 token 有效期触发刷新后再刷新文件列表。 | API 请求可继续成功；refresh token 失效时回到登录页；刷新失败产生审计记录且不记录 token 内容。 |
| 文件列表与导航 | 打开文件页，进入文件夹、返回上级目录、下拉刷新。 | 列表显示真实文件和文件夹；空目录、加载中、错误状态显示正确；分页或刷新不重复追加异常数据。 |
| 上传队列 | 使用 FilePicker 选择图片、视频和普通文件上传。 | 上传队列显示等待、上传中、完成或失败状态；上传成功后当前目录出现文件；失败项可明确识别。 |
| 图片预览 | 从文件页打开图片文件。 | 图片可以加载大图或缩略图；加载失败时显示重试入口；返回文件页后导航状态正常。 |
| 视频播放 | 从文件页打开 MP4 视频。 | 视频能播放；进度拖动可用；后端 Range 响应支持播放；加载失败时显示重试入口。 |
| 回收站删除与恢复 | 删除一个文件，从 Settings 进入回收站并恢复。 | 删除后普通目录不再显示该文件；回收站显示该文件；恢复后回到原目录；同名冲突有明确提示。 |
| 永久删除与清空回收站 | 对回收站文件执行永久删除，再测试清空回收站。 | 永久删除和清空都有二次确认；确认后文件不再出现在回收站；取消确认不删除数据。 |
| 退出登录 | 从设置页退出登录后重启 App。 | 本地 token 被清理；重新启动后停留在登录页；后端产生退出登录审计记录。 |
| iOS/Android 平台差异 | 分别在目标平台检查状态栏、安全区域、键盘遮挡、文件选择器和视频播放。 | 内容不被系统栏或键盘遮挡；触控区域可用；平台差异不阻塞 MVP Core 主流程。 |

### 移动端真实设备执行记录

执行阶段 6.5 后，把结果追加到下表。记录只保留可复现信息，不记录密码、access token、refresh token 或个人敏感数据。

| 日期 | 平台与设备 | 系统版本 | App 构建号 | 后端提交 | 测试账号 | 结果 | 问题与备注 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 2026-05-08 | Android Emulator Pixel 9 Pro | API 36 | 1.0 (1) | `97e2bec` | admin | 通过 | 已完成 MVP Core 内测验收：账号密码登录、文件页加载、上传入口、回收站入口和 Compose API 访问可用；未记录密码或 token。iOS/真实设备验收后续按发布需要补充。 |
| 2026-05-15 | Android Emulator Pixel 9 Pro / sdk_gphone64_x86_64 | Android 16 / API 36 | 1.0 (1) | `4b56ff2` | 未登录 | 部分通过 | V1.2 RC APK clean install 后通过显式 Activity 启动；登录页截图 `artifacts/runtime/v12-rc-android/13-am-start-themed.png` 通过视觉闸门，原生 Entry 下划线/双边框已消除，无裁切或溢出；软键盘在该模拟器中未弹出，`mInputShown=false`，键盘遮挡待真机或可弹软键盘环境补验。 |
| 2026-05-15 | RC 自动复验 / Windows Host | .NET SDK 10.0.204 | MAUI Debug / Android APK | 本轮 RC 收口提交 | 未登录 | 通过 | `git diff --check`、PowerShell 脚本 Parser、`docker compose config`、本地栈 Preflight、SkipStart 全栈健康检查、后端 build/test、MAUI Windows/Android 顺序构建、Android APK publish 均通过；09:54 复验时 `adb devices` 无已连接设备/模拟器，设备安装启动沿用同日 Pixel 9 Pro 视觉闸门证据，真机键盘遮挡仍为环境边界。 |

## V1 微信登录真实设备验收清单

阶段 8.2 需要在真实 Android 和 iOS 设备上执行。执行前必须准备微信开放平台移动应用、正式 `AppId`/`AppSecret`、Android 包名与签名、iOS Bundle Identifier 与 URL Scheme，并确保后端 API 可被设备访问。

当前实现状态：后端 WeChat code 交换、绑定、解绑、OpenIddict 自定义 grant、审计和限流已接入；MAUI 登录页和 Settings 绑定入口已接入；Android 已接入 WeChat SDK 原生授权桥接。Windows/iOS 仍使用默认不可用平台实现。Android 模拟器如果未安装微信，只能完成构建和按钮隐藏验证，不能完成真实授权。

| 范围 | 检查步骤 | 预期结果 |
| --- | --- | --- |
| 后端配置 | 设置 `WECHAT_ENABLED=true`、`WECHAT_APP_ID`、`WECHAT_APP_SECRET`、平台包名/签名或 URL Scheme，重启 API。 | `/api/mobile-auth/wechat/settings` 返回 `isEnabled=true` 和公开配置；响应不包含 `AppSecret`。 |
| Android 授权入口 | 在已安装微信的 Android 真机启动 App，打开登录页。 | 后端启用且平台服务可用时显示微信登录按钮；未安装微信时不进入授权流程。 |
| iOS 授权入口 | 在已安装微信的 iPhone 启动 App，打开登录页。 | URL Scheme 与微信开放平台配置一致；按钮显示受后端配置和平台可用性控制。 |
| 用户取消授权 | 点击微信登录后在微信授权页取消。 | App 停留在登录页；已输入的账号密码不被清空；已有账号密码登录 Token 不被清理。 |
| 未绑定首次登录 | 使用未绑定的微信账号授权。 | 后端返回绑定票据；App 进入绑定已有 PrivateCloudDrive 账号流程，不直接创建管理员。 |
| 绑定已有账号 | 在绑定流程输入普通用户或管理员账号密码。 | 密码正确时创建微信绑定；密码错误时不消费 `bindingTicket`，并触发 Identity access-failed/lockout 策略。 |
| 已绑定微信登录 | 退出账号密码会话后再次使用同一微信账号登录。 | 登录成功进入文件页；后端记录 `WeChatLogin` 成功审计；不会暴露 openid、unionid 或微信 token。 |
| 已登录绑定 | 使用账号密码登录后进入 Settings，点击绑定微信。 | 成功绑定当前用户；已被其他用户绑定的微信身份不能迁移。 |
| 解绑微信 | Settings 中点击解绑并确认。 | 账号仍有密码登录能力时解绑成功；解绑后微信登录重新进入未绑定流程，不强制退出当前账号密码会话。 |
| 锁定用户 | 将已绑定用户设置为 Identity lockout 后尝试微信登录。 | 不签发 Token；返回失败并记录 `user_locked_out` 审计。 |
| 限流 | 在同一设备或账号维度连续触发微信登录、绑定、解绑超过配置阈值。 | 返回 `wechat_rate_limited`；失败审计不包含 code、AppSecret、access token 或 refresh token。 |
| 证据记录 | 记录设备型号、系统版本、App 构建号、后端提交、AppId 后四位、测试账号、关键接口状态和审计日志时间。 | 验收记录可复现，且不包含明文 AppSecret、密码、access token、refresh token 或微信 access token。 |

### V1 微信登录执行记录

执行阶段 8.2 后，把 Android 和 iOS 结果分别追加到下表。`AppId` 只记录后四位，禁止记录 `AppSecret`、微信 access token、openid、unionid、业务 access token 或 refresh token。

| 日期 | 平台与设备 | 系统版本 | App 构建号 | 后端提交 | AppId 后四位 | 微信版本 | 结果 | 问题与备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 待执行 | Android | 待填写 | 待填写 | 待填写 | 待填写 | 待填写 | 待执行 | 待填写 |
| 待执行 | iOS | 待填写 | 待填写 | 待填写 | 待填写 | 待填写 | 待执行 | 待填写 |

## RC Docker 一键启动复验（2026-06-17）

验证命令：
```powershell
cd D:\Devs\Projects\Personal\PrivateCloudDrive
docker compose down  # 清理旧容器
docker compose up -d --build
```

### 容器状态

| 容器 | 状态 | 重启次数 |
| --- | --- | --- |
| postgres (17-alpine) | Up (healthy) | 0 |
| redis (7-alpine) | Up (healthy) | 0 |
| db-migrator | Exited (0) — 迁移全部成功 | 0 |
| api | Up (0.0.0.0:8080) | 0 |
| media-worker | Up | 0 |

### 验证结果

- `docker compose up -d --build` — ✅ 无构建错误，所有容器正常启动
- Swagger UI (`http://localhost:8080/swagger/`) — ✅ HTTP 200
- Swagger JSON (`/swagger/v1/swagger.json`) — ✅ 200，返回完整 OpenAPI 3.0.4 规范
- db-migrator 日志 — ✅ 所有数据库迁移完成，种子数据（含 QA 测试账号）写入成功
- media-worker 日志 — ✅ 正常启动，无错误/异常
- PostgreSQL 健康检查 — ✅ pass
- Redis 健康检查 — ✅ pass
- API 可正常处理请求 — ✅ shares 端点返回 200
- 默认管理员登录 (`admin`/`<配置的密码>`) — ✅ `{"result":1,"description":"Success"}`
- QA 测试账号登录 (`qa_user`) — ✅ 登录成功
- QA 备选账号登录 (`qa_user_alt`) — ✅ 登录成功

### 构建信息

- 提交：`76eec5e`（`docs: testing.md 同步 MAUI 构建验证脚本更新`）
- 镜像基础：`mcr.microsoft.com/dotnet/sdk:10.0`（build）/ `mcr.microsoft.com/dotnet/aspnet:10.0`（runtime）
- 容器均自提交后重建，无 Docker 缓存残留

## RC MAUI 顺序构建验证（2026-06-17）

验证命令（与 `scripts/verify-maui-build.sh` 等价）：

```powershell
cd D:\Devs\Projects\Personal\PrivateCloudDrive
pwsh -NoProfile -File scripts/verify-maui-build.ps1 -Configuration Debug
```

### 构建环境

| 项目 | 值 |
| --- | --- |
| .NET SDK | 10.0.204 |
| MSBuild | 18.3.3 |
| RID | win-x64 |
| PowerShell | 7.6.2 |
| Windows 版本 | 10.0.26200 |
| 提交 | `76eec5e` |

### Workload

| Workload | 版本 |
| --- | --- |
| android | 36.1.53/10.0.100 ✅ |
| ios | 26.4.10259/10.0.100 ✅ |
| maccatalyst | 26.4.10259/10.0.100 ✅ |
| maui-windows | 10.0.20/10.0.100 ✅ |

### 构建结果

| 平台 | 检查项 | 结果 |
| --- | --- | --- |
| Preflight | dotnet-cli | PASS |
| Preflight | maui-project | PASS |
| Preflight | maui-windows-wl | PASS |
| Preflight | android-wl | PASS |
| Windows | build (net10.0-windows10.0.19041.0, win-x64) | PASS |
| Windows | artifact (.exe, 284 KB) | PASS |
| Android | build (net10.0-android) | PASS |
| Android | artifact (.apk, 20 MB) | PASS |

**汇总：PASS 8 / WARN 0 / FAIL 0**

### ADB 设备状态

| 检查项 | 结果 |
| --- | --- |
| ADB daemon | Running |
| 已连接设备 | **无** — 无 Android 真机或模拟器连接 |
| AVD 列表 | N/A — ANDROID_HOME 未设置，无 Android SDK |

**影响**：Android APK 构建通过，但真实设备安装、启动和主链路触控验收需在可用 Android 设备上按本文件“移动端真实设备手动验收清单”补填。当前环境限制已记录，不切换技术栈。

### Secret Scan

- `scripts/secret-log-scan.py --archive-ref HEAD` → **PASS**，0 findings，619 tracked files
- 构建输出无 Token、密码、client_secret 或分享 URL 泄露

### 详细日志

完整构建环境、命令输出和 artifact 验证见 `docs/validation/maui-build-2026-06-17.log`。

## 当前边界

- 自动化测试主要集中在后端应用层、领域层和 EF Core 集成测试；MAUI 端目前以构建验证为主。
- Docker Compose 配置展开已验证；完整容器启动可通过 `scripts/verify-docker-stack.ps1` 复验，实际结果仍依赖本机 Docker daemon、镜像缓存和网络环境。
- 第一阶段不覆盖 NAS 文件协议、桌面同步、Office 在线协作、AI 相册或多节点高可用。
- 账号密码登录页、OpenIddict password grant 错误凭据链路、管理员账号密码登录、Refresh Token 刷新和 refresh token 撤销已完成手动探针验证。
- 移动端登录、刷新和登出审计已接入 MAUI 客户端；后端匿名写入 204 与管理员查询 200 已通过临时 API 探针验证。
- V1 操作日志查询后端已接入 `/api/operation-logs`；临时 API 探针已验证管理员 Bearer Token 可按 `Source`、`Action` 和 `UserName` 查询移动认证审计记录。
- V1 分享、标签和收藏已提供显式 HTTP 路由，并接入 MAUI 文件详情页；临时 API 探针已验证收藏、标签绑定、分享创建和测试数据清理均成功。
- V1 管理员管理所有分享已接入 `/api/file-center/shares/all`；临时 API 探针已验证管理员列表命中分享、禁用分享返回 204、禁用后列表显示 `IsEnabled=false`。
- V1 图片/视频媒体库已接入 `/api/file-center/media/images` 和 `/api/file-center/media/videos`，并接入 MAUI Photos/Videos 底部导航；临时 API 探针已验证两个媒体库入口均返回 200。
- V1 操作日志已接入 MAUI Settings 入口和列表页，Windows/Android 构建已验证。
- V1 微信登录后端骨架已接入：默认禁用配置、`WechatUserBinding`、绑定/解绑接口、OpenIddict 自定义 grant、绑定票据、分布式缓存限流和审计测试已验证；临时 API 探针确认未配置时返回 `wechat_disabled` 且账号密码登录正常。
- V1 微信登录 MAUI 端已接入登录页和 Settings 绑定入口；Android 已接入 WeChat SDK 原生授权桥接，按钮显示同时受后端 settings、设备是否安装微信和平台可用性控制；Windows/iOS 仍报告不可用。微信授权或 token grant 失败时不会清理已有账号密码登录 Token，也不会清空登录页已输入的账号密码；正式 AppId/AppSecret、Android 应用签名和真机授权流程仍需回填验收结果。
- 账号密码登录失败已接入用户名和 IP 双维度分布式限流；Android Emulator Pixel 9 Pro API 36 已完成 MVP Core 内测验收，iOS/真实设备体验后续按发布需要补充。
- 如果在同一个 Redis 实例上先运行过旧版 API，再更新 `PrivateCloudDrive_App` 的 OpenIddict grant 权限，可能会命中旧客户端缓存；本地验收时可重启 API 并刷新对应 Redis 缓存，或使用独立 Redis 逻辑库做临时探针。
- MAUI MVP Core 页面状态已通过 Windows/Android 构建验证，覆盖启动、登录、文件、上传、详情、预览、回收站和设置页；Android 模拟器交互验收已完成。
