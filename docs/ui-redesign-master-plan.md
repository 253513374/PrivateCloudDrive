# PrivateCloudDrive UI/UX 重设计总方案

日期：2026-05-14
负责人：Hermes 产品总监 / UI 设计总监
适用范围：PrivateCloudDrive MAUI App

---

## 1. 设计总监结论

当前 PrivateCloudDrive 的 UI 问题不是局部“不够漂亮”，而是整体产品气质和交互模型需要重建。

当前主要问题：

1. 视觉风格偏 Doodle / 手绘 / 玩具感，不适合承载个人文件、照片、视频和私有云服务。
2. `DeliusSwashCaps`、亮蓝 `#49B6E5`、粗边框、卡通式按钮让产品显得不够专业。
3. 文件页、媒体库、上传、设置页的信息架构还偏“功能堆叠”，没有形成清晰用户路径。
4. 上传/失败/处理/服务器状态等关键状态没有被提升为产品体验核心。
5. 当前 App 更像“工程功能集合”，还不像一个可长期使用的高级私有云盘产品。

新的设计方向：

> PrivateCloudDrive 应重新设计为一个“安静、可信、专业、移动优先、内容优先”的私有云盘与轻量媒体库 App。

一句话：

> 不做可爱 Demo，不做花哨云盘；做一个让用户敢把真实文件和照片放进去的高级私有云工具。

---

## 2. 新 UI 设计关键词

| 维度 | 关键词 |
|---|---|
| 产品气质 | 可信、稳定、私有、清晰、长期使用 |
| 视觉风格 | 中性、安静、轻材质、低噪声、内容优先 |
| 交互体验 | 低心智、状态可见、路径清楚、任务可恢复 |
| 媒体体验 | 时间线、缩略图优先、沉浸预览、照片产品感 |
| 文件体验 | 高效扫描、目录清楚、多选明确、操作可撤销 |
| 运维体验 | 服务地址透明、容量可见、错误可诊断 |

---

## 3. 必须放弃的旧风格

以下风格不再作为 PrivateCloudDrive App 主 UI 方向：

| 旧风格 | 处理决定 | 原因 |
|---|---|---|
| Doodle / 手绘风 | 移出核心 App UI | 降低专业感和数据安全信任感 |
| Delius Swash Caps 字体 | 不再用于页面标题、按钮、文件名 | 中文混排差，趣味化过强 |
| 亮蓝 `#49B6E5` 主色 | 不再作为全局主视觉 | 过于轻快，不够稳重 |
| 2px 深色粗边框 | 普通组件全部移除 | 产生玩具感和漫画感 |
| 大量卡片堆叠 | 减少 | 降低扫描效率 |
| 装饰性 Logo / 插画 | 只用于品牌或空状态 | 不能抢文件和媒体内容的视觉优先级 |

`Design.md` 可以保留为“品牌/插画探索稿”，但不再作为 App 核心界面设计依据。

---

## 4. 新视觉体系

### 4.1 色彩系统

推荐从当前 Doodle 蓝改为更稳重的“私有云专业蓝 / 中性灰”体系。

| Token | 浅色模式 | 深色模式 | 用途 |
|---|---:|---:|---|
| PageBackground | `#F8FAFC` | `#0F172A` | 页面背景 |
| Surface | `#FFFFFF` | `#111827` | 卡片、列表、底部导航 |
| SurfaceAlt | `#F1F5F9` | `#1E293B` | 搜索框、筛选条、占位背景 |
| Primary | `#2563EB` | `#60A5FA` | 主按钮、选中态、关键交互 |
| PrimarySoft | `#EFF6FF` | `#172554` | 轻提示、选中背景 |
| TextPrimary | `#0F172A` | `#F8FAFC` | 主文本 |
| TextSecondary | `#475569` | `#CBD5E1` | 元信息、说明 |
| TextTertiary | `#94A3B8` | `#64748B` | 占位、弱文本 |
| Border | `#E2E8F0` | `#334155` | 分割线、轻边框 |
| Success | `#16A34A` | `#86EFAC` | 上传成功、完成状态 |
| Warning | `#D97706` | `#FDBA74` | 空间不足、暂停、提醒 |
| Danger | `#DC2626` | `#FCA5A5` | 删除、永久删除、退出登录 |

设计原则：

- 页面以中性色为主。
- Primary 只用于关键动作和选中态。
- Danger 只用于危险操作。
- 媒体页面让图片/视频本身成为视觉焦点。
- 不使用大面积渐变、重阴影、彩色装饰。

---

### 4.2 字体系统

建议：

- 标题：`OpenSansSemibold` 或系统 Semibold。
- 正文：`OpenSansRegular` 或系统 Regular。
- 技术信息、服务地址、日志：`JetBrainsMono` 小字号。
- 不再使用 `DeliusSwashCaps` 作为通用字体。

| 层级 | 字号 | 字重 | 用途 |
|---|---:|---|---|
| PageTitle | 24-28 | Semibold | 主 Tab 页面标题 |
| StackTitle | 20-22 | Semibold | 详情页标题 |
| SectionTitle | 16-17 | Semibold | 分组标题 |
| Body | 15 | Regular | 文件名、正文 |
| BodyStrong | 15 | Semibold | 重点文件名、用户名 |
| Meta | 13 | Regular | 时间、大小、路径 |
| Caption | 12 | Regular | 状态、辅助说明 |
| Button | 15 | Semibold | 按钮 |

---

### 4.3 组件形态

| 组件 | 新规则 |
|---|---|
| Button | 44px 最小高度，8-12 圆角，无粗边框 |
| Card | 轻边框或浅背景，不使用重阴影 |
| ListItem | 高效扫描，图标/缩略图 + 文件名 + 元信息 + 更多 |
| SearchBar | 轻背景，固定高度，聚焦态明确 |
| Badge | 小尺寸胶囊，表达状态，不做装饰 |
| EmptyState | 简洁图标 + 明确文案 + 主操作 |
| ErrorState | 必须有文字原因和重试动作 |
| BottomSheet | 文件更多操作、多选、复杂选择优先使用 |

---

## 5. 新信息架构建议

当前底部导航为：文件 / 媒体库 / 相册 / 上传 / 设置。

建议目标结构升级为：

```text
首页 / 媒体 / 文件 / 传输 / 我的
```

### 5.1 首页

定位：状态总览 + 最近内容 + 待处理事项。

首页回答：

- 服务器是否正常？
- 有没有上传失败？
- 最近上传了什么？
- 最近照片/文件在哪里？
- 空间是否快满？

模块：

1. 搜索入口。
2. 当前服务器和空间简要状态。
3. 上传/失败任务提示。
4. 最近照片/视频。
5. 最近文件。
6. 快捷操作：上传照片、上传文件、新建文件夹。

### 5.2 媒体

定位：轻量相册体验。

模块：

- 全部 / 照片 / 视频 / 相册。
- 时间线网格。
- 视频时长标记。
- 处理状态入口。
- 长按多选。
- 图片/视频沉浸预览。

### 5.3 文件

定位：传统目录式文件管理。

模块：

- 当前路径。
- 搜索、排序、视图切换。
- 上传、新建文件夹。
- 文件夹优先列表。
- 长按多选。
- 更多操作底部面板。

### 5.4 传输

定位：所有上传、下载、备份、失败任务中心。

模块：

- 上传中。
- 下载中。
- 已完成。
- 失败。
- 重试全部失败。
- 暂停/继续全部。
- 仅 Wi-Fi 上传提示。

### 5.5 我的

定位：账号、服务器、空间、安全、设置。

模块：

- 当前账号和服务器。
- 存储空间。
- 相册备份。
- 分享管理。
- 回收站。
- 安全设置。
- 服务诊断。
- 外部登录。
- 退出登录。

---

## 6. 核心页面重设计方向

### 6.1 登录页

目标：建立信任感，而不是欢迎页装饰。

结构：

1. 品牌：PrivateCloudDrive。
2. 副标题：连接你的私有云盘。
3. 当前服务器地址 / 可达状态。
4. 账号/邮箱。
5. 密码 + 显示/隐藏。
6. 登录按钮。
7. 错误提示。
8. 第三方登录仅在可用时显示。

交互要求：

- 服务器不可达要明确提示。
- 登录失败不清空服务器地址。
- 账号密码登录始终最高优先级。
- 登录中按钮显示 loading。

---

### 6.2 文件页

目标：最高频、最高效、最专业。

改版重点：

- 顶部只保留标题、当前路径、搜索/排序/上传等关键操作。
- 文件列表用高效密度，不再每项大卡片化。
- 文件夹、图片、视频、文档使用一致图标体系。
- 删除、详情、分享收进“更多”操作，避免按钮噪声。
- 长按进入多选，底部出现批量操作栏。

验收：

- 用户 3 秒内知道当前目录。
- 用户 1 次点击可上传。
- 用户长按可进入多选。
- 空文件夹能清楚引导上传或新建文件夹。

---

### 6.3 媒体库页

目标：像轻量照片 App，不像文件列表换皮。

改版重点：

- 默认时间线。
- 图片/视频混排。
- 视频显示播放标识和时长。
- 缩略图稳定占位，避免跳动。
- 处理失败/处理中状态可见。
- 点击进入沉浸预览。

验收：

- 媒体默认按时间倒序。
- 视频和图片一眼可区分。
- 缩略图失败不影响打开原文件。
- 长按可选择多个媒体。

---

### 6.4 上传/传输页

目标：消除上传焦虑。

改版重点：

- 上传任务独立成传输中心。
- 每个任务显示文件名、进度、速度、状态、失败原因。
- 失败任务保留在队列中，支持重试。
- 上传完成可以清理记录。
- 首页也要显示上传失败提醒。

验收：

- 上传开始后，用户 1 次点击内进入任务详情。
- 上传失败原因必须可见。
- 失败任务不会丢失。
- 支持重试失败任务。

---

### 6.5 设置 / 我的页

目标：从功能堆叠改为“账号与私有云状态中心”。

推荐分组：

1. 账号与服务器。
2. 存储空间。
3. 常用管理：回收站、我的分享、操作日志、媒体处理。
4. 登录方式：微信、Google、GitHub。
5. 服务与安全：本地会话、API 地址、版本、诊断。
6. 危险操作：退出登录。

验收：

- 回收站入口一级可见。
- 存储状态一级可见。
- 服务地址可找到。
- 退出登录必须二次确认，并说明不会删除云端文件。

---

## 7. MAUI 工程落地计划

### 7.1 第一阶段：全局 Token 降噪

优先改：

```text
maui/PrivateCloudDrive.App/Resources/Styles/Colors.xaml
maui/PrivateCloudDrive.App/Resources/Styles/Styles.xaml
```

任务：

1. 将 `Primary` 从 `#49B6E5` 改为 `#2563EB`。
2. 将默认 Button 字体从 `DeliusSwashCaps` 改为 `OpenSansSemibold`。
3. 将 `PageTitleLabel` / `SectionTitleLabel` 改为 `OpenSansSemibold`。
4. 将默认 Border 厚度从 2 改为 1。
5. 保留 `Doodle*` key，但映射到新中性色，避免页面崩溃。
6. 新增 Card、SoftCard、InputContainer、IconButton、GhostButton 等现代样式。

收益：风险最低，立刻降低玩具感。

---

### 7.2 第二阶段：资源字典拆分

建议新增：

```text
maui/PrivateCloudDrive.App/Resources/Styles/Tokens.xaml
maui/PrivateCloudDrive.App/Resources/Styles/Typography.xaml
maui/PrivateCloudDrive.App/Resources/Styles/Controls.xaml
maui/PrivateCloudDrive.App/Resources/Styles/Components.xaml
maui/PrivateCloudDrive.App/Resources/Styles/Layouts.xaml
```

`App.xaml` 合并顺序：

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
    <ResourceDictionary Source="Resources/Styles/Tokens.xaml" />
    <ResourceDictionary Source="Resources/Styles/Typography.xaml" />
    <ResourceDictionary Source="Resources/Styles/Controls.xaml" />
    <ResourceDictionary Source="Resources/Styles/Components.xaml" />
    <ResourceDictionary Source="Resources/Styles/Layouts.xaml" />
    <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

原则：先保留旧 `Styles.xaml` 作为兼容层，不一次性删除。

---

### 7.3 第三阶段：建立内部组件库

新增目录：

```text
maui/PrivateCloudDrive.App/Controls/
```

第一批组件：

```text
AppPageHeader.xaml
AppCard.xaml
AppStateView.xaml
AppEmptyView.xaml
AppIconBadge.xaml
StatusBadge.xaml
```

第二批组件：

```text
FileListItem.xaml
MediaGridItem.xaml
AppListItem.xaml
AppSection.xaml
StorageUsageCard.xaml
SelectionToolbar.xaml
```

不要第一阶段引入重型第三方 UI 库。当前问题主要是设计系统和 XAML 结构，不是缺控件库。

---

### 7.4 页面重构顺序

| Sprint | 页面 | 目标 |
|---|---|---|
| Sprint 1 | Colors.xaml / Styles.xaml | 全局降噪，去 Doodle 化 |
| Sprint 2 | LoginPage / AppShell | 第一印象和主导航专业化 |
| Sprint 3 | FilesPage / FileDetailsPage | 核心文件体验专业化 |
| Sprint 4 | PhotosPage / MediaAlbumsPage / MediaPreviewPage | 媒体库照片产品化 |
| Sprint 5 | SettingsPage / UploadsPage / TrashPage / SharesPage / OperationLogsPage | 管理与状态页面统一 |

---

## 8. 首个最小安全 PR 建议

如果现在开始执行，第一步不要大改页面。

第一个 PR 只改：

```text
maui/PrivateCloudDrive.App/Resources/Styles/Colors.xaml
maui/PrivateCloudDrive.App/Resources/Styles/Styles.xaml
```

目标：

1. 新色板。
2. 移除默认 Doodle 字体。
3. 降低边框厚度。
4. 按钮现代化。
5. 标题现代化。
6. 保持旧 key 兼容。

验收命令：

```bash
cd /d/Devs/Projects/Personal/PrivateCloudDrive

dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj \
  -f net10.0-windows10.0.19041.0 \
  -c Debug

dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj \
  -f net10.0-android \
  -c Debug
```

检查遗留 Doodle：

```bash
grep -R "DeliusSwashCaps\|Doodle" maui/PrivateCloudDrive.App/Resources maui/PrivateCloudDrive.App/Views
```

---

## 9. UI 验收标准

### 9.1 全局观感

- 不再有大面积手绘/卡通感。
- 字体统一、清晰、专业。
- 边框轻，不再像线框草图。
- 主色克制，不抢内容。
- 浅色/深色模式都可读。

### 9.2 导航

- 底部 Tab 不超过 5 个。
- 每个 Tab 的用户心智清楚。
- 上传/失败任务有固定入口。
- 文件和媒体入口明确区分。

### 9.3 文件页

- 当前路径清楚。
- 上传、新建、搜索、排序易发现。
- 文件列表扫描效率高。
- 多选、删除、详情不会互相干扰。
- 空状态有主操作。

### 9.4 媒体库

- 默认时间线清楚。
- 视频有时长和播放标识。
- 缩略图失败有占位。
- 预览页沉浸且能返回。

### 9.5 传输

- 上传进度清晰。
- 失败原因可见。
- 失败任务可重试。
- 首页或传输页能快速发现失败。

### 9.6 设置 / 我的

- 账号、服务器、容量、安全分组清晰。
- 回收站、分享、日志入口易找到。
- 退出登录独立且二次确认。
- 服务地址和版本信息可用于排障。

---

## 10. 立即执行清单

1. 确认本方案作为新的 UI 重设计主方向。
2. 将 `Design.md` 标记为探索稿，不再指导核心 App UI。
3. 修改 `Colors.xaml` 和 `Styles.xaml`，完成第一轮去 Doodle 化。
4. 重构 LoginPage，建立专业第一印象。
5. 重构 FilesPage，优先改善最高频体验。
6. 重构 SettingsPage，改为账号/服务器/容量/安全状态中心。
7. 再推进媒体库、上传队列、相册页。

---

## 11. 最终设计方向一句话

> PrivateCloudDrive 的新 UI 不追求“可爱”，而追求“可信”；不靠装饰变漂亮，而靠清晰的信息架构、专业的视觉系统和可恢复的任务状态，让用户愿意长期把真实文件和照片交给它。
