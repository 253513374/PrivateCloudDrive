# 第一阶段部署文档

## 1. 部署目标

第一阶段交付目标是把 PrivateCloudDrive 部署为“本机私有部署 + Android 模拟器内测”的 MVP Core 可验收版本。

本阶段只证明以下核心链路：

- 账号密码登录。
- Token 刷新。
- 文件夹和文件列表。
- 上传与下载。
- 图片预览和图片详情展示。
- 视频播放。
- 删除、回收站、恢复、永久删除。
- Docker Compose 私有部署。
- MAUI Android 模拟器访问本机 Compose API。

本阶段不把微信登录、真实手机局域网访问、生产证书、对象存储迁移、多节点部署作为阻塞项。

## 2. 当前基线

- 验收日期：2026-05-08。
- 第一阶段功能基线提交：`608084d`。部署时可以使用包含该提交的后续版本。
- 后端默认地址：`http://localhost:8080`。
- Android 模拟器访问地址：`http://10.0.2.2:8080`。
- Windows MAUI 客户端访问地址：`http://localhost:8080`。
- 回收站入口：`设置 -> 回收站`。
- 微信登录：V1 可选能力，第一阶段默认不可用且不阻塞账号密码登录。

## 3. 环境要求

本机需要：

- Windows + PowerShell。
- Docker Desktop。
- .NET SDK `10.0.203`，以仓库 `global.json` 为准。
- MAUI workload，可构建 Android 和 Windows 目标。
- Android Emulator，第一阶段已用 Pixel 9 Pro / API 36 验收。
- 可访问 Docker Hub 和 Microsoft Container Registry。

检查命令：

```powershell
dotnet --version
docker version
docker compose version
dotnet workload list
```

## 4. 获取代码

进入仓库根目录：

```powershell
cd D:\Devs\Projects\Personal\PrivateCloudDrive
git status --short
git rev-parse --short HEAD
```

第一阶段交付时工作区应保持干净，提交历史应包含 `608084d` 或后续部署文档提交。

## 5. 配置环境变量

首次部署复制环境模板：

```powershell
Copy-Item .env.example .env
```

第一阶段本机内测建议保持：

```text
PUBLIC_URL=http://localhost:8080
WECHAT_ENABLED=false
```

注意：

- `.env` 中的密码和密钥只用于本机私有部署，不提交到 Git。
- 第一阶段不要把 `WECHAT_APP_SECRET` 放入 MAUI 客户端。
- 如果已经有旧 `.env`，先确认 `PUBLIC_URL` 与本机访问地址一致。

## 6. 启动 Docker Compose 栈

先验证 Compose 配置：

```powershell
docker compose config
```

启动完整栈：

```powershell
docker compose up -d --build
```

栈内服务：

| 服务 | 作用 |
| --- | --- |
| `postgres` | PostgreSQL 数据库 |
| `redis` | ABP 分布式缓存、限流、OpenIddict 缓存 |
| `db-migrator` | 数据库迁移和种子数据 |
| `api` | ABP API Host、OpenIddict、FileCenter API |
| `media-worker` | 后台媒体任务、缩略图、视频封面和元数据 |

查看运行状态：

```powershell
docker compose ps
```

预期：

- `postgres` 为 healthy。
- `redis` 为 healthy。
- `api` 处于 Up。
- `media-worker` 处于 Up。
- `db-migrator` 已成功退出。

## 7. 快速验收后端

打开 Swagger：

```powershell
start http://localhost:8080/swagger/index.html
```

或用 PowerShell 检查：

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:8080/swagger/index.html
Invoke-WebRequest -UseBasicParsing http://localhost:8080/.well-known/openid-configuration
```

推荐执行完整栈验证脚本：

```powershell
.\scripts\verify-docker-stack.ps1
```

该脚本会验证：

- Docker CLI 和 Compose 可用。
- PostgreSQL/Redis 健康。
- DbMigrator 成功。
- API Swagger 可访问。
- Media Worker 正常运行。

## 8. 构建 MAUI 客户端

Windows 目标：

```powershell
dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64
```

Android 目标：

```powershell
dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android
```

两个目标建议顺序执行，不要并行执行，避免 `obj/project.assets.json` 在不同目标 restore 时互相覆盖。

## 9. 运行 Android 模拟器 App

确认 Android 模拟器已启动，然后运行：

```powershell
cd .\maui\PrivateCloudDrive.App
dotnet build .\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android -t:Run
```

如果 `adb` 不在 PATH，可以使用 Visual Studio 默认路径：

```powershell
& 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe' devices
& 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe' shell pidof com.companyname.privateclouddrive.app
```

预期：

- `adb devices` 显示 `emulator-5554 device` 或其它在线模拟器。
- `pidof` 返回 App 进程号。
- App 登录页显示 API 地址 `http://10.0.2.2:8080`。

## 10. 第一阶段手动验收

使用 Android 模拟器执行：

| 范围 | 操作 | 预期 |
| --- | --- | --- |
| 登录 | 使用账号密码登录 | 成功进入文件页 |
| 文件列表 | 打开根目录和子目录 | 列表加载成功，根目录不重复显示标题 |
| 上传 | 上传图片、视频、普通文件 | 上传队列显示状态，成功后文件出现在列表 |
| 图片详情 | 打开图片详情 | 顶部全宽显示图片，无标题、无边距 |
| 图片预览 | 从图片入口预览 | 图片可加载，失败时有重试 |
| 视频播放 | 打开视频 | 可播放，Range 下载生效 |
| 回收站 | 删除文件，从设置进入回收站 | 可恢复、永久删除、清空 |
| 退出登录 | 从设置退出登录 | 本地 token 清理，回到登录页 |

验收记录只写：

- 设备。
- 系统版本。
- App 构建号。
- 后端提交号。
- 接口状态。
- 结果。
- 问题。

禁止记录：

- 密码。
- access token。
- refresh token。
- 微信 AppSecret。

## 11. 常见问题

### Android 模拟器文件列表 401 或 FileCenter request failed

确认后端提交包含固定 OpenIddict issuer 的修复，当前基线已包含。然后在 App 中退出登录并重新登录，或清理 App 数据，避免旧 token 残留。

### Android 模拟器无法访问 API

模拟器访问宿主机应使用：

```text
http://10.0.2.2:8080
```

不要在 Android 模拟器中使用 `http://localhost:8080`，它会指向模拟器自身。

### Docker 镜像拉取失败

配置 Docker Desktop HTTPS 代理或镜像源后重试：

```powershell
docker compose up -d --build
```

### MAUI Windows/Android 构建互相影响

顺序执行构建命令。如果刚刚并行构建失败，重新单独执行目标构建即可。

## 12. 停止和清理

停止服务但保留数据：

```powershell
docker compose down
```

停止并删除本阶段数据卷：

```powershell
docker compose down -v
```

删除数据卷会清空 PostgreSQL、Redis、上传文件、缩略图、视频封面和临时分片。只有在确认不需要本机内测数据时才执行。

## 13. 下一阶段边界

第一阶段部署完成后，下一阶段进入 V0.2 内测体验修整：

- 继续使用本阶段 Docker Compose 部署方式。
- 用真实图片、视频和多文件夹数据集做体验复查。
- 继续收口 UI 细节，不新增后端业务 API。
- 真实手机局域网部署、生产 HTTPS、iOS WeChat SDK、正式微信凭据真机验收、MinIO 对象存储作为后续独立任务。
