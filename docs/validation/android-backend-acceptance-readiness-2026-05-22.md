# Android 可见验收后端准备记录（2026-05-22）

记录时间：2026-05-22 23:05:03 +0800
任务：t_4120a245

## 结论

后端已按 Android App 可见验收范围补齐 API/权限说明，并修复公开分享 AppService 被 ABP Conventional Controller 动态暴露导致 Swagger/OpenAPI method/path 冲突的风险。公开分享匿名 HTTP 入口继续由显式 `PublicFileSharesController` 承载；后端测试覆盖 FileCenter、MobileAuth、OperationLogs 相关路径。

本文档已按脱敏规则编写：不包含密码、token、Cookie、AppSecret、连接字符串或完整公开分享 URL。

## 验收 API 与权限清单

| 能力 | 主要 API/入口 | 认证/权限要求 | Android/QA 验收点 |
| --- | --- | --- | --- |
| OpenIddict 登录 | `/.well-known/openid-configuration`、token 端点 | 登录前可读取 discovery；token 端点按 OpenIddict 策略认证 | App 能发现认证配置并完成测试账号登录 |
| 文件列表/目录 | `api/app/file-center/folders*` 或显式 FileCenter 控制器路由 | 已登录；FileCenter View/Manage 相关权限 | 文件页可加载根目录、子目录、空状态 |
| 小文件上传 | `api/file-center/files/upload-small` | `PrivateCloudDrivePermissions.FileCenter.Upload` | 上传样例文件成功，失败有明确提示 |
| 分片上传 | `api/file-center/upload-sessions`、`{id}/chunks/{chunkIndex}`、`{id}/complete`、`{id}` DELETE | `PrivateCloudDrivePermissions.FileCenter.Upload` | 可创建会话、上传分片、完成或取消；记录会话状态但不记录文件隐私内容 |
| 下载/Range/预览 | `api/file-center/files/{id}/download`、`api/file-center/files/{id}/content` | `PrivateCloudDrivePermissions.FileCenter.Download` | 下载返回文件元数据；Range/预览路径不返回 5xx |
| 分享管理 | `api/file-center/shares`、`api/file-center/shares/all` | 创建/我的分享需 Share；全量管理需 Manage | 可创建、查看、取消分享；不记录完整分享 URL |
| 公开分享匿名入口 | `api/public/shares/{token}`、`verify-password`、`download` | 匿名入口；密码通过 Header；密码相关接口启用限流 | 公开分享元数据/下载可用；密码不放入 Query |
| 回收站 | `api/file-center/trash`、`api/file-center/nodes/{id}/restore`、`permanent`、批量 restore/delete | View/Delete/Manage 按操作区分 | 删除、恢复、永久删除路径可见且权限正确 |
| 系统健康 | `api/file-center/system-health/summary`、`api/file-center/storage/usage` | `PrivateCloudDrivePermissions.FileCenter.View` | 未登录应 401；登录后只返回脱敏健康摘要 |
| 移动认证审计 | `api/mobile-auth/audit-logs` POST/GET | POST 匿名写入；GET 需 `PrivateCloudDrivePermissions.MobileAuth.AuditLogs` | App 行为可写入审计；管理员可按时间窗口查询 |
| 操作日志 | `api/operation-logs` | `PrivateCloudDrivePermissions.OperationLogs.View` | QA 可按验收时间窗口查询关键行为 |

## 测试账号角色说明

- 建议 QA 使用具备 admin 角色或等价权限集合的测试账号，覆盖 FileCenter View/Upload/Download/Share/Delete/Manage、MobileAuth AuditLogs、OperationLogs View。
- 普通外部登录用户可用于补充登录体验验证，但权限不足以覆盖完整文件中心、系统健康和审计查询验收。
- 密码、临时 token、Cookie、AppSecret、完整分享链接必须通过安全渠道传递，不写入本仓库文档、Kanban 评论或日志。

## 异常接口修复说明

### 问题

`IFileCenterPublicSharesAppService` 存在两个 `GetDownloadAsync` 重载。若该 AppService 被 ABP Conventional Controller 自动暴露，会与显式 `PublicFileSharesController` 的公开下载入口产生 OpenAPI method/path 冲突，表现为 `GET /swagger/v1/swagger.json` 可能返回 500。

### 修复

- 在 `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/IFileCenterPublicSharesAppService.cs` 为接口添加 `[RemoteService(false)]`。
- 保留 `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/FileCenter/PublicFileSharesController.cs` 作为唯一公开分享匿名 HTTP 入口。
- 在 `PublicFileSharesControllerSecurityTests` 增加回归测试，确保公开分享 AppService 不再被 Conventional Controller 暴露。

## QA 审计记录时间点

建议 QA 在执行 Android 可见验收时记录以下脱敏时间点，便于后端查询审计与操作日志：

| 时间点 | 建议记录内容 | 查询用途 |
| --- | --- | --- |
| T0 | 开始验收时间（本地时区） | 限定审计查询窗口 |
| T1 | 首次登录成功或失败时间 | `mobile-auth/audit-logs` 登录相关事件 |
| T2 | 文件上传开始/完成时间 | FileCenter 操作日志、上传会话状态 |
| T3 | 下载/Range 访问时间 | 下载能力与权限验证 |
| T4 | 分享创建/访问/取消时间 | 分享管理与公开入口验证 |
| T5 | 删除/恢复/永久删除时间 | 回收站与权限验证 |
| T6 | 结束验收时间 | 汇总日志查询窗口 |

记录格式建议：`2026-05-22 HH:mm:ss +0800，事件类型，脱敏文件名/遮罩标识，结果 PASS/WARN/FAIL`。

## 当前残余风险

1. 本任务只保证后端契约、权限说明、回归测试与脱敏文档准备；最终 Android 可见通过仍依赖本地 API 栈启动、APK 安装和 QA 截图证据。
2. 若后续运行 Swagger JSON 探针仍出现 500，应优先检查是否存在其他 AppService 重载被 Conventional Controller 暴露。
3. 构建过程中如继续出现第三方包安全 warning，应单独登记升级任务，不在本文档中混入密钥或连接信息。
