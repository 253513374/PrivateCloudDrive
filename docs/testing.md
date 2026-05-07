# 测试与验证说明

本文档记录当前自动化测试覆盖范围、手动验证命令和已知边界。阶段完成前应至少运行对应阶段涉及的构建和测试命令，并把结果写入 `docs/progress.md`。

## 自动化测试覆盖

当前主要测试位于 `aspnet-core/test/PrivateCloudDrive.EntityFrameworkCore.Tests/EntityFrameworkCore/FileCenter/`，覆盖以下 FileCenter 核心行为：

| 范围 | 覆盖点 |
| --- | --- |
| 文件夹管理 | 创建文件夹、同目录重名校验、分页列表、移动校验、回收站列表、恢复、永久删除 |
| 文件节点仓储 | 子节点查询、软删除过滤、排序、父子目录约束 |
| 小文件上传 | BlobObject 与 FileNode 创建、文件名重名校验、单文件大小限制、用户容量配额超限、删除到回收站、永久删除后释放 Blob |
| 文件下载 | 普通下载、HTTP Range、文件夹不可下载、缩略图下载 |
| 分片上传 | 创建上传会话、上传分片、查询已上传分片、完成合并、SHA256 校验、取消会话并清理临时分片 |
| 媒体任务 | 图片和视频上传后创建 MediaAsset、图片缩略图、视频封面与元数据、处理失败记录、删除清理 |
| 分享链接 | 创建分享、公开摘要、密码错误、密码校验、公开下载、过期链接、禁用链接 |
| 标签和收藏 | 创建标签、重复标签校验、绑定/解绑标签、收藏状态、按标签和收藏筛选 |
| HTTP 控制器 | 文件下载和缩略图 Range 响应头、上传表单参数传递 |

## 常用验证命令

后端完整验证：

```powershell
cd aspnet-core
dotnet build .\PrivateCloudDrive.slnx
dotnet test .\PrivateCloudDrive.slnx --no-build
```

MAUI 构建验证：

```powershell
cd maui/PrivateCloudDrive.App
dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0
dotnet build .\PrivateCloudDrive.App.csproj -f net10.0-android
```

Docker Compose 配置验证：

```powershell
docker compose config
```

Docker Compose 栈预检查：

```powershell
.\scripts\verify-docker-stack.ps1 -PreflightOnly
```

## 当前边界

- 自动化测试主要集中在后端应用层、领域层和 EF Core 集成测试；MAUI 端目前以构建验证为主。
- Docker Compose 当前已做配置展开校验；完整容器启动还依赖本机 Docker daemon、镜像拉取和网络环境。
- 第一阶段不覆盖 NAS 文件协议、桌面同步、Office 在线协作、AI 相册或多节点高可用。
