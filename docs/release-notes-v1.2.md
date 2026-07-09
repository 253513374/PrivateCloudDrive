# PrivateCloudDrive V1.2 Release Notes — 媒体库产品化

| 元数据 | 值 |
|--------|-----|
| 发布日期 | 2026-07-07 |
| 文档版本 | 1.0 |
| 负责人 | Hermes 产品总监 (pm) |
| 文档定位 | V1.2 正式发布说明候选稿（非 RC 文件名）；当前仅作为 RC 通过后的提升材料，不替代 V1.2 RC 闸门结论 |
| 基线文档 | `docs/architecture-v1.2-rc-boundary.md`、`docs/scenario-matrix-v1.2-rc.md`、`docs/release-notes-v1.2-rc.md` |
| 前置版本 | V1.1 文件管理体验增强 |

> 命名口径：当前发布候选阶段以 `docs/release-notes-v1.2-rc.md` 为权威 Release Notes；本文档保留为正式 V1.2 候选稿，只有在 RC 验收全部通过并完成发布裁决后，才提升为正式发布说明。

---

## 产品定位

V1.2 将 PrivateCloudDrive 从"文件型私有云盘"推进到"移动优先的私人媒体库"。本版不扩展 NAS、AI 相册或协作套件范围，而是聚焦媒体回看、整理、状态可见和移动端可交付质量。

## 本版包含的功能

### P0：核心媒体库体验

| 功能 | 说明 |
|------|------|
| 媒体时间线 | 图片+视频混合时间线，按月份分组，时间倒序。时间来源优先级：TakenAt > CreationTime > LastModificationTime。仅返回当前用户/租户媒体 |
| 媒体类型过滤 | 全部/图片/视频过滤准确；过滤后排序和分组不改变 |
| 视频封面与时长 | 处理完成的视频显示封面缩略图；视频卡片右下角显示 mm:ss/hh:mm:ss 时长 |
| 媒体处理状态可视化 | Pending=等待处理、Processing=正在处理、Failed=处理失败、Completed=已完成。处理未完成时不显示空白缩略图 |
| 播放错误与重试 | 播放加载态明确；失败时展示可理解原因和重试入口；处理未完成时告知"视频处理中" |
| 相册创建/重命名/删除 | 名称必填，同名提示清晰；创建后出现在相册列表；删除不删原文件；只操作当前用户相册 |
| 相册添加/移除媒体 | 只能添加当前用户图片/视频；移除媒体不删原文件；同一媒体不可重复加入同一相册 |
| 跨用户隔离 | 时间线、相册、处理状态、重试均只返回当前用户数据 |
| 错误脱敏 | ProcessErrorSummary 不包含物理路径、token、secret、connection string 或堆栈信息 |

### P1：辅助体验增强

| 功能 | 说明 |
|------|------|
| 相册封面 | 默认取最新一张已完成缩略图的媒体；可手动设置封面 |
| 处理状态聚合入口 | 媒体库顶部或设置页显示"处理中：N / 失败：N" |
| 视频处理失败重新处理 | 失败项可重新处理；重试前校验文件归属和状态 |
| 时间线下拉刷新 | 移动端下拉可刷新媒体处理结果 |
| 相册排序 | 相册列表支持按更新时间/创建时间排序 |

## API 与数据结构

新增/完善媒体库接口：
- `GET /api/file-center/media/timeline` — 媒体时间线
- `GET /api/file-center/media/{fileNodeId}/detail` — 媒体详情
- `GET /api/file-center/media/processing-status` — 处理状态列表
- `POST /api/file-center/media/{fileNodeId}/retry-processing` — 重试处理

新增相册接口：
- `GET/POST /api/file-center/media/albums` — 相册列表/创建
- `GET/PUT/DELETE /api/file-center/media/albums/{albumId}` — 相册详情/编辑/删除
- `POST /api/file-center/media/albums/{albumId}/items` — 添加媒体到相册
- `DELETE /api/file-center/media/albums/{albumId}/items/{fileNodeId}` — 从相册移除媒体
- `POST /api/file-center/media/albums/{albumId}/cover/{fileNodeId}` — 设置封面

新增实体：
- `MediaAlbum`（相册）：`(TenantId, OwnerId, NormalizedName)` 唯一约束
- `MediaAlbumItem`（相册项）：`(AlbumId, FileNodeId)` 唯一约束
- `MediaAsset` 补充：`ProcessStatus`、`ProcessErrorSummary`、`TakenAt`、`DurationMilliseconds`、`ThumbnailBlobObjectId`

---

## V1.2 已知限制

| 编号 | 限制 | 影响 | 后续版本 |
|------|------|------|---------|
| LIM-V12-01 | 时间线基于拍摄时间/上传时间，不支持修改时间轴顺序 | 用户无法自定义排序 | 待定 |
| LIM-V12-02 | 视频只保证 mp4/mov/webm 主链路播放，其他格式视 ffmpeg 兼容性 | 部分格式无法预览 | 待定 |
| LIM-V12-03 | 相册封面仅限已完成缩略图的媒体；处理中和失败项无法设封面 | 相册列表可能有空封面占位 | V1.2 P1 |
| LIM-V12-04 | 相册不支持嵌套/子相册 | 组织结构单一 | V2 候选 |
| LIM-V12-05 | 媒体处理失败重试限于单文件操作，无批量重新处理入口 | 多个文件失败时需逐个操作 | V1.2 P1 |
| LIM-V12-06 | 历史媒体（V1.2 前上传）可能缺少 MediaAsset 记录，时间线中显示为等待处理 | 旧文件状态不完整 | V1.2 扫描补偿任务 |
| LIM-V12-07 | 时间线月份分组基于后端返回的扁平列表在 MAUI 端分组，超长列表跨页月份边界需验证 | 极端场景月份可能断裂 | 当前验证 |
| LIM-V12-08 | 相册不改变文件原始目录结构，删除相册或移除相册项不改变原文件位置 | 与文件浏览隔离 | 设计如此 |
| LIM-V12-09 | iOS 客户端不在 V1.2 范围内；MAUI 构建仅验证 Windows 和 Android 目标 | 仅 Android + Windows 可用 | 待定 |

---

## 本版不包含

- AI 自动分类、人物识别、OCR 或语义搜索
- 视频多码率转码、HLS/DASH、在线播放自适应码率
- NAS 协议、桌面同步、多节点高可用
- iOS 完整回归（当前仅 Android + Windows 验证）
- 家庭空间/团队空间/相册共享权限
- 相册对外公开分享增强
- 媒体元数据编辑（标签/描述/位置）
- MinIO/OSS 存储迁移

## V1.2 RC 验证结果摘要

V1.2 能力在 RC 阶段已完成端到端验证：

| 范围 | 结果 |
|------|:----:|
| 后端构建 (`dotnet build`) | ✅ 通过，0 警告 0 错误 |
| 后端测试 (`dotnet test`) | ✅ 通过，101 个 EF 集成测试 |
| 本地栈健康检查 | ✅ PASS 19 / WARN 4 / FAIL 0 |
| MAUI Windows 构建 | ✅ PASS |
| MAUI Android 构建 | ✅ PASS |
| Android APK 发布 | ✅ 已生成 Signed APK |
| Android 模拟器启动 | ✅ Pixel 9 Pro API 36，登录页可通过 |
| Secret 日志扫描 | ✅ 0 findings，619 tracked files |
| 安全加固复验 | ✅ 公开分享密码请求头 + 限速策略验证通过 |

完整验证记录见 `docs/release-notes-v1.2-rc.md`。

---

## 当前验收状态

V1.2 当前处于**验收中**阶段。以下验收任务已完成：

- [x] `docs/scenario-matrix-v1.2-rc.md` — 当前 RC 场景矩阵完成
- [x] `docs/scenario-matrix-v1.2.md` — 正式 V1.2 候选场景矩阵完成（含 §11 已知限制索引）
- [x] `docs/testing.md` — 更新 V1.2 验收矩阵 + 已知限制清单
- [x] `docs/release-notes-v1.2-rc.md` — 当前 RC Release Notes 与验证结果
- [x] `docs/release-notes-v1.2.md` — 本文件，正式 V1.2 候选稿与已知限制同步
- [x] `docs/product-planning-hub.md` — V1.2 状态更新为"验收中"
- [x] `docs/architecture-v1.2-rc-boundary.md` — RC 发布边界、风险与闸门口径完成

待完成：

- [ ] 后端 P0 修复确认（V12-FIX-01~05）
- [ ] API 端到端验证
- [ ] Android 真机验收记录
- [ ] secret 日志扫描最终确认
- [ ] 发布闸门全部通过
