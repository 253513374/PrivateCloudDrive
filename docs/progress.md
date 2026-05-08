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
| 阶段 6：MVP Core 产品体验与账号密码认证深化 | 进行中 | `de2c6f9`, `bce5e4e` | 任务 6.1 MAUI 设计系统、任务 6.2 MVP Core 页面状态、任务 6.3 账号密码登录/Refresh Token/撤销端点、任务 6.4 移动端认证审计和账号/IP 双维度登录失败限流已落地并提交；任务 6.5 真实设备手动验收清单和执行记录模板已补充，真实设备执行尚未完成 | 2026-05-08：后端 build 成功；后端测试通过 63 个 EF 集成测试；MAUI Windows/Android 构建成功；临时 API password grant、refresh_token、revocation 和 mobile auth audit 探针通过；`docs/testing.md` 已追加移动端验收清单和结果记录模板 |
| 阶段 7：V1 分享、标签、收藏与操作日志 | 已完成 | `bb654ee`, `4f4f6a1`, `158cbc3`, `de2c6f9` | 分享链接、公开访问与密码校验、管理员管理所有分享、标签管理、收藏筛选、图片/视频媒体库、操作日志查询后端与 HTTP 入口已实现；MAUI 文件详情、图片页、视频页和操作日志页已接入对应入口并随收尾提交 | 2026-05-08：后端测试通过 60 个 EF 集成测试；临时 API 已验证 `/api/operation-logs`、分享/标签/收藏、`/api/file-center/media/images`、`/api/file-center/media/videos` 和 `/api/file-center/shares/all`；MAUI Windows/Android 构建通过 |
| 阶段 8：V1 微信登录可选接入 | 进行中 | `de2c6f9` | 后端 WeChat 配置、`WechatUserBinding`、绑定/解绑接口、绑定票据、OpenIddict 自定义 grant、审计记录和 MAUI 登录/设置页入口骨架已实现并提交；`WechatUserBinding` PostgreSQL Host/Tenant 唯一索引已加固；首次绑定已有账号和已绑定微信登录均对齐 Identity lockout；登录、绑定和解绑已接入分布式缓存限流；解绑审计已覆盖无绑定场景；真实 WeChat SDK 原生授权仍待 AppId/AppSecret 与平台审核后接入 | 2026-05-08：后端 build 通过；EF 集成测试通过 60 个；DbMigrator 已应用 `AddedWechatUserBindings` 与 `FixedWechatUserBindingUniqueIndexes`；临时 API 探针验证 WeChat disabled、password grant 和 custom grant fail-closed；MAUI Windows/Android 目标框架构建通过 |

## 最近验证记录

### 2026-05-08

- 阶段 8 V1 微信登录可选接入
  - 后端：新增 `Authentication:WeChat` 配置、`WechatUserBinding` 实体和 EF 迁移、绑定票据缓存、WeChat code 交换服务、绑定/解绑应用服务与 HTTP 控制器、OpenIddict `urn:privateclouddrive:wechat` 自定义 grant。
  - 安全：默认禁用 WeChat；未配置时 `/api/mobile-auth/wechat/settings` 不返回 `AppSecret`；WeChat 登录、绑定、解绑失败只记录安全错误码，不记录 code、AppSecret 或 WeChat access token；登录、绑定和解绑基于 ABP 分布式缓存限流。
  - 测试：新增 `EfCoreWechatAuthAppServiceTests` 覆盖未绑定登录票据、绑定已有账号、禁止迁移绑定、解绑不移除密码登录、交换失败审计脱敏、DTO 无敏感字段和微信操作限流。
  - MAUI：登录页同时按后端 settings 和平台可用性显示 WeChat 按钮；设置页显示绑定状态并提供绑定/解绑入口；当前 `DefaultWechatPlatformAuthService` 为原生 SDK 占位实现，默认报告不可用，因此未接入真实 WeChat SDK 的构建不会显示绑定/登录按钮。
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
  - 结果：已追加人工执行步骤和预期结果；真实 WeChat SDK、正式 AppId/AppSecret、平台签名/URL Scheme 和真机执行结果仍待后续回填。
- 阶段 5 到阶段 8 完成度审计
  - 文件：`docs/completion-audit.md`
  - 覆盖：MVP Core 收尾、MVP 体验与认证深化、V1 分享标签日志、V1 微信登录要求到证据映射，以及未完成项和不能作为完成证明的代理信号。
  - 结果：审计结论为目标尚未完成；阶段 6.5 真机验收、阶段 8.2 真实 WeChat SDK/凭据/真机验收仍是阻塞项；当时的阶段 Git 提交阻塞项已由后续 `de2c6f9` 补齐。
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

## 下一步

- 阶段 8 后续需要接入真实 WeChat Android/iOS SDK 或平台适配实现，使用正式 AppId/AppSecret 验证 code 获取、绑定已有账号和已绑定 WeChat 登录端到端流程。
- 阶段 6 后续需要在真实 Android/iOS 设备上执行 MVP Core 手动验收清单，并回填体验问题或验收结论。
- 后续阶段完成后必须先验证对应构建/测试，再提交 Git，并单独更新本进度文档。
