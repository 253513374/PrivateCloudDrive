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
- 删除到回收站。

Codex 指令：

```text
实现 FileCenter 文件夹管理 Application Service 和 API。MVP Core 包括创建文件夹、查询目录列表、软删除到回收站。重命名和移动留到 V1。补充单元测试或集成测试。
```

验收：

- Swagger 能调用文件夹接口。
- 目录列表支持分页。
- 回收站文件不出现在普通目录列表。

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

- 接入第一方账号密码登录。
- 保存 Token。
- 自动刷新 Token。

Codex 指令：

```text
在 MAUI App 中实现账号密码登录功能，接入后端 OpenIddict Token Endpoint。Token 使用 SecureStorage 保存。实现登录、刷新、退出和 API 授权请求。
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

### 任务 4.4：图片预览和视频播放

目标：

- 图片大图预览。
- 视频播放页。
- 从文件首页进入图片预览和视频播放。

Codex 指令：

```text
实现 MAUI 图片预览和视频播放。图片从文件首页缩略图进入大图预览，视频从文件首页封面进入 MediaElement 播放后端 Range 视频地址。独立图片网格页和视频列表页留到 V1。
```

验收：

- 图片缩略图正常显示。
- MP4 视频可以播放和拖动进度。

## 阶段 5：MVP Core 回收站、部署与质量收尾

### 任务 5.1：回收站 API 与 App 入口

目标：

- 支持查看回收站。
- 支持恢复文件。
- 支持永久删除文件。
- 支持清空回收站。
- 恢复时处理同名冲突。

Codex 指令：

```text
根据 docs/requirements.md 和 docs/ui-design.md 实现 MVP Core 回收站。后端提供回收站列表、恢复、永久删除、清空接口；MAUI App 增加回收站入口和对应空、加载、错误状态。永久删除和清空必须二次确认。不要实现分享、标签、收藏或微信登录。
```

验收：

- 删除文件后进入回收站。
- 回收站文件不出现在普通目录列表。
- 可以恢复文件。
- 同名冲突时有明确提示或自动重命名策略。
- 可以永久删除和清空回收站。

### 任务 5.2：Docker Compose 本地私有部署

目标：

- 提供 MVP Core 本地私有部署。
- 初始化数据库。
- 配置 Volume。
- 写部署文档。

Codex 指令：

```text
为 MVP Core 添加或校正 Dockerfile 和 docker-compose.yml。包含 API Host、PostgreSQL、Redis、Media Worker。MinIO 保持可选，不作为 MVP Core 阻塞项。写明环境变量、Volume、首次启动流程和管理员初始化方式。
```

验收：

- 新机器可以通过 Docker Compose 启动后端依赖和 API。
- 文件存储 Volume 可持久化。
- DbMigrator 可以初始化管理员和默认权限。

### 任务 5.3：MVP Core 测试补齐

目标：

- 补齐核心业务测试。
- 保证登录、上传、下载、Range、媒体任务、回收站稳定。

Codex 指令：

```text
为 MVP Core 补齐测试。覆盖管理员初始化、账号密码登录、Token 刷新、小文件上传、分片上传、流式下载、Range、图片缩略图、视频封面、回收站恢复和永久删除。不要把分享、标签、收藏、微信登录作为 MVP Core 测试阻塞项。
```

验收：

- 后端测试全部通过。
- MAUI Windows 和 Android build 通过。

### 任务 5.4：README 和使用文档

目标：

- 写项目说明。
- 写开发环境搭建。
- 写私有部署步骤。
- 写 App 配置说明。
- 明确 MVP Core 和 V1 边界。

Codex 指令：

```text
编写 README 和部署文档。内容包括项目介绍、MVP Core 范围、V1 范围、技术栈、开发环境、后端启动、MAUI 启动、Docker Compose 部署、常见问题。
```

验收：

- 新开发者可以按文档启动 MVP Core。
- 文档明确微信登录不阻塞 MVP Core。

## 阶段 6：MVP Core 产品体验与账号密码认证深化

本阶段基于 `docs/ui-design.md` 和 `docs/auth-design.md` 执行，目标是把 MVP Core 做成稳定可用的移动端体验。

### 任务 6.1：落地 MAUI 设计系统

目标：

- 按 `docs/ui-design.md` 统一颜色、字体、间距、圆角、按钮、输入框、列表项、底部导航和操作菜单。
- 清理 MAUI 模板遗留的非产品色彩和营销式样式。
- 保持 Android、iOS、Windows 构建可用。

Codex 指令：

```text
根据 docs/ui-design.md 重构 MAUI App 的全局样式资源。只做设计系统和 MVP Core Shell/导航基础调整，不改后端业务。更新 Colors.xaml、Styles.xaml、AppShell.xaml 以及必要页面样式。MVP Core 底部导航包含 Files、Uploads、Trash、Settings；Photos 和 Videos 作为 V1 入口暂不实现。
```

验收：

- 登录页和主界面使用统一颜色与字号。
- 底部导航包含 Files、Uploads、Trash、Settings。
- 按钮、输入框、列表项触控区域不小于 44 x 44。
- MAUI Windows 和 Android 构建通过。

### 任务 6.2：补齐 MVP Core 页面状态

目标：

- 为启动页、登录页、文件首页、上传队列页、文件详情页、图片预览页、视频播放页、回收站页、设置页补齐空、加载、错误状态。
- 文件和媒体页面以内容为中心，提高大量文件扫描效率。

Codex 指令：

```text
根据 docs/ui-design.md 补齐 MAUI App MVP Core 页面状态。优先实现页面结构、状态展示和交互入口，保持现有 API 调用可用。不要新增营销页、分享设置页、独立图片媒体库、独立视频媒体库或装饰背景。
```

验收：

- MVP Core 每个页面都有明确空状态、加载状态和错误状态。
- 图片从文件首页进入预览。
- 视频从文件首页进入播放。
- 上传队列能显示进行中、失败、完成状态。
- 回收站能显示空、加载、错误和同名冲突提示。

### 任务 6.3：实现账号密码登录与 Token 生命周期

目标：

- 按 `docs/auth-design.md` 支持 MAUI 第一方账号密码登录。
- 使用 OpenIddict Token Endpoint 获取 access token 和 refresh token。
- 使用 SecureStorage 保存 Token。
- 退出登录撤销或清理 Token。

Codex 指令：

```text
根据 docs/auth-design.md 调整后端 OpenIddict 移动客户端和 MAUI AuthService。MVP Core 主登录方式为账号密码登录，支持 PrivateCloudDrive_App 使用 password 和 refresh_token。Token 必须使用 SecureStorage 保存，退出登录必须清理本地 Token。authorization_code 只作为兼容或后续方案，微信登录不要在本任务实现。
```

验收：

- 管理员可以在 MAUI App 用账号密码登录。
- Access Token 过期前可以刷新。
- Refresh Token 失效后回到登录页。
- 退出登录后 SecureStorage 中没有 Token。
- 登录失败不会保存任何 Token。

### 任务 6.4：补齐登录审计与安全策略

目标：

- 记录账号密码登录成功、失败、刷新失败、退出登录。
- 登录失败限流和锁定策略对齐 ABP Identity。
- Token 和密码不进入日志。

Codex 指令：

```text
根据 docs/auth-design.md 为 MVP Core 移动端认证补齐审计日志和安全策略。优先使用 ABP Identity、ABP 审计和 OpenIddict 机制，不自建身份系统。补充测试验证登录成功、登录失败、刷新失败和退出登录审计记录。
```

验收：

- 后端能查询登录审计日志。
- 登录失败有明确错误码。
- 日志中不包含密码、access token、refresh token。

### 任务 6.5：真实设备 MVP Core 体验验收

目标：

- 在真实 Android 或 iOS 设备上验证账号密码登录、刷新、上传、预览、播放、回收站和退出。
- 记录体验问题和后续优化任务。

Codex 指令：

```text
根据 docs/ui-design.md 和 docs/auth-design.md 制定真实设备 MVP Core 手动验收清单，并在 docs/testing.md 追加移动端体验验收项。不要修改业务代码，先输出检查清单和需要人工执行的步骤。
```

验收：

- `docs/testing.md` 包含 Android/iOS 账号密码登录、Token 刷新、上传、图片预览、视频播放、回收站、退出登录检查项。
- 每个检查项都有预期结果。

## 阶段 7：V1 分享、标签、收藏与操作日志

### 任务 7.1：分享链接

目标：

- 创建分享链接。
- 支持密码和过期时间。
- 支持公开访问接口。
- 支持管理所有分享。

Codex 指令：

```text
根据 docs/requirements.md 实现 V1 分享链接功能。支持文件和文件夹分享、访问密码、过期时间、是否允许下载、公开访问 API、权限校验和管理员管理所有分享。不要实现微信登录。
```

验收：

- 分享链接可访问。
- 过期或密码错误不可访问。
- 管理员可以管理所有分享。

### 任务 7.2：标签和收藏

目标：

- 创建标签。
- 给文件绑定标签。
- 收藏文件。
- 按标签和收藏筛选。

Codex 指令：

```text
根据 docs/requirements.md 实现 V1 标签和收藏功能。用户可以创建自己的标签，给文件绑定标签，收藏文件，并在文件列表和媒体页筛选。标签筛选和收藏筛选只能在模块启用后显示。
```

验收：

- 文件可以添加标签。
- 文件可以收藏。
- 文件可以按标签和收藏查询。

### 任务 7.3：图片和视频媒体库入口

目标：

- 实现独立图片网格页。
- 实现独立视频列表页。
- 支持标签和收藏筛选启用后的入口。

Codex 指令：

```text
根据 docs/ui-design.md 实现 V1 图片网格页和视频列表页。图片页使用缩略图网格，视频页使用封面和时长列表。标签和收藏筛选仅在对应模块启用后显示。
```

验收：

- 图片页显示真实缩略图。
- 视频页显示真实封面和时长。
- 筛选入口与模块启用状态一致。

### 任务 7.4：操作日志查询

目标：

- 查询登录、上传、删除、恢复、永久删除、分享、管理员操作日志。
- 管理员可以按用户、操作类型、时间范围筛选。

Codex 指令：

```text
根据 docs/requirements.md 实现 V1 操作日志查询。优先复用 ABP 审计与安全日志机制，补充必要的 FileCenter 和 MobileAuth 业务审计记录。普通用户不能查看其他用户日志。
```

验收：

- 管理员可以查询操作日志。
- 普通用户不能查看其他用户日志。
- 日志不包含密码、access token、refresh token、微信 AppSecret。

## 阶段 8：V1 微信登录可选接入

本阶段基于 `docs/wechat-login-design.md` 执行。微信登录必须受配置开关控制，未配置或失败时不能影响账号密码登录。

### 任务 8.1：微信登录后端基础

目标：

- 增加微信登录配置。
- 实现微信身份换取服务。
- 实现 `WechatUserBinding`。
- 实现绑定/解绑接口。
- 实现或预留 OpenIddict 微信扩展 grant。

Codex 指令：

```text
根据 docs/wechat-login-design.md 实现 V1 微信登录后端基础。新增配置项、WechatUserBinding 实体、EF 迁移、微信 code 换取身份服务、绑定/解绑接口，以及 OpenIddict 微信扩展 grant 的骨架或完整实现。WechatUserBinding 放在 MobileAuth 或认证扩展模块，不放入 FileCenter。不要在代码中写死 AppId/AppSecret。
```

验收：

- 未启用微信配置时，后端返回 wechat_disabled，不影响账号密码登录。
- 后端配置项可通过 appsettings 和环境变量覆盖。
- 微信绑定表有唯一约束。
- 绑定、解绑、登录失败均有审计日志。

### 任务 8.2：MAUI 微信登录与绑定入口

目标：

- MAUI App 通过平台微信 SDK 拉起授权。
- App 获取 code 后交给后端。
- 支持首次绑定、已登录绑定和解绑。

Codex 指令：

```text
根据 docs/wechat-login-design.md 在 MAUI App 中增加微信登录平台抽象和 UI 入口。Android/iOS 分别封装平台实现；未安装微信或未启用配置时隐藏或禁用微信按钮。微信失败不能影响账号密码登录。
```

验收：

- 微信按钮只在启用且平台可用时显示。
- 用户取消微信授权时停留登录页。
- 未绑定微信进入绑定账号流程。
- 已登录用户可以从设置页绑定或解绑微信。
- 微信失败不清理本地账号密码登录 Token。
