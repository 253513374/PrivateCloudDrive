# PrivateCloudDrive V1.0 RC Release Notes

发布日期：待定
发布状态：Release Candidate
产品形态：面向个人、家庭和小团队的私有部署移动优先云盘 + 媒体库

## 1. 产品定位

PrivateCloudDrive V1.0 RC 的目标不是继续扩展大功能，而是把现有 MVP 能力收口为可安装、可验证、可长期使用的私有云盘发布候选版本。

核心价值：

- 把自己的服务器、NAS、迷你主机或开发机变成可被手机稳定访问的私有文件中心。
- 支持文件、图片、视频的基础管理与移动端访问。
- 通过 Docker Compose 降低私有部署门槛。
- 通过健康检查、构建脚本和发布验收清单降低发布风险。

## 2. 本版包含能力

| 范围 | 能力 |
| --- | --- |
| 文件管理 | 文件夹、文件列表、上传、下载、移动、重命名、删除、回收站、恢复、永久删除 |
| 上传下载 | 小文件上传、大文件分片上传、断点分片查询、HTTP Range 下载 |
| 媒体能力 | 图片缩略图、视频封面、视频元数据、图片/视频媒体库入口 |
| 分享与整理 | 分享链接、分享禁用、标签、收藏 |
| 认证 | 账号密码登录、Refresh Token、登录失败限流、移动认证审计 |
| 外部登录 | 微信/Google/GitHub 可选接入边界；默认不强制启用 |
| 运维 | Docker Compose 部署、PostgreSQL、Redis、media-worker、健康检查脚本 |
| 客户端 | .NET MAUI Windows/Android 构建验证；Android 真实设备主链路待发布前回填 |

## 3. 本版明确不包含

- NAS OS、RAID、ZFS、Btrfs、磁盘池管理。
- SMB、NFS、AFP 等 NAS 文件协议。
- 桌面同步客户端。
- Office 在线协同编辑。
- AI 相册、AI 搜索、复杂图像识别。
- 多节点高可用、Kubernetes 部署。
- 企业级组织架构、审批流、复杂部门权限。

## 4. 部署前置条件

| 项目 | 要求 |
| --- | --- |
| Docker | Docker Desktop 或 Docker Engine 可用 |
| Compose | `docker compose` 可用 |
| .NET | .NET 10 SDK，可构建后端和 MAUI 项目 |
| MAUI | Windows 构建需要 Windows MAUI workload；Android 构建需要 Android workload/JDK/SDK |
| 存储 | Docker volume `privateclouddrive_stack_storage` 必须纳入备份范围 |
| 配置 | `.env` 必须从 `.env.example` 复制并替换默认密码和加密短语 |
| 移动端 | Android 真机验收需要设备可访问的 `PUBLIC_URL` |

## 5. 发布验收清单

发布前必须逐项回填结果。不要在验收记录中写入密码、access token、refresh token、OAuth code、client secret、provider token 或连接字符串。

| 验收项 | 命令/动作 | 预期 | 结果 |
| --- | --- | --- | --- |
| Git 状态确认 | `git status --short` | 只包含本次预期变更；敏感配置不进入提交 | 待执行 |
| 后端构建 | `dotnet build aspnet-core/PrivateCloudDrive.slnx --no-restore` | 构建通过 | 待执行 |
| 后端测试 | `dotnet test aspnet-core/PrivateCloudDrive.slnx --no-restore` | 测试通过 | 待执行 |
| Compose 配置 | `docker compose config` | 配置展开通过 | 待执行 |
| 本地栈预检查 | `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-local-stack.ps1 -PreflightOnly` | 输出 PASS/WARN/FAIL 汇总；无 FAIL | 待执行 |
| 本地栈完整检查 | `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-local-stack.ps1` | PostgreSQL/Redis/API/media-worker/Swagger/storage/ffmpeg 全部可用 | 待执行 |
| MAUI Windows 构建 | `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-maui-build.ps1 -SkipAndroid` | Windows 构建通过 | 待执行 |
| MAUI Android 构建 | `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-maui-build.ps1 -SkipWindows` | Android 构建通过 | 待执行 |
| Android 真机主链路 | 登录、上传、下载、图片预览、视频播放、删除、恢复、分享 | 主流程可用，问题已记录 | 待执行 |
| 外部登录边界 | 未配置微信/Google/GitHub 时账号密码登录正常 | 不阻塞主登录；settings 不返回 secret | 待执行 |
| 备份恢复说明 | 检查 `docs/deployment.md` | DB、storage、`.env` 备份边界明确 | 待执行 |

## 6. 安全与隐私要求

- `WECHAT_APP_SECRET`、`GOOGLE_CLIENT_SECRET`、`GITHUB_CLIENT_SECRET`、数据库密码、加密短语只允许存在于后端配置、环境变量或密钥系统中。
- 移动端 settings 接口只能返回公开配置，不能返回 client secret、access token、refresh token 或 provider token。
- 审计日志、验收记录和故障报告不得包含密码、token、OAuth code、provider token 或完整连接字符串。
- 分享链接、公开下载和媒体预览必须遵守已定义的访问边界。
- 发布前必须确认 `.env` 没有使用模板密码或默认加密短语。

## 7. 已知限制

- 微信真实端到端登录需要微信开放平台正式移动应用、真实 AppId/AppSecret、Android 包名与签名、安装微信的真机环境。
- iOS 微信 SDK 平台实现仍待后续版本完善。
- Android 真机验收需要后端 `PUBLIC_URL` 是手机可访问地址，不能只使用 `localhost`。
- 当前 MAUI 自动化测试主要以构建验证和手动验收为主。
- 完整 Docker 栈检查依赖本机 Docker daemon、镜像拉取能力和网络环境。

## 8. 发布后建议

V1.0 RC 通过后，再进入 V1.1 文件管理体验增强：

1. 文件搜索。
2. 排序和筛选。
3. 批量选择、批量删除、批量恢复。
4. 上传队列重试/取消。
5. 容量使用展示。
6. 分享管理体验增强。
