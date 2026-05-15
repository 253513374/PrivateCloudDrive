# Android UI 阶段验收评审（产品 + 设计介入）

时间：2026-05-15
设备：Android Emulator `emulator-5554`
App：`com.companyname.privateclouddrive.app`
服务端：`http://10.0.2.2:8080`

## 本轮已执行

1. 登录 App 并逐页查看主要 UI：登录、文件、媒体库、相册、上传、设置。
2. 保存截图证据到 `artifacts/ui-review/screenshots/`。
3. 发现设置页“系统健康”返回失败，检查 Docker API 日志确认旧镜像对 `/api/file-center/system-health/summary` 返回 404。
4. 已重新构建并启动 Docker Compose `api` 与 `media-worker`。
5. 重新登录后复验系统健康接口：后端返回 200，App 设置页显示“运行正常”。

## 截图证据

| 页面 | 截图 |
|---|---|
| 文件页 | `artifacts/ui-review/screenshots/01-files.png` |
| 媒体库 | `artifacts/ui-review/screenshots/02-media-library.png` |
| 相册 | `artifacts/ui-review/screenshots/03-albums.png` |
| 设置顶部 | `artifacts/ui-review/screenshots/04-settings-top.png` |
| 设置下部（修复前） | `artifacts/ui-review/screenshots/05-settings-lower.png` |
| 上传 | `artifacts/ui-review/screenshots/06-upload.png` |
| 后端重建后登录页 | `artifacts/ui-review/screenshots/10-after-token-expired.png` |
| 系统健康修复后 | `artifacts/ui-review/screenshots/14-settings-health-fixed.png` |
| 登录错误中文化复验 | `artifacts/android-ui/login-invalid-error.png` |
| 上传页空状态入口复验 | `artifacts/android-ui/upload-empty-with-entry.png` |
| 最新系统健康复验 | `artifacts/android-ui/settings-system-health-latest.png` |

## 产品验收结论

当前 App 已具备 MVP 主要信息架构和可用路径：账号登录、文件列表、媒体库缩略图、相册空状态、上传队列、设置与系统健康均可见。整体已经从“功能拼装”进入“可用性打磨”阶段。

但仍不建议直接判定为最终可交付，需要先完成下列 P0/P1 收口。

## P0：必须修复/复验

| 编号 | 问题 | 证据 | 建议 |
|---|---|---|---|
| P0-1 | 设置页系统健康曾显示“无法读取系统健康状态 / FileCenter request failed.” | `05-settings-lower.png`，Docker 日志显示 404 | 已通过重建 Docker API 修复；后续发布前必须把“重建镜像 + 重新登录 + 健康 200”纳入验收清单。 |
| P0-2 | Docker API 重建导致既有本地会话失效，切换页面时被带回登录页 | `10-after-token-expired.png`，后端日志显示文件列表 401 后执行 token revocation | 属于开发环境可接受现象，但产品上应在 401 时展示“会话已过期，请重新登录”，避免用户误以为数据丢失。 |
| P0-3 | 系统健康详情文案过长，在设置卡片中形成大段技术文本 | `14-settings-health-fixed.png` | 面向普通用户只显示“运行正常/异常 + 关键项”，技术明细折叠到“查看详情”。 |

## P1：产品与交互体验优先优化

| 编号 | 页面 | 问题 | 建议 |
|---|---|---|---|
| P1-1 | 文件页 | 筛选/排序控件横向滚动，右侧“名称 A-Z”在首屏被截断，用户不容易发现还有后续筛选项。 | 改成两行栅格：第一行搜索，第二行三个筛选 Chip 等宽；“全局搜索/清除”放到搜索下方或更多筛选面板。 |
| P1-2 | 文件页 | 单个文件卡片右侧同时显示“详情”和红色“删除”，删除入口过于靠前，误触风险高。 | 删除移动到更多菜单或详情页；列表主操作保留“详情/打开”。 |
| P1-3 | 媒体库 | 缩略图展示清晰，但顶部统计与“处理”按钮含义偏工程化，用户不确定“处理”会做什么。 | 将按钮改为“刷新索引”或“处理媒体”，增加处理中/完成/失败说明。 |
| P1-4 | 相册 | 页面同时出现顶部“新建”和空状态“新建相册”两个 CTA，重复。 | 空状态保留主 CTA；顶部区域在空状态时只显示统计，避免重复。 |
| P1-5 | 上传页 | 上传入口只提示“从文件页开始上传”，但当前页没有直接选择文件入口。 | 已在上传页空状态增加“去文件页上传”按钮，先收敛到可发现的上传入口；后续可进一步直接拉起文件选择器。 |
| P1-6 | 登录页 | 登录错误提示为英文 `Invalid username or password!`，与中文界面不一致。 | 已本地化为“用户名或密码错误，请检查大小写后重试。”，Android 模拟器复验通过。 |
| P1-7 | 设置页 | 登录方式区微信/Google 未启用但仍占据大块空间；GitHub 可绑定，信息层级不够清楚。 | 未启用项置灰并说明“管理员未配置”；GitHub 显示“可绑定/已绑定”状态标签。 |

## P2：视觉细节打磨

| 编号 | 问题 | 建议 |
|---|---|---|
| P2-1 | 大屏模拟器下页面顶部留白较大，首屏信息密度偏低。 | 适当降低 Header 与卡片间距，保持 16/24 spacing 体系。 |
| P2-2 | 图标容器风格统一，但文件类型图标 `DIR/IMG` 偏开发占位感。 | 使用真实文件夹/图片/视频图标或小缩略图；未知类型再用扩展名。 |
| P2-3 | 底部导航视觉清晰，但上传作为核心操作和普通 Tab 平级。 | 后续可考虑居中突出上传 FAB 或在文件页提供更强主按钮。 |
| P2-4 | 设置页容量卡片蓝色渐变识别度高，但进度条太细，容量占比难读。 | 提高进度条高度，补充“0.05% 已用”或剩余容量标签。 |

## 已修复并复验的运行问题

### 系统健康 404

修复前后端日志：

```text
GET /api/file-center/system-health/summary -> 404
```

执行：

```bash
docker compose -f /d/Devs/Projects/Personal/PrivateCloudDrive/docker-compose.yml up -d --build api media-worker
```

复验后端日志：

```text
GET /api/file-center/system-health/summary -> 200
GET /api/file-center/storage/usage -> 200
```

App 复验：`artifacts/ui-review/screenshots/14-settings-health-fixed.png`，系统健康显示“运行正常”。最新安装包复验截图为 `artifacts/android-ui/settings-system-health-latest.png`，仍显示 API、DB、Redis、FileSystem、FFmpeg、FFprobe 正常。

## 最新 App 可见改动复验（2026-05-15 补充）

验证设备：Android Emulator `emulator-5554`。

验证包：`maui/PrivateCloudDrive.App/bin/Debug/net10.0-android/com.companyname.privateclouddrive.app-Signed.apk`。

验证命令：

```bash
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None
adb install -r maui/PrivateCloudDrive.App/bin/Debug/net10.0-android/com.companyname.privateclouddrive.app-Signed.apk
```

补充说明：直接安装默认 Debug APK 时 Android 端触发 Fast Deployment assembly 缺失崩溃；已使用 `-p:EmbedAssembliesIntoApk=true` 重新构建并安装，App 可正常启动。

| 项目 | 结果 | 证据 |
|---|---|---|
| 登录错误中文化 | 输入错误账号密码后显示“用户名或密码错误，请检查大小写后重试。” | `artifacts/android-ui/login-invalid-error.png` |
| 上传页入口 | 上传页空状态显示“去文件页上传”按钮 | `artifacts/android-ui/upload-empty-with-entry.png` |
| 设置页系统健康 | 系统健康显示“运行正常”，API、DB、Redis、存储、FFmpeg、FFprobe 均正常 | `artifacts/android-ui/settings-system-health-latest.png` |

## 下一步收口建议

1. 优先修 P1-1、P1-2、P1-5、P1-6：这些直接影响主路径发现、误触风险、上传入口和错误理解。
2. 将 Docker API 重建后的 App 复验流程写入发布前检查：登录、文件列表、设置页容量、系统健康、媒体库缩略图。
3. 完成一轮 App 可见改动后，重新安装/启动最新 Android App 并补齐截图证据。
