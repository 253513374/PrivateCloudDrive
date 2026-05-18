# 开发进度记录

本文档用于记录每个阶段的完成状态、对应提交和验证证据。阶段完成后必须更新本文件，并随阶段提交一起进入 Git 历史。

## 阶段完成规则

- 只有满足 `docs/codex-development-plan.md` 中对应阶段验收项，并完成构建或测试验证，才可以标记为已完成。
- 每完成一个阶段必须提交 Git。
- 进度记录必须包含完成范围、验证命令、验证结果和遗留事项。
- 进行中的代码不能标记为阶段完成。

## 阶段状态

| 阶段 | 状态 | 对应提交 | 完成范围 | 验证证据 |
| --- | --- | --- | --- | --- |
| 阶段 0：项目初始化 | 已完成 | `e8b050a` | ABP 后端骨架、MAUI App 骨架、基础 Docker PostgreSQL 配置 | 历史提交已包含项目骨架；后续阶段构建持续验证通过 |
| 阶段 1：后端文件核心 | 已完成 | `216b8a4`, `ad3ac95`, `540853b` | FileCenter 模块结构、FileNode 实体、文件夹管理 API | 后续 `dotnet build .\PrivateCloudDrive.slnx` 和后端测试持续通过 |
| 阶段 2：上传下载 | 已完成 | `011d13d`, `d20a927`, `75a4138` | BlobObject 本地存储、小文件上传、文件下载、HTTP Range、分片上传 | 2026-05-07：`dotnet build .\PrivateCloudDrive.slnx` 成功；`dotnet test .\PrivateCloudDrive.slnx --no-build` 通过 34 个后端集成测试 |
| 阶段 3：媒体处理 | 已完成 | `75a4138` | MediaAsset 实体、上传后创建媒体任务、图片缩略图、视频封面和元数据处理基础、缩略图访问 API | 2026-05-07：后端构建成功；媒体处理相关集成测试包含图片、视频、缩略图和永久删除清理场景 |
| 阶段 4：MAUI App 核心 | 已完成 | `8dfd5a6` | OpenIddict 登录接入、Token 安全保存和刷新、真实文件列表、文件夹导航、新建文件夹、当前目录上传、图片预览、MediaElement 视频播放 | 2026-05-07：后端构建和测试通过；`dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0` 成功；`dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-android` 成功 |
| 阶段 5：MVP Core 回收站、部署与质量收尾 | 已完成 | `de2c6f9` | 任务 5.1 回收站 API 与 App 入口已实现；Docker Compose、README、部署说明和测试说明已完成收尾复核；本地运行时 `App_Data` 已加入忽略规则并随阶段收尾提交 | 2026-05-08：预提交刷新验证中，后端 build 成功；后端测试通过 60 个 EF 集成测试；MAUI Windows/Android 构建成功；`docker compose config` 复验通过 |
| 阶段 6：MVP Core 产品体验与账号密码认证深化 | 已完成 | `de2c6f9`, `bce5e4e`, `d4f4f76`, `97e2bec` | 任务 6.1 MAUI 设计系统、任务 6.2 MVP Core 页面状态、任务 6.3 账号密码登录/Refresh Token/撤销端点、任务 6.4 移动端认证审计和账号/IP 双维度登录失败限流已落地；MVP 内测版已对齐本地化文案、Compose API 地址和 Settings 回收站入口；任务 6.5 已在 Android Emulator Pixel 9 Pro API 36 完成内测验收 | 2026-05-08：后端 build 成功；后端测试通过 63 个 EF 集成测试；临时 API 验证 password grant、refresh_token、revocation、mobile auth audit、password rate limit 和 Compose 小文件上传/Range/回收站链路；MAUI Windows/Android 构建成功；Android 模拟器完成 MVP Core 内测验收；`docs/testing.md` 已记录执行结果 |
| 阶段 7：V1 分享、标签、收藏与操作日志 | 已完成 | `bb654ee`, `4f4f6a1`, `158cbc3`, `de2c6f9` | 分享链接、公开访问与密码校验、管理员管理所有分享、标签管理、收藏筛选、图片/视频媒体库、操作日志查询后端与 HTTP 入口已实现；MAUI 文件详情、图片页、视频页和操作日志页已接入对应入口并随收尾提交 | 2026-05-08：后端测试通过 60 个 EF 集成测试；临时 API 已验证 `/api/operation-logs`、分享/标签/收藏、`/api/file-center/media/images`、`/api/file-center/media/videos` 和 `/api/file-center/shares/all`；MAUI Windows/Android 构建通过 |
| 阶段 8：V1 微信登录可选接入 | 进行中 | `de2c6f9`, 本次提交 | 后端 WeChat 配置、`WechatUserBinding`、绑定/解绑接口、绑定票据、OpenIddict 自定义 grant、审计记录和 MAUI 登录/设置页入口已实现；`WechatUserBinding` PostgreSQL Host/Tenant 唯一索引已加固；首次绑定已有账号和已绑定微信登录均对齐 Identity lockout；登录、绑定和解绑已接入分布式缓存限流；解绑审计已覆盖无绑定场景；Android 已接入 WeChat SDK 原生授权桥接；iOS 平台 SDK 和真实微信凭据真机验收仍待执行 | 2026-05-08：后端隔离输出 build 通过；EF 集成测试通过 63 个；Android WeChat SDK 构建通过；MAUI Windows/Android 目标框架构建通过；真实 WeChat AppId/AppSecret、Android 签名和真机授权结果待回填 |
| 阶段 9：V1.1 文件管理体验 | 已完成 | `a3394ce` | 文件列表搜索、排序、类型/媒体筛选、批量删除/恢复/永久删除/移动/收藏、容量统计、我的分享管理页，以及后端 API 与 MAUI 客户端对接已完成 | 2026-05-09：`dotnet build .\aspnet-core\PrivateCloudDrive.slnx` 成功；`dotnet test .\aspnet-core\PrivateCloudDrive.slnx` 通过 79 个 EF 集成测试；`.\scripts\verify-maui-build.ps1 -SkipAndroid` 通过 Windows MAUI 构建；Docker stack 验证通过，Swagger 可访问 |
| 阶段 10：V1.2 媒体库体验 | 已完成，RC 已验证，待提交 | 本次提交 | 媒体时间线、详情、处理状态、相册 CRUD/成员管理/封面、失败重试 API 与迁移已完成；MAUI 媒体库时间线、相册、处理状态和预览页状态体验已接入；V1.2 RC 发布说明和测试记录已补齐 | 2026-05-14：后端 solution build 成功，0 警告 0 错误；`dotnet test` 通过，`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 101 个测试；MAUI Windows/Android 顺序构建通过，PASS 4 / WARN 0 / FAIL 0；Android Debug Signed APK 已生成到 `artifacts/verify-v12-rc-maui-apk/com.companyname.privateclouddrive.app-Signed.apk`；当前无 adb 设备，APK 安装与截图验收待设备可用后回填 |

## 最近验证记录

### 2026-05-18

- Private Backup MVP：存储用量页信任边界说明进入 Android 验收
  - 范围：MAUI `StorageUsagePage` 在存储健康卡片中新增“存储位置 / 恢复边界 / 隐私边界”三项；容量未配置时根据存储后端区分 FileSystem 与 AliyunOss 文案，避免将对象存储误描述为“按服务器磁盘可用空间备份”；读取失败态改为保守提示，不展示连接串、密钥、Token、服务器内部路径或存储内部标识。
  - 内部会诊：登录后页面验收最初被凭据问题阻塞后，按 Identity/Auth、Backend、QA/Release 视角复核；结论为不得暴力尝试密码，不得把启动截图当作登录后页面验收证据。随后经用户授权，仅对本地 Docker 验收库重置 admin 测试密码，并用 `/connect/token` 验证成功后补齐登录后页面验收。
  - `dotnet test PrivateCloudDrive.slnx --configuration Debug --no-restore`
    - 工作目录：`aspnet-core`
    - 结果：命令 0 退出；`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 107 个测试；`TestBase`、`Domain.Tests`、`Application.Tests` 当前无可发现测试。
    - 日志：`docs/validation/backend-tests-2026-05-18.log`。
  - `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None`
    - 工作目录：项目根目录
    - 结果：MAUI Android Debug APK 构建成功，0 个错误；存在既有 AndroidX NU1608 与 XA1037 警告。
    - 日志：`docs/validation/maui-android-build-2026-05-18.log`。
  - 后端健康：`docker compose ps` 显示 API、media-worker、PostgreSQL、Redis 运行；`http://localhost:8080/swagger/index.html` 返回 200。
  - Android 安装启动验收：安装最新 `com.companyname.privateclouddrive.app-Signed.apk` 后执行 `adb shell pm clear com.companyname.privateclouddrive.app` 清理数据并启动 App；前台 Activity 为 `com.companyname.privateclouddrive.app/crc644ff135ff239f5ce3.MainActivity`；logcat 未发现 `FATAL EXCEPTION` / AndroidRuntime 崩溃。
  - 截图证据：
    - `docs/validation/storage-trust-boundary-latest-2026-05-18.png`：首次安装启动到 PrivateCloudDrive 登录页，默认服务器为 `http://10.0.2.2:8080`，无黑屏、崩溃弹窗或系统错误覆盖。
    - `docs/validation/storage-trust-boundary-admin-login-2026-05-18.png`：授权重置本地验收 admin 密码后，App 成功登录并进入 `MSI_MEG.jpg` 文件详情页，证明登录链路已恢复。
    - `docs/validation/storage-trust-boundary-my-page-2026-05-18.png`：登录后“我的”页显示当前账号、在线状态、存储空间概览与“存储用量”入口。
    - `docs/validation/storage-trust-boundary-storage-page-2026-05-18.png`：登录后进入 StorageUsagePage，可见“存储位置 / 恢复边界 / 隐私边界”文案；页面只展示概念性存储位置与后端类型，不泄露连接串、密钥、Token、服务器绝对路径或对象存储 bucket 名。
  - 登录后 Android 验收：已执行 `adb shell pm clear com.companyname.privateclouddrive.app` 清理数据并重新启动；本地 `/connect/token` 返回 Bearer/refresh token（未输出 token 明文）；App 内用 admin 登录成功后进入 StorageUsagePage；`docs/validation/android-logcat-admin-login-2026-05-18.log` 与 `docs/validation/android-logcat-storage-page-2026-05-18.log` 未发现 `FATAL EXCEPTION` / AndroidRuntime 崩溃。
  - 验收结论：StorageUsagePage App 可见验收通过；“存储位置 / 恢复边界 / 隐私边界”展示符合信任边界预期，且未发现敏感配置泄露。

- Private Backup MVP：备份/恢复非破坏性演练入口与真实 storage volume 修正
  - 范围：新增 `scripts/run-backup-restore-drill.ps1`，一键执行“创建备份 → 校验 `manifest.json` / `postgres.dump` / `storage.tar.gz` → 恢复 dry-run → 生成 `docs/validation` 演练报告”；默认不复制 `.env`，不执行破坏性恢复。同步修正 `backup-local-stack.ps1` / `restore-local-stack.ps1`，从运行中 API 容器 `/app/storage` 挂载解析真实 Docker volume 名，并把 `storage.dockerVolume` 写入 manifest，避免 Compose project 前缀导致备份到空 volume。
  - 发现与修正：初次演练生成的 `storage.tar.gz` 仅 87 bytes；复核 API 容器挂载后确认实际 volume 为 Compose project 前缀后的 FileCenter storage volume，修正脚本后重新演练生成 `storage.tar.gz` 约 57 MB，包含 `/app/storage` 下文件负载。
  - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/run-backup-restore-drill.ps1`
    - 工作目录：项目根目录
    - 结果：PASS 14 / WARN 0 / FAIL 0；生成 PostgreSQL dump、真实 FileCenter storage archive、环境变量恢复清单与恢复 dry-run 报告；未复制 `.env.secret`，未覆盖任何目标数据。
    - 报告：`docs/validation/backup-restore-drill-20260518-193513.md`。
  - PowerShell 语法检查：`backup-local-stack.ps1`、`restore-local-stack.ps1`、`run-backup-restore-drill.ps1` 均通过 AST 解析。
  - Git 差异检查：`git diff --check` 通过；仅存在换行符提示，无 whitespace error。

### 2026-05-17

- Private Backup MVP：备份队列成功时间与失败优先摘要
  - 范围：MAUI 备份队列项新增 `CompletedAt`/`CompletedAtText`，完成后显示“完成时间”；队列摘要在存在失败任务时优先提示“有失败任务待重试”，无失败且存在完成任务时展示“上次成功 HH:mm”，帮助用户判断最近一次备份是否成功。
  - 多 Agent 复核：代码复核通过，确认 XAML `CompletedAtText` 绑定有效、重试上传会清空旧成功时间、失败提示优先级符合产品目标。
  - `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None --no-restore`
    - 工作目录：项目根目录
    - 结果：MAUI Android Debug APK 构建成功，0 个错误；存在既有 AndroidX NU1608/XA1037 警告。
  - Android 启动验收：安装最新 Debug APK 后执行 `adb shell pm clear com.companyname.privateclouddrive.app` 清理数据并启动 App；前台 Activity 为 `com.companyname.privateclouddrive.app/crc644ff135ff239f5ce3.MainActivity`，logcat 未发现 `FATAL EXCEPTION` / AndroidRuntime 崩溃。
  - 截图证据：`docs/validation/app-startup-queue-summary-2026-05-17.png`。截图确认最新包可正常进入 PrivateCloudDrive 登录页，无崩溃弹窗、黑屏或系统错误覆盖。

- Private Backup MVP：设置页存储信任边界说明
  - 范围：系统健康 DTO/API 新增存储位置说明、恢复备份范围和隐私边界说明；后端根据 FileSystem/AliyunOss Provider 输出安全可展示文案，不暴露本地绝对路径、OSS Bucket 名、连接串、AccessKey 或 token；MAUI 设置页系统健康卡片同步展示“数据存放/恢复备份/隐私边界”，未登录或健康摘要不可用时显示保守提示。
  - TDD：新增 `EfCoreFileCenterSystemHealthAppServiceTests.Should_Return_Provider_Aware_Backup_Scope_For_Aliyun_Oss`，先确认对象存储模式会因展示 Bucket 名和备份范围误导失败，再实现 Provider-aware 文案与 Bucket 名隐藏。
  - 多 Agent 复核：spec review 和 code review 均指出 Aliyun OSS 备份范围不能复用本地 `FileCenter 存储目录` 且 Bucket 名应避免在 App 展示；已按复核意见修正并补充对象存储测试。
  - `dotnet test test/PrivateCloudDrive.EntityFrameworkCore.Tests/PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~EfCoreFileCenterSystemHealthAppServiceTests`
    - 工作目录：`aspnet-core`
    - 结果：通过 3 个系统健康集成测试，覆盖默认 FileSystem 健康状态、媒体工具降级和 Aliyun OSS 存储/恢复边界文案。
  - `dotnet build aspnet-core/PrivateCloudDrive.slnx --no-restore`
    - 工作目录：项目根目录
    - 结果：后端解决方案构建成功，0 个警告，0 个错误。
  - `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None --no-restore`
    - 工作目录：项目根目录
    - 结果：MAUI Android Debug APK 构建成功，0 个错误；存在既有 AndroidX NU1608/XA1037 警告。
  - Android 启动验收：已启动 AVD `pixel_9_pro_-_api_36_0`，确认本地 Compose 后端 API/PostgreSQL/Redis 运行；安装 embedded-assemblies Debug APK 后执行 `adb shell pm clear com.companyname.privateclouddrive.app` 清理数据并启动 App。
  - 截图证据：`docs/validation/app-startup-2026-05-17.png`。截图确认 PrivateCloudDrive 登录页正常显示，包含“当前连接”、默认服务器 `http://10.0.2.2:8080`、后端地址输入框、“切换服务器”、“恢复默认”和账号登录表单；前台 Activity 为 `com.companyname.privateclouddrive.app/crc644ff135ff239f5ce3.MainActivity`，logcat 未发现 `FATAL EXCEPTION` / AndroidRuntime 崩溃。

### 2026-05-15

- 设置页系统健康摘要闭环
  - 范围：新增 `IFileCenterSystemHealthAppService`、`FileCenterSystemHealthAppService`、`/api/file-center/system-health/summary` 显式 HTTP 入口和 `FileCenterSystemHealthDto`，向已登录用户返回 API、存储 Provider、当前用户容量配额与安全诊断信息；MAUI `SettingsPage` 新增“系统健康”卡片，展示 API/存储状态、Provider 和诊断摘要。
  - TDD：先新增 `EfCoreFileCenterSystemHealthAppServiceTests.Should_Return_System_Health_Summary_For_Current_User` 并确认缺少 `IFileCenterSystemHealthAppService` 时编译失败，再实现应用服务与 DTO。
  - FFmpeg/FFprobe 增强：系统健康 DTO 和 MAUI 模型新增 `FfmpegStatus`、`FfprobeStatus`；后端根据媒体处理配置返回 FFmpeg/FFprobe 已配置/未配置诊断，任一媒体工具未配置时整体状态降级；设置页健康详情展示 API、存储、FFmpeg 和 FFprobe 四项状态，不暴露可执行文件物理路径或敏感配置。
  - DB/Redis 增强：系统健康 DTO 和 MAUI 模型新增 `DatabaseStatus`、`RedisStatus`；数据库状态复用容量统计 repository 查询成功作为可访问证据；Redis/分布式缓存通过 1 分钟 TTL 临时探针完成 Set/Get/Remove 验证；设置页健康详情同步展示 API、DB、Redis、存储、FFmpeg、FFprobe 六项状态，不暴露连接串、缓存 key secret 或基础设施敏感配置。
  - 存储磁盘空间增强：系统健康 DTO 和 MAUI 模型新增 `StorageDiskAvailableBytes`、`StorageDiskTotalBytes`；本地 FileSystem 存储通过 `DriveInfo` 读取存储根目录所在磁盘剩余/总空间，对象存储返回“不适用本地磁盘空间”；设置页诊断摘要展示“存储磁盘剩余 X / Y”，不暴露存储物理路径。
  - `dotnet test test/PrivateCloudDrive.EntityFrameworkCore.Tests/PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~EfCoreFileCenterSystemHealthAppServiceTests`
    - 结果：通过 2 个系统健康集成测试，覆盖默认 API/DB/Redis/存储/磁盘空间/媒体工具健康状态和媒体工具未配置降级。
  - `dotnet build PrivateCloudDrive.slnx --no-restore && dotnet test PrivateCloudDrive.slnx --no-build --filter EfCoreFileCenterSystemHealthAppServiceTests`
    - 工作目录：`aspnet-core`
    - 结果：后端解决方案构建成功，0 个警告，0 个错误；系统健康筛选测试通过 2 个。
  - `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0 -p:TargetFrameworks=net10.0-windows10.0.19041.0 -p:OutputPath=artifacts/verify-health-ffmpeg-maui-windows/`
    - 结果：MAUI Windows 构建成功，0 个警告，0 个错误。
  - `dotnet test test/PrivateCloudDrive.EntityFrameworkCore.Tests/PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --no-restore`
    - 结果：通过 106 个 EF 集成测试。
  - `dotnet build ../maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -p:TargetFrameworks=net10.0-android -p:OutputPath=../artifacts/verify-health-ffmpeg-maui-android/`
    - 工作目录：`aspnet-core`
    - 结果：MAUI Android 构建成功，0 个警告，0 个错误。
  - `dotnet build ../maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0 -p:TargetFrameworks=net10.0-windows10.0.19041.0 -p:OutputPath=../artifacts/verify-system-health-db-redis-maui-windows/`
    - 工作目录：`aspnet-core`
    - 结果：DB/Redis 健康展示同步后 MAUI Windows 构建成功，0 个警告，0 个错误。
  - `dotnet build ../maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -p:TargetFrameworks=net10.0-android -p:OutputPath=../artifacts/verify-system-health-db-redis-maui-android/`
    - 工作目录：`aspnet-core`
    - 结果：DB/Redis 健康展示同步后 MAUI Android 构建成功，0 个警告，0 个错误。
  - `dotnet build ../maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0 -p:TargetFrameworks=net10.0-windows10.0.19041.0 -p:OutputPath=../artifacts/verify-system-health-disk-maui-windows/`
    - 工作目录：`aspnet-core`
    - 结果：磁盘空间健康展示同步后 MAUI Windows 构建成功，0 个警告，0 个错误。
  - `dotnet build ../maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -p:TargetFrameworks=net10.0-android -p:OutputPath=../artifacts/verify-system-health-disk-maui-android/`
    - 工作目录：`aspnet-core`
    - 结果：磁盘空间健康展示同步后 MAUI Android 构建成功，0 个警告，0 个错误。

- V1.2 RC / V1.3 运维前置：本地栈备份与恢复演练说明
  - 范围：新增 `scripts/backup-local-stack.ps1` 与 `scripts/restore-local-stack.ps1`。备份脚本覆盖 PostgreSQL custom dump、`privateclouddrive_stack_storage` volume 归档、可选 Redis/MinIO/`.env` 处理和不含明文 secret 的 `manifest.json`；恢复脚本默认 dry-run，只有显式传入 `-ConfirmDestructiveRestore` 才会覆盖目标数据库与 storage volume；`docs/deployment.md` 补齐备份组成、恢复演练步骤、`.env` 敏感边界和 OSS bucket 额外备份责任。
  - PowerShell 语法检查：通过。
  - `powershell -NoProfile -ExecutionPolicy Bypass -File D:/Devs/Projects/Personal/PrivateCloudDrive/scripts/backup-local-stack.ps1 -OutputDirectory D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-backup-local-stack`
    - 结果：PASS 6 / WARN 1 / FAIL 0；已生成 `postgres.dump`、`storage.tar.gz` 和 `manifest.json`，Redis 按默认策略未备份并输出 WARN 提示；备份输出位于 ignored artifacts 目录，未进入 Git。
  - `powershell -NoProfile -ExecutionPolicy Bypass -File D:/Devs/Projects/Personal/PrivateCloudDrive/scripts/restore-local-stack.ps1 -BackupDirectory D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-backup-local-stack/20260515-141611`
    - 结果：dry-run PASS 6 / WARN 1 / FAIL 0；未改动任何数据，已展示破坏性恢复计划与确认开关。

### 2026-05-14

- V1.2 RC 发布候选质量闸门
  - 后端构建：`dotnet build /d/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core/PrivateCloudDrive.slnx -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-v12-rc-backend-build/` 成功，0 警告，0 错误。
  - 后端测试：`dotnet test /d/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core/PrivateCloudDrive.slnx --no-build -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-v12-rc-backend-build/ --logger "trx;LogFilePrefix=v12-rc-backend" --results-directory D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/test-results/v12-rc-backend` 通过；`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 101 个测试，其它测试项目当前没有可发现测试。
  - MAUI 顺序构建：`powershell -NoProfile -ExecutionPolicy Bypass -File D:/Devs/Projects/Personal/PrivateCloudDrive/scripts/verify-maui-build.ps1 -Configuration Debug` 通过，Windows 与 Android 目标均 PASS，汇总 PASS 4 / WARN 0 / FAIL 0。
  - Android APK：`dotnet publish` 已生成 Debug Signed APK，并复制到 `artifacts/verify-v12-rc-maui-apk/com.companyname.privateclouddrive.app-Signed.apk`。
  - 设备边界：`adb devices` 当前未列出设备或模拟器，因此未执行 APK 安装、启动截图和触控验收；后续在 Android 设备可用时按 `docs/testing.md` 的 V1.2 手动验收清单回填。
  - 文档：新增 `docs/release-notes-v1.2-rc.md`，并更新 `docs/testing.md` 与本进度记录。

- V1.2 RC 本地栈健康检查脚本修复与复验
  - 问题：`scripts/verify-local-stack.ps1` 在完整模式执行 `docker compose up -d --build` 时，Docker Compose 将正常构建进度写入 stderr；脚本级 `$ErrorActionPreference = "Stop"` 会把这些 native stderr 行提升为 `NativeCommandError`，导致尚未读取真实 `$LASTEXITCODE` 就提前失败。
  - 修复：`Invoke-External` 在捕获外部命令 stdout/stderr 时临时将 `$ErrorActionPreference` 调整为 `Continue`，再用真实退出码决定 PASS/FAIL，避免把 Compose 进度输出误判为脚本失败。
  - PowerShell 语法检查：通过。
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/verify-local-stack.ps1 -PreflightOnly`
    - 结果：PASS 10 / WARN 3 / FAIL 0；WARN 均为本地 `.env` 使用模板加密短语、默认 PostgreSQL 密码和 localhost `PUBLIC_URL`，不打印任何 secret。
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/verify-local-stack.ps1 -SkipStart`
    - 结果：PASS 19 / WARN 4 / FAIL 0；当前容器 PostgreSQL、Redis、db-migrator、API、media-worker、Swagger、`/app/storage`、ffmpeg、ffprobe 均可用。
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/verify-local-stack.ps1`
    - 结果：PASS 20 / WARN 3 / FAIL 0；脚本可正常启动/更新 Compose 栈并完成完整健康检查。

- MAUI 产品化 UI 第一轮落地收口
  - 范围：延续 `docs/ui-redesign-master-plan.md` 的“安静、可信、专业、内容优先”方向，收口 Login、Files、Settings、媒体、相册、上传、分享、回收站、日志等页面的未提交 UI 改造。
  - 设计系统：`Colors.xaml` / `Styles.xaml` 已形成专业蓝 + 中性灰色板、现代按钮、卡片、输入容器、标题/元信息字体层级，并保留旧 `Doodle*` key 作为兼容别名。
  - Android 输入框：`MauiProgram.cs` 移除 Delius 字体注册，并在 Android `Entry` / `Editor` handler 中清空原生背景，降低自定义输入容器内出现系统下划线/双边框的风险。
  - 页面清理：文件页、视频页、媒体处理页和设置页不再直接使用 `DoodleInk` 作为边框或进度色，改为设计系统中的 `Border` / `Primary` 语义色。
  - `grep -RIn "DeliusSwashCaps\|DoodleInk" maui/PrivateCloudDrive.App/Resources maui/PrivateCloudDrive.App/Views maui/PrivateCloudDrive.App/MauiProgram.cs`
    - 结果：仅剩 `Colors.xaml` 中 `DoodleInk` 兼容色定义，业务页面与启动代码不再引用 Delius/DoodleInk。
  - XAML 解析检查
    - 结果：18 个 Views/Styles XAML 文件解析通过。
  - `git diff --check`
    - 结果：未发现空白错误；仅输出 LF/CRLF 工作区换行提示。
  - `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64 -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-ui-windows/`
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-ui-android/`
    - 结果：成功，0 个警告，0 个错误。

### 2026-05-09

- V1.2 媒体库体验
  - 后端：新增 `/api/file-center/media/timeline`、`/{fileNodeId}/detail`、`processing-status`、`retry-processing`，新增 `/api/file-center/media/albums` 相册 CRUD、成员添加/移除和封面接口；新增 `MediaAlbum`、`MediaAlbumItem` 与 `20260509153342_AddedMediaAlbums` 迁移。
  - MAUI：Photos 页升级为媒体库时间线入口，支持全部/图片/视频筛选和月份分组；新增相册列表、相册详情、处理状态页；预览页可展示 Pending/Processing/Failed 状态并支持重新处理。
  - 测试：新增媒体时间线排序、TakenAt 优先、类型筛选、用户隔离、详情状态、处理状态、错误脱敏以及相册新增/去重/成员校验/删除不删文件/封面等 EF 集成测试。
  - `dotnet build .\PrivateCloudDrive.slnx`
    - 工作目录：`aspnet-core`
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet test .\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --no-restore`
    - 工作目录：`aspnet-core`
    - 结果：通过 91 个 EF 集成测试。
  - `dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0 -p:OutputPath=artifacts\verify-build\`
    - 工作目录：`maui/PrivateCloudDrive.App`
    - 结果：成功，0 个警告，0 个错误。默认输出目录构建被当前运行中的 `PrivateCloudDrive.App (75188)` 锁定，已改用隔离输出目录验证代码可构建。

- V1.1 文件管理体验
  - 后端：文件列表支持搜索、全盘搜索、节点类型筛选、媒体类型筛选和排序；新增容量统计显式路由 `/api/file-center/storage/usage`；新增批量节点 API `/api/file-center/nodes/batch/*`；个人分享列表现在返回已禁用和已过期状态。
  - MAUI：Files 页新增搜索、排序、类型/媒体筛选、全盘搜索开关和批量工具栏；Trash 页新增批量恢复和批量永久删除；Settings 页新增容量卡和“我的分享”入口；新增 Shares 页支持复制链接和禁用分享。
  - 测试：新增批量节点操作、分享列表状态和容量统计测试。
  - `dotnet build .\aspnet-core\PrivateCloudDrive.slnx`
    - 工作目录：仓库根目录
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet test .\aspnet-core\PrivateCloudDrive.slnx`
    - 工作目录：仓库根目录
    - 结果：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 79 个测试；其它测试项目当前没有可发现测试。
  - `.\scripts\verify-maui-build.ps1 -SkipAndroid`
    - 工作目录：仓库根目录
    - 结果：Windows MAUI 构建通过；Android 构建按参数跳过。
  - `docker compose config`
    - 工作目录：仓库根目录
    - 结果：Compose 配置展开成功。
  - `docker compose up -d --build`
    - 工作目录：仓库根目录
    - 结果：API、media-worker 和 db-migrator 镜像重建成功；PostgreSQL/Redis 复用运行中容器；db-migrator 成功退出；API 和 media-worker 启动。
  - `.\scripts\verify-docker-stack.ps1`
    - 工作目录：仓库根目录
    - 结果：PostgreSQL 和 Redis healthy；db-migrator ready；API 和 media-worker ready；Swagger `http://localhost:8080/swagger/index.html` 返回可用。

### 2026-05-08

- 阶段 8 Android WeChat SDK 原生授权接入
  - 后端契约：`WechatLoginSettingsDto` 透出公开 `Scope`，App 从 `/api/mobile-auth/wechat/settings` 获取 `AppId`、`Scope` 和平台公开配置；`AppSecret` 仍只存在后端配置。
  - MAUI Android：新增 `AndroidWechatPlatformAuthService`、`WechatAuthCallbackStore`、Java `WechatAuthBridge` 和 `.wxapi.WXEntryActivity`；通过官方 `com.tencent.mm.opensdk:wechat-sdk-android` 拉起 `SendAuth.Req`，回调后把 `code/state` 接入现有微信登录和绑定流程。
  - 平台边界：Windows/iOS 仍使用默认不可用实现；Android 真机授权仍需要微信开放平台正式移动应用、包名签名匹配、后端正式 `AppId/AppSecret`、设备安装微信并能访问 API。
  - `dotnet build .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-wechat-sdk-build\`
    - 工作目录：`aspnet-core`
    - 结果：成功，0 个警告，0 个错误；默认 `bin` 输出目录被正在运行的 `PrivateCloudDrive.HttpApi.Host` 锁定，因此使用隔离输出目录验证。
  - `dotnet test .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-wechat-sdk-test\`
    - 工作目录：`aspnet-core`
    - 结果：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 63 个测试；其它测试项目当前没有可发现测试。
  - `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64`
    - 工作目录：`maui/PrivateCloudDrive.App`
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android`
    - 工作目录：`maui/PrivateCloudDrive.App`
    - 结果：成功，0 个警告，0 个错误。

- MVP Core 内测收口
  - MAUI：新增 `AppText` 本地化文本入口，登录、文件、上传、详情、媒体、回收站、设置和操作日志页面改用统一中文/英文文案；Windows 默认 API 地址为 `http://localhost:8080`，Android 模拟器默认 API 地址为 `http://10.0.2.2:8080`；Android 开发构建允许 cleartext 访问本地 Compose API。
  - 导航：底部导航保留 Files、Photos、Videos、Uploads、Settings；Trash 改为从 Settings 页进入，符合 MVP 内测收口计划。
  - 文档：README、部署说明和测试说明已明确本地 Compose API 地址、Settings 进入 Trash、真实设备需改为局域网地址，以及 WeChat 仍为 V1 可选能力。
  - `dotnet build .\PrivateCloudDrive.slnx`
    - 工作目录：`aspnet-core`
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet test .\PrivateCloudDrive.slnx`
    - 工作目录：`aspnet-core`
    - 结果：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 63 个测试；其它测试项目当前没有可发现测试。
  - `dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64`
    - 工作目录：仓库根目录
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android`
    - 工作目录：仓库根目录
    - 结果：成功，0 个警告，0 个错误。
  - `docker compose config`
    - 工作目录：仓库根目录
    - 结果：成功展开 Compose 配置，包含 PostgreSQL、Redis、DbMigrator、API、media-worker 和 `privateclouddrive_stack_storage` 持久化 volume。
  - `.\scripts\verify-docker-stack.ps1`
    - 工作目录：仓库根目录
    - 结果：Docker CLI/Compose 正常；必需镜像存在；PostgreSQL 和 Redis healthy；DbMigrator exited 0；API 与 media-worker running；Swagger `http://localhost:8080/swagger/index.html` 返回 200。
  - Compose API MVP 探针
    - 覆盖：password grant、refresh_token grant、根目录列表、小文件上传、上传后列表命中、Range 下载、删除到回收站、回收站列表命中、恢复、再次删除并永久清理。
    - 结果：password grant 200 且有 access/refresh token；refresh grant 200；根目录列表 200；上传 200；Range 下载 206 且返回 8 bytes；删除到回收站 204；恢复 200；永久清理 204。探针输出未记录 access token、refresh token 或密码。
  - 真实设备/模拟器验收
    - 结果：未执行交互式 App 验收；`adb` 当前不在 PATH，无法直接驱动 Android 设备或模拟器。仍需按 `docs/testing.md` 阶段 6.5 在真实设备或可用模拟器上回填人工验收记录。
- Android 模拟器文件列表授权修复
  - 问题：Android 模拟器通过 `http://10.0.2.2:8080` 登录后，文件列表请求返回授权失败；API 日志显示 `/api/app/file-center-folders` 未通过 OpenIddict validation，随后 401 错误页又因缺少 UI bundle 资源转成 500，MAUI 端显示 `FileCenter request failed.`。
  - 修复：`PrivateCloudDriveHttpApiHostModule` 在配置了 `AuthServer:Authority` 时调用 `OpenIddictServerBuilder.SetIssuer(...)`，固定 token issuer，不再随 Android 请求 Host 变化。
  - 复现探针：修复前，用 `Host: 10.0.2.2:8080` 请求 `/connect/token` 得到 token 后，再请求 `/api/app/file-center-folders` 返回 500。
  - 验证：后端 `dotnet build .\PrivateCloudDrive.slnx` 成功；`dotnet test .\PrivateCloudDrive.slnx` 中 `PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 63 个测试；重建 Compose API/media-worker 后，同一 Android Host 头探针返回 token 200、文件列表 200。
  - 操作提示：模拟器中需要退出登录后重新登录，或清理 App 数据，以丢弃修复前签发的旧 token。
- 阶段 6.5 Android 模拟器内测验收完成
  - 设备：Android Emulator Pixel 9 Pro，API 36。
  - 后端提交：`97e2bec`。
  - 结果：用户确认 App 已成功运行，文件页可正常加载，MVP Core 内测验收通过；`docs/testing.md` 已回填执行记录。
  - 边界：iOS/真实设备验收未执行，不阻塞当前 MVP 内测版；后续面向外部分发或应用商店发布前再补充。

- 阶段 8 V1 微信登录可选接入
  - 后端：新增 `Authentication:WeChat` 配置、`WechatUserBinding` 实体和 EF 迁移、绑定票据缓存、WeChat code 交换服务、绑定/解绑应用服务与 HTTP 控制器、OpenIddict `urn:privateclouddrive:wechat` 自定义 grant。
  - 安全：默认禁用 WeChat；未配置时 `/api/mobile-auth/wechat/settings` 不返回 `AppSecret`；WeChat 登录、绑定、解绑失败只记录安全错误码，不记录 code、AppSecret 或 WeChat access token；登录、绑定和解绑基于 ABP 分布式缓存限流。
  - 测试：新增 `EfCoreWechatAuthAppServiceTests` 覆盖未绑定登录票据、绑定已有账号、禁止迁移绑定、解绑不移除密码登录、交换失败审计脱敏、DTO 无敏感字段和微信操作限流。
  - MAUI：登录页同时按后端 settings 和平台可用性显示 WeChat 按钮；设置页显示绑定状态并提供绑定/解绑入口；Android 已由后续 `AndroidWechatPlatformAuthService` 接入 WeChat SDK 授权桥接，Windows/iOS 仍由 `DefaultWechatPlatformAuthService` 报告不可用。
  - `dotnet build .\aspnet-core\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-build\`
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-test\`
    - 结果：通过 53 个 EF 集成测试。
  - `dotnet run --project .\PrivateCloudDrive.DbMigrator.csproj`
    - 工作目录：`aspnet-core/src/PrivateCloudDrive.DbMigrator`
    - 结果：成功应用 `20260508021008_AddedWechatUserBindings`；PostgreSQL 中确认 `AppMobileAuthWechatUserBindings` 表存在，`__EFMigrationsHistory` 命中该 migration。
  - 临时 API 探针：`http://127.0.0.1:5080/api/mobile-auth/wechat/settings` 返回 200 且 `isEnabled=false`、`appId=null`；`POST /connect/token` 的 password grant 返回 200 和 token；`grant_type=urn:privateclouddrive:wechat` 返回 400、`wechat_disabled`。
  - MAUI 目标框架构建：`dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android` 成功；`dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64` 成功。
  - 说明：仓库新增 `global.json` 固定 SDK `10.0.203`；当前 MAUI 默认多目标恢复会同时拉取 Android/iOS/MacCatalyst/Windows 图并命中本机缺失的 `Microsoft.NETCore.App.Runtime.Mono.win-x64 10.0.7`，因此本轮验证使用目标框架限定命令；不同目标框架构建需顺序执行，避免并行 restore 覆盖同一个 `obj/project.assets.json`。

- 阶段 5 MVP Core 部署与文档收尾复核
  - README：补充本地开发 Redis 依赖、`global.json` SDK 固定、MAUI target-scoped 顺序构建命令和微信登录可选边界。
  - Docker Compose：保留 PostgreSQL、Redis、DbMigrator、API、Media Worker 和可选 MinIO；API 容器新增 `WECHAT_*` 环境变量映射，默认关闭微信且不写死 `AppSecret`。
  - 部署说明：补充 WeChat 可选配置变量、后端密钥边界和真实 SDK/真机验收边界。
  - `docker compose config`
    - 工作目录：仓库根目录
    - 结果：成功展开 Compose 配置，包含 PostgreSQL、Redis、DbMigrator、API、Media Worker 和持久化 storage volume。
- 阶段 8 WeChat 绑定唯一性加固
  - 后端：新增 `FixedWechatUserBindingUniqueIndexes` 迁移，把 `WechatUserBinding` 唯一约束拆为 Host/Tenant 两组 PostgreSQL 部分唯一索引，避免可空 `TenantId` 让 `AppId + OpenId` 或 `UnionId` 绑定唯一性失效。
  - 测试：新增 EF 模型元数据测试，验证旧索引名不存在，Host/Tenant `AppId + OpenId` 与 `UnionId` 索引均为唯一索引并包含正确 filter。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~EfCoreWechatAuthAppServiceTests -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-wechat-index-test\`
    - 结果：通过 7 个 WeChat 相关 EF 测试。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-ef-after-wechat-index\`
    - 结果：通过 54 个 EF 集成测试。
  - `dotnet ef migrations list --project .\src\PrivateCloudDrive.EntityFrameworkCore\PrivateCloudDrive.EntityFrameworkCore.csproj --context PrivateCloudDrive.EntityFrameworkCore.PrivateCloudDriveDbContext --no-connect`
    - 工作目录：`aspnet-core`
    - 结果：成功列出 `20260508033000_FixedWechatUserBindingUniqueIndexes`，确认迁移元数据可被 EF Core 发现。
  - `dotnet run --project .\PrivateCloudDrive.DbMigrator.csproj`
    - 工作目录：`aspnet-core/src/PrivateCloudDrive.DbMigrator`
    - 环境：`ConnectionStrings__Default=Host=localhost;Port=5432;Database=PrivateCloudDrive;Username=privateclouddrive;Password=privateclouddrive;`，`Redis__Configuration=localhost:6379`
    - 结果：成功应用数据库迁移和种子。
  - PostgreSQL 直接确认：
    - `__EFMigrationsHistory` 命中 `20260508033000_FixedWechatUserBindingUniqueIndexes`。
    - `pg_indexes` 中 `AppMobileAuthWechatUserBindings` 存在 `UX_WechatUserBindings_Host_AppId_OpenId`、`UX_WechatUserBindings_Host_UnionId`、`UX_WechatUserBindings_Tenant_AppId_OpenId`、`UX_WechatUserBindings_Tenant_UnionId` 四个部分唯一索引。
- 阶段 8 MAUI WeChat 失败安全边界
  - MAUI：`SignInWithWechatCodeAsync` 不再在发起 WeChat token grant 前清理本地 Token；当后端返回 `wechat_binding_required` 或其他微信登录失败时，保留现有账号密码登录会话，只有微信登录成功才覆盖保存新 Token；登录页 WeChat 流程失败时不再清空已输入的账号密码。
  - `dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64`
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android`
    - 结果：成功，0 个警告，0 个错误。
- 阶段 8 WeChat 首次绑定密码失败策略
  - 后端：`BindExistingAsync` 的账号密码校验接入 `IdentityUserManager` lockout 支持；用户已锁定时拒绝绑定，密码错误时调用 `AccessFailedAsync` 并记录脱敏失败原因，密码正确后调用 `ResetAccessFailedCountAsync`。
  - 测试：新增 `Should_Count_Failed_Bind_Existing_Password_Attempts_Without_Consuming_Ticket`，验证错误密码增加 `AccessFailedCount`、不消费 `bindingTicket`、随后正确密码可绑定并重置失败计数。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~EfCoreWechatAuthAppServiceTests -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-wechat-lockout-test\`
    - 结果：通过 8 个 WeChat 相关 EF 测试。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-ef-after-wechat-lockout\`
    - 结果：通过 55 个 EF 集成测试。
- 阶段 8 WeChat 已绑定登录 lockout 策略
  - 后端：`LoginAsync` 在签发 WeChat 登录结果前检查 `IdentityUserManager.IsLockedOutAsync`；已锁定用户返回 `invalid_grant`，不签发 Token，不创建新的绑定票据，并记录 `user_locked_out` 审计。
  - 测试：新增 `Should_Prevent_Locked_Out_User_From_Login_With_Bound_Wechat`，验证已绑定微信用户被锁定后无法通过微信登录，并记录客户端、设备和失败原因。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~EfCoreWechatAuthAppServiceTests -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-wechat-login-lockout-test\`
    - 结果：通过 9 个 WeChat 相关 EF 测试。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-ef-after-wechat-login-lockout\`
    - 结果：通过 56 个 EF 集成测试。
- 阶段 8 WeChat 解绑审计补齐
  - 后端：`UnbindAsync` 在当前用户没有有效微信绑定时保持接口幂等返回，同时记录 `WeChatUnbind` 失败审计，失败原因 `wechat_binding_not_found`。
  - 测试：新增无绑定解绑审计覆盖，验证不抛错且会记录当前用户、动作、结果和失败原因。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~EfCoreWechatAuthAppServiceTests -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-wechat-unbind-audit-test\`
    - 结果：通过 10 个 WeChat 相关 EF 测试。
  - `dotnet test .\aspnet-core\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-ef-after-wechat-unbind-audit\`
    - 结果：通过 57 个 EF 集成测试。
- 阶段 8 WeChat 登录、绑定、解绑限流
  - 后端：新增 `DistributedCacheWechatAuthRateLimiter`，通过 `Authentication:WeChat:RateLimitWindowSeconds` 和 `RateLimitMaxAttempts` 控制微信登录、绑定当前账号、绑定已有账号和解绑的限流窗口；超限返回并审计 `wechat_rate_limited`。
  - 配置：Host、DbMigrator、Docker Compose、`.env.example` 和部署文档均补充 WeChat 限流参数；Compose 配置展开后确认 API 环境变量包含 `Authentication__WeChat__RateLimitWindowSeconds` 和 `Authentication__WeChat__RateLimitMaxAttempts`。
  - `dotnet test .\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~EfCoreWechatAuthAppServiceTests -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-wechat-rate-limit-test\`
    - 工作目录：`aspnet-core`
    - 结果：通过 13 个 WeChat 相关 EF 测试。
  - `dotnet test .\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-ef-after-wechat-rate-limit\`
    - 工作目录：`aspnet-core`
    - 结果：通过 60 个 EF 集成测试。
  - `dotnet test .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-sln-test-after-wechat-rate-limit-rerun\`
    - 工作目录：`aspnet-core`
    - 结果：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 60 个测试；其它测试项目当前没有可发现测试。
  - `dotnet build .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-build-after-wechat-rate-limit\`
    - 工作目录：`aspnet-core`
    - 结果：成功，0 个警告，0 个错误。
  - `docker compose config`
    - 工作目录：仓库根目录
    - 结果：成功展开 Compose 配置，新增 WeChat 限流环境变量映射有效。
  - JSON 配置检查
    - 命令：逐文件 `Get-Content -Raw | ConvertFrom-Json`
    - 结果：`HttpApi.Host` 与 `DbMigrator` 的 `appsettings.json` 均解析成功。
- 阶段 8 WeChat 真实设备验收清单
  - 文件：`docs/testing.md`
  - 覆盖：后端 WeChat 配置、Android/iOS 授权入口、用户取消授权、未绑定首次登录、绑定已有账号、已绑定微信登录、已登录绑定、解绑、锁定用户、限流和验收证据记录。
  - 结果：已追加人工执行步骤和预期结果；Android SDK 桥接已接入，正式 AppId/AppSecret、Android 签名、iOS SDK/URL Scheme 和真机执行结果仍待后续回填。
- 阶段 5 到阶段 8 完成度审计
  - 文件：`docs/completion-audit.md`
  - 覆盖：MVP Core 收尾、MVP 体验与认证深化、V1 分享标签日志、V1 微信登录要求到证据映射，以及未完成项和不能作为完成证明的代理信号。
  - 结果：审计结论为目标尚未完成；阶段 6.5 真机验收、阶段 8.2 微信凭据/真机验收和 iOS SDK 仍是阻塞项；当时的阶段 Git 提交阻塞项已由后续 `de2c6f9` 补齐。
- 阶段提交整理准备
  - 文件：`.gitignore`、`docs/commit-plan.md`
  - 覆盖：忽略本地运行时 `App_Data` 存储，按阶段 5 到阶段 8 拆分建议提交批次，列出预提交验证命令和跨阶段改动风险。
  - 结果：`App_Data` 已被 Git 识别为 ignored；提交计划已补充，后续已通过 `de2c6f9` 完成 checkpoint 提交。
- 预提交验证刷新
  - `dotnet build .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-build-before-commit\`
    - 工作目录：`aspnet-core`
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet test .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-test-before-commit\`
    - 工作目录：`aspnet-core`
    - 结果：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 60 个测试；其它测试项目当前没有可发现测试。
  - `dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64`
    - 工作目录：仓库根目录
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android`
    - 工作目录：仓库根目录
    - 结果：成功，0 个警告，0 个错误。
  - `docker compose config`
    - 工作目录：仓库根目录
    - 结果：成功展开 Compose 配置。
  - `git diff --check`
    - 工作目录：仓库根目录
    - 结果：未发现空白错误；只输出 LF/CRLF 工作区换行提示。
  - `git ls-files --others --exclude-standard`
    - 工作目录：仓库根目录
    - 结果：未跟踪文件中未发现 `App_Data`、`bin/`、`obj/`、`artifacts`、图片、视频、日志或二进制构建产物。
- 阶段 5 到阶段 8 收尾提交
  - 提交：`de2c6f9 Checkpoint staged PrivateCloudDrive features`
  - 范围：阶段 5 MVP Core 收尾、阶段 6 体验/认证深化、阶段 7 分享标签日志、阶段 8 微信后端与 MAUI 骨架，以及配套文档、迁移、测试和忽略规则。
  - 仓库状态：提交后 `git status --short` 为空，当前无未提交工作区变更。
- 阶段 6 账号密码登录失败限流
  - 提交：`bce5e4e Add password login rate limiting`
  - 后端：新增 `MobileAuth:LoginRateLimit` 配置、账号密码登录失败分布式缓存限流服务，以及 OpenIddict password grant token endpoint 前置检查和失败计数处理；限流按用户名和请求 IP 双维度生效，成功登录会清理用户名维度失败计数。
  - 配置：Host `appsettings.json`、Docker Compose、`.env.example` 和部署文档均补充 `PASSWORD_LOGIN_RATE_LIMIT_*` 变量；Compose 配置展开确认 API 容器包含 `MobileAuth__LoginRateLimit__*` 映射。
  - 测试：新增 `EfCorePasswordLoginRateLimiterTests` 覆盖用户名限流、IP 限流和成功后用户名计数清理。
  - `dotnet test .\test\PrivateCloudDrive.EntityFrameworkCore.Tests\PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~EfCorePasswordLoginRateLimiterTests -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-password-rate-limit-test-rerun\`
    - 工作目录：`aspnet-core`
    - 结果：通过 3 个账号密码登录限流测试。
  - `dotnet build .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-build-password-rate-limit-rerun\`
    - 工作目录：`aspnet-core`
    - 结果：成功，0 个警告，0 个错误。
  - `dotnet test .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-test-password-rate-limit\`
    - 工作目录：`aspnet-core`
    - 结果：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 63 个测试；其它测试项目当前没有可发现测试。
  - `docker compose config`
    - 工作目录：仓库根目录
    - 结果：成功展开 Compose 配置，新增账号密码登录限流环境变量映射有效。
  - 临时 API 账号密码登录限流探针
    - 工作目录：`aspnet-core/src/PrivateCloudDrive.HttpApi.Host`
    - 环境：`ASPNETCORE_URLS=http://127.0.0.1:5081`，`MobileAuth__LoginRateLimit__MaxFailedAttempts=2`，`Redis__Configuration=localhost:6379,defaultDatabase=14`
    - 结果：同一用户名连续两次错误密码返回 `invalid_grant`，第三次在 token endpoint 前置检查中返回 `password_login_rate_limited`；探针结束后已停止临时 API 进程。

- `dotnet build .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-build\`
  - 工作目录：`aspnet-core`
  - 结果：成功，0 个警告，0 个错误。
  - 说明：普通 `bin\Debug` 构建被正在运行的 `PrivateCloudDrive.HttpApi.Host` 进程锁定，隔离输出目录用于验证当前代码可编译。
- `dotnet test .\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-test\`
  - 工作目录：`aspnet-core`
  - 结果：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 47 个测试；其它测试项目当前没有可发现测试。
- `dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。
- `dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-android`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。
- 任务 6.2 MAUI 页面状态复核
  - 覆盖：启动页、登录页、文件首页、上传队列页、文件详情页、图片/视频预览页、回收站页、设置页。
  - 结果：页面具备空、加载、错误或状态提示；文件首页可进入图片/视频预览，普通文件进入详情页；上传队列显示 waiting/uploading/failed/completed 状态；回收站错误提示包含同名冲突处理线索。
- `dotnet run --project .\PrivateCloudDrive.DbMigrator.csproj`
  - 工作目录：`aspnet-core/src/PrivateCloudDrive.DbMigrator`
  - 环境：`ConnectionStrings__Default=Host=localhost;Port=5432;Database=PrivateCloudDrive;Username=privateclouddrive;Password=privateclouddrive;`，`Redis__Configuration=localhost:6379`
  - 结果：成功执行迁移和种子，`OpenIddictApplications` 中 `PrivateCloudDrive_App` 已包含 `gt:password` 与 `gt:refresh_token`。
- 临时 API 探针：`http://127.0.0.1:5080/.well-known/openid-configuration` 与 `POST /connect/token`
  - 工作目录：`aspnet-core/src/PrivateCloudDrive.HttpApi.Host`
  - 结果：`/.well-known/openid-configuration` 返回 200；使用错误账号密码请求 `grant_type=password` 返回 `invalid_grant` 和 `Invalid username or password!`。
  - 说明：默认 Redis 逻辑库中曾保留旧 OpenIddict 客户端缓存，临时探针使用 `Redis__Configuration=localhost:6379,defaultDatabase=15` 验证数据库种子后的真实行为；现有运行中的 Compose API 若仍使用旧缓存，需要重启或刷新对应缓存后再验收新登录链路。
- 临时 API Token 生命周期探针：`POST /connect/token` 与 `POST /connect/revocation`
  - 工作目录：`aspnet-core/src/PrivateCloudDrive.HttpApi.Host`
  - 结果：`admin` 账号密码登录返回 access token 与 refresh token；`grant_type=refresh_token` 返回新 access token；`/connect/revocation` 返回 200。
  - 说明：验证输出只记录布尔状态、token 类型和 HTTP 状态，不输出 access token 或 refresh token。
- `dotnet ef migrations add AddedMobileAuthAuditLogs --project .\src\PrivateCloudDrive.EntityFrameworkCore\PrivateCloudDrive.EntityFrameworkCore.csproj --context PrivateCloudDrive.EntityFrameworkCore.PrivateCloudDriveDbContext --no-build`
  - 工作目录：`aspnet-core`
  - 结果：生成 `AppMobileAuthAuditLogs` 表迁移。
- `dotnet run --project .\PrivateCloudDrive.DbMigrator.csproj`
  - 工作目录：`aspnet-core/src/PrivateCloudDrive.DbMigrator`
  - 环境：`ConnectionStrings__Default=Host=localhost;Port=5432;Database=PrivateCloudDrive;Username=privateclouddrive;Password=privateclouddrive;`，`Redis__Configuration=localhost:6379`
  - 结果：成功应用移动端认证审计迁移。
- 临时 API 移动端认证审计探针：`POST /api/mobile-auth/audit-logs` 与 `GET /api/mobile-auth/audit-logs`
  - 工作目录：`aspnet-core/src/PrivateCloudDrive.HttpApi.Host`
  - 结果：匿名记录审计日志返回 204；管理员 Bearer Token 查询返回 200，并返回探针日志。
- 任务 6.5 真实设备 MVP Core 手动验收清单
  - 文件：`docs/testing.md`
  - 覆盖：Android/iOS 账号密码登录、登录失败、Token 刷新、上传、图片预览、视频播放、回收站、退出登录和平台差异。
  - 结果：已追加人工执行步骤和预期结果；真实设备执行结果待人工验收后回填。
- 任务 7.4 操作日志查询
  - 后端：新增操作日志查询契约、权限、应用服务和 `/api/operation-logs` HTTP 入口，聚合 ABP 审计动作、ABP 安全日志和 MobileAuth 审计日志。
  - 测试：`EfCoreOperationLogsAppServiceTests` 覆盖移动认证日志聚合查询、时间范围过滤和查询契约脱敏字段。
  - 临时 API 探针：`POST /api/mobile-auth/audit-logs` 返回 204；管理员 Bearer Token 查询 `/api/operation-logs?Source=MobileAuth&Action=PasswordLogin&UserName=operation-probe-*` 返回 200 并命中唯一探针日志。
- V1 分享、标签和收藏移动端入口
  - 后端：新增 `/api/file-center/shares`、`/api/file-center/tags`、`/api/file-center/nodes/{id}/tags/{tagId}` 和 `/api/file-center/nodes/{id}/favorite` 显式路由。
  - MAUI：文件详情页新增收藏切换、标签创建/绑定和创建分享链接；文件列表新增 More 入口，图片和视频也可进入详情操作。
  - 临时 API 探针：创建测试文件夹成功；收藏设置成功；标签创建和绑定返回 204；分享创建成功；分享、标签和测试文件夹清理均返回 204。
- V1 操作日志移动端展示入口
  - MAUI：Settings 页新增 Operation logs 入口，新增操作日志列表页，展示来源、操作类型、结果、用户、时间和摘要。
  - 结果：`dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0` 成功；`dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-android` 成功。
- V1 图片/视频媒体库入口
  - 后端：新增 `/api/file-center/media/images` 和 `/api/file-center/media/videos`，支持当前用户图片、视频、标签和收藏筛选。
  - MAUI：新增 Photos 与 Videos 底部导航页，图片页使用缩略图网格，视频页使用封面/文件列表并可进入媒体预览。
  - 测试：`EfCoreFileCenterMediaLibraryAppServiceTests` 覆盖图片/视频分离和收藏媒体筛选。
  - 临时 API 探针：管理员 Bearer Token 查询 `/api/file-center/media/images?MaxResultCount=5` 与 `/api/file-center/media/videos?MaxResultCount=5` 均返回 200。
- V1 管理员管理所有分享
  - 后端：`/api/file-center/shares/all` 支持管理员查看租户内所有分享，`DELETE /api/file-center/shares/all/{id}` 支持管理员禁用任意分享。
  - 测试：`Should_Allow_Admin_To_Manage_All_Shares` 覆盖跨用户分享列表、管理员禁用和公开访问失效。
  - 临时 API 探针：创建测试文件夹和分享成功；管理员全量列表命中该分享；禁用返回 204；禁用后列表显示 `IsEnabled=false`；测试文件夹回收站删除和永久删除均返回 204。

### 2026-05-07

- `dotnet build .\PrivateCloudDrive.slnx`
  - 工作目录：`aspnet-core`
  - 结果：成功，0 个警告，0 个错误。
- `dotnet test .\PrivateCloudDrive.slnx --no-build`
  - 工作目录：`aspnet-core`
  - 结果：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 38 个测试；其它测试项目当前没有可发现测试。
- `docker compose config`
  - 工作目录：仓库根目录
  - 结果：成功展开 Compose 配置，包含 PostgreSQL、Redis、DbMigrator、API、媒体 Worker 和可选 MinIO profile；Redis 镜像调整为当前本机可用的 `redis:7-alpine`。
- `docker compose up -d --build`
  - 工作目录：仓库根目录
  - 结果：未完成真实启动验收。首次执行因 Docker Desktop 未配置 HTTPS 代理，无法从 Docker Hub 拉取 `redis:8-alpine`；改为 `redis:7-alpine` 后再次执行 10 分钟超时，当前环境仍无法完成后端镜像构建/拉取验证。
- `docker compose up -d postgres redis`
  - 工作目录：仓库根目录
  - 结果：成功启动完整部署 Compose 中的 PostgreSQL 和 Redis；`privateclouddrive` 数据库账号连接成功，Redis `PING` 返回 `PONG`。
- `dotnet run`
  - 工作目录：`aspnet-core/src/PrivateCloudDrive.DbMigrator`
  - 结果：在 Compose PostgreSQL/Redis 依赖上成功执行迁移和种子。
- `dotnet run --no-build`
  - 工作目录：`aspnet-core/src/PrivateCloudDrive.HttpApi.Host`
  - 结果：在 Compose PostgreSQL/Redis 依赖上成功启动宿主机 API；`/swagger/index.html`、`/api/abp/application-configuration`、`/.well-known/openid-configuration` 均返回 200。
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-docker-stack.ps1 -PreflightOnly`
  - 工作目录：仓库根目录
  - 结果：Docker CLI、Docker Compose 和 Compose 配置检查通过；脚本正确报告缺少 `mcr.microsoft.com/dotnet/sdk:10.0` 与 `mcr.microsoft.com/dotnet/aspnet:10.0`，用于继续完整栈验收。
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-docker-stack.ps1`
  - 工作目录：仓库根目录
  - 结果：成功构建 `privateclouddrive-api`、`privateclouddrive-media-worker`、`privateclouddrive-db-migrator` 镜像；PostgreSQL/Redis 健康；DbMigrator 退出码 0；API 和 Media Worker 运行；Swagger `http://localhost:8080/swagger/index.html` 返回 200。
- `dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。
- `dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-android`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。

### 2026-05-08 V0.2 产品化 UI 基线

- 新增 `docs/product-ui-baseline.md`
  - 范围：明确 V0.2 “私有部署内测版”的 UI 产品原则、信息架构、视觉基线、核心页面基线、组件规则和验收标准。
  - 边界：不新增后端业务 API，不把微信登录作为 V0.2 阻塞项，不设计营销首页。
- MAUI 产品化 UI 第一轮收口
  - 文件：`LoginPage.xaml`、`FilesPage.xaml`、`SettingsPage.xaml`、`TrashPage.xaml`。
  - 结果：登录页强化产品标识和本地 API 诊断信息；文件页将上传、新建文件夹、刷新放回页面内核心操作区；设置页将回收站提升为明确入口；回收站页补充页面内刷新/清空和正式空状态。
- `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。
- `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。

### 2026-05-08 图片详情预览

- MAUI 图片详情页补充图片显示
  - 文件：`FileDetailsPage.xaml`、`FileDetailsPage.xaml.cs`、`AppText.cs`。
  - 结果：当详情页接收 `Image` 类型文件时，页面顶部显示“图片预览”区域；优先加载缩略图，缩略图不可用时回退加载原图内容；失败时展示错误和重试入口。
- MAUI 图片详情页预览样式调整
  - 文件：`FileDetailsPage.xaml`、`AppText.cs`。
  - 结果：移除预览区域标题，图片容器取消内边距并使用 `AspectFill` 铺满区域；加载和失败状态保留居中提示。
- MAUI 图片详情页预览容器贴边
  - 文件：`FileDetailsPage.xaml`。
  - 结果：图片预览区域移除外层卡片、圆角和页面左右边距，作为全宽媒体区展示；文件信息和操作区仍保留正常详情页边距。
- `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。
- `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。
- `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android -t:Run`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功重新部署并启动 Android 模拟器 App，`adb shell pidof com.companyname.privateclouddrive.app` 返回进程号 `20462`。

### 2026-05-08 主 Tab 标题去重

- MAUI 主 Tab 页面隐藏 Shell 顶部标题栏
  - 文件：`FilesPage.xaml`、`PhotosPage.xaml`、`VideosPage.xaml`、`UploadsPage.xaml`、`SettingsPage.xaml`。
  - 结果：主页面只保留页面内部标题，不再出现 Shell 标题和页面标题重复；文件首页根目录下隐藏重复的当前路径标题，进入子文件夹后才显示返回和路径。
- MAUI 上传页操作入口迁移
  - 文件：`UploadsPage.xaml`。
  - 结果：原导航栏 `Clear Done` 移到页面标题右侧，避免隐藏 Shell 标题栏后丢失操作入口。
- `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。
- `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功，0 个警告，0 个错误。
- `dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android -t:Run`
  - 工作目录：`maui/PrivateCloudDrive.App`
  - 结果：成功重新部署并启动 Android 模拟器 App，`adb shell pidof com.companyname.privateclouddrive.app` 返回进程号 `21062`。

### 2026-05-08 第一阶段部署文档

- 新增 `docs/phase-1-deployment.md`
  - 范围：固化第一阶段“本机 Docker Compose + Android 模拟器内测”的部署步骤、环境要求、后端启动、MAUI 构建运行、手动验收、常见问题和清理方式。
  - 边界：不覆盖微信登录、真实手机局域网访问、生产 HTTPS、MinIO 对象存储或多节点部署。
- 更新 `README.md`
  - 结果：目录结构中补充第一阶段部署文档入口。

## 下一步

- 下一阶段产品目标为 V0.2 私有部署内测版：围绕正式 UI 基线、真实数据集、私有部署稳定性和内测问题闭环继续收口。
- 微信登录继续作为 V1 可选能力，不作为 V0.2 阻塞项。
- 后续阶段完成后必须先验证对应构建/测试，再提交 Git，并单独更新本进度文档。
