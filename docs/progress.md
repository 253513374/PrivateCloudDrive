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
| 阶段 9：V1.1 文件管理体验 | 已完成，待提交 | 工作区待提交 | 文件列表搜索、排序、类型/媒体筛选、批量删除/恢复/永久删除/移动/收藏、容量统计、我的分享管理页，以及后端 API 与 MAUI 客户端对接已完成 | 2026-05-09：`dotnet build .\aspnet-core\PrivateCloudDrive.slnx` 成功；`dotnet test .\aspnet-core\PrivateCloudDrive.slnx` 通过 79 个 EF 集成测试；`.\scripts\verify-maui-build.ps1 -SkipAndroid` 通过 Windows MAUI 构建；Docker stack 验证通过，Swagger 可访问 |

## 最近验证记录

### 2026-05-09

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
