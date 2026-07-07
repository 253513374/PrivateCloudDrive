# PrivateCloudDrive V1.1 发布范围定义与验收口径

| 元数据 | 值 |
|---|---|
| 文档版本 | 1.0 |
| 日期 | 2026-07-03 |
| 负责人 | Hermes 产品总监 (pm) |
| 前置版本 | V1.0 RC → `docs/release-plan-v1.0-rc.md` |
| 详细实现审计 | `docs/release-plan-v1.1-file-management.md`（代码级审计、测试覆盖、MAUI 组件状态） |
| 架构边界基线 | `docs/architecture-v1.1-file-management-boundary.md`（组件白名单、禁止项、技术债务评分、回滚方案） |

---

## 1. 版本认识与命名

| 文档 | 对应版本 |
|---|---|
| `product-roadmap-next.md` §4.2 | V1.1 文件管理体验增强 |
| `prd-v1.1-file-management-experience.md` | V1.1 PRD |
| `architecture-v1.1-file-management-boundary.md` | V1.1 架构边界 + 技术债务基线 |
| `product-planning-hub.md` §6 | Now 阶段（V1.2 RC 收口周期中已有的 V1.1 能力） |
| **本文档** | V1.1 发布范围与验收口径 |

### 版本命名对齐

- `product-roadmap-next.md` (2026-05-09) 首次定义 V1.1 范围：搜索、排序/筛选、批量操作、重命名/移动、容量、上传队列、分享管理。
- `product-planning-hub.md` (2026-05-14) 将当时阶段重新命名为 V1.2 RC，但 V1.1 功能在 roadmap 撰写前后已经实现。架构基线文档（2026-07-03）确认后端 P0/P1 已基本完成。
- **V1.1 的实际交付状态**：后端 + MAUI 的 V1.1 功能已基本实现，当前阶段重心是验收确认、安全边界加固、缺陷修复和文档同步，不是从零开发。

---

## 2. 版本目标

> PrivateCloudDrive V1.1 = 文件管理体验增强 — 让用户日常管理大量文件时更顺手。

核心目标：在 V1.0 RC 发布边界上，把"日常管理大量文件"这条用户路径补齐。

### 用户价值

| 场景 | V1.0 RC 状态 | V1.1 解决 |
|---|---|---|
| 找文件 | 只能手动翻文件夹 | 关键词搜索 + 排序 + 类型/媒体/收藏/标签筛选 |
| 整理文件 | 只能单文件删除/收藏 | 多选 → 批量删除、恢复、永久删除、移动、收藏 |
| 文件组织闭环 | 无重命名/移动 UI | 文件详情可重命名、移动 |
| 容量不透明 | 没有展示 | Settings 容量卡片 + 上传超限提示 |
| 上传无反馈 | 无重试/取消 | 上传队列重试/取消 |
| 分享管理不便 | 只能创建分享 | 我的分享列表、复制链接、取消分享 |

### 本版不做

- 不新增媒体库能力（V1.2 规划）
- 不引入多用户/家庭空间（V2 规划）
- 不改变部署架构（Docker Compose、PostgreSQL、Redis、OpenIddict 不变）
- 不替换 MAUI 或做大规模 UI 视觉重构

---

## 3. 发布范围

### 3.1 P0：核心文件管理增强（必须通过才能发布 V1.1）

| 编号 | 能力 | 验收标准 | 后端状态 | MAUI 状态 | 测试覆盖 | 风险等级 | 回滚方案 |
|---|---|---|---|---|---|---|---|
| V1.1-P0-01 | **文件名搜索** | 输入关键词返回匹配文件/文件夹；支持当前目录搜索和全盘搜索；搜索范围限当前用户/租户，不跨用户泄露；空结果有可理解提示；分页正常 | ✅ 已实现（`SearchKeyword`、`SearchScope`、`ILIKE`） | ✅ 已实现（`FilesSearchBar` + `SearchAllSwitch`） | ✅ EF 集成测试覆盖 | 低 | 如全局搜索性能或安全不稳定，可隐藏"全盘搜索"开关，仅保留当前目录搜索 |
| V1.1-P0-02 | **排序与筛选** | 文件页可切换排序（名称/修改时间/大小/类型）；可筛选（类型/媒体/收藏/标签）；筛选和搜索可组合；排序字段来自服务端 allowlist，未知排序值降级到默认 | ✅ 已实现（allowlist switch + ABP Sorting） | ✅ 已实现（`SortPicker`、`TypeFilterPicker`、`MediaFilterPicker`） | ✅ 排序测试已覆盖；筛选隐含在搜索测试 | 低 | 保持当前实现，降级到默认排序即可 |
| V1.1-P0-03 | **批量选择与批量操作** | 多选后可批量删除（移入回收站）、恢复、永久删除、移动、收藏/取消收藏；批量上限 100 项；逐项 owner/tenant 校验；危险操作二次确认；永久删除文案明确不可恢复；部分失败有可理解错误 | ✅ 已实现（`BatchFileNodeInput`、`MaxBatchItemCount=100`、逐项校验） | ✅ 已实现（`BatchToolbar` + 确认弹窗） | ✅ EF 集成测试覆盖 | 中 | 如批量永久删除稳定性不确定，可先隐藏批量永久删除入口，仅保留单项永久删除和批量移入回收站 |
| V1.1-P0-04 | **重命名** | 文件/文件夹可重命名；同级重名冲突展示可读错误；非法字符/空名/超长校验 | ✅ 已实现（`RenameAsync`） | ⚠️ 需确认（详情页入口是否存在） | ✅ 重命名测试已覆盖 | 低 | 若 MAUI 入口未完成，标记为已知限制 |
| V1.1-P0-05 | **移动（跨文件夹）** | 文件可移动到目标文件夹；循环移动检测拒绝；目标目录归属校验；刷新后列表和路径正确 | ✅ 已实现（`MoveAsync`、`MoveManyAsync`、循环检测） | ⚠️ 仅支持移至根目录（完整目录选择器未确认） | ✅ 循环移动拒绝测试已覆盖 | 低 | 如目录选择器不稳定，保留后端能力和批量移至根目录，隐藏复杂移动入口 |
| V1.1-P0-06 | **容量展示** | Settings 页显示已用/配额/剩余/百分比/单文件上限；上传超限时错误文案可区分容量不足、单文件过大、网络失败、认证过期；不泄露服务器物理路径 | ✅ 已实现（`StorageUsageDto`、`GetUsageAsync`） | ⚠️ 硬编码（Progress="1"，未接入 API） | 单元测试存在 | 中 | 若未完成接入 API，保留硬编码值，标记为 Degraded/未知状态 |
| V1.1-P0-07 | **批量移动端安全加固** | 搜索不返回其他用户文件；逐项 owner/tenant 校验；排序字段来自 allowlist；日志不泄露 token/password/secret/物理路径 | ✅ 已实现（DAO 层已按 TenantId+OwnerId 过滤） | — | ✅ EF 集成测试覆盖跨用户不可见 | 低 | 安全测试稳定可降级到仅当前目录搜索 |

### 3.2 P1：辅助体验增强（鼓励通过，不阻塞发布）

| 编号 | 能力 | 验收标准 | 后端状态 | MAUI 状态 | 风险等级 | 规避方案 |
|---|---|---|---|---|---|---|
| V1.1-P1-01 | **上传队列重试/取消** | 上传失败后可重试；上传中可取消；错误信息可读；列表在当前 session 内反映队列状态 | ✅ 已实现（`UploadSession Cancelled/Pending/Completed`） | ✅ 已实现（`UploadStatusPanel` 显示进度/状态） | 低 | 保持现有上传机制，不阻塞发布 |
| V1.1-P1-02 | **分享管理体验** | 用户可查看"我的分享"列表；可复制分享链接；可取消/禁用分享；可查看有效期、密码状态、访问次数；不显示密码明文；不泄露他人分享 | ✅ 已实现（`GetSharesAsync`、`DisableShareAsync`） | ⚠️ 待确认（API client 已就绪，分享管理页面可能存在缺口） | 中 | 若分享管理页面未完成，保持 V1.0 RC 的创建分享和取消当前文件分享能力，隐藏"我的分享"聚合页 |
| V1.1-P1-03 | **操作日志覆盖** | 批量删除、永久删除、分享停用、容量拒绝等关键行为记录可审计事件 | ⚠️ 需确认覆盖度 | — | 中 | 若未完全覆盖，写入已知限制 |
| V1.1-P1-04 | **文档同步** | `docs/testing.md` 增加 V1.1 验收矩阵；Release Notes 写入已知限制 | — | — | 低 | — |

---

## 4. 当前实现状态摘要

> 详细代码级审计见 `docs/release-plan-v1.1-file-management.md`（242 行），含后端实现状态、MAUI 组件状态、测试覆盖清单。

### 4.1 后端 V1.1 功能状态

| 功能 | 状态 | 关键证据 |
|---|---|---|
| 搜索、排序、筛选 | ✅ 全部完成 | `GetFolderChildrenInput` 含 `SearchKeyword`/`SearchScope`/`NodeType`/`MediaType`/`Sorting`/`IsFavorite`/`TagId` |
| 批量操作 | ✅ 全部完成 | `BatchFileNodeInput` + `DeleteManyAsync`/`RestoreManyAsync`/`PermanentDeleteManyAsync`/`MoveManyAsync`/`SetFavoriteManyAsync` |
| 重命名/移动 | ✅ 全部完成 | `RenameAsync`/`MoveAsync` + 循环移动检测 |
| 容量 | ✅ 全部完成 | `StorageUsageDto` + `GetUsageAsync` |
| 上传会话 | ✅ 全部完成 | `UploadSession` (Pending/Completed/Cancelled) |
| 分享管理 | ✅ 全部完成 | `GetSharesAsync`/`DisableShareAsync` + owner 校验 |

### 4.2 MAUI V1.1 组件状态

| 功能 | 状态 | 证据 |
|---|---|---|
| 搜索框 (+ 全局/当前目录切换) | ✅ 已完成 | `FilesSearchBar` + `SearchAllSwitch` |
| 排序选择器（名称/时间/大小/类型） | ✅ 已完成 | `SortPicker` |
| 类型/媒体筛选 | ✅ 已完成 | `TypeFilterPicker` + `MediaFilterPicker` |
| 批量选择模式 + 工具栏 | ✅ 已完成 | `SelectionModeButton` + `BatchToolbar`（删除/收藏/取消收藏/移至根目录） |
| 上传状态面板 | ✅ 已完成 | `UploadStatusPanel`（进度条 + 重试/取消） |
| 容量展示 | ⚠️ 硬编码待接入 | ProgressBar Progress="1" 未绑定 API |
| 重命名/分享管理入口 | ⚠️ 待确认 | 详情页/设置页入口需验证 |

### 4.3 测试覆盖

| 测试范围 | 状态 | 文件 |
|---|---|---|
| 搜索当前目录 + 全盘搜索 + 用户隔离 | ✅ 已覆盖 | `EfCoreFileCenterFoldersAppServiceTests.cs` |
| 排序 | ✅ 已覆盖 | 同上 |
| 批量操作（移动/收藏/删除/恢复/永久删除） | ✅ 已覆盖 | 同上 |
| 重命名/移动 + 循环移动拒绝 | ✅ 已覆盖 | 同上 |
| 分块上传会话 | ✅ 已覆盖 | `EfCoreFileCenterFileUploadServiceTests.cs` |
| 跨用户/跨租户不可见 | ✅ 已覆盖 | 搜索测试包含 `Should_Not_Return_Other_User_Nodes_When_Searching_All` |

---

## 5. 技术债务发布门禁

> 来源：`architecture-v1.1-file-management-boundary.md` §4。以下是直接影响 V1.1 发布可信度的技术债务项。

### P0 — 必须修复或明确降级

| 编号 | 技术债务 | 影响 | 处理方案 | 负责人 |
|---|---|---|---|---|
| TD-01 | 搜索/筛选/排序安全契约与测试矩阵 | 隐私、越权 | 补充 EF 集成测试：跨用户不可见、跨租户不可见、类型筛选、排序 fallback | backend-eng |
| TD-02 | 批量操作误删/越权/部分失败边界 | 数据安全、不可恢复风险 | 固定 100 上限、二次确认、逐项归属校验、危险操作审计；明确全失败/部分成功策略 | backend-eng + mobile-eng |
| TD-03 | 排序字段 allowlist | 查询安全 | 禁止自由排序表达式；补未知 sorting fallback 测试 | backend-eng |
| TD-05 | 永久删除与 Blob/媒体文件清理回归验证 | 存储成本、不可恢复 | 补删除树/共享 blob/媒体资产清理测试；UI 写明不可恢复 | backend-eng + mobile-eng |

### P1 — 鼓励修复，不阻塞发布

| 编号 | 技术债务 | 影响 | 处理方案 | 负责人 |
|---|---|---|---|---|
| TD-06 | 容量展示与上传失败原因一致性 | Settings、上传体验 | 明确 used/quota/available/maxSingleFileSize 来源；上传超限可读错误 | backend-eng + mobile-eng |
| TD-08 | 分享管理区分"我的分享"和管理员全局管理 | 分享安全 | 普通用户仅管理自己的分享；管理员全局管理未完成则后置 | backend-eng + security-reviewer |
| TD-09 | 操作日志对批量/分享/删除关键行为覆盖 | 审计排障 | 批量删除、永久删除、分享停用、容量拒绝记录事件 | backend-eng |
| TD-10 | V1.1 文档同步 | 发布可信度 | `docs/testing.md` + Release Notes + 已知限制 | pm |

---

## 6. 明确不做（Out-of-Scope）

| 方向 | 原因 | 规划版本 |
|---|---|---|
| NAS OS / RAID / 磁盘池管理 | 不是云盘产品主线 | 不建议 |
| SMB/NFS/AFP 协议 | 不是移动优先场景 | 不建议 |
| 桌面同步客户端 | 冲突解决、离线、双向同步成本高 | V2 候选 |
| Office 在线协作文档 | 技术复杂度高，偏离文件与媒体中心 | 不建议 |
| AI 相册 / 语义搜索 | 隐私、索引、算力、模型治理风险高 | V2 候选 |
| HLS 转码 / 视频低清预览 | V1.2 媒体库增强 | V1.2 |
| 多用户/家庭空间/团队空间 | 改变 owner/tenant 权限模型 | V2 候选 |
| MinIO/OSS 存储迁移与回滚 | 引入凭据和一致性风险 | V1.3+ |
| 文件夹打包下载（流式压缩） | 压缩流/超时/磁盘占用/取消策略复杂度高 | V1.2 P2 |
| iOS 客户端第一版 | 未进入移动端双平台目标 | 待定 |
| 微服务拆分 | 放大部署/事务/测试成本 | V2+ |
| 替换 OpenIddict 或自研 JWT | 高安全风险，破坏移动端 token 生命周期 | 不建议 |
| 引入 Elasticsearch/Meilisearch | 增加运维和部署复杂度 | V2 候选 |
| 大规模 UI 视觉重构 | V1.1 应补交互闭环，不改变工具型基线 | 独立设计任务 |

---

## 7. 依赖顺序与版本边界

### 7.1 与 V1.0 RC 的边界

```mermaid
flowchart LR
    RC[V1.0 RC 发布] -->|稳定基线| V11[V1.1 文件管理体验增强]
    V11 --> V12[V1.2 媒体库产品化]
```

V1.1 **不依赖** V1.0 RC 的任何新功能，但依赖 V1.0 RC 的**稳定发布基础**：

| V1.0 RC 项 | 对 V1.1 的意义 | 处理 |
|---|---|---|
| Docker 健康检查 | V1.1 容量展示/Settings 依赖 Storage/DB/Redis 健康 | 作为 V1.1 发布前置门禁 |
| Secret/日志扫描 | V1.1 搜索、批量、分享、上传错误日志需纳管 | 继续作为发布门禁 |
| 备份恢复边界 | V1.1 批量和永久删除入口增加数据安全风险 | 发布前强调 DB + storage + .env 备份 |
| 外部登录降级 | V1.1 文件管理主链路不可被可选登录阻塞 | 保持账号密码 + refresh token 主线可用 |
| Android 真机主链路验收 | 扩展为 V1.1 真机验收 | 搜索/排序/筛选/批量/分享/容量/上传 |
| MAUI 构建脚本验收 | V1.1 MAUI 改动后必须跑分平台构建 | FilesPage/Trash/Shares/Settings 是高改动区域 |
| ABP 分层治理 | V1.1 新增契约/服务/测试必须按层落位 | 防止体验修复演变成越层技术债 |
| 审计日志覆盖 | 扩展到批量、分享、永久删除、容量拒绝 | V1.1 操作更危险，需排障追踪 |

### 7.2 推荐发布顺序

| 阶段 | 内容 | 负责人 | 交付物 | 建议工期 |
|---|---|---|---|---|
| **Phase 1：安全与测试加固** | 补充搜索/排序/筛选安全集成测试；批量操作逐项校验回归；排序 allowlist 加固；永久删除 Blob 清理回归 | backend-eng | EF 集成测试 + Application Service 测试 | 1-2 天 |
| **Phase 2：MAUI 缺陷修复** | 容量展示接入 API（替换硬编码）；确认重命名入口；确认分享管理页面状态 | mobile-eng | MAUI 代码修改 + 构建验证 | 1-2 天 |
| **Phase 3：真机验收** | Android 真机完成 V1.1 P0 全部功能验收；操作日志审计覆盖度确认 | qa-eng + security-reviewer | `docs/testing.md` V1.1 验收记录 | 2-3 天 |
| **Phase 4：文档与发布** | 更新 testing.md、Release Notes、已知限制；同步 planning hub 状态 | pm | `docs/release-notes-v1.1.md`；planning hub 更新 | 1 天 |

### 7.3 推荐两批发布方案

> 来源：`architecture-v1.1-file-management-boundary.md` §9

如果团队无法一次性完成全部体验增强：

| 方案 | 范围 | 可跳过项 |
|---|---|---|
| **V1.1a（推荐最小发布）** | 搜索、排序/筛选、批量删除/恢复、重命名、容量展示 | — |
| **V1.1b（后置增强）** | 完整移动目录选择、批量永久删除、上传队列重试/取消、分享管理列表页 | 不阻塞 V1.1a 发布 |

---

## 8. 指派团队与职责

| 角色 | Profile | 事项 | 优先级 | 交付物 |
|---|---|---|---|---|
| 包后端 | backend-eng | 安全契约加固 + 测试补全 + 排序 allowlist + 永久删除回归 | P0 | EF 集成测试、Application Service 测试 |
| 莫移动 | mobile-eng | 容量 API 接入 + 重命名入口确认 + 分享管理页确认 | P0 | MAUI 代码修改、构建验证 |
| 丁 DevOps | devops-eng | V1.1 验收命令纳入发布清单 + MAUI 构建验证 | P1 | `docs/testing.md` 更新 |
| 安安全 | security-reviewer | 搜索越权复核 + 批量误删防护 + 日志脱敏 | P0 | 安全复核报告 |
| 齐 QA | qa-eng | V1.1 验收矩阵形成 + Android 真机验收记录 | P1 | 真机测试记录 PASS/WARN/FAIL |

---

## 9. 发布闸门

| 闸门 | 标准 | 对应项 |
|---|---|---|
| G0 范围冻结 | 只做 V1.1 P0/P1 范围内缺陷修复，不新增功能 | 本文档 §3 |
| G1 构建测试 | `dotnet build/test` 通过；MAUI Windows/Android 构建通过 | V1.0 RC 已有基线 |
| G2 安全合规 | 搜索不跨用户泄露；排序 allowlist 加固；批量逐项校验；日志不泄露敏感信息 | TD-01/02/03/05 |
| G3 后端验收 | P0 API 全部可用：搜索/排序/筛选/批量/重命名/移动/容量 | 本文档 §3.1 |
| G4 MAUI 验收 | 文件页搜索/排序/筛选/多选/批量操作可达可操作；容量展示不空白/不混淆 | 本文档 §3.1 |
| G5 Android 真机验收 | 至少一台 Android 真机完成 V1.1 P0 全部功能验收 | qa-eng 验收记录 |
| G6 文档同步 | testing.md、release-notes、已知限制同步 | pm 交付 |
| G7 回滚就绪 | P0 功能都有明确的回滚/降级方案 | 本文档 §3.1 回滚方案列 |

### 放行标准

```
P0 = 0 阻塞缺陷
P1 = 0 缺陷，或每个 P1 都有明确规避方案和发布批准
P2 已记录到已知限制
```

---

## 10. 已知限制（V1.1 发布）

- 搜索使用 PostgreSQL `ILIKE`（`NormalizedName.Contains`），不是全文搜索引擎，个人/家庭规模下性能充足，大目录（10万+文件）未实测。
- 容量展示当前 MAUI 端未接入 `StorageUsageDto` API，ProgressBar 为硬编码值；若 Phase 2 未修复，标记为 Degraded 状态。
- 分享管理页 MAUI 端可能存在入口缺口（我的分享列表页）；若未完成，保留单文件分享创建/取消能力。
- 批量操作前端选择当前局限在当期页面加载项，跨页全量多选未实现。
- 移动操作 MAUI 端当前仅支持"移至根目录"，完整文件夹选择器未确认可用。
- 操作日志对批量删除/永久删除/分享停用的审计事件覆盖度需 Phase 3 确认。
- iOS 客户端不在 V1.1 范围内；MAUI 构建仅验证 Windows 和 Android 目标。
- 微信/Google/GitHub 外部登录保持 V1.0 RC 的降级策略：未配置时不显示入口，不影响账号密码主链路。

---

## 11. 立即可执行的任务提示词

### 给 backend-eng

```text
你现在在 PrivateCloudDrive 仓库中工作。当前分支 `docs/android-evidence-r2`。
请阅读：
- docs/architecture-v1.1-file-management-boundary.md §5（V11-FIX-01/02/03）
- docs/release-plan-v1.1.md §5（TD-01/02/03/05）

任务：
1. 确认 `EfCoreFileNodeRepository` 的搜索/排序/筛选查询已按 TenantId + OwnerId 严格过滤，补充跨用户/跨租户不可见的 EF 集成测试。
2. 确认 Sorting allowlist 拒绝未知排序字段，降级到默认排序而不是抛异常。
3. 确认 `PermanentDeleteManyAsync` 的 Blob/媒体缩略图/预览清理不会误删共享引用。
4. 确认批量操作最大 100 项限制、去重、空 Guid 过滤、逐项归属校验有效。

验收：
- `dotnet test aspnet-core/PrivateCloudDrive.slnx` 全部通过（现有 + 新增测试）。
- 跨用户搜索请求返回空结果。
- 未知排序值不会导致 500 错误。
```

### 给 mobile-eng

```text
你现在在 PrivateCloudDrive 仓库中工作。当前分支 `docs/android-evidence-r2`。
请阅读：
- docs/release-plan-v1.1.md §3.1（V1.1-P0-04/05/06）
- docs/release-plan-v1.1-file-management.md §2.2（MAUI 当前状态）

任务：
1. 确认 FilesPage 的容量 ProgressBar 当前值，如未接入 `StorageUsageDto` API，接入并将硬编码替换为真实值。
2. 确认文件详情页或更多菜单是否包含重命名入口；如缺失，评估是否需要补充或标记已知限制。
3. 确认 MAUI 端是否存在"我的分享"管理页面（`SharesPage` 或 `SettingsPage` 分享入口）；如缺失，评估补充或标记已知限制。
4. 确认批量操作（删除/恢复/永久删除）的二次确认弹窗文案准确，永久删除明确写明"不可恢复"。

验收：
- `scripts/verify-maui-build.sh` (Windows + Android) 通过。
- 容量展示非硬编码值（或标记为 Degraded 已知限制）。
```

---

## 12. 决策记录

| 决策 | 选择 | 原因 |
|---|---|---|
| 搜索方案 | PostgreSQL ILIKE，不引入外部搜索引擎 | 个人/家庭规模性能充足，零运维成本 |
| 排序实现 | 服务端 allowlist，不传客户端自由表达式 | 防止注入和不可预期查询 |
| 批量上限 | 100 项 | 平衡性能和误删风险 |
| 容量来源 | `StorageUsageDto`（后端统计），不要求实时文件夹递归 | 个人规模下精确性可接受 |
| 上传队列 | 客户端队列优先，服务端 UploadSession 状态作为辅助 | 降低 V1.1 复杂度 |
| 分享管理 | 普通用户仅管理自己的分享，管理员全局管理后置 | 防止越权和数据泄露 |
| MAUI 平台策略 | 保持现有 MAUI 实现，不做 Flutter/RN 替换 | 避免 V1.1 被平台迁移拖垮 |

---

*本文档是对 V1.1 文件管理体验增强的产品发布范围定义与验收口径。V1.1 后端和 MAUI 前端功能已基本实现，当前阶段重心是安全加固、MAUI 缺陷修复、真机验收和文档同步。详细代码级审计见 `docs/release-plan-v1.1-file-management.md`。*
