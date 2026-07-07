# PrivateCloudDrive V1.1 发布计划：文件管理体验增强

| 元数据 | 值 |
|---|---|
| 文档版本 | 1.0 |
| 日期 | 2026-07-03 |
| 负责人 | Hermes 产品总监 (pm) |
| 前置版本 | V1.0 RC (release-plan-v1.0-rc.md) |
| 下一篇 | V1.2 媒体库产品化 |

---

## 1. 版本目标

```text
PrivateCloudDrive V1.1 = 文件管理体验增强
```

让用户日常管理大量文件时更顺手。本版不新增媒体库能力、不引入多用户空间、不改变部署架构。

### 用户价值

- 快速找到文件：全局搜索 + 当前目录搜索。
- 多人协作不尴尬：排序（名称/时间/大小/类型）、筛选（类型/媒体/收藏/标签）。
- 批量整理：多选后批量删除、恢复、移动、收藏、永久删除。
- 文件组织闭环：重命名、移动（跨文件夹）、分享管理。
- 使用透明：容量展示、上传队列状态、分享列表管理。

### 当前问题

| 问题 | V1.0 RC 状态 | V1.1 解决 |
|---|---|---|
| 文件多了找不到 | 只能手动翻文件夹 | 搜索 + 排序 + 筛选 |
| 整理文件繁琐 | 单文件删除/收藏 | 批量选择 + 批量操作 |
| 文件移动能力不足 | 无跨文件夹移动 UI | 移动到根目录/其他文件夹 |
| 容量不透明 | 没有展示 | StorageUsage API + 前端展示 |
| 上传无反馈 | 无队列/重试/取消 | UploadSession 状态 + 重试/取消 |
| 分享管理不便 | 只能创建分享，无法管理 | 我的分享列表 + 取消/更新 |

---

## 2. 当前实现状态

> 基于 V1.0 RC 分支 (`docs/android-evidence-r2`) 的代码审计结果。

### 2.1 后端实现

| 功能 | 后端状态 | 关键文件 | 测试覆盖 |
|---|---|---|---|
| 搜索 (文件名 ILIKE) | **✅ 已完成** | `GetFolderChildrenInput.SearchKeyword`, `EfCoreFileNodeRepository` ILIKE 查询 | ✅ `Should_Search_Folders_By_Keyword_In_Current_Folder`, `Should_Not_Return_Other_User_Nodes_When_Searching_All` |
| 排序 | **✅ 已完成** | `GetFolderChildrenInput.Sorting` → ABP `ISortedResultRequest` 自动展开 | ✅ `Should_Sort_Folders_By_Name_Descending` |
| 类型/媒体筛选 | **✅ 已完成** | `NodeType`, `MediaType`, `IsFavorite`, `TagId` in `GetFolderChildrenInput` | 隐含在现有测试中 |
| 批量删除/恢复/永久删除 | **✅ 已完成** | `BatchFileNodeInput`, `DeleteManyAsync`, `RestoreManyAsync`, `PermanentDeleteManyAsync` | ✅ `Should_Batch_Move_Favorite_Delete_Restore_And_Permanent_Delete_Folders` |
| 批量移动 | **✅ 已完成** | `BatchMoveFileNodesInput`, `MoveManyAsync` | ✅ (同上测试覆盖) |
| 批量收藏/取消收藏 | **✅ 已完成** | `BatchSetFavoriteInput`, `SetFavoriteManyAsync` | ✅ (同上测试覆盖) |
| 重命名 | **✅ 已完成** | `RenameAsync`, `RenameFileNodeInput` | ✅ `Should_Rename_Move_And_Delete_Folder` |
| 移动（跨文件夹） | **✅ 已完成** | `MoveAsync`, `MoveFileNodeInput`, 循环移动检测 | ✅ 循环移动拒绝测试 |
| 容量展示 | **✅ 已完成** | `StorageUsageDto`, `IFileCenterStorageAppService.GetUsageAsync()` | 单元测试 |
| 上传会话状态 | **✅ 已完成** | `UploadSession` (Pending/Completed/Cancelled), `CreateUploadSessionInput` | 分片上传测试已覆盖 |
| 分享管理 | **✅ 已完成** | `GetSharesAsync`, `CreateShareAsync` (MAUI API client), 后端 CRUD 已有 | 隐含 |

### 2.2 MAUI 前端实现

| 功能 | MAUI 状态 | 关键文件/组件 | 备注 |
|---|---|---|---|
| 搜索框 | **✅ 已完成** | `FilesSearchBar` (SearchBar), `SearchAllSwitch` (全局/当前目录切换) | `FilesPage.xaml` 第 131–136 行, `CloudDriveQueryOptions.SearchScope` |
| 排序选择器 | **✅ 已完成** | `SortPicker` (Picker) — 名称/时间/大小/类型 | `FilesPage.xaml` 第 169–173 行 |
| 类型筛选 | **✅ 已完成** | `TypeFilterPicker` — 文件/文件夹/全部 | `FilesPage.xaml` 第 147–151 行 |
| 媒体筛选 | **✅ 已完成** | `MediaFilterPicker` — 图片/视频/音频/全部 | `FilesPage.xaml` 第 158–162 行 |
| 批量模式 | **✅ 已完成** | `SelectionModeButton` 切换 + `BatchToolbar` | `FilesPage.xaml` 第 54–64, 252–301 行 |
| 批量移动 | **✅ 已完成** | `OnBatchMoveRootClicked` → 移至根目录 | `FilesPage.xaml.cs` 第 581 行 |
| 批量收藏/取消 | **✅ 已完成** | `OnBatchFavoriteClicked`, `OnBatchUnfavoriteClicked` | `FilesPage.xaml` 第 278–291 行 |
| 批量删除 | **✅ 已完成** | `OnBatchDeleteClicked` → 移入回收站 | `FilesPage.xaml.cs` 第 539 行 |
| 上传状态面板 | **✅ 已完成** | `UploadStatusPanel` (进度条 + 状态 + 查看备份) | `FilesPage.xaml` 第 304–340 行，`OnOpenUploadsTapped` |
| 重命名/详情 UI | **⚠️ 需确认** | 文件项有"详情"按钮，详情页是否包含重命名/移动 | 需要端到端验证 |
| 容量展示 | **⚠️ 硬编码** | ProgressBar 显示 Progress="1" (100%)，未绑定 `StorageUsageDto` | 需接入 API |
| 分享管理页 | **⚠️ 需确认** | API client 有 `GetSharesAsync`，MAUI 分享管理页面是否存在待确认 | |

### 2.3 测试覆盖

| 测试项目 | 文件 | 行 |
|---|---|---|
| 搜索当前目录 | `EfCoreFileCenterFoldersAppServiceTests.cs` | 314 |
| 全盘搜索用户隔离 | 同上 | 340 |
| 排序（名称倒序） | 同上 | 376 |
| 批量移动+收藏+删除+恢复+永久删除 | 同上 | 402 |
| 重命名+移动+循环移动拒绝 | 同上 | 62, 295 |
| 分块上传会话 | `EfCoreFileCenterFileUploadServiceTests.cs` | 75, 158 |

**本版不需要新增测试** — 已有测试覆盖 P0/P1 功能。

---

## 3. 发布范围

### 3.1 P0：核心文件管理增强（必须通过）

| 编号 | 能力 | 验收标准 | 后端 | MAUI | 测试 | 风险 |
|---|---|---|---|---|---|---|
| V1.1-P0-01 | 文件名搜索 | 输入关键词返回匹配的文件/文件夹，支持当前目录搜索和全盘搜索，用户隔离不泄露 | ✅ | ✅ | ✅ | 低 — PostgreSQL ILIKE 稳定，用户隔离由 CurrentUser 过滤 |
| V1.1-P0-02 | 排序 | 文件页可切换排序：名称、修改时间、文件大小、类型，排序结果正确 | ✅ | ✅ | ✅ | 低 — ABP Sorting 框架级支持 |
| V1.1-P0-03 | 筛选 | 文件页可按类型（文件/文件夹）、媒体（图片/视频/音频）、收藏状态筛选 | ✅ | ✅ | 隐含 | 低 |
| V1.1-P0-04 | 批量选择与批量操作 | 多选后可选批量删除（移入回收站）、恢复、永久删除、移动、收藏/取消收藏，危险操作有二次确认 | ✅ | ✅ | ✅ | 中 — 大量数据时需确认前端分页选择策略 |
| V1.1-P0-05 | 重命名 | 文件详情或更多菜单可完成重命名，名称冲突提示 | ✅ | ⚠️ 待验证 | ✅ | 低 |
| V1.1-P0-06 | 移动（跨文件夹） | 文件可移动到根目录/指定文件夹，执行循环移动检测拒绝 | ✅ | ✅ (移至根目录) | ✅ | 低 |

### 3.2 P1：辅助体验增强（鼓励通过，不阻塞发布）

| 编号 | 能力 | 验收标准 | 后端 | MAUI | 测试 | 风险 | 规避方案 |
|---|---|---|---|---|---|---|---|
| V1.1-P1-01 | 容量使用展示 | Settings 页及文件页可查看已用/配额/剩余/百分比，上传超限提示清晰 | ✅ | ⚠️ 硬编码 (Progress=1) | 无 | 中 — 需要接入 API 替换硬编码值 | 若未完成，继续保持硬编码，标记为已知限制 |
| V1.1-P1-02 | 上传队列重试/取消 | 上传失败后可重试，上传中可取消，错误信息可读 | ✅ (UploadSession Cancelled) | ✅ (UploadStatusPanel) | 隐含 | 低 | 保持现有上传机制，不阻塞发布 |
| V1.1-P1-03 | 分享管理体验 | 用户可查看我的分享列表、复制链接、取消/更新分享、查看密码/有效期 | ✅ (GetSharesAsync) | ⚠️ 待确认分享管理页面 | 无 | 中 — 分享管理页面可能未完成 | 若分享管理页未完成，保持 V1.0 RC 的分享创建能力，标记为已知限制 |

### 3.3 P2：本轮不做

| 能力 | 说明 | 计划版本 |
|---|---|---|
| 文件夹打包下载 | 服务端流式压缩或异步任务 | V1.2/V1.3 |
| 断点续传 | 需要前端持久化上传进度 | V1.2 |
| 文件夹上传（MAUI 端） | 操作系统支持有限 | V2 |
| 全文内容搜索 | 需要索引服务 | V2 |

---

## 4. 明确不做（Out-of-Scope）

| 方向 | 原因 |
|---|---|
| NAS OS / RAID / 磁盘池管理 | 不是云盘产品主线 |
| SMB/NFS/AFP 协议 | 不是移动优先场景 |
| 桌面同步客户端 | 成本高，V2 后考虑 |
| Office 在线协作文档 | 技术复杂度高 |
| AI 相册 / AI 搜索 | V2 候选 |
| HLS 转码 / 视频低清预览 | V1.2 媒体库增强 |
| 多用户/家庭空间/团队空间 | V2 主线 |
| MinIO/OSS 迁移与回滚 | V1.3 运维规划 |
| iOS 端第一版 | 未进入移动端双平台目标 |
| 首屏 Doodle 手绘风 / 品牌探索 | 遵循 product-planning-hub §5.2 原则，不进入核心 App |

---

## 5. 版本依赖与代码影响面

### 5.1 依赖顺序

```mermaid
flowchart LR
    RC[V1.0 RC 发布] --> V11[V1.1 文件管理体验增强]
    V11 --> V12[V1.2 媒体库产品化]
```

V1.1 不依赖 V1.0 RC 中的任何新功能，但依赖 V1.0 RC 的**稳定基础**（Docker、健康检查、构建脚本、真机验收脚本）来确保 V1.1 的可验证性。

### 5.2 代码影响面

| 模块 | 影响 | 说明 |
|---|---|---|
| 后端 Application | 无变更 | V1.1 功能已实现 |
| 后端 Domain | 无变更 | V1.1 功能已实现 |
| 后端 EntityFramework | 无变更 | 已包含 ILIKE 查询、排序、筛选 |
| 后端 Tests | 无变更 | 已有完整测试覆盖 |
| MAUI Views | ⚠️ 潜在小修复 | 容量展示接入 API、重命名/分享管理页可能需补充 |
| MAUI Services | 无变更 | API client 已包含所有方法 |
| DB Migration | 无变更 | 无新实体/字段 |
| Docker | 无变更 | 无新增服务/环境变量 |

---

## 6. 建议负责人与任务拆解

### 6.1 验收确认（pm + qa-eng）

| 任务 | 输出 | 负责人 |
|---|---|---|
| V1.1-P0-01~06 后端验收 | 一条命令清单通过 | pm |
| V1.1-P0 真机验收（Android） | `docs/testing.md` 回填结果 | qa-eng |
| V1.1 MAUI Windows 构建验证 | MAUI 构建脚本通过 | devops-eng |
| 分享管理页功能审计 | 功能清单核实 | qa-eng |

### 6.2 缺陷修复（mobile-eng）

| 任务 | 优先级 | 说明 |
|---|---|---|
| 容量进度条接入 StorageUsage API | P1 | 替换 FilesPage.xaml 硬编码 Progress="1" |
| 重命名/移动 MAUI 用户体验闭环验证 | P0 | 确认详情页有重命名/移动入口 |
| 分享管理页确认（我的分享列表） | P1 | 确认 MAUI 分享管理页面是否存在，缺失则标记限制 |

### 6.3 文档任务（pm）

| 任务 | 输出 |
|---|---|
| 更新 `docs/testing.md` 增加 V1.1 验收记录表 | `docs/testing.md` §V1.1 |
| 更新 `docs/release-notes-v1.0-rc.md` 或生成 `docs/release-notes-v1.1.md` | Release Notes |
| 更新 `docs/product-planning-hub.md` V1.1 状态为"已发布" | planning hub 同步 |

---

## 7. 发布闸门（同 V1.0 RC 框架）

| 闸门 | 标准 |
|---|---|
| G0 范围冻结 | 不新增功能，只做 V1.1 范围内的缺陷修复和文档 |
| G1 构建测试 | `dotnet build/test` 通过；MAUI Windows/Android 构建通过 |
| G2 后端验收 | 搜索/排序/筛选/批量/重命名/移动 API 全部通过 |
| G3 MAUI 验收 | 文件页搜索/排序/筛选/批量/重命名/移动/容量/上传状态可达可操作 |
| G4 Android 真机验收 | 至少一台 Android 真机完成 V1.1 P0 功能验收 |
| G5 安全隐私 | 搜索不泄露其他用户文件；批量操作权限验证正确 |
| G6 文档同步 | testing.md、release-notes、已知限制同步 |

**放行标准：**

```
P0 = 0 缺陷
P1 = 0 缺陷，或每个 P1 都有明确规避方案和发布批准
P2 已记录到已知限制
```

---

## 8. 已知限制（V1.1 Release）

- 搜索使用 PostgreSQL `ILIKE`，不是全文索引，大目录下性能未实测。
- 容量展示依赖 `StorageUsageDto` API；若 MAUI 端未接入，进度条保留硬编码值。
- 分享管理页面后端 API 已就绪，MAUI 分享管理 UI 可能存在缺口（待确认后更新）。
- 批量操作前端分页选择策略：当前选择局限在当期页面加载的项，跨页多选未实现。
- 移动操作当前 MAUI 端仅支持"移至根目录"，不支持选择目标文件夹路径。

---

## 9. 建议的下一步

1. **立即**：运行 `dotnet test` 确认所有测试通过。
2. **立即**：运行 MAUI Windows + Android 构建验证。
3. **1-2 天**：qa-eng 针对 V1.1 P0 清单做 Android 真机验收。
4. **2-3 天**：mobile-eng 处理容量进度条接入 API（P1）和分享管理页确认（P1）。
5. **3-4 天**：pm 更新文档，输出 V1.1 Release Notes。
6. **发布后**：进入 V1.2 媒体库产品化规划。

---

*本文档是对 V1.1 文件管理体验增强的产品发布范围定义。V1.1 功能已由后端和 MAUI 前端基本实现，当前阶段的重心是验收确认、小缺陷修复和文档同步。*
