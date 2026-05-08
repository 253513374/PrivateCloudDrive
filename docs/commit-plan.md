# 提交整理计划

日期：2026-05-08

## 目的

当前工作区包含阶段 5 到阶段 8 的大量已修改和未跟踪文件。阶段完成规则要求每个阶段完成后提交 Git，因此提交前需要先分组、排除运行时数据，并避免把多个阶段混在一个不可审计的大提交里。

## 预提交排除项

- `**/App_Data/`：本地 FileCenter 运行时存储，已加入 `.gitignore`。
- `artifacts/`、`bin/`、`obj/`、测试输出和包产物：已由现有 `.gitignore` 排除。
- `.env`、证书和私钥：已由现有 `.gitignore` 排除。

提交前建议先运行：

```powershell
git status --short --ignored
```

确认只剩业务代码、迁移、配置模板和文档变更。

## 建议提交批次

### 1. MVP Core 收尾和部署文档

目的：收敛阶段 5 的回收站、部署、README 和本地运行文档。

候选范围：

- `.gitignore`
- `README.md`
- `docker-compose.yml`
- `.env.example`
- `global.json`
- `docs/deployment.md`
- `docs/testing.md`
- `docs/progress.md`
- `docs/completion-audit.md`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/IFileCenterFoldersAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.Application/FileCenter/FileCenterFoldersAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/FileCenter/FileCenterTrashController.cs`
- `maui/PrivateCloudDrive.App/Views/TrashPage.xaml`
- `maui/PrivateCloudDrive.App/Views/TrashPage.xaml.cs`
- 与回收站入口相关的 `AppShell`、Files 页面和测试文件。

注意：`docs/testing.md` 与 `docs/progress.md` 同时记录多个阶段，提交时可接受作为阶段性总账，也可以用 `git add -p` 按段拆分。

### 2. MVP 体验与账号密码认证深化

目的：收敛阶段 6 的 MAUI 设计系统、页面状态、账号密码登录、Refresh Token、撤销和移动端认证审计。

候选范围：

- `docs/auth-design.md`
- `docs/ui-design.md`
- `docs/codex-development-plan.md`
- `maui/PrivateCloudDrive.App/Resources/Styles/`
- `maui/PrivateCloudDrive.App/Resources/AppIcon/`
- `maui/PrivateCloudDrive.App/Services/OpenIddictAuthService.cs`
- `maui/PrivateCloudDrive.App/Services/IAuthService.cs`
- `maui/PrivateCloudDrive.App/Views/LoginPage.xaml`
- `maui/PrivateCloudDrive.App/Views/LoginPage.xaml.cs`
- `maui/PrivateCloudDrive.App/Views/StartupPage.xaml`
- `maui/PrivateCloudDrive.App/Views/StartupPage.xaml.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/MobileAuth/CreateMobileAuthAuditLogInput.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/MobileAuth/IMobileAuthAuditLogsAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.Application/MobileAuth/MobileAuthAuditLogsAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.Domain/MobileAuth/MobileAuthAuditLog.cs`
- `aspnet-core/src/PrivateCloudDrive.EntityFrameworkCore/Migrations/20260508003850_AddedMobileAuthAuditLogs*`
- `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/MobileAuth/MobileAuthAuditLogsController.cs`
- 相关 EF 测试。

注意：MobileAuth 目录同时包含阶段 8 WeChat 文件，提交时不要把 WeChat 文件误放进阶段 6 提交。

### 3. V1 分享、标签、收藏、媒体库与操作日志

目的：收敛阶段 7 的分享、标签、收藏、媒体库和操作日志。

候选范围：

- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/GetMediaFilesInput.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/IFileCenterMediaLibraryAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/IFileCenterSharesAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/OperationLogs/`
- `aspnet-core/src/PrivateCloudDrive.Application/FileCenter/FileCenterMediaLibraryAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.Application/FileCenter/FileCenterSharesAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.Application/OperationLogs/`
- `aspnet-core/src/PrivateCloudDrive.Domain.Shared/OperationLogs/`
- `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/FileCenter/FileCenterMediaLibraryController.cs`
- `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/FileCenter/FileCenterNodesController.cs`
- `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/FileCenter/FileCenterSharesController.cs`
- `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/FileCenter/FileCenterTagsController.cs`
- `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/OperationLogs/`
- `maui/PrivateCloudDrive.App/Models/CloudDriveShare.cs`
- `maui/PrivateCloudDrive.App/Models/CloudDriveTag.cs`
- `maui/PrivateCloudDrive.App/Models/CloudOperationLog.cs`
- `maui/PrivateCloudDrive.App/Models/MediaLibraryItem.cs`
- `maui/PrivateCloudDrive.App/Views/FileDetailsPage*`
- `maui/PrivateCloudDrive.App/Views/OperationLogsPage*`
- `maui/PrivateCloudDrive.App/Views/PhotosPage*`
- `maui/PrivateCloudDrive.App/Views/VideosPage*`
- 相关 EF 测试。

注意：`FileCenterSharesAppService`、权限定义和 MAUI 文件页可能同时带有阶段 5 到阶段 7 的改动，适合用 `git add -p` 复核。

### 4. V1 微信登录可选接入

目的：收敛阶段 8 的 WeChat 后端、OpenIddict 自定义 grant、MAUI 入口骨架、限流和验收文档。

候选范围：

- `docs/wechat-login-design.md`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/MobileAuth/BindCurrentWechatInput.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/MobileAuth/BindExistingWechatInput.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/MobileAuth/IWechatAuthAppService.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/MobileAuth/WechatBindingDto.cs`
- `aspnet-core/src/PrivateCloudDrive.Application.Contracts/MobileAuth/WechatLoginSettingsDto.cs`
- `aspnet-core/src/PrivateCloudDrive.Application/MobileAuth/*Wechat*`
- `aspnet-core/src/PrivateCloudDrive.Domain.Shared/MobileAuth/*Wechat*`
- `aspnet-core/src/PrivateCloudDrive.Domain/MobileAuth/WechatUserBinding.cs`
- `aspnet-core/src/PrivateCloudDrive.EntityFrameworkCore/MobileAuth/`
- `aspnet-core/src/PrivateCloudDrive.EntityFrameworkCore/Migrations/20260508021008_AddedWechatUserBindings*`
- `aspnet-core/src/PrivateCloudDrive.EntityFrameworkCore/Migrations/20260508033000_FixedWechatUserBindingUniqueIndexes.cs`
- `aspnet-core/src/PrivateCloudDrive.HttpApi.Host/MobileAuth/WechatTokenGrantHandler.cs`
- `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/MobileAuth/MobileAuthWechatController.cs`
- `maui/PrivateCloudDrive.App/Models/Wechat*`
- `maui/PrivateCloudDrive.App/Services/*Wechat*`
- WeChat 相关登录页、设置页和 API 客户端改动。
- WeChat 相关 EF 测试。

注意：Android WeChat SDK 已接入授权桥接，但正式凭据、Android 真机授权和 iOS SDK 尚未验收，此提交不应把阶段 8 标记为已完成。

## 提交前验证顺序

1. `dotnet build .\aspnet-core\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-build-before-commit\`
2. `dotnet test .\aspnet-core\PrivateCloudDrive.slnx -p:OutDir=D:\Devs\Projects\Personal\PrivateCloudDrive\artifacts\verify-test-before-commit\`
3. `dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-windows10.0.19041.0 -f net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64`
4. `dotnet build .\maui\PrivateCloudDrive.App\PrivateCloudDrive.App.csproj -p:TargetFrameworks=net10.0-android -f net10.0-android`
5. `docker compose config`

## 当前风险

- 当前变更跨越多个阶段，直接 `git add .` 会产生难以审计的大提交。
- EF migration snapshot、权限定义、HTTP 控制器和 MAUI 文件页包含跨阶段改动，必要时需要 hunk 级 staging。
- 阶段 8 不能因为后端测试和 MAUI 构建通过就标记完成，真实 SDK 和真机链路仍缺失。
