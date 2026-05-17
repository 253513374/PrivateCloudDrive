# Private Backup MVP Slice 1 Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** 将 App 第一条产品链路从“泛云盘”推进到“可配置后端地址、能看到私有备份状态与存储可信入口”的最小可验收闭环。

**Architecture:** 先不引入新的后端表和复杂同步调度，优先复用现有上传队列、系统健康、存储用量和文件上传能力。移动端新增可持久化的后端地址配置，并在设置页直接提供连接保存、测试、恢复默认能力；传输页文案改为“备份队列”；底部导航把“动态”调整为“备份”。

**Tech Stack:** .NET MAUI, Preferences, SecureStorage, ABP backend existing file-center APIs.

---

## 当前能力盘点

- App 已有 `UploadQueueService` 和 `UploadQueueItem`，可显示等待、上传中、失败、完成任务。
- 文件页已有批量选择本机文件上传能力，但入口和文案仍偏“文件管理”。
- 设置页已能读取 `GetStorageUsageAsync()` 和 `GetSystemHealthSummaryAsync()`，能展示容量、API/DB/Redis/存储/FFmpeg 状态。
- 后端已有 `PrivateCloudDriveSettings.FileCenter.StorageRootPath`、容量配额、系统健康摘要、上传会话和 Blob 存储能力。
- App 的 `AppSettings.ApiBaseUrl` 目前是编译期固定值，不能由用户配置；这是私有部署产品的 P0 缺口。

## Slice 1 验收目标

1. 设置页“服务与安全”变成“私有备份服务”。
2. 用户可看到当前后端地址，并输入新的后端地址。
3. 用户可保存后端地址到本机 Preferences。
4. 用户可一键恢复默认后端地址。
5. 保存/恢复后清理本地 token，避免旧服务器会话误用。
6. API Client 和 Auth Client 使用最新 `AppSettings.ApiBaseUrl`。
7. 传输页改称“备份队列”，底部 Tab 从“动态”改为“备份”。
8. 构建通过，并完成 Android Debug APK 安装启动验收。

## Task 1: 持久化可配置后端地址

**Files:**
- Modify: `maui/PrivateCloudDrive.App/Services/AppSettings.cs`

**Steps:**
1. 引入 `Microsoft.Maui.Storage`。
2. 增加 `CustomApiBaseUrlKey`。
3. 将 `ApiBaseUrl` 改为优先读取 Preferences 中的自定义 URL。
4. 增加 `DefaultApiBaseUrl`、`SetApiBaseUrl(string)`、`ResetApiBaseUrl()`。
5. `SetApiBaseUrl` 需要 trim、校验 http/https、去掉末尾 `/`。

## Task 2: API/Auth Client 支持切换后的地址

**Files:**
- Modify: `maui/PrivateCloudDrive.App/Services/CloudDriveApiClient.cs`
- Modify: `maui/PrivateCloudDrive.App/Services/OpenIddictAuthService.cs`

**Steps:**
1. 在 `CloudDriveApiClient.CreateAuthenticatedRequestAsync` 中使用 `new Uri(new Uri(AppSettings.ApiBaseUrl), requestUri.TrimStart('/'))` 构造绝对 URI。
2. 在 `OpenIddictAuthService` 增加 `EnsureBaseAddressCurrent()`。
3. 在 token、revocation、audit 请求前调用 `EnsureBaseAddressCurrent()`。

## Task 3: 设置页服务地址编辑与信任说明

**Files:**
- Modify: `maui/PrivateCloudDrive.App/Views/SettingsPage.xaml`
- Modify: `maui/PrivateCloudDrive.App/Views/SettingsPage.xaml.cs`

**Steps:**
1. 将“服务与安全”改为“私有备份服务”。
2. 将服务器行改成可编辑 Entry + 保存/恢复默认按钮。
3. 增加后端地址说明：数据会上传到该地址对应服务器，实际存储位置以服务器配置为准。
4. 保存或恢复默认后调用 `_authService.SignOutAsync()` 并刷新设置状态。

## Task 4: 备份中心文案收敛

**Files:**
- Modify: `maui/PrivateCloudDrive.App/AppShell.xaml`
- Modify: `maui/PrivateCloudDrive.App/Views/UploadsPage.xaml`
- Modify: `maui/PrivateCloudDrive.App/Views/CreateActionPage.xaml`
- Modify: `maui/PrivateCloudDrive.App/Views/CreateActionPage.xaml.cs`

**Steps:**
1. Tab “动态”改为“备份”。
2. 上传页标题改为“备份队列”。
3. 空状态文案改为：从文件页选择照片/文件备份后，进度会显示在这里。
4. 快速创建页改为“开始备份”，突出照片/文件备份。

## Task 5: 验证

**Commands:**

```bash
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None
```

Then start backend, clear App data, install APK, launch App, capture screenshot.
