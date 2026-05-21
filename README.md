# PrivateCloudDrive

> 开源、自托管、移动优先的私有文件与照片/视频备份网盘。

PrivateCloudDrive 面向个人、家庭和小团队，目标是把自己的服务器、NAS、迷你主机或云主机变成一个手机可稳定访问的私有文件与媒体中心。当前公司级产品方向聚焦“手机优先私有备份可信闭环”：连接自己的后端、清楚看到数据位置、备份照片/视频/文件、理解失败与重试、能够恢复数据，并明确隐私边界。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Product Stage](https://img.shields.io/badge/stage-private--backup--MVP-blue)](docs/roadmap-public.md)

## 为什么开源

PrivateCloudDrive 是一个自托管数据产品，信任来自可审计的源代码、清晰的部署文档和可复现的备份恢复流程。开源后的产品原则：

- 源代码开放，默认不绑定第三方云。
- 用户文件、数据库和环境密钥由部署者自己掌控。
- 移动端体验优先，先证明“备份可信闭环”，再扩展高级功能。
- 文档、路线图、验收标准和已知限制公开透明。

## 当前产品边界

- 当前主线：手机优先私有备份网盘。
- 已具备基础：账号密码登录、文件/文件夹管理、上传下载、媒体预览、回收站、分享、标签/收藏、操作日志、Docker Compose 私有部署、MAUI Android/Windows 客户端。
- 当前收口重点：备份中心、存储位置与健康状态、失败重试、恢复说明、隐私信任文案、Android 真实页面验收。
- 明确暂不做：NAS OS、RAID/磁盘池、SMB/NFS、Office 在线协作、企业审批流、桌面同步客户端、AI 相册/AI 搜索。

## 技术栈

- 后端：ABP Framework 10.3.0、.NET 10、Entity Framework Core、OpenIddict、PostgreSQL、Redis。
- 文件存储：ABP Blob Storing，默认使用本地文件系统目录，可选 Aliyun OSS / MinIO 方向。
- 媒体处理：后台任务调用 `ffmpeg` 和 `ffprobe` 生成视频封面、读取视频元数据，并生成图片缩略图。
- 客户端：.NET MAUI，使用 OpenIddict 登录、SecureStorage 保存 Token、MediaElement 播放视频。
- 部署：Docker Compose，包含 PostgreSQL、Redis、DbMigrator、API Host、Media Worker，可选 MinIO profile。

## 目录结构

- `aspnet-core/`：ABP 后端解决方案，入口解决方案为 `PrivateCloudDrive.slnx`。
- `maui/PrivateCloudDrive.App/`：MAUI 客户端。
- `docs/requirements.md`：产品需求。
- `docs/codex-development-plan.md`：分阶段开发任务。
- `docs/progress.md`：阶段完成状态、提交和验证证据。
- `docs/deployment.md`：Docker Compose 私有部署说明。
- `docs/phase-1-deployment.md`：第一阶段本机私有部署和 Android 模拟器内测部署文档。
- `docs/testing.md`：测试覆盖和验证边界。
- `docs/product-ui-baseline.md`：V0.2 产品化 UI 执行基线。
- `docs/external-login-design.md`：Google/GitHub 外部登录与账号绑定说明。

## 本地开发

先准备 .NET 10 SDK、Docker、PostgreSQL 或 Docker PostgreSQL、Redis、MAUI workload。本仓库通过 `global.json` 固定 .NET SDK `10.0.203`。开发数据库可以直接使用仓库里的 PostgreSQL Compose，并启动 Redis：

```powershell
docker compose -f docker-compose.postgres.yml up -d
docker compose up -d redis
```

后端默认连接字符串在 `aspnet-core/src/PrivateCloudDrive.DbMigrator/appsettings.json` 与 `aspnet-core/src/PrivateCloudDrive.HttpApi.Host/appsettings.json` 中，默认开发库为 `PrivateCloudDrive`，用户 `root`，密码 `myPassword`。
本地 ABP 分布式缓存默认使用 `localhost:6379` 的 Redis；如果改用完整 `docker-compose.yml` 中的 PostgreSQL，则需要同步覆盖连接字符串为 `privateclouddrive/privateclouddrive` 凭据。

初始化或更新数据库：

```powershell
cd aspnet-core/src/PrivateCloudDrive.DbMigrator
dotnet run
```

启动 API Host：

```powershell
cd aspnet-core/src/PrivateCloudDrive.HttpApi.Host
dotnet run
```

Swagger 地址：

```text
https://localhost:44343/swagger
```

## MAUI 客户端

MVP 内测版客户端默认连接本地 Docker Compose API：

```csharp
public static string ApiBaseUrl
{
    get
    {
#if ANDROID
        return "http://10.0.2.2:8080";
#else
        return "http://localhost:8080";
#endif
    }
}
public const string OAuthClientId = "PrivateCloudDrive_App";
public const string OAuthRedirectUri = "privateclouddrive://callback";
```

Windows 客户端使用 `http://localhost:8080`，Android 模拟器使用 `http://10.0.2.2:8080`。真实手机内测时，需要把 `ApiBaseUrl` 改成设备可访问的局域网地址；如果回调 URI 发生变化，再同步更新 DbMigrator 中 `PrivateCloudDrive_App` 的 RedirectUri 配置并重新运行迁移种子。

当前底部导航保留 Files、Photos、Videos、Uploads、Settings。MVP 回收站入口在 Settings 页中进入。微信登录仍是 V1 可选能力；Android 已接入 WeChat SDK 原生授权，后端启用且设备安装微信时才显示登录或绑定入口，Windows/iOS 仍保持默认不可用平台实现。

常用构建命令：

```powershell
cd maui/PrivateCloudDrive.App
dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64
dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android
```

Windows 和 Android 目标建议顺序构建；默认多目标 restore/build 会同时解析本机未安装的平台运行时，可能在缺少对应 workload 或 runtime 时失败。

## Docker Compose 部署

复制环境变量模板并修改密码和公网地址：

```powershell
Copy-Item .env.example .env
docker compose up -d --build
```

完整部署说明见 `docs/deployment.md`。默认 API 地址为 `http://localhost:8080/swagger`，文件、缩略图、封面和临时分片会保存在 `privateclouddrive_stack_storage` volume。

微信登录默认关闭。Android App 只消费后端公开 settings 中的 `AppId`、`Scope` 和平台公开配置，`AppSecret` 只能通过后端配置、环境变量或密钥系统提供。真实微信登录还需要微信开放平台移动应用、Android 包名与签名配置，以及安装微信的真机验收。

## 验证

后端：

```powershell
cd aspnet-core
dotnet build .\PrivateCloudDrive.slnx
dotnet test .\PrivateCloudDrive.slnx --no-build
```

Compose 配置：

```powershell
docker compose config
```

V1.0 RC 本地栈健康检查：

```powershell
.\scripts\verify-local-stack.ps1 -PreflightOnly
.\scripts\verify-local-stack.ps1
```

MAUI 顺序构建：

```powershell
.\scripts\verify-maui-build.ps1 -SkipAndroid
.\scripts\verify-maui-build.ps1 -SkipWindows
```

更多测试覆盖说明见 `docs/testing.md`；基础私有部署验收见 `docs/release-notes-v1.0-rc.md`，当前媒体库发布候选验收见 `docs/release-notes-v1.2-rc.md`。

## 开源仓库文档入口

| 文档 | 用途 |
|---|---|
| [docs/open-source.md](docs/open-source.md) | 开源发布说明、许可证、边界和维护政策 |
| [docs/roadmap-public.md](docs/roadmap-public.md) | 公司产品方向、版本路线和公开迭代节奏 |
| [docs/deployment.md](docs/deployment.md) | Docker Compose 私有部署、环境变量、备份恢复 |
| [docs/disaster-recovery.md](docs/disaster-recovery.md) | 备份恢复与灾难恢复 Runbook、恢复验收清单、演练证据规范 |
| [docs/testing.md](docs/testing.md) | 测试范围、验证命令和验收边界 |
| [docs/product-planning-hub.md](docs/product-planning-hub.md) | 内部产品规划中枢和版本决策依据 |
| [docs/team-operating-model-private-backup.md](docs/team-operating-model-private-backup.md) | 多 Agent 公司组织架构与 Private Backup Sprint 作战模型 |
| [CONTRIBUTING.md](CONTRIBUTING.md) | 贡献指南 |
| [SECURITY.md](SECURITY.md) | 安全策略与漏洞报告 |
| [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | 社区行为准则 |

## 快速开始

### 1. 克隆仓库

```bash
git clone https://github.com/253513374/PrivateCloudDrive.git
cd PrivateCloudDrive
```

### 2. 准备环境

- .NET SDK 10.0.x（仓库 `global.json` 固定 `10.0.203`，允许 latest patch roll-forward）。
- Docker / Docker Compose。
- 可选：.NET MAUI workload、Android SDK、ffmpeg/ffprobe。

### 3. 启动本地私有部署栈

```powershell
Copy-Item .env.example .env
# 第一次生产或公网部署前必须修改 .env 中所有密码、加密短语和公网地址。
docker compose up -d --build
```

默认本地 API / Swagger：

```text
http://localhost:8080/swagger
```

### 4. 验证本地栈

```powershell
.\scripts\verify-local-stack.ps1 -PreflightOnly
.\scripts\verify-local-stack.ps1
```

### 5. 构建后端

```powershell
cd aspnet-core
dotnet build .\PrivateCloudDrive.slnx
dotnet test .\PrivateCloudDrive.slnx --no-build
```

### 6. 构建 MAUI 客户端

```powershell
.\scripts\verify-maui-build.ps1 -SkipAndroid
.\scripts\verify-maui-build.ps1 -SkipWindows
```

Android Debug APK 手工验收建议使用完整嵌入程序集构建，避免 Fast Deployment 缺少 assembly 导致启动崩溃：

```powershell
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None
```

## 安全与密钥边界

- `.env`、生产数据库备份、storage 归档、私钥、证书和第三方登录 AppSecret 不允许提交。
- `.env.example` 只保留本地模板值；生产部署必须替换 `POSTGRES_PASSWORD`、`STRING_ENCRYPTION_PASSPHRASE`、MinIO 密码和外部登录密钥。
- WeChat / Google / GitHub AppSecret 只能配置在后端，不应进入 MAUI App。
- 默认本地 HTTP/Swagger 配置只用于开发验证；公网部署应使用 HTTPS 并关闭或保护 Swagger。
- 安全问题请按 [SECURITY.md](SECURITY.md) 报告。

## 许可证

本项目以 MIT License 开源，详见 [LICENSE](LICENSE)。
