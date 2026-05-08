# 微信登录接入设计，V1 可选能力

本文档定义 PrivateCloudDrive MAUI App 接入移动应用微信登录的技术方案。微信登录属于 V1 可选能力，不属于 MVP Core，不能替代本地账号密码登录，不能影响管理员账号密码登录。

## 1. 设计目标

- 通过微信开放平台接入移动应用微信登录。
- MAUI App 只负责拉起微信授权并拿到授权 `code`。
- 后端使用 `code`、`AppId`、`AppSecret` 向微信服务端换取微信用户身份。
- 后端把微信身份绑定到 ABP Identity 用户。
- 支持首次微信登录绑定账号。
- 支持已登录账号绑定和解绑微信。
- 微信配置全部通过配置项和环境变量管理，不写死在代码中。
- 微信失败、未安装、未配置或审核未通过时，本地账号密码登录仍然正常。
- 微信开放平台审核、AppId、真机 SDK 配置不能作为 MVP Core 的完成阻塞项。

## 2. 前置条件

需要在微信开放平台完成：

- 注册开发者账号。
- 创建并审核通过移动应用。
- 获得移动应用 `AppId` 和 `AppSecret`。
- 申请并审核通过微信登录能力。
- 配置 Android 包名和应用签名。
- 配置 iOS Bundle Identifier 和 URL Scheme。
- 确认 App 端使用的回调 Scheme 与平台配置一致。

注意：

- `AppSecret` 只能保存在后端配置或密钥系统中，不能进入 MAUI App。
- Android 和 iOS 的微信 SDK 接入细节不同，需要分别封装平台实现。
- iOS 上未安装微信时应隐藏微信登录按钮，避免审核风险；Android 可显示按钮并引导安装。

## 3. 总体架构

```text
MAUI App
  -> 微信 SDK 拉起授权
  -> 获取 code/state
  -> 调用后端 OpenIddict 微信扩展登录

ABP API Host
  -> 使用 AppId/AppSecret 调用微信接口
  -> 解析 openid/unionid
  -> 查询或创建绑定关系
  -> 通过 OpenIddict 签发本系统 Token

PostgreSQL
  -> ABP Identity 用户
  -> 微信绑定关系
  -> 登录审计日志
```

关键原则：

- Token 签发仍由 OpenIddict 负责。
- 微信身份只作为外部登录凭证，不替代 ABP 用户。
- `unionid` 优先作为跨应用唯一标识；没有 `unionid` 时使用 `appid + openid`。
- 不长期保存微信 access_token 和 refresh_token，除非后续有明确微信 API 调用需求。

ABP 模块边界：

- `WechatUserBinding`、微信绑定票据、微信扩展 grant、微信登录审计均属于 `MobileAuth` 或认证扩展模块。
- `FileCenter` 不保存微信身份、不处理微信登录、不依赖微信 SDK。
- 微信登录成功后只产生标准 PrivateCloudDrive 用户会话；文件、媒体、回收站权限仍由 ABP Identity 和权限系统决定。

## 4. 微信授权流程

### 4.1 App 获取 code

MAUI App 调用平台微信 SDK：

```text
scope = snsapi_userinfo
state = cryptographic-random-string
```

微信返回：

| 字段 | 说明 |
| --- | --- |
| errCode | 0 表示用户同意 |
| code | 授权临时票据，只能短期使用 |
| state | App 发起请求时传入的随机值 |
| lang | 微信客户端语言 |
| country | 微信用户国家信息 |

App 处理规则：

- `errCode = 0` 且 state 匹配时，才把 code 发给后端。
- 用户取消时显示轻提示，不算登录失败。
- 用户拒绝授权时提示“已取消微信授权”。
- state 不匹配时直接丢弃结果并记录本地诊断日志。

### 4.2 后端 code 换取身份

后端调用微信接口：

```http
GET https://api.weixin.qq.com/sns/oauth2/access_token
  ?appid={AppId}
  &secret={AppSecret}
  &code={Code}
  &grant_type=authorization_code
```

微信成功响应包含：

```json
{
  "access_token": "...",
  "expires_in": 7200,
  "refresh_token": "...",
  "openid": "...",
  "scope": "snsapi_userinfo",
  "unionid": "..."
}
```

如果需要昵称和头像，后端继续调用：

```http
GET https://api.weixin.qq.com/sns/userinfo
  ?access_token={WeChatAccessToken}
  &openid={OpenId}
```

后端处理规则：

- `AppSecret` 从配置读取。
- 微信返回 `errcode` 时转为系统错误码，不把原始敏感数据返回给 App。
- 后端只保存 `openid`、`unionid`、昵称、头像 URL、绑定时间、最近登录时间。
- 微信 access_token 只用于本次身份确认，不保存到数据库。

## 5. Token 签发方案

推荐实现 OpenIddict 自定义扩展 Grant：

```text
grant_type=urn:privateclouddrive:wechat
```

请求草案：

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=urn:privateclouddrive:wechat&
client_id=PrivateCloudDrive_App&
code=WECHAT_AUTH_CODE&
state=STATE&
platform=android&
device_id=DEVICE_ID&
scope=openid profile email roles offline_access PrivateCloudDrive
```

后端处理：

1. 校验 `PrivateCloudDrive_App` 是否允许微信扩展 grant。
2. 使用 code 向微信换取 openid/unionid。
3. 查询微信绑定关系。
4. 若已绑定且用户可登录，创建 ABP 用户 ClaimsPrincipal。
5. 由 OpenIddict 返回 access token 和 refresh token。
6. 记录微信登录审计日志。

未绑定时：

- 不签发系统 Token。
- 创建短期 `bindingTicket`，保存到 Redis/Distributed Cache。
- 返回明确错误：`wechat_binding_required`。
- App 跳转首次绑定流程。

错误响应草案：

```json
{
  "error": "wechat_binding_required",
  "error_description": "WeChat account is not bound to a PrivateCloudDrive user.",
  "binding_ticket": "short-lived-ticket"
}
```

## 6. 绑定关系设计

### 6.1 实体建议

实体名：`WechatUserBinding`

所属模块：`MobileAuth` 或认证扩展模块。该实体不放入 `FileCenter`。

字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | Guid | 主键 |
| TenantId | Guid? | 多租户预留 |
| UserId | Guid | ABP Identity 用户 |
| AppId | string | 微信开放平台 AppId |
| OpenId | string | 当前应用下微信用户标识 |
| UnionId | string? | 开放平台统一标识 |
| NickName | string? | 微信昵称，显示用 |
| AvatarUrl | string? | 头像 URL，显示用 |
| IsEnabled | bool | 是否启用 |
| LastLoginTime | DateTime? | 最近微信登录时间 |
| CreationTime | DateTime | 绑定时间 |

索引：

- `UX_WechatUserBindings_AppId_OpenId`
- `UX_WechatUserBindings_UnionId`，当 `UnionId` 不为空时唯一。
- `IX_WechatUserBindings_UserId`

ABP Identity 兼容：

- 可以同步写入 ABP Identity 外部登录信息，Provider 使用 `WeChat`。
- ProviderKey 优先使用 `unionid`，无 unionid 时使用 `{appId}:{openid}`。

### 6.2 绑定票据

`bindingTicket` 存在 Redis/Distributed Cache。

内容：

```json
{
  "appId": "wx...",
  "openId": "...",
  "unionId": "...",
  "nickName": "...",
  "avatarUrl": "...",
  "createdAt": "2026-05-07T14:00:00Z",
  "expiresAt": "2026-05-07T14:05:00Z"
}
```

规则：

- 有效期默认 5 分钟。
- 只能使用一次。
- 绑定成功后立即删除。
- 不包含微信 access_token。

## 7. 首次微信登录绑定流程

### 7.1 绑定已有账号

流程：

1. 用户点击微信登录。
2. 微信授权成功，App 获得 code。
3. App 调用微信扩展 grant。
4. 后端发现未绑定，返回 `bindingTicket`。
5. App 显示绑定页，要求输入 PrivateCloudDrive 账号和密码。
6. 后端校验账号密码。
7. 后端创建微信绑定关系。
8. 后端按 OpenIddict 规则返回系统 Token，或返回可换 Token 的一次性票据。
9. App 保存 Token 并进入 Files。

接口草案：

```http
POST /api/mobile-auth/wechat/bind-existing
Content-Type: application/json

{
  "bindingTicket": "...",
  "userNameOrEmail": "admin",
  "password": "***"
}
```

规则：

- 账号密码错误不删除 bindingTicket，但失败次数受限。
- 管理员绑定微信必须输入管理员密码。
- 被禁用用户不能绑定。
- 已被其他用户绑定的微信身份不能重复绑定。

### 7.2 注册并绑定新账号

如果系统开启注册，可支持首次微信登录创建普通用户：

```http
POST /api/mobile-auth/wechat/register-and-bind
Content-Type: application/json

{
  "bindingTicket": "...",
  "userName": "new-user",
  "email": "user@example.com",
  "password": "***"
}
```

规则：

- 默认角色为 User。
- 不能通过微信注册创建管理员。
- 如果系统关闭注册，App 不显示注册入口，只允许绑定已有账号。

## 8. 已有账号绑定与解绑

### 8.1 已登录账号绑定微信

入口：设置 -> 账号安全 -> 微信绑定。

流程：

1. 用户已通过账号密码或已有 Token 登录。
2. 点击“绑定微信”。
3. App 拉起微信授权，获得 code。
4. App 调用绑定当前账号接口。
5. 后端换取微信身份并检查唯一性。
6. 创建绑定关系。
7. 记录审计日志。

接口草案：

```http
POST /api/mobile-auth/wechat/bind-current
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "code": "WECHAT_AUTH_CODE",
  "state": "STATE",
  "platform": "ios"
}
```

### 8.2 解绑微信

入口：设置 -> 账号安全 -> 微信绑定 -> 解绑。

接口草案：

```http
DELETE /api/mobile-auth/wechat/binding
Authorization: Bearer {access_token}
```

规则：

- 解绑需要二次确认。
- 如果账号没有密码或没有其他登录方式，禁止解绑。
- 管理员解绑微信不影响账号密码登录。
- 解绑后立即使微信登录失效，但不强制退出当前账号密码会话。

## 9. 配置项

配置示例：

```json
{
  "Authentication": {
    "WeChat": {
      "Enabled": false,
      "AppId": "",
      "AppSecret": "",
      "Scope": "snsapi_userinfo",
      "CallbackScheme": "privateclouddrive",
      "Android": {
        "PackageName": "com.companyname.privateclouddrive.app",
        "Signature": ""
      },
      "iOS": {
        "BundleId": "com.companyname.privateclouddrive.app",
        "UrlScheme": "wx-your-app-id"
      },
      "BindingTicketLifetimeMinutes": 5,
      "RequestTimeoutSeconds": 10,
      "RateLimitWindowSeconds": 300,
      "RateLimitMaxAttempts": 60
    }
  }
}
```

Docker 环境变量建议：

```text
Authentication__WeChat__Enabled=true
Authentication__WeChat__AppId=wx-your-app-id
Authentication__WeChat__AppSecret=change-this-secret
Authentication__WeChat__Scope=snsapi_userinfo
Authentication__WeChat__CallbackScheme=privateclouddrive
Authentication__WeChat__Android__PackageName=com.companyname.privateclouddrive.app
Authentication__WeChat__Android__Signature=your-android-signature
Authentication__WeChat__iOS__BundleId=com.companyname.privateclouddrive.app
Authentication__WeChat__iOS__UrlScheme=wx-your-app-id
Authentication__WeChat__BindingTicketLifetimeMinutes=5
Authentication__WeChat__RequestTimeoutSeconds=10
Authentication__WeChat__RateLimitWindowSeconds=300
Authentication__WeChat__RateLimitMaxAttempts=60
```

MAUI 配置建议：

```text
Wechat__Enabled=true
Wechat__AppId=wx-your-app-id
Wechat__CallbackScheme=privateclouddrive
```

MAUI App 只能包含 `AppId`、Scheme 和平台公开配置，不能包含 `AppSecret`。

## 10. App UI 行为

登录页：

- 微信已启用且平台支持时显示“使用微信登录”。
- 未安装微信时，iOS 隐藏按钮，Android 可显示并提示安装。
- 微信登录失败只影响微信流程，不清空账号密码输入框。

首次绑定页：

- 显示“绑定 PrivateCloudDrive 账号”。
- 输入账号和密码。
- 如果允许注册，显示“创建新账号并绑定”入口。
- 显示微信昵称和头像时必须来自本次授权结果，不能当作系统用户资料的唯一来源。

设置页：

- 显示微信绑定状态。
- 已绑定时显示昵称、绑定时间和解绑入口。
- 未绑定时显示绑定入口。

## 11. 安全策略

- `AppSecret` 只存在后端配置、环境变量或密钥系统。
- 后端调用微信接口必须设置超时。
- 微信 code 只能使用一次。
- `bindingTicket` 有效期短，且绑定成功后删除。
- 绑定、解绑、微信登录都要基于后端分布式缓存限流，超限时返回 `wechat_rate_limited`，不能影响账号密码登录。
- 解绑必须确认账号仍有密码登录能力。
- 微信登录不能创建管理员账号。
- 微信服务不可用时不影响本地账号密码登录。
- 所有失败都记录审计日志，但不记录 code、AppSecret、微信 access_token。
- 如果 `unionid` 缺失，使用 `{appId}:{openid}` 作为 ProviderKey，避免不同开放平台应用的 openid 冲突。

## 12. 错误码草案

| 错误码 | 场景 | App 行为 |
| --- | --- | --- |
| `wechat_disabled` | 后端未启用微信登录 | 隐藏或禁用微信按钮 |
| `wechat_client_not_installed` | 本机未安装微信 | 提示安装或隐藏按钮 |
| `wechat_user_cancelled` | 用户取消授权 | 轻提示，停留登录页 |
| `wechat_auth_denied` | 用户拒绝授权 | 轻提示，停留登录页 |
| `wechat_invalid_state` | state 不匹配 | 中断流程并提示重试 |
| `wechat_code_exchange_failed` | 后端换取微信身份失败 | 提示微信登录失败 |
| `wechat_binding_required` | 微信未绑定系统用户 | 进入绑定页 |
| `wechat_already_bound` | 微信已绑定其他账号 | 提示联系管理员或换账号 |
| `wechat_unbind_not_allowed` | 解绑后无可用登录方式 | 禁止解绑 |
| `wechat_rate_limited` | 微信登录、绑定或解绑请求过于频繁 | 提示稍后重试 |

## 13. 后续实现边界

- 不复制 Cloudreve 或其他项目的微信登录代码。
- 不把微信 access_token 当作 PrivateCloudDrive API Token。
- 不在 MAUI App 中请求 AppSecret。
- 不绕过 ABP Identity 直接创建非 ABP 用户。
- 不绕过 OpenIddict 手写 JWT。
- 微信用户头像和昵称仅作为外部登录展示信息，不作为账号唯一标识。

## 14. 验收标准

- 以下验收标准只适用于 V1 微信登录任务，不作为 MVP Core 阻塞项。
- 未配置微信时，账号密码登录正常，微信按钮隐藏或禁用。
- 配置微信后，App 能拉起微信授权并拿到 code。
- 后端能使用 code 换取 openid/unionid。
- 未绑定微信首次登录时进入绑定账号流程，不直接创建管理员。
- 绑定已有账号后可通过微信登录进入系统。
- 已登录用户可以绑定微信。
- 已登录用户可以解绑微信，前提是仍有账号密码登录能力。
- 微信登录失败不清理本地账号密码登录 Token。
- 登录、绑定、解绑均有审计日志。

## 15. 参考资料

- 微信开放平台移动应用微信登录开发指南: https://developers.weixin.qq.com/doc/oplatform/Mobile_App/WeChat_Login/Development_Guide.html
- 微信开放平台授权后接口调用 UnionID: https://developers.weixin.qq.com/doc/oplatform/Mobile_App/WeChat_Login/Authorized_API_call_UnionID.html
- 微信开放平台: https://open.weixin.qq.com/
