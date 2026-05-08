# 认证体系设计

本文档定义 PrivateCloudDrive 的账号体系、Token 生命周期、管理员初始化、MAUI 本地安全、登录审计和后续接口草案。后端继续使用 ABP Identity + OpenIddict，不自建身份系统，不绕过 OpenIddict 直接签发自定义 JWT。

版本边界：

- MVP Core：账号密码登录、管理员初始化、Token 刷新、退出登录、SecureStorage、基础登录审计。
- V1：微信登录、微信绑定/解绑、微信扩展 grant、操作日志查询增强。

## 1. 设计目标

- 管理员账号必须始终可以通过账号密码登录。
- 普通用户支持账号密码登录、Token 刷新和退出登录。
- MAUI App 使用 SecureStorage 保存 Token，不保存用户密码。
- 微信登录是 V1 可选第三方登录能力，失败或未配置时不影响本地账号登录。
- 所有登录、刷新失败、退出、绑定和解绑行为必须记录审计日志。
- 后端接口必须可由 Codex 分阶段实现和测试。

## 2. 技术基线

现有基线：

- 后端：ABP Framework、ABP Identity、OpenIddict、PostgreSQL、Redis。
- 移动端：.NET MAUI，使用第一方 `PrivateCloudDrive_App` public client 和 SecureStorage Token 保存。
- OpenIddict 移动端客户端：`PrivateCloudDrive_App`。
- 当前推荐继续使用 `offline_access` 获取 refresh token。

目标登录方式：

| 登录方式 | 是否必需 | 说明 |
| --- | --- | --- |
| 账号密码登录 | 必需 | 管理员和普通用户的主登录方式 |
| Refresh Token | 必需 | App 无感刷新访问令牌 |
| Authorization Code | 后续/兼容 | 可保留给 WebAuthenticator 或浏览器登录，不作为 MVP Core 主链路 |
| 微信登录 | V1 可选 | 第三方登录与绑定能力，不作为唯一登录 |
| 生物识别解锁 | 后续 | 只解锁本地 Token，不替代服务端登录 |

## 3. 账号模型

### 3.1 用户类型

- Admin：系统管理员，拥有用户、系统、存储、分享和审计管理权限。
- User：普通用户，只能管理自己有权限访问的文件和媒体。

### 3.2 管理员初始化

首次部署必须通过 DbMigrator 或启动种子初始化管理员。

配置项建议：

```json
{
  "Auth": {
    "Admin": {
      "UserName": "admin",
      "Email": "admin@privateclouddrive.local",
      "Password": "ChangeMe_123456",
      "ForceChangePasswordOnFirstLogin": true
    }
  }
}
```

环境变量建议：

```text
Auth__Admin__UserName=admin
Auth__Admin__Email=admin@example.com
Auth__Admin__Password=change-this-password
Auth__Admin__ForceChangePasswordOnFirstLogin=true
```

规则：

- 初始化只在管理员不存在时创建，不覆盖已有管理员密码。
- 生产环境必须要求显式配置强密码；不能长期使用 ABP 默认密码。
- 管理员即使绑定微信，也必须保留账号密码登录能力。
- 禁用微信登录、微信配置错误、微信接口不可达时，管理员账号密码登录仍必须可用。

## 4. OpenIddict 客户端设计

### 4.1 MAUI Public Client

客户端名称：`PrivateCloudDrive_App`

建议权限：

- Endpoint：token、revocation、introspection。
- Grant Type：password、refresh_token。
- Scope：openid、profile、email、roles、offline_access、PrivateCloudDrive。

账号密码登录可以直接使用 OpenIddict Token Endpoint 的 password grant。这样 Token 仍由 OpenIddict 统一签发，后续权限、刷新、撤销和审计都能集中处理。

兼容与 V1 扩展：

- `authorization_code` 可作为 WebAuthenticator 或浏览器登录兼容方案保留，但不作为 MVP Core 主登录方式。
- 微信扩展 grant 属于 V1，必须受配置开关控制；未启用时不能影响 `password` 和 `refresh_token`。
- 如启用 `authorization_code`，才需要配置 `RedirectUri` 和 `PostLogoutRedirectUri`。

### 4.2 Swagger Client

保留现有 `PrivateCloudDrive_Swagger`，用于开发测试。生产环境应限制 Swagger 可见性或仅内网启用。

## 5. Token 生命周期

建议默认值：

| Token | 生命周期 | 存储位置 | 说明 |
| --- | --- | --- | --- |
| Access Token | 30 分钟 | MAUI SecureStorage | API Bearer Token |
| Refresh Token | 14 到 30 天 | MAUI SecureStorage | 用于续期 |
| WeChat Access Token | 不长期保存 | 后端临时内存/变量 | 仅用于换取微信身份 |
| Binding Ticket | 5 分钟 | Redis/Distributed Cache | 微信首次绑定临时票据 |

刷新规则：

- App 在 Access Token 过期前 2 分钟尝试刷新。
- 刷新成功后覆盖 SecureStorage 中的 TokenSet。
- 刷新失败、refresh token 过期或被撤销时，清理 SecureStorage 并回到登录页。
- 后端应启用 refresh token 撤销和可选轮换。

SecureStorage Key 建议：

```text
auth.tokens
auth.server
auth.user
```

`auth.tokens` 保存结构：

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "tokenType": "Bearer",
  "expiresAt": "2026-05-07T14:30:00Z"
}
```

安全规则：

- 不保存密码。
- 不把 Token 写入日志、崩溃上报或普通 Preferences。
- SecureStorage 读写异常时清理本地认证状态并要求重新登录。
- Android 需要评估 Auto Backup 对 SecureStorage 的影响；生产版本建议排除 SecureStorage 相关 shared preferences 备份。
- iOS 首次安装或切换用户场景需要考虑 Keychain 残留，必要时通过首次启动标记清理旧 Token。

## 6. 登录流程

### 6.1 账号密码登录

流程：

1. App 校验服务器地址是否可访问。
2. 用户输入账号和密码。
3. App 调用 `/connect/token`。
4. 后端通过 ABP Identity 校验用户、密码、锁定状态和启用状态。
5. OpenIddict 返回 access token 和 refresh token。
6. App 写入 SecureStorage。
7. App 调用当前用户接口获取用户资料和权限。
8. 进入 Files。

请求草案：

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&
client_id=PrivateCloudDrive_App&
username=admin&
password=***&
scope=openid profile email roles offline_access PrivateCloudDrive
```

成功响应使用 OpenIddict 标准 Token Response。

失败处理：

- `invalid_grant`：账号或密码错误。
- `user_locked_out`：账号被锁定。
- `user_inactive`：账号被禁用。
- 网络失败：提示服务器不可达。

### 6.2 Token 刷新

请求草案：

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&
client_id=PrivateCloudDrive_App&
refresh_token=...
```

规则：

- 刷新接口失败不重试无限次，最多一次即时重试。
- 刷新失败后必须清理本地 Token。
- 文件上传中 Token 过期时，应先刷新 Token，再继续当前分片；刷新失败则暂停队列并要求重新登录。

### 6.3 退出登录

流程：

1. App 禁用当前页面操作。
2. 调用后端撤销 refresh token。
3. 清理 SecureStorage。
4. 清理内存中的 HttpClient Authorization Header。
5. 清理上传队列中的认证上下文，保留可恢复任务元数据。
6. 跳转登录页。

请求草案：

```http
POST /connect/revocation
Content-Type: application/x-www-form-urlencoded

client_id=PrivateCloudDrive_App&
token=...&
token_type_hint=refresh_token
```

规则：

- 即使撤销接口失败，也必须清理本地 Token。
- 退出登录必须记录审计日志。
- 退出后不能继续访问本地缓存的私有缩略图，后续实现应按用户隔离缓存目录。

## 7. App 认证状态机

| 状态 | 进入条件 | 行为 |
| --- | --- | --- |
| Unknown | App 启动 | 读取 SecureStorage |
| SignedOut | 无 Token 或 Token 无效 | 显示登录页 |
| Refreshing | Access Token 即将过期 | 调用刷新接口 |
| SignedIn | Token 有效 | 进入主界面 |
| Expired | Refresh 失败 | 清理 Token，回登录页 |
| SigningOut | 用户退出 | 撤销 Token 并清理本地状态 |

启动流程：

1. 启动页读取 Token。
2. 若 Access Token 未过期，进入 Files。
3. 若 Access Token 过期但有 Refresh Token，尝试刷新。
4. 刷新失败则清理 Token 并进入登录页。

## 8. 登录安全策略

### 8.1 密码策略

沿用 ABP Identity 密码策略，并建议生产环境启用：

- 最小长度 8 到 12。
- 必须包含大小写字母、数字和特殊字符，具体可配置。
- 管理员初始密码首次登录后必须修改。
- 支持失败次数锁定。

### 8.2 限流与锁定

- 登录失败按账号和 IP 双维度限流。
- 账号连续失败达到阈值后使用 ABP Identity Lockout。
- 微信 code 换取失败也要限流，但不能影响本地账号密码登录。
- Refresh Token 异常频率过高时撤销该设备 Token。

### 8.3 HTTPS 与网络

- 生产环境必须使用 HTTPS。
- App 默认拒绝明文公网 HTTP；本地开发环境可以允许 `localhost` 或局域网调试地址。
- 服务器地址保存前必须经过格式校验。

### 8.4 审计日志

必须记录：

- 账号密码登录成功。
- 账号密码登录失败。
- Token 刷新失败。
- 退出登录。
- 管理员初始化。

V1 增加记录：

- 微信登录成功。
- 微信登录失败。
- 微信绑定成功。
- 微信绑定失败。
- 微信解绑成功。

日志字段建议：

| 字段 | 说明 |
| --- | --- |
| UserId | 已识别用户 |
| UserName | 登录账号，失败时可脱敏 |
| Provider | Password / RefreshToken / WeChat，WeChat 属于 V1 |
| Result | Success / Failed |
| FailureReason | 错误原因枚举 |
| ClientId | OpenIddict ClientId |
| DeviceIdHash | 设备标识哈希 |
| IpAddress | 请求 IP |
| UserAgent | 客户端 User-Agent |
| CreationTime | 发生时间 |

不要记录：

- 密码。
- Access Token。
- Refresh Token。
- 微信 AppSecret。
- 微信 access_token。

## 9. 后端接口草案

### 9.1 当前用户与会话

```http
GET /api/mobile-auth/session
Authorization: Bearer {access_token}
```

响应：

```json
{
  "userId": "guid",
  "userName": "admin",
  "displayName": "Admin",
  "email": "admin@example.com",
  "roles": ["admin"],
  "permissions": ["FileCenter.Files.Default"],
  "storageUsed": 1048576,
  "storageQuota": 10737418240,
  "wechatBound": true
}
```

用途：

- App 登录后加载账号、容量和权限摘要。
- 设置页展示账号安全状态。
- `wechatBound` 仅在 V1 微信模块启用后返回；MVP Core 可省略。

### 9.2 修改密码

```http
POST /api/mobile-auth/change-password
Authorization: Bearer {access_token}
```

请求：

```json
{
  "currentPassword": "***",
  "newPassword": "***"
}
```

### 9.3 登出辅助接口

如果 OpenIddict revocation 不能满足审计需求，可增加应用层登出辅助接口：

```http
POST /api/mobile-auth/logout
Authorization: Bearer {access_token}
```

职责：

- 记录业务审计日志。
- 可选撤销当前设备 refresh token。
- 不替代 `/connect/revocation`，只补充业务上下文。

## 10. ABP 模块边界

移动端认证能力放入 `MobileAuth` 或认证扩展模块，不放入 `FileCenter`。

职责划分：

- Domain：移动端登录审计、设备会话摘要、V1 `WechatUserBinding`。
- Application.Contracts：`/api/mobile-auth/*` 所需 DTO、AppService 接口、错误码常量。
- Application：当前会话查询、密码修改、登出辅助、登录审计写入；V1 增加微信 code 换取身份、绑定票据、绑定/解绑流程。
- EntityFrameworkCore：移动登录审计和 V1 微信绑定实体映射、索引、迁移。
- HttpApi：`/api/mobile-auth/session`、`/api/mobile-auth/change-password`、`/api/mobile-auth/logout`，V1 增加微信绑定相关接口。
- OpenIddict 扩展：MVP Core 配置 `password` 与 `refresh_token`；V1 配置微信扩展 grant。

跨模块规则：

- `MobileAuth` 依赖 ABP Identity 和 OpenIddict，但不处理文件、媒体、回收站业务。
- `FileCenter` 依赖当前用户和权限，不直接管理 Token、微信绑定或登录审计。

## 11. 配置项汇总

```json
{
  "MobileAuth": {
    "AccessTokenLifetimeMinutes": 30,
    "RefreshTokenLifetimeDays": 30,
    "RefreshSkewMinutes": 2,
    "AllowPasswordGrantForMobileApp": true,
    "AllowAuthorizationCodeForMobileApp": false,
    "RequireHttpsOutsideDevelopment": true,
    "LoginRateLimit": {
      "MaxFailedAttempts": 5,
      "WindowMinutes": 15,
      "LockoutMinutes": 15
    }
  },
  "OpenIddict": {
    "Applications": {
      "PrivateCloudDrive_App": {
        "ClientId": "PrivateCloudDrive_App",
        "RedirectUri": "privateclouddrive://callback",
        "PostLogoutRedirectUri": "privateclouddrive://callback"
      }
    }
  }
}
```

Docker 环境变量建议：

```text
MobileAuth__AccessTokenLifetimeMinutes=30
MobileAuth__RefreshTokenLifetimeDays=30
MobileAuth__AllowPasswordGrantForMobileApp=true
MobileAuth__AllowAuthorizationCodeForMobileApp=false
OpenIddict__Applications__PrivateCloudDrive_App__RedirectUri=privateclouddrive://callback
OpenIddict__Applications__PrivateCloudDrive_App__PostLogoutRedirectUri=privateclouddrive://callback
```

`RedirectUri` 与 `PostLogoutRedirectUri` 仅在启用 `authorization_code` 兼容路径时必需；MVP Core 的账号密码主链路不依赖回调 Scheme。

## 12. 验收标准

- 管理员可以通过账号密码登录 MAUI App。
- 普通用户可以通过账号密码登录 MAUI App。
- Access Token 过期后 App 可以使用 Refresh Token 刷新。
- Refresh Token 失效后 App 清理 SecureStorage 并回到登录页。
- 退出登录清理 SecureStorage。
- 微信登录未配置或失败时，账号密码登录仍可使用，V1。
- 后端能查询登录成功、失败、退出审计日志；绑定、解绑审计日志属于 V1 微信模块。
- Token、密码、微信 AppSecret 不出现在日志中。

## 13. 参考资料

- .NET MAUI SecureStorage: https://learn.microsoft.com/dotnet/maui/platform-integration/storage/secure-storage
- .NET MAUI WebAuthenticator: https://learn.microsoft.com/dotnet/maui/platform-integration/communication/authentication
- ABP Framework: https://abp.io/framework
- OpenIddict: https://documentation.openiddict.com/
