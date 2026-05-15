# PrivateCloudDrive 产品 UI 竞品研究与 Agent 升级准则

日期：2026-05-15
负责人：Hermes 产品总监 / UI 设计团队
范围：PrivateCloudDrive MAUI App、产品设计评审方法、后续 Agent 工作准则

---

## 1. 本次升级结论

这次内部学习和复盘后，PrivateCloudDrive 的 UI/UX 设计准则升级为：

> PrivateCloudDrive 不是功能演示 App，也不是普通文件列表工具；它应被设计成一个专业、可信、安静、内容优先、状态透明、异常可恢复的私有云盘产品。

设计团队需要停止以下低水平决策：

1. 因为某个功能存在，就把它放到主 Tab。
2. 因为某个服务有状态，就把它做成独立页面。
3. 用工程模块组织界面，而不是用用户任务组织界面。
4. 只看正常态，不设计空态、失败态、权限态、弱网态。
5. 只改颜色和按钮，不重构信息架构。
6. 把“好看”理解成装饰，而不是降低用户认知负担和提升信任。

---

## 2. 外部设计资料学习摘要

### 2.1 Apple Human Interface Guidelines

参考：
- https://developer.apple.com/design/human-interface-guidelines/
- https://developer.apple.com/design/human-interface-guidelines/navigation
- https://developer.apple.com/design/human-interface-guidelines/tab-bars
- https://developer.apple.com/design/human-interface-guidelines/accessibility

可落地原则：

| 原则 | 对 PrivateCloudDrive 的含义 |
|---|---|
| Clarity 清晰 | 文案直接，状态明确，用户知道文件在哪里、任务怎样了。 |
| Deference 克制 | UI 不与文件、照片、视频内容抢视觉。 |
| Depth 层级 | 主导航、二级页面、底部面板、上下文菜单要各司其职。 |
| Platform Consistency | MAUI 跨平台可以统一品牌，但交互应尊重 Android/iOS 平台习惯。 |
| Accessibility | 字号、对比度、触控区域、状态图标不能只靠颜色表达。 |

### 2.2 Material Design 3

参考：
- https://m3.material.io/
- https://m3.material.io/components/navigation-bar/overview
- https://m3.material.io/components/search/overview
- https://m3.material.io/components/snackbar/overview
- https://m3.material.io/components/progress-indicators/overview

可落地原则：

| 原则 | 对 PrivateCloudDrive 的含义 |
|---|---|
| Navigation Bar 只放核心目的地 | 上传状态、处理状态、临时任务不应默认放底部主导航。 |
| Search 是核心组件 | 文件、媒体、相册都应稳定提供搜索或筛选能力。 |
| Snackbar/状态条用于即时反馈 | 上传开始、失败、完成应状态化提示，而不是强行进入独立主页面。 |
| Progress Indicators | 上传、处理、同步要有明确进度或不确定进度反馈。 |
| Semantic Color | Primary/Success/Warning/Danger 必须语义稳定，不做装饰色滥用。 |

### 2.3 Nielsen Norman Group 可用性原则

参考：
- https://www.nngroup.com/articles/ten-usability-heuristics/
- https://www.nngroup.com/articles/visibility-system-status/
- https://www.nngroup.com/articles/error-message-guidelines/
- https://www.nngroup.com/articles/empty-state-interface-design/

可落地原则：

| NN/g 原则 | 对 PrivateCloudDrive 的含义 |
|---|---|
| 系统状态可见 | 上传、同步、媒体处理、服务器连接、容量状态必须可见。 |
| 用户控制与自由 | 上传失败可重试，删除可恢复，危险操作可撤销或确认。 |
| 错误预防 | 删除、覆盖、清空、共享公开链接要防误操作。 |
| 识别优于记忆 | 文件路径、当前位置、最近访问、任务入口要可见。 |
| 错误恢复 | 错误文案要说明原因和下一步，不只写“失败”。 |
| 美观且极简 | 少装饰、少重复、少技术字段，只保留对任务有帮助的信息。 |

### 2.4 Figma / Linear / Notion / Stripe 等产品方法

参考：
- Figma Design Systems: https://www.figma.com/best-practices/components-styles-and-shared-libraries/
- Linear Method: https://linear.app/method
- Notion Blog: https://www.notion.com/blog
- Stripe Design: https://stripe.com/

可借鉴：

| 产品 | 可借鉴点 | 对 PrivateCloudDrive 的应用 |
|---|---|---|
| Figma | 组件系统、状态系统、协作权限清晰 | 文件行、任务行、空态、错误态全部组件化。 |
| Linear | 安静、高效、高密度、状态明确 | 文件管理应高效扫描，少噪音，不做玩具感卡片。 |
| Notion | 渐进披露、空状态友好 | 高级设置和复杂能力不要首屏堆叠。 |
| Stripe | 信任感、清晰层级、精致但克制 | 私有云盘需要专业稳定，而不是花哨。 |
| Apple | 内容优先和平台原生 | 照片和文件内容是主角。 |

---

## 3. 同类网盘产品调研结论

调研范围：Google Drive、Dropbox、OneDrive、iCloud Drive/Photos、Synology Drive/Photos、Nextcloud、百度网盘、阿里云盘。

### 3.1 底部导航规律

| 产品 | 常见主导航 | 对本项目启发 |
|---|---|---|
| Google Drive | Home / Starred / Shared / Files | 主导航放长期内容域，不放上传状态。 |
| Dropbox | Home / Files / Photos / Shared / Account | 文件和照片可并列；上传用 + 或上下文入口。 |
| OneDrive | Home / Files / Shared / Photos / Me | 文件、共享、照片、个人是稳定主域。 |
| iCloud Files | Recents / Shared / Browse | 系统级文件产品不把上传作为主导航。 |
| iCloud Photos | Library / Albums / Search 等 | 照片备份是库状态，不是上传 Tab。 |
| Synology Drive | Files / Shared / Offline / More | 私有云/NAS 更重视文件、共享、离线、更多。 |
| Synology Photos | Photos / Albums / Sharing / More | 照片产品主导航围绕照片组织。 |
| Nextcloud | Files / Photos / Activity / More | Activity 是事件流，不等同上传状态。 |
| 百度网盘 | 文件/首页/我的/传输等，版本差异大 | 强下载资源盘才会更突出传输，不宜盲目照搬。 |
| 阿里云盘 | 首页/文件/相册/我的等 | 文件与相册是主域，传输通常是任务入口。 |

结论：

> 上传/传输状态通常不是主 Tab。主 Tab 应放长期、高频、稳定的信息域，例如文件、照片、共享、首页、我的。

### 3.2 上传入口规律

成熟产品通常把上传作为动作入口，而不是信息架构主域：

1. 文件页顶部或右下角 +。
2. 当前文件夹内的“上传到此处”。
3. 照片页的“开启自动备份”或“手动上传”。
4. 系统分享入口。
5. 首页快捷操作。

对 PrivateCloudDrive 的决策：

- 文件页保留上传按钮。
- 上传状态通过任务条、失败横幅、角标和二级页面呈现。
- 不再把上传状态管理放底部主 Tab。

### 3.3 文件 / 媒体 / 相册关系

建议采用“文件 + 照片双核心”模型：

| 概念 | 产品定义 |
|---|---|
| 文件 | 真实目录结构，管理任意文件类型。 |
| 媒体/照片 | 图片和视频的聚合时间线视图。 |
| 相册 | 照片下的组织集合，不必等同文件夹。 |
| 备份 | 照片功能状态，不是主 Tab。 |
| 上传/传输 | 后台任务状态，不是内容域。 |

---

## 4. PrivateCloudDrive 新产品设计原则

### 4.1 一级导航原则

进入底部主导航必须满足多数条件：

1. 高频使用。
2. 长期稳定。
3. 用户能用一个清晰名词理解。
4. 是内容域或核心场景，而非单个动作。
5. 内部能承载多个相关子任务。
6. 不依赖临时状态才有价值。

不应进入主导航：

- 上传状态
- 媒体处理状态
- 清理缓存
- 操作日志
- 单个技术队列
- 单个维护动作
- 仅失败时有价值的页面

### 4.2 推荐导航演进

当前阶段：

```text
文件 / 媒体库 / 相册 / 设置
```

后续更成熟阶段可评估：

```text
首页 / 文件 / 照片 / 共享 / 我的
```

或：

```text
文件 / 照片 / 共享 / 活动 / 我的
```

其中“活动”只有在同时承载上传、下载、同步、分享事件、系统告警、备份状态时才有资格成为主入口。

### 4.3 状态驱动原则

上传、同步、媒体处理、备份、失败任务都应采用状态驱动：

| 状态 | 展示方式 |
|---|---|
| 无任务 | 不打扰，不出现入口或弱化入口。 |
| 进行中 | 状态条、进度条、系统通知、行内进度。 |
| 失败 | 红点/横幅/任务条，优先显示重试入口。 |
| 完成 | 弱提示，可在历史记录查看，不长期占主视觉。 |
| 需要用户决策 | 弹出明确可操作页面，例如冲突、权限、空间不足。 |

### 4.4 空态设计原则

每个空态必须回答：

1. 这里为什么为空？
2. 用户下一步可以做什么？
3. 这个页面的价值是什么？

错误示例：

```text
上传队列为空。
暂无上传任务。
```

正确示例：

```text
暂无上传任务
从文件页选择文件上传后，进度会显示在这里。上传失败时可以在这里重试。
[去文件页选择]
```

### 4.5 错误态设计原则

错误文案结构：

```text
发生了什么 + 可能原因 + 用户能做什么
```

示例：

```text
无法上传 3 个文件。服务器空间不足，请清理空间后重试。
```

不应只写：

```text
上传失败
```

---

## 5. AI 产品设计 Agent 升级准则

以后 Hermes 产品/UI Agent 在 PrivateCloudDrive 中做设计决策时，必须遵守以下流程：

### 5.1 先判断用户任务，不先判断页面

每个页面设计前必须写清：

| 问题 | 必须回答 |
|---|---|
| 用户是谁？ | 普通用户、管理员、家庭成员、技术用户？ |
| 用户目标是什么？ | 找文件、看照片、上传、分享、恢复、诊断？ |
| 使用场景是什么？ | 移动端、弱网、私有服务器、批量文件？ |
| 是否高频？ | 高频才考虑主导航。 |
| 是否必须独立页面？ | 不是所有功能都需要页面。 |
| 是否有异常/空态？ | 必须同时设计。 |

### 5.2 一级导航决策模板

每次新增或调整主导航，必须填写：

```text
导航项：
用户目标：
使用频率：
是否长期稳定：
是否内容域/核心场景：
是否只是单个动作或临时状态：
与其他导航是否重叠：
竞品是否普遍这样做：
最终判断：主导航 / 二级页面 / 状态入口 / 上下文操作 / 设置项
```

### 5.3 状态矩阵模板

每个核心页面必须覆盖：

```text
默认态：
加载态：
空态：
正常态：
部分成功态：
失败态：
离线态：
权限不足态：
进行中态：
完成态：
用户可恢复动作：
验收截图要求：
```

### 5.4 竞品研究输出模板

```text
竞品：
主导航：
上传入口：
任务状态展示：
文件/照片/相册关系：
空态/失败态做法：
值得借鉴：
不应照搬：
对 PrivateCloudDrive 的决策影响：
```

### 5.5 设计验收标准模板

```text
功能/页面：
用户目标：
入口层级：
主流程：
异常流程：
空态文案：
错误文案：
危险操作保护：
跨平台注意事项：
Android 截图验收：
构建命令：
不做范围：
```

---

## 6. PrivateCloudDrive 后续设计检查清单

### 6.1 全局

- [ ] 主导航是否只放高频稳定目标？
- [ ] 是否存在把状态页当主功能的情况？
- [ ] 是否存在工程术语直接暴露？
- [ ] 是否所有主页面都有空态、加载态、失败态？
- [ ] 是否所有可见改动都有 Android 截图验收？

### 6.2 文件

- [ ] 当前路径是否清楚？
- [ ] 上传、新建、搜索、排序是否容易发现？
- [ ] 文件行是否高效扫描？
- [ ] 删除/移动/分享是否是上下文操作而非主导航？
- [ ] 空文件夹是否提供上传和新建入口？

### 6.3 媒体/相册

- [ ] 媒体库是否以内容为主，不被操作按钮干扰？
- [ ] 相册和文件夹概念是否清楚区分？
- [ ] 缩略图加载失败是否有占位和重试？
- [ ] 处理状态是否状态驱动，而不是常驻主按钮？

### 6.4 上传/任务

- [ ] 上传是否从文件/照片上下文发起？
- [ ] 上传状态是否只在有任务时突出？
- [ ] 失败任务是否优先显示并可重试？
- [ ] 完成记录是否弱化，不抢主视觉？
- [ ] 是否明确“清除完成记录”不是删除文件？

### 6.5 设置/我的

- [ ] 设置是否按用户心智分组，而不是按技术模块？
- [ ] 服务器、账号、安全、容量是否清楚？
- [ ] 危险操作是否隔离并确认？
- [ ] 诊断/日志是否作为低频入口？

---

## 7. 后续 Agent 行为约束

从本文件生效后，Hermes 在 PrivateCloudDrive 项目中做产品/UI 决策时：

1. 不能只从已有页面出发维护旧结构。
2. 必须主动质疑主导航是否合理。
3. 必须把“状态管理”和“核心功能”区分开。
4. 必须参考竞品和平台规范形成决策，而不是凭感觉。
5. 必须把设计结论写入项目文档。
6. 涉及 App 可见改动，必须构建、安装、截图验收。
7. 如果用户质疑设计合理性，必须重新开内部设计评审，不应辩护旧方案。

---

## 8. 一句话升级准则

> 好的产品设计不是把所有功能摆出来，而是把用户完成目标所需的能力，在正确的层级、正确的时机、正确的状态下呈现出来。
