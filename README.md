# PrivateCloudDrive

PrivateCloudDrive 是一个可私有部署的文件、图片、视频管理系统。项目参考 Cloudreve 的文件管理体验，但当前范围聚焦文件管理、媒体预览、上传下载、分享链接和移动端访问，不包含 NAS 操作系统、磁盘阵列、SMB/NFS、远程下载或集群能力。

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
- `docs/testing.md`：测试覆盖和验证边界。

## 本地开发

先准备 .NET 10 SDK、Docker、PostgreSQL 或 Docker PostgreSQL、MAUI workload。开发数据库可以直接使用仓库里的 PostgreSQL Compose：

```powershell
docker compose -f docker-compose.postgres.yml up -d
```

后端默认连接字符串在 `aspnet-core/src/PrivateCloudDrive.DbMigrator/appsettings.json` 与 `aspnet-core/src/PrivateCloudDrive.HttpApi.Host/appsettings.json` 中，默认开发库为 `PrivateCloudDrive`，用户 `root`，密码 `myPassword`。

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

客户端 API 地址在 `maui/PrivateCloudDrive.App/Services/AppSettings.cs` 中：

```csharp
public const string ApiBaseUrl = "https://localhost:44343";
public const string OAuthClientId = "PrivateCloudDrive_App";
public const string OAuthRedirectUri = "privateclouddrive://callback";
```

移动设备或 Android 模拟器访问本机后端时，通常需要把 `ApiBaseUrl` 改成设备可访问的局域网地址，并同步更新 DbMigrator 中 `PrivateCloudDrive_App` 的 RedirectUri 配置后重新运行迁移种子。

常用构建命令：

```powershell
cd maui/PrivateCloudDrive.App
dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0
dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-android
```

## Docker Compose 部署

复制环境变量模板并修改密码和公网地址：

```powershell
Copy-Item .env.example .env
docker compose up -d --build
```

完整部署说明见 `docs/deployment.md`。默认 API 地址为 `http://localhost:8080/swagger`，文件、缩略图、封面和临时分片会保存在 `privateclouddrive_storage` volume。

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

更多测试覆盖说明见 `docs/testing.md`。
