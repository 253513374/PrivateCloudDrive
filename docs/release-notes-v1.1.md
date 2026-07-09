# PrivateCloudDrive V1.1 Release Notes

发布日期：2026-07-07
发布状态：正式发布
前置版本：[V1.0 RC](release-notes-v1.0-rc.md)
产品形态：面向个人、家庭和小团队的私有部署移动优先云盘 + 文件管理体验增强

## 1. 版本定位

PrivateCloudDrive V1.1 是 V1.0 RC 发布后的文件管理体验增强版本，聚焦于让用户在日常管理大量文件时更顺手。核心价值：

- 从"只能手动翻文件夹"升级为"关键词搜索 + 排序 + 筛选"。
- 从"只能单文件操作"升级为"多选批量操作"。
- 补齐文件组织闭环：重命名、移动、容量透明、上传队列反馈、分享管理。

V1.1 不是独立的新安装包，而是在 V1.0 RC 稳定基线基础上的增量更新。当前所有 V1.1 功能已通过后端验证、MAUI 构建验证和 Android 验收。

## 2. 本版新增功能

### 2.1 P0：核心文件管理增强

| 功能 | 说明 |
| --- | --- |
| **文件名搜索** | 输入关键词返回匹配文件/文件夹；支持当前目录搜索和全盘搜索；搜索范围限当前用户/租户，不跨用户泄露；空结果有可理解提示 |
| **排序与筛选** | 可切换排序（名称/修改时间/大小/类型）；可筛选（类型/媒体/收藏/标签）；筛选和搜索可组合；排序字段来自服务端 allowlist，未知排序值降级到默认 |
| **批量选择与批量操作** | 多选后可批量删除（移入回收站）、恢复、永久删除、移动、收藏/取消收藏；批量上限 100 项；逐项 owner/tenant 校验；危险操作二次确认；部分失败有可理解错误 |
| **重命名** | 文件/文件夹可重命名；同级重名冲突展示可读错误；非法字符/空名/超长有前端校验 |
| **移动（跨文件夹）** | 文件可移动到目标文件夹；循环移动检测拒绝；目标目录归属校验；支持移至根目录 |
| **容量展示** | Settings 页显示已用/配额/剩余/百分比/单文件上限；上传超限时错误文案可区分不同原因；API 失败时显示 Degraded 状态 |
| **批量移动端安全加固** | 搜索不返回其他用户文件；逐项 owner/tenant 校验；排序字段来自 allowlist；日志不泄露 token/password/secret/物理路径 |

### 2.2 P1：辅助体验增强

| 功能 | 说明 |
| --- | --- |
| **上传队列重试/取消** | 上传失败后可重试；上传中可取消；错误信息可读；列表在当前 session 内反映队列状态 |
| **分享管理体验** | 用户可查看"我的分享"列表；可复制分享链接；可取消/禁用分享；可查看有效期、密码状态、访问次数；不显示密码明文 |
| **操作日志覆盖** | 批量删除、永久删除、分享停用等关键行为通过 ABP 审计管线自动记录可审计事件 |

## 3. 修复

- 容量展示修复：ProgressBar 从硬编码值（Progress="1"）替换为真实 `StorageUsageDto` API 数据，包括已用/配额/百分比/剩余/单文件上限（PR #40）。
- 重命名入口确认：FileDetailsPage 已有完整重命名入口，补充空名/同名/超长/非法字符前端校验和 ABP 本地化错误提取（PR 已并入 main）。
- 分享管理页面确认：SharesPage + SettingsPage 入口完整实现，列表显示、复制链接、禁用分享、密码保护均已可用（PR 已并入 main）。
- 排序 allowlist 加固：拒绝未知排序字段，降级到默认排序而非抛异常（V11-FIX PR #38/#39）。

## 4. 部署说明

V1.1 不改变部署架构。Docker Compose、PostgreSQL、Redis、OpenIddict、MAUI 客户端均与 V1.0 RC 一致。更新方式：

### 4.1 Docker Compose（推荐）

```bash
cd /path/to/PrivateCloudDrive
git pull
docker compose down
docker compose up -d --build
```

验证：

```bash
# 后端健康检查
curl http://localhost:8080/swagger/v1/swagger.json

# 本地栈健康
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-local-stack.ps1

# MAUI 构建验证
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-maui-build.ps1
```

### 4.2 手动构建

```bash
# 后端
cd aspnet-core
dotnet build PrivateCloudDrive.slnx
dotnet test PrivateCloudDrive.slnx --no-build

# MAUI
cd ../maui/PrivateCloudDrive.App
dotnet build -f net10.0-android
```

### 4.3 前置条件

| 项目 | 要求 |
| --- | --- |
| Docker | Docker Desktop 或 Docker Engine 可用 |
| .NET | .NET 10 SDK |
| MAUI | Windows 构建需要 Windows MAUI workload；Android 构建需要 Android workload/JDK/SDK |
| 存储 | Docker volume `privateclouddrive_stack_storage` 必须纳入备份范围 |
| 配置 | `.env` 必须从 `.env.example` 复制并替换默认密码和加密短语 |
| 移动端 | Android 真机验收需要设备可访问的 `PUBLIC_URL` |

## 5. 本版明确不包含

- 不新增媒体库能力（V1.2 规划）
- 不引入多用户/家庭空间（V2 规划）
- 不改变部署架构（Docker Compose、PostgreSQL、Redis、OpenIddict 不变）
- 不替换 MAUI 或做大规模 UI 视觉重构
- NAS OS / RAID / 磁盘池管理
- SMB/NFS/AFP 协议
- 桌面同步客户端
- Office 在线协作文档
- AI 相册 / 语义搜索
- iOS 客户端第一版
- MinIO/OSS 存储迁移与回滚
- 微服务拆分

## 6. 安全与隐私要求

- `WECHAT_APP_SECRET`、`GOOGLE_CLIENT_SECRET`、`GITHUB_CLIENT_SECRET`、数据库密码、加密短语只允许存在于后端配置、环境变量或密钥系统中。
- 移动端 settings 接口只能返回公开配置，不能返回 client secret、access token、refresh token 或 provider token。
- 审计日志、验收记录和故障报告不得包含密码、token、OAuth code、provider token 或完整连接字符串。
- 搜索、排序、筛选、批量操作均按 TenantId + OwnerId 严格过滤，不跨用户泄露。
- 发布前必须确认 `.env` 没有使用模板密码或默认加密短语。

## 7. 已知限制

- 搜索使用 PostgreSQL ILIKE（`NormalizedName.Contains`），不是全文搜索引擎；个人/家庭规模下性能充足，大目录（10 万 + 文件）未实测。
- 批量操作前端选择局限在当前页面加载项，跨页全量多选未实现。
- 移动操作 MAUI 端当前仅支持"移至根目录"，完整文件夹选择器未确认可用。
- 操作日志对批量删除/永久删除/分享停用的审计事件通过 ABP 管线自动记录，但无独立审计条目覆盖度确认。
- iOS 客户端不在 V1.1 范围内；MAUI 构建仅验证 Windows 和 Android 目标。
- 微信/Google/GitHub 外部登录保持 V1.0 RC 的降级策略：未配置时不显示入口，不影响账号密码主链路。
- Android 真机验收需要后端 `PUBLIC_URL` 是手机可访问地址，不能只使用 `localhost`。
- 当前 MAUI 自动化测试主要以构建验证和手动验收为主。
- 完整 Docker 栈检查依赖本机 Docker daemon、镜像拉取能力和网络环境。

## 8. 发布后建议

V1.1 发布后，产品重心建议转向：

1. **V1.2 媒体库产品化**：时间线、相册、处理状态、失败重试等能力的产品化收口。
2. **V1.3 运维与规划版**：服务健康页、存储状态页、备份恢复脚本、升级回滚 SOP、管理员管理。
3. **多用户/家庭空间（V2 候选）**：改变 owner/tenant 权限模型，引入家庭和团队空间。
