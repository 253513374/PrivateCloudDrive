# 私有文件与媒体管理系统需求规格

## 1. 项目定位

本项目目标是开发一个可私有部署的文件、图片、视频管理系统。系统参考 Cloudreve 的文件管理体验，但不实现完整 NAS 操作系统能力。核心目标是让个人、家庭、小团队可以在自己的服务器、迷你主机、NAS 主机或云服务器上部署一个统一的私有网盘和媒体库。

项目后端采用 ABP Framework 开发，移动端采用 .NET MAUI 开发。第一阶段优先实现稳定的文件管理、图片预览、视频播放、上传下载、分享和私有部署能力。

## 2. 目标用户

### 2.1 个人用户

- 管理自己的文档、图片、视频、压缩包等文件。
- 手机端上传照片、视频到私有服务器。
- 在局域网或公网安全访问自己的文件。
- 通过链接临时分享文件给他人。

### 2.2 家庭用户

- 统一管理家庭照片和视频。
- 多个家庭成员拥有独立账号和容量空间。
- 管理员可以管理用户、容量、文件分享和系统设置。

### 2.3 小团队用户

- 管理团队共享文件。
- 控制不同用户的文件权限。
- 审计文件上传、删除、分享等操作。

## 3. 非目标范围

第一阶段不实现以下功能：

- RAID、ZFS、Btrfs、磁盘池、SMART 硬盘健康管理。
- SMB、NFS、AFP 等传统 NAS 文件协议。
- 桌面同步客户端。
- Office 在线协同编辑。
- BT、磁力、Aria2、qBittorrent 远程下载。
- 多节点集群、高可用、跨区域同步。
- AI 语义搜索、人脸识别、智能相册。
- Kubernetes 部署。

这些能力可以作为后续版本规划，不进入 MVP 阶段。

## 4. 技术栈

### 4.1 后端

- 框架：ABP Framework 最新稳定版。
- 运行时：.NET 最新 LTS 版本。
- 架构：模块化单体，后续可拆分 Worker 或独立服务。
- 数据库：PostgreSQL。
- ORM：Entity Framework Core。
- 认证授权：ABP Identity + OpenIddict。
- 缓存：Redis。
- 后台任务：ABP Background Jobs，生产环境可接 Hangfire。
- 文件存储：ABP BLOB Storing。
- 本地存储 Provider：File System。
- 对象存储 Provider：MinIO，后续兼容 S3。
- API 文档：Swagger / OpenAPI。

### 4.2 移动端

- 框架：.NET MAUI。
- 平台：Android、iOS，后续可支持 Windows/macOS。
- API 调用：HttpClient，文件上传下载使用显式流式接口。
- 登录：OpenIddict OAuth/OIDC Token。
- 本地缓存：SQLite 或 MAUI Essentials SecureStorage。
- 图片预览：MAUI Image 控件或第三方图片查看组件。
- 视频播放：.NET MAUI Community Toolkit MediaElement 或平台原生播放器封装。

### 4.3 媒体处理

- 图片缩略图：ImageSharp 或 libvips。
- EXIF 提取：MetadataExtractor 或 ExifLib。
- 视频信息：FFprobe。
- 视频封面：FFmpeg。
- 后续可选：HLS 转码、低清预览文件。

### 4.4 部署

- Docker Compose 私有部署。
- 服务组成：
  - API Host
  - PostgreSQL
  - Redis
  - Media Worker
  - MinIO，可选
  - Nginx 或 Caddy，可选
- 文件存储目录必须支持宿主机 Volume 映射。

## 5. 系统角色

### 5.1 管理员

- 管理用户、角色、权限。
- 配置存储位置、容量限制、上传限制。
- 查看系统状态和任务状态。
- 查看文件操作日志。
- 管理非法或异常分享链接。
- 执行回收站清理。

### 5.2 普通用户

- 管理自己的文件和文件夹。
- 上传、下载、移动、复制、重命名、删除文件。
- 浏览图片和视频。
- 添加标签、收藏。
- 创建分享链接。
- 查看自己的容量使用情况。

### 5.3 分享访问者

- 通过分享链接访问文件或文件夹。
- 根据分享设置输入密码。
- 根据权限预览或下载文件。

## 6. 核心业务模块

### 6.1 用户与权限模块

#### 功能需求

- 支持管理员初始化。
- 支持用户登录、退出、刷新 Token。
- 支持用户禁用、启用。
- 支持角色：
  - Admin
  - User
- 支持权限：
  - 文件查看
  - 文件上传
  - 文件下载
  - 文件删除
  - 文件分享
  - 管理用户
  - 管理系统设置
- 支持用户容量配额。
- 支持单文件上传大小限制。
- 支持用户总空间使用统计。

#### 验收标准

- 未登录用户不能访问私有 API。
- 普通用户不能访问管理 API。
- 用户超过容量配额时不能继续上传。
- 被禁用用户不能登录。

### 6.2 文件夹与文件管理模块

#### 功能需求

- 创建文件夹。
- 获取目录文件列表。
- 支持分页、排序、筛选。
- 支持按名称、大小、类型、创建时间、修改时间排序。
- 支持文件重命名。
- 支持文件移动到其他目录。
- 支持文件复制。
- 支持文件软删除到回收站。
- 支持从回收站恢复。
- 支持永久删除。
- 支持文件详情。
- 支持计算目录大小。
- 支持收藏文件。
- 支持给文件添加标签。

#### 文件类型分类

- Folder
- Image
- Video
- Audio
- Document
- Archive
- Other

#### 验收标准

- 同一目录下不允许出现同名文件或文件夹。
- 删除文件后默认进入回收站。
- 回收站文件不在正常目录列表展示。
- 移动目录时不能移动到自身或自己的子目录。
- 大目录列表必须支持分页。

### 6.3 上传模块

#### 功能需求

- 支持普通小文件上传。
- 支持大文件分片上传。
- 支持上传会话。
- 支持上传进度。
- 支持上传失败重试。
- 支持断点续传。
- 支持上传完成后自动合并分片。
- 支持通过 SHA256 或其他哈希做秒传准备。
- 支持限制文件类型黑名单。
- 支持限制单文件大小。
- 支持限制用户剩余容量。

#### 分片上传流程

1. 客户端调用创建上传会话接口。
2. 服务端返回 UploadSessionId、ChunkSize、已上传分片列表。
3. 客户端逐个上传分片。
4. 服务端保存分片到临时目录。
5. 客户端调用完成上传接口。
6. 服务端校验所有分片。
7. 服务端合并文件并写入 Blob Storage。
8. 服务端创建 FileNode 和 BlobObject。
9. 服务端投递媒体处理任务。
10. 服务端删除临时分片。

#### 验收标准

- 1GB 视频文件可以稳定上传。
- 上传中断后再次上传时可以跳过已完成分片。
- 上传完成后文件大小和哈希必须正确。
- 上传失败的临时分片可以被后台清理。

### 6.4 下载与在线播放模块

#### 功能需求

- 支持文件下载。
- 支持文件夹打包下载，后续版本。
- 支持 HTTP Range 请求。
- 支持视频在线播放。
- 支持图片原图访问。
- 支持缩略图访问。
- 支持下载权限校验。
- 支持分享链接下载。

#### 验收标准

- 视频播放时可以拖动进度条。
- 大文件下载不能一次性加载到内存。
- 未授权用户不能下载私有文件。
- 分享过期后不能继续访问。

### 6.5 图片管理模块

#### 功能需求

- 支持图片缩略图。
- 支持图片大图预览。
- 支持图片基本信息：
  - 宽度
  - 高度
  - 格式
  - 文件大小
- 支持 EXIF 信息：
  - 拍摄时间
  - 相机型号
  - GPS，若存在
  - 方向
- 支持按拍摄时间浏览图片。
- 支持相册视图，第一阶段可用文件夹作为相册。
- 支持图片收藏。
- 支持图片标签。

#### 验收标准

- 上传图片后后台自动生成缩略图。
- 图片方向必须根据 EXIF 正确显示。
- 图片列表必须优先展示缩略图，不直接加载原图。

### 6.6 视频管理模块

#### 功能需求

- 支持视频封面图。
- 支持视频在线播放。
- 支持视频基础信息：
  - 时长
  - 宽度
  - 高度
  - 编码格式
  - 帧率，可选
  - 比特率，可选
- 支持按时间、大小、时长排序。
- 支持视频收藏。
- 支持视频标签。
- 后续支持 HLS 转码。

#### 验收标准

- 上传视频后自动生成封面图。
- 视频播放接口支持 Range。
- 移动端播放常见 MP4 文件时可以正常播放和拖动。

### 6.7 媒体处理任务模块

#### 功能需求

- 上传图片后创建缩略图任务。
- 上传视频后创建封面和元数据任务。
- 支持任务状态：
  - Pending
  - Processing
  - Completed
  - Failed
- 支持失败重试。
- 支持管理员查看失败任务。
- 支持后台清理无效临时文件。

#### 验收标准

- 媒体处理失败不影响文件本身上传成功。
- 失败任务可以重新执行。
- Worker 可以独立部署或与 API 同进程运行。

### 6.8 分享模块

#### 功能需求

- 支持创建文件分享链接。
- 支持创建文件夹分享链接。
- 支持设置过期时间。
- 支持设置访问密码。
- 支持设置是否允许下载。
- 支持查看分享访问次数。
- 支持取消分享。
- 支持管理员管理所有分享。

#### 验收标准

- 分享 Token 不能可预测。
- 过期分享不可访问。
- 密码错误不能访问分享内容。
- 不允许下载时只能预览支持的文件类型。

### 6.9 搜索与筛选模块

#### MVP 功能需求

- 按文件名搜索。
- 按文件类型筛选。
- 按图片、视频、文档分类筛选。
- 按创建时间范围筛选。
- 按标签筛选。
- 按收藏筛选。

#### 后续功能

- 全文搜索。
- OCR 搜索。
- AI 语义搜索。

#### 验收标准

- 普通用户只能搜索自己有权限访问的文件。
- 搜索结果分页返回。

### 6.10 回收站模块

#### 功能需求

- 删除文件默认进入回收站。
- 支持恢复文件。
- 支持永久删除文件。
- 支持清空回收站。
- 支持自动清理超过保留天数的文件。

#### 验收标准

- 回收站文件不参与普通文件列表。
- 恢复时若原目录存在同名文件，需要提示或自动重命名。
- 永久删除后释放容量和 Blob 引用。

### 6.11 系统设置模块

#### 功能需求

- 设置站点名称。
- 设置默认用户容量。
- 设置允许上传的最大文件大小。
- 设置是否允许用户注册。
- 设置回收站保留天数。
- 设置缩略图尺寸。
- 设置默认存储策略。
- 设置公网访问地址。

#### 验收标准

- 只有管理员可以修改系统设置。
- 设置修改后对后续请求生效。

### 6.12 审计与日志模块

#### 功能需求

- 记录用户登录。
- 记录文件上传。
- 记录文件下载，可选。
- 记录文件删除、恢复、永久删除。
- 记录分享创建和取消。
- 记录管理员操作。
- 记录媒体任务失败日志。

#### 验收标准

- 管理员可以按用户、操作类型、时间范围查询日志。
- 普通用户不能查看其他用户日志。

## 7. MAUI App 需求

### 7.1 页面结构

- 启动页。
- 登录页。
- 文件首页。
- 图片页。
- 视频页。
- 上传队列页。
- 文件详情页。
- 图片预览页。
- 视频播放页。
- 分享设置页。
- 设置页。

### 7.2 文件首页

#### 功能需求

- 展示当前目录路径。
- 展示文件夹和文件列表。
- 支持列表模式和网格模式。
- 支持进入文件夹。
- 支持返回上级目录。
- 支持新建文件夹。
- 支持上传文件。
- 支持重命名、移动、删除、分享。
- 支持下拉刷新。

#### 验收标准

- 目录文件超过 1000 个时仍然分页加载。
- 文件图标能区分图片、视频、文档、压缩包和其他类型。

### 7.3 图片页

#### 功能需求

- 以网格方式展示图片。
- 优先显示缩略图。
- 支持按时间分组。
- 支持点击进入大图预览。
- 支持左右滑动切换图片。
- 支持下载原图。
- 支持收藏、标签、分享。

### 7.4 视频页

#### 功能需求

- 展示视频封面。
- 展示视频时长。
- 支持点击播放。
- 支持横竖屏播放。
- 支持进度条拖动。
- 支持下载原文件。

### 7.5 上传队列

#### 功能需求

- 支持从系统文件选择器选择文件。
- 支持选择图片和视频。
- 支持多文件上传。
- 显示上传进度。
- 显示上传速度。
- 支持暂停、继续、取消，MVP 可只做取消和重试。
- 上传失败后支持重试。

#### 验收标准

- App 关闭或网络中断后，未完成上传可以恢复，后续版本。
- MVP 至少支持失败重试。

### 7.6 本地安全

#### 功能需求

- Token 使用 SecureStorage 保存。
- 支持刷新 Token。
- 退出登录时清理本地 Token。
- 后续支持生物识别解锁。

## 8. 后端领域模型建议

### 8.1 FileNode

用于表示用户看到的文件或文件夹。

字段建议：

- Id
- TenantId，可选，MVP 可关闭多租户
- OwnerUserId
- ParentId
- Name
- NodeType
- FileCategory
- Extension
- MimeType
- Size
- BlobObjectId
- PathHash
- IsDeleted
- DeletedTime
- CreationTime
- LastModificationTime

### 8.2 BlobObject

用于表示真实文件内容。

字段建议：

- Id
- StorageProvider
- ContainerName
- BlobName
- Size
- Sha256
- RefCount
- CreationTime

### 8.3 UploadSession

用于表示一次分片上传。

字段建议：

- Id
- UserId
- ParentId
- FileName
- TotalSize
- ChunkSize
- TotalChunks
- UploadedChunksJson
- Sha256
- Status
- ExpireTime
- CreationTime

### 8.4 MediaAsset

用于保存图片或视频元数据。

字段建议：

- Id
- FileNodeId
- MediaType
- Width
- Height
- Duration
- Codec
- TakenAt
- ThumbnailBlobObjectId
- PreviewBlobObjectId
- MetadataJson
- ProcessStatus
- ProcessError

### 8.5 FileShare

用于保存分享链接。

字段建议：

- Id
- FileNodeId
- OwnerUserId
- Token
- PasswordHash
- ExpireTime
- AllowDownload
- VisitCount
- IsEnabled
- CreationTime

### 8.6 FileTag

字段建议：

- Id
- OwnerUserId
- Name
- Color

### 8.7 FileNodeTag

字段建议：

- FileNodeId
- TagId

## 9. API 设计建议

### 9.1 文件 API

- `GET /api/file-center/nodes`
- `GET /api/file-center/nodes/{id}`
- `POST /api/file-center/folders`
- `PUT /api/file-center/nodes/{id}/rename`
- `PUT /api/file-center/nodes/{id}/move`
- `POST /api/file-center/nodes/{id}/copy`
- `DELETE /api/file-center/nodes/{id}`
- `POST /api/file-center/nodes/{id}/restore`
- `DELETE /api/file-center/nodes/{id}/permanent`

### 9.2 上传 API

- `POST /api/file-center/upload-sessions`
- `GET /api/file-center/upload-sessions/{id}`
- `PUT /api/file-center/upload-sessions/{id}/chunks/{chunkIndex}`
- `POST /api/file-center/upload-sessions/{id}/complete`
- `DELETE /api/file-center/upload-sessions/{id}`

### 9.3 下载与预览 API

- `GET /api/file-center/files/{id}/download`
- `GET /api/file-center/files/{id}/content`
- `GET /api/file-center/files/{id}/thumbnail`
- `GET /api/file-center/files/{id}/preview`

### 9.4 媒体 API

- `GET /api/file-center/media/images`
- `GET /api/file-center/media/videos`
- `GET /api/file-center/media/{fileId}`
- `POST /api/file-center/media/{fileId}/reprocess`

### 9.5 分享 API

- `POST /api/file-center/shares`
- `GET /api/file-center/shares`
- `DELETE /api/file-center/shares/{id}`
- `GET /api/public/shares/{token}`
- `POST /api/public/shares/{token}/verify-password`
- `GET /api/public/shares/{token}/download`

### 9.6 标签 API

- `GET /api/file-center/tags`
- `POST /api/file-center/tags`
- `PUT /api/file-center/tags/{id}`
- `DELETE /api/file-center/tags/{id}`
- `POST /api/file-center/nodes/{id}/tags/{tagId}`
- `DELETE /api/file-center/nodes/{id}/tags/{tagId}`

## 10. 权限定义建议

- `FileCenter.Files.Default`
- `FileCenter.Files.Create`
- `FileCenter.Files.Update`
- `FileCenter.Files.Delete`
- `FileCenter.Files.Download`
- `FileCenter.Files.Share`
- `FileCenter.Media.Default`
- `FileCenter.Tags.Default`
- `FileCenter.Tags.Manage`
- `FileCenter.Shares.Default`
- `FileCenter.Shares.ManageAll`
- `FileCenter.Settings.Manage`
- `FileCenter.Admin`

## 11. 存储设计

### 11.1 本地文件系统

MVP 默认使用本地文件系统。

目录结构建议：

```text
storage/
  blobs/
    ab/
      cd/
        {blobId}
  thumbnails/
  previews/
  temp/
    uploads/
      {uploadSessionId}/
```

### 11.2 MinIO

后续支持 MinIO。

Bucket 建议：

- `file-center-blobs`
- `file-center-thumbnails`
- `file-center-temp`

### 11.3 安全要求

- 存储中的文件名不使用用户原始文件名。
- 用户原始文件名只存在数据库中。
- 文件访问必须通过 API 权限校验。
- 不直接暴露宿主机真实路径。

## 12. 私有部署需求

### 12.1 Docker Compose

必须提供 `docker-compose.yml`，包含：

- API Host
- PostgreSQL
- Redis
- Media Worker
- MinIO，可选

### 12.2 配置项

必须通过环境变量配置：

- 数据库连接字符串
- Redis 连接字符串
- Blob 存储路径
- JWT/OIDC 地址
- 站点公网地址
- 管理员初始账号
- 最大上传大小
- 上传临时目录

### 12.3 初始化

- 第一次启动自动迁移数据库。
- 第一次启动创建管理员账号。
- 第一次启动创建默认角色和权限。
- 第一次启动创建默认存储配置。

## 13. 安全要求

- 所有私有 API 必须认证。
- 所有文件访问必须校验权限。
- 分享 Token 使用安全随机字符串。
- 分享密码只保存哈希。
- 上传文件名必须做安全处理，避免路径穿越。
- 下载接口必须防止任意路径读取。
- 上传大小必须在服务端校验。
- API 必须启用 CORS 白名单。
- 生产环境必须使用 HTTPS。
- Token 过期时间和刷新机制必须合理。

## 14. 性能要求

- 文件列表接口必须分页。
- 上传下载不能把大文件完整读入内存。
- 视频播放必须支持 Range。
- 缩略图异步生成。
- 首页文件列表 200ms 到 800ms 内返回，取决于部署环境。
- 单用户上传 1GB 文件应稳定完成。
- 常见图片缩略图应在上传后短时间内生成。

## 15. 测试要求

### 15.1 后端单元测试

- 文件重名校验。
- 文件移动校验。
- 容量配额校验。
- 分享过期校验。
- 分片上传完整性校验。

### 15.2 后端集成测试

- 登录后上传文件。
- 上传图片后生成媒体任务。
- 删除文件进入回收站。
- 分享链接访问文件。
- Range 下载返回正确状态码。

### 15.3 MAUI 手动测试

- Android 登录。
- 上传图片。
- 上传视频。
- 浏览文件夹。
- 预览图片。
- 播放视频。
- 删除文件。
- 创建分享。

## 16. 版本规划

### 16.1 MVP

- ABP 后端项目搭建。
- MAUI App 项目搭建。
- 用户登录。
- 文件夹和文件列表。
- 小文件上传。
- 分片上传。
- 文件下载。
- 回收站。
- 图片缩略图。
- 视频封面。
- 视频 Range 播放。
- Docker Compose 部署。

### 16.2 V1

- 分享链接。
- 标签和收藏。
- 图片时间线。
- 视频列表页。
- 上传队列增强。
- MinIO 存储。
- 管理后台基础页面。
- 操作日志。

### 16.3 V2

- 文件夹打包下载。
- HLS 视频转码。
- 全文搜索。
- 自动备份任务。
- OIDC/LDAP 登录。
- 生物识别解锁。

### 16.4 V3

- 桌面端。
- 多存储策略。
- 多用户共享空间。
- AI 图片/视频识别。
- 智能相册。

## 17. Codex 开发约束

- 优先使用 ABP 原生模块和约定，不重复造身份、权限、设置、审计基础设施。
- 文件和媒体业务放在独立 `FileCenter` 模块。
- 大文件上传、下载、视频播放接口使用显式 Controller，不依赖自动 API 生成。
- 所有实体先建立清晰领域模型，再写 Application Service。
- 每个阶段必须有可运行结果。
- 每完成一个模块必须补充最小测试。
- 不直接复制 Cloudreve 代码，只参考功能和产品结构。

## 18. 第一阶段完成定义

第一阶段完成时，系统必须满足：

- 可以通过 Docker Compose 启动后端依赖。
- 可以运行 ABP API Host。
- 可以运行 MAUI Android App。
- 管理员可以登录。
- 用户可以创建文件夹。
- 用户可以上传图片和视频。
- 用户可以在 App 中看到文件列表。
- 用户可以预览图片缩略图。
- 用户可以播放 MP4 视频。
- 用户可以删除文件并从回收站恢复。
- 后端有基本集成测试。

## 19. 参考资料

- ABP Framework: https://abp.io/framework
- ABP BLOB Storing: https://abp.io/docs/latest/framework/infrastructure/blob-storing
- ABP Background Jobs: https://abp.io/docs/latest/framework/infrastructure/background-jobs
- ABP MAUI Template: https://abp.io/docs/latest/get-started/maui
- .NET MAUI FilePicker: https://learn.microsoft.com/dotnet/maui/platform-integration/storage/file-picker
- MAUI Community Toolkit MediaElement: https://learn.microsoft.com/dotnet/communitytoolkit/maui/views/mediaelement
- Cloudreve Docs: https://docs.cloudreve.org/
