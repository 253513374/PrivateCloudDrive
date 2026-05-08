# PrivateCloudDrive

PrivateCloudDrive 是一个可私有部署的文件、图片、视频管理系统。项目参考 Cloudreve 的文件管理体验，但当前 MVP Core 范围聚焦账号密码登录、文件管理、媒体预览、上传下载、基础回收站、移动端访问和本地私有部署，不包含 NAS 操作系统、磁盘阵列、SMB/NFS、远程下载或集群能力。

版本边界：

- MVP Core：账号密码登录、文件夹/文件列表、小文件上传、分片上传、流式下载、HTTP Range 视频播放、图片缩略图、视频封面、回收站、Docker Compose 本地私有部署。
- V1：分享链接、标签、收藏、按标签/收藏筛选、操作日志查询、独立图片/视频媒体库入口、微信登录接入。
- 微信登录是 V1 可选能力，不阻塞 MVP Core 的账号密码登录和本地文件管理。

## 技术栈

- 后端：ABP Framework 10.3.0、.NET 10、Entity Framework Core、OpenIddict、PostgreSQL、Redis。
- 文件存储：ABP Blob Storing，默认使用本地文件系统目录。
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

Docker 栈预检查：

```powershell
.\scripts\verify-docker-stack.ps1 -PreflightOnly
```

更多测试覆盖说明见 `docs/testing.md`。
