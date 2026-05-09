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
| HTTP 控制器 | 文件下载和缩略图 Range 响应头、上传表单参数传递 |
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

MAUI 顺序构建验证：

```powershell
.\scripts\verify-maui-build.ps1 -SkipAndroid
.\scripts\verify-maui-build.ps1 -SkipWindows
```

Windows 和 Android 目标必须顺序构建，避免多目标 restore/build 同时解析本机未安装的平台 workload 或 runtime。若当前机器只具备 Windows MAUI 环境，先执行 `-SkipAndroid`；Android 构建和真机验收在具备 Android SDK/JDK/workload 的环境中回填结果。

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

V1.1 文件管理体验验证：

```powershell
dotnet build .\aspnet-core\PrivateCloudDrive.slnx
dotnet test .\aspnet-core\PrivateCloudDrive.slnx
.\scripts\verify-maui-build.ps1 -SkipAndroid
```

2026-05-09 执行结果：后端 build 成功；`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 79 个测试；MAUI Windows 构建通过，Android 按参数跳过；`docker compose config`、`docker compose up -d --build` 和 `.\scripts\verify-docker-stack.ps1` 验证通过，Swagger 可访问。

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
