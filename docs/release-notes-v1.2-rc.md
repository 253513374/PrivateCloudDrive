# PrivateCloudDrive V1.2 RC Release Notes

发布日期：2026-05-14
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
- iOS/真实 Android 设备完整回归；当前 RC 以 Windows/Android 构建与 APK 产物为工程闸门，交互验收需在可用设备上继续回填。

## RC 验证结果

| 范围 | 命令/证据 | 结果 |
| --- | --- | --- |
| 后端构建 | `dotnet build /d/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core/PrivateCloudDrive.slnx -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-v12-rc-backend-build/` | 通过，0 警告，0 错误 |
| 后端测试 | `dotnet test /d/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core/PrivateCloudDrive.slnx --no-build -p:OutDir=D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/verify-v12-rc-backend-build/ --logger "trx;LogFilePrefix=v12-rc-backend" --results-directory D:/Devs/Projects/Personal/PrivateCloudDrive/artifacts/test-results/v12-rc-backend` | 通过；`PrivateCloudDrive.EntityFrameworkCore.Tests` 101 个测试通过，其它测试项目当前无可发现测试 |
| MAUI 顺序构建 | `powershell -NoProfile -ExecutionPolicy Bypass -File D:/Devs/Projects/Personal/PrivateCloudDrive/scripts/verify-maui-build.ps1 -Configuration Debug` | Windows 与 Android 目标均通过，PASS 4 / WARN 0 / FAIL 0 |
| Android APK 产物 | `dotnet publish ... -f net10.0-android -c Debug -p:TargetFrameworks=net10.0-android -p:AndroidPackageFormat=apk` | 已生成 Signed APK |
| APK 路径 | `artifacts/verify-v12-rc-maui-apk/com.companyname.privateclouddrive.app-Signed.apk` | 约 17 MB |
| 设备检测 | `adb devices` | 当前无已连接 Android 设备/模拟器，未执行安装与截图验收 |

## 已知边界与发布风险

- 当前机器未检测到 Android 设备或模拟器，因此 APK 安装、启动截图和真实触控链路尚未完成。
- 本轮后端测试统计为 101 个 EF 集成测试通过；部分测试程序集没有可发现测试，属于当前测试项目结构现状，不视为失败。
- MAUI `-NoRestore` 在 assets 未包含目标框架时会失败；RC 构建应允许脚本自行 restore，或先显式 restore 对应目标框架。
- Android/iOS 微信登录仍依赖真实开放平台配置、包名签名、设备安装微信和可访问后端地址。
- 私有部署生产环境必须替换 `.env` 模板密钥、默认数据库密码和 localhost 公网地址。

## 验收建议

1. 在 Android Emulator 或真机上安装 `artifacts/verify-v12-rc-maui-apk/com.companyname.privateclouddrive.app-Signed.apk`。
2. 启动本地 Docker Compose 后端，并确保 Android 端可访问 `http://10.0.2.2:8080` 或局域网 API 地址。
3. 按 `docs/testing.md` 的 V1.2 手动验收清单回归媒体时间线、相册、处理状态、视频详情和重新处理。
4. 回填设备型号、系统版本、App 构建号、后端提交和结果；禁止记录密码、access token、refresh token、AppSecret 或微信 token。
