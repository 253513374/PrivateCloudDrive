# Codex 开发任务清单

本文档用于把项目拆成 Codex 可以连续执行的任务。每个任务都应做到可运行、可测试、可提交。

## 阶段 0：项目初始化

### 任务 0.1：确认本机环境

目标：

- 检查 .NET SDK。
- 检查 ABP CLI。
- 检查 Docker。
- 检查 Android / MAUI 工作负载。

Codex 指令：

```text
检查当前机器的 .NET SDK、ABP CLI、Docker、MAUI workload 环境。不要修改项目文件，只输出缺失项和建议安装命令。
```

验收：

- 输出环境检查结果。
- 明确哪些组件已安装，哪些需要安装。

### 任务 0.2：创建 ABP 后端解决方案

目标：

- 创建 ABP 后端解决方案。
- 使用模块化单体结构。
- 使用 PostgreSQL。
- 启用 Identity、OpenIddict、Swagger。

Codex 指令：

```text
在当前工作区创建 ABP 后端解决方案，项目名暂定为 PrivateCloudDrive。使用 ABP 最新稳定模板，数据库使用 PostgreSQL。创建后运行 restore/build，并记录启动方式。
```

验收：

- 后端项目可以 build。
- Swagger 可以访问。
- 数据库迁移可以执行。

### 任务 0.3：创建 MAUI App

目标：

- 创建 MAUI 客户端项目。
- 建立基础页面结构。
- 配置 API BaseUrl。

Codex 指令：

```text
创建 .NET MAUI App 项目，项目名为 PrivateCloudDrive.App。实现启动页、登录页、文件首页的空页面和基础导航。暂时使用 Mock API Client。
```

验收：

- MAUI 项目可以 build。
- Android 目标可以启动。

## 阶段 1：后端文件核心

### 任务 1.1：创建 FileCenter 模块结构

目标：

- 建立 FileCenter Domain、Application、Contracts、EntityFrameworkCore 层。
- 定义权限常量。
- 注册模块依赖。

Codex 指令：

```text
在 ABP 后端中创建 FileCenter 业务模块。按 ABP 分层约定添加 Domain、Application、Contracts、EntityFrameworkCore 代码。先只建立模块结构、权限定义和基础菜单/配置，不实现业务。
```

验收：

- 项目 build 通过。
- 权限定义可被 ABP 识别。

### 任务 1.2：实现 FileNode 实体

目标：

- 创建 FileNode 实体。
- 创建 EF Core 配置。
- 创建迁移。
- 实现基础仓储查询。

Codex 指令：

```text
实现 FileNode 实体和 EF Core 映射。支持文件夹和文件两种 NodeType。添加同目录同名唯一约束需要考虑 IsDeleted。创建迁移并确保数据库更新成功。
```

验收：

- 数据库生成 FileNodes 表。
- 同目录同名创建被拒绝。

### 任务 1.3：实现文件夹管理 API

目标：

- 创建文件夹。
- 查询目录列表。
- 重命名。
- 移动。
- 删除到回收站。

Codex 指令：

```text
实现 FileCenter 文件夹管理 Application Service 和 API。包括创建文件夹、查询目录列表、重命名、移动、软删除。补充单元测试或集成测试。
```

验收：

- Swagger 能调用文件夹接口。
- 目录列表支持分页。
- 移动目录不能移动到自身子目录。

## 阶段 2：上传下载

### 任务 2.1：实现 BlobObject 与本地存储

目标：

- 定义 BlobObject。
- 使用 ABP BLOB Storing 保存真实文件。
- 默认使用本地文件系统存储。

Codex 指令：

```text
实现 BlobObject 实体和本地文件系统 Blob 存储配置。文件真实内容必须保存到配置的 storage 目录，数据库只保存元数据。
```

验收：

- 上传测试文件后，storage 目录有真实文件。
- 数据库有 BlobObject 记录。

### 任务 2.2：实现小文件上传

目标：

- 支持直接上传小文件。
- 创建 FileNode 和 BlobObject。
- 校验容量和文件名。

Codex 指令：

```text
实现小文件上传 API。使用 multipart/form-data，上传后写入 Blob Storage，创建 BlobObject 和 FileNode。限制单文件大小，校验用户容量。补充集成测试。
```

验收：

- Swagger 可以上传文件。
- 上传后目录列表出现文件。
- 超过大小限制返回明确错误。

### 任务 2.3：实现文件下载和 Range

目标：

- 支持普通下载。
- 支持 Range 下载。
- 支持视频播放基础能力。

Codex 指令：

```text
实现文件下载 API。下载必须使用流式响应，不能一次性读入内存。实现 HTTP Range 请求支持，返回正确的 206 Partial Content。
```

验收：

- 普通下载成功。
- Range 请求返回 206。
- 未授权用户不能下载。

### 任务 2.4：实现分片上传

目标：

- 创建 UploadSession。
- 上传分片。
- 查询已上传分片。
- 完成合并。
- 清理临时文件。

Codex 指令：

```text
实现分片上传。包含创建上传会话、上传 chunk、查询会话、完成上传、取消上传。完成上传时合并分片并写入 Blob Storage。补充分片上传集成测试。
```

验收：

- 1GB 以内大文件可以分片上传。
- 中断后可查询已上传分片。
- 完成后文件哈希正确。

## 阶段 3：媒体处理

### 任务 3.1：实现 MediaAsset 实体

目标：

- 创建 MediaAsset。
- 上传图片/视频后自动创建 MediaAsset。
- 状态初始为 Pending。

Codex 指令：

```text
实现 MediaAsset 实体和媒体识别逻辑。上传图片或视频文件后创建 MediaAsset，并创建后台处理任务。
```

验收：

- 上传图片后有 MediaAsset 记录。
- 上传视频后有 MediaAsset 记录。

### 任务 3.2：图片缩略图

目标：

- 生成图片缩略图。
- 提取宽高和 EXIF 拍摄时间。

Codex 指令：

```text
实现图片后台处理任务。生成固定尺寸缩略图，提取图片宽高和 EXIF 拍摄时间。缩略图保存到 Blob Storage，并提供缩略图访问 API。
```

验收：

- 上传图片后可以访问缩略图。
- 图片方向显示正确。

### 任务 3.3：视频封面和元数据

目标：

- 使用 FFprobe 提取视频信息。
- 使用 FFmpeg 生成封面。

Codex 指令：

```text
实现视频后台处理任务。调用 FFprobe 提取时长、宽高、编码信息，调用 FFmpeg 生成视频封面。处理失败要记录错误并允许重试。
```

验收：

- 上传 MP4 后有封面图。
- 视频时长和分辨率正确。

## 阶段 4：MAUI App 核心

### 任务 4.1：登录接入

目标：

- 接入 OpenIddict 登录。
- 保存 Token。
- 自动刷新 Token。

Codex 指令：

```text
在 MAUI App 中实现登录功能，接入后端 OpenIddict。Token 使用 SecureStorage 保存。实现登录、退出和 API 授权请求。
```

验收：

- App 可以登录真实后端。
- 登录后可以调用当前用户接口。

### 任务 4.2：文件列表页

目标：

- 调用后端目录列表。
- 展示文件和文件夹。
- 支持进入文件夹。
- 支持刷新。

Codex 指令：

```text
实现 MAUI 文件列表页，调用后端 FileCenter API。支持进入文件夹、返回上级目录、下拉刷新、分页加载。文件类型显示不同图标。
```

验收：

- App 显示真实文件列表。
- 可以进入和返回文件夹。

### 任务 4.3：上传文件

目标：

- 使用 FilePicker 选择文件。
- 小文件直接上传。
- 大文件分片上传。
- 显示上传进度。

Codex 指令：

```text
实现 MAUI 上传队列。使用 FilePicker 选择多个文件，小文件直接上传，大文件走分片上传。显示进度、状态、失败重试。
```

验收：

- App 可以上传图片和视频。
- 上传进度正确显示。

### 任务 4.4：图片和视频预览

目标：

- 图片网格页。
- 图片大图预览。
- 视频列表页。
- 视频播放页。

Codex 指令：

```text
实现 MAUI 图片和视频预览。图片使用缩略图网格和大图预览，视频使用封面列表和 MediaElement 播放后端 Range 视频地址。
```

验收：

- 图片缩略图正常显示。
- MP4 视频可以播放和拖动进度。

## 阶段 5：分享、标签、部署完善

### 任务 5.1：分享链接

目标：

- 创建分享链接。
- 支持密码和过期时间。
- 支持公开访问接口。

Codex 指令：

```text
实现分享链接功能。支持文件和文件夹分享、访问密码、过期时间、是否允许下载。提供公开访问 API 和权限校验。
```

验收：

- 分享链接可访问。
- 过期或密码错误不可访问。

### 任务 5.2：标签和收藏

目标：

- 创建标签。
- 给文件绑定标签。
- 收藏文件。
- 按标签和收藏筛选。

Codex 指令：

```text
实现标签和收藏功能。用户可以创建自己的标签，给文件绑定标签，收藏文件，并在列表和媒体页筛选。
```

验收：

- 文件可以添加标签。
- 文件可以按标签查询。

### 任务 5.3：Docker Compose 私有部署

目标：

- 提供完整 Docker Compose。
- 初始化数据库。
- 配置 Volume。
- 写部署文档。

Codex 指令：

```text
为项目添加 Dockerfile 和 docker-compose.yml。包含 API Host、PostgreSQL、Redis、Media Worker、可选 MinIO。写明环境变量、Volume、首次启动流程和管理员初始化方式。
```

验收：

- 新机器可以通过 Docker Compose 启动后端。
- 文件存储 Volume 可持久化。

## 阶段 6：质量收尾

### 任务 6.1：后端测试补齐

目标：

- 补齐核心业务测试。
- 保证上传、下载、权限、分享稳定。

Codex 指令：

```text
为 FileCenter 核心模块补齐测试。覆盖文件夹管理、上传下载、Range、回收站、分享、容量配额、媒体任务创建。
```

验收：

- 测试全部通过。

### 任务 6.2：README 和使用文档

目标：

- 写项目说明。
- 写开发环境搭建。
- 写私有部署步骤。
- 写 App 配置说明。

Codex 指令：

```text
编写 README 和部署文档。内容包括项目介绍、技术栈、开发环境、后端启动、MAUI 启动、Docker Compose 部署、常见问题。
```

验收：

- 新开发者可以按文档启动项目。
