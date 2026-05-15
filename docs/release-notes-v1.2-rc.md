# PrivateCloudDrive V1.2 RC Release Notes

发布日期：2026-05-15
发布类型：V1.2 Release Candidate

## 产品定位

V1.2 RC 将 PrivateCloudDrive 从“文件型私有云盘”推进到“移动优先的私人媒体库”。本轮不扩展 NAS、AI 相册或协作套件范围，而是聚焦媒体回看、整理、状态可见和移动端可交付质量。

## 本版包含

### 媒体库时间线

- Photos/媒体库首页支持图片和视频混合时间线。
- 支持全部、图片、视频筛选。
- 时间排序优先使用 `MediaAsset.TakenAt`，缺失时回退到文件创建时间。
- MAUI 端按月份分组展示媒体项目。
- Pending、Processing、Failed、Completed 状态均可在媒体卡片或详情链路中被识别，不再把处理中/失败媒体表现为空白。

### 媒体详情与处理状态

- 新增媒体详情接口，返回处理状态、尺寸、时长、缩略图和脱敏失败摘要。
- 新增处理状态列表，便于用户查看处理中或失败的媒体。
- 失败媒体支持重新处理入口。
- 视频预览页区分可播放、处理中和失败状态。

### 相册

- 支持创建、重命名/描述更新、删除相册。
- 支持向相册添加或移除图片/视频。
- 删除相册不会删除原文件。
- 支持基础封面设置和相册项目数量展示。
- 同一用户同名相册、同一媒体重复加入相册均有后端约束和测试覆盖。

### API 与数据结构

- 新增/完善媒体库接口：
  - `GET /api/file-center/media/timeline`
  - `GET /api/file-center/media/{fileNodeId}/detail`
  - `GET /api/file-center/media/processing-status`
  - `POST /api/file-center/media/{fileNodeId}/retry-processing`
- 新增相册接口：
  - `GET/POST /api/file-center/media/albums`
  - `GET/PUT/DELETE /api/file-center/media/albums/{albumId}`
  - `POST /api/file-center/media/albums/{albumId}/items`
  - `DELETE /api/file-center/media/albums/{albumId}/items/{fileNodeId}`
  - `POST /api/file-center/media/albums/{albumId}/cover/{fileNodeId}`
- 新增 `MediaAlbum`、`MediaAlbumItem` 与对应 EF Core 迁移。

## 不包含

- AI 自动分类、人物识别、OCR 或语义搜索。
- 视频多码率转码、在线播放自适应码率或服务端转封装。
- NAS 协议、桌面同步、多节点高可用。
- iOS/真实 Android 设备完整回归；当前 RC 已完成 Android Emulator 启动与登录页视觉闸门，真实设备媒体库全流程仍需按发布需要继续回填。

## RC 验证结果

| 范围 | 命令/证据 | 结果 |
| --- | --- | --- |
| 后端构建 | `dotnet build /d/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core/PrivateCloudDrive.slnx -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-v12-rc-backend-build/` | 通过，0 警告，0 错误 |
| 后端测试 | `dotnet test /d/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core/PrivateCloudDrive.slnx --no-build -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-v12-rc-backend-build/ --logger "trx;LogFilePrefix=v12-rc-backend" --results-directory D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/test-results/v12-rc-backend` | 通过；`PrivateCloudDrive.EntityFrameworkCore.Tests` 101 个测试通过，其它测试项目当前无可发现测试 |
| 本地栈健康检查 | `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/verify-local-stack.ps1 -SkipStart -TimeoutSeconds 120` | 通过；Docker/Compose、PostgreSQL、Redis、db-migrator、API、media-worker、Swagger、`/app/storage`、ffmpeg、ffprobe 均 PASS；本机 `.env` 模板口令、默认数据库口令、localhost 地址保留为本地验证 WARN，不视为产品缺陷 |
| MAUI 顺序构建 | `powershell -NoProfile -ExecutionPolicy Bypass -File D:/Devs/Projects/Personal/PrivateCloudDrive/scripts/verify-maui-build.ps1 -Configuration Debug` | Windows 与 Android 目标均通过，PASS 4 / WARN 0 / FAIL 0 |
| Android APK 产物 | `dotnet publish ... -f net10.0-android -c Debug -p:TargetFrameworks=net10.0-android -p:AndroidPackageFormat=apk` | 已生成 Signed APK |
| APK 路径 | `artifacts/verify-v12-rc-maui-apk/com.companyname.privateclouddrive.app-Signed.apk` | 约 96 MB；2026-05-15 09:53:29 +0800 重新生成，启用 `EmbedAssembliesIntoApk=true` 避免 clean install 后 fast deployment 依赖缺失 |
| Android 模拟器安装启动 | `adb install artifacts/verify-v12-rc-maui-apk/com.companyname.privateclouddrive.app-Signed.apk`；`adb shell am start -W -n com.companyname.privateclouddrive.app/crc644ff135ff239f5ce3.MainActivity` | 通过；Pixel 9 Pro API 36 模拟器，Android 16 / SDK 36；`firstInstallTime=2026-05-15 01:02:51`，登录页可见，未出现崩溃；09:54 复验时本机暂无已连接设备/模拟器，归类为环境边界 |
| Android 登录页视觉闸门 | 截图 `artifacts/runtime/v12-rc-android/13-am-start-themed.png` | 通过；最新卡片式登录 UI 可见，Android 原生 Entry 下划线/双边框已消除，未发现裁切或溢出 |
| Android 键盘边界 | 截图 `artifacts/runtime/v12-rc-android/16-keyboard-hardkeyboard-enabled.png` 与 `adb shell dumpsys input_method` | 未完成；模拟器当前 `mInputShown=false`，软键盘未弹出，因此键盘遮挡仍需真机或可弹出软键盘的模拟器继续回填 |
| 本轮 RC 复验 | `git diff --check`；PowerShell 脚本 Parser；`docker compose config`；`verify-local-stack.ps1 -PreflightOnly`；`verify-local-stack.ps1 -SkipStart -TimeoutSeconds 120`；后端 build/test；`verify-maui-build.ps1 -Configuration Debug`；Android APK publish | 通过；diff 无空白错误，脚本语法通过，Compose 配置通过，后端 0 警告/0 错误且 101 个 EF 测试通过，MAUI Windows/Android 顺序构建 PASS 4 / WARN 0 / FAIL 0；本地栈预检 PASS 10 / WARN 3 / FAIL 0，SkipStart 全栈健康检查 PASS 19 / WARN 4 / FAIL 0；WARN 仅为本机 `.env` 模板/localhost 与跳过启动的发布前确认项 |
| 安全加固复验 | `dotnet build ... -p:OutDir=artifacts/verify-security-hardening-build/`；`dotnet test ... --filter PublicFileSharesControllerSecurityTests`；`docker compose config`；`git diff --check` | 通过；后端 0 警告/0 错误，公开分享密码入口新增 3 个安全回归测试并全部通过；Docker Compose 配置有效；公开分享密码改为 `X-Share-Password` 请求头，密码校验/下载入口启用 `PublicSharePassword` 限速；生产启动新增 HTTPS、默认口令、默认加密口令 fail-fast 门禁；生产 Swagger 默认关闭，Docker 本地验证显式开启 |

## 已知边界与发布风险

- Android 模拟器已完成 clean install、启动和登录页视觉闸门；真实 Android 设备、iOS 与媒体库全流程触控回归仍需在对应设备上执行。
- 模拟器键盘验证未完成：当前 Pixel 9 Pro API 36 环境点击输入框后 `mInputShown=false`，软键盘未弹出；需在真机或调整输入法/硬件键盘设置后的模拟器上继续验证键盘遮挡。
- 本机 Docker 栈可用；生产化部署前仍必须把 `.env` 中模板口令、默认数据库口令和 localhost 公网地址替换为部署环境值；后端已加入生产启动 fail-fast 门禁，未显式标记本地验证时会拒绝 `RequireHttpsMetadata=false`、HTTP Authority/SelfUrl、默认数据库口令和默认加密口令。
- 生产 Swagger 默认关闭；本地 Docker Compose 为健康检查显式设置 `Swagger__Enabled=true`，真实生产环境应保持关闭或仅在内网/VPN/管理员通道开放。
- 公开分享密码不再通过 URL Query 传递；受密码保护的公开下载必须使用 `X-Share-Password` 请求头，且密码校验/下载入口受 `PublicSharePassword` 限速策略保护。
- 本轮后端测试统计为 101 个 EF 集成测试通过；部分测试程序集没有可发现测试，属于当前测试项目结构现状，不视为失败。
- MAUI `-NoRestore` 在 assets 未包含目标框架时会失败；RC 构建应允许脚本自行 restore，或先显式 restore 对应目标框架。
- Android/iOS 微信登录仍依赖真实开放平台配置、包名签名、设备安装微信和可访问后端地址。
- 私有部署生产环境必须替换 `.env` 模板密钥、默认数据库密码和 localhost 公网地址。

## 验收建议

1. 使用 `artifacts/verify-v12-rc-maui-apk/com.companyname.privateclouddrive.app-Signed.apk` 在 Android Emulator 或真机执行 clean install；Debug APK 发布时保留 `-p:EmbedAssembliesIntoApk=true`，避免卸载重装后缺少 fast deployment assemblies。
2. 启动本地 Docker Compose 后端，并确保 Android 端可访问 `http://10.0.2.2:8080` 或局域网 API 地址。
3. 按 `docs/testing.md` 的 V1.2 手动验收清单回归媒体时间线、相册、处理状态、视频详情和重新处理。
4. 在可弹出软键盘的设备上补充账号/密码输入、键盘遮挡和登录按钮可达性验证。
5. 回填设备型号、系统版本、App 构建号、后端提交和结果；禁止记录密码、access token、refresh token、AppSecret 或微信 token。
