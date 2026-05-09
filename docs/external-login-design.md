# Google/GitHub 登录接入说明

## 范围

本阶段接入 Google 与 GitHub 外部账号登录，能力边界与微信登录一致：外部账号只作为 ABP Identity 用户的登录凭证，不创建管理员，不替代账号密码登录，不影响 password/refresh_token 主链路。

已实现能力：

- 后端 `Authentication:External` 配置组。
- Google/GitHub 授权码换取外部身份。
- `ExternalUserBinding` 绑定表与 Host/Tenant 唯一索引。
- `/api/mobile-auth/external/settings` 公开配置查询，不返回 secret。
- `/api/mobile-auth/external/bind-current`、`bind-existing`、`bindings`、`bindings/{provider}`。
- OpenIddict 自定义 grant：`urn:privateclouddrive:external`。
- 登录、绑定、解绑审计与分布式限流。
- MAUI 登录页 Google/GitHub 入口。
- MAUI Settings 页 Google/GitHub 绑定和解绑入口。

## 配置

Docker Compose 使用 `.env` 中的变量：

```env
GOOGLE_LOGIN_ENABLED=false
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
GOOGLE_REDIRECT_URI=privateclouddrive://callback
GOOGLE_SCOPE=openid profile email
GOOGLE_USE_PKCE=true

GITHUB_LOGIN_ENABLED=false
GITHUB_CLIENT_ID=
GITHUB_CLIENT_SECRET=
GITHUB_REDIRECT_URI=privateclouddrive://callback
GITHUB_SCOPE=read:user user:email
GITHUB_USE_PKCE=true
```

`GOOGLE_CLIENT_SECRET` 对移动端 installed-app/PKCE 场景可为空；如果使用 Web client，则按 Google 控制台配置填写。`GITHUB_CLIENT_SECRET` 由后端换取 access token 时使用，不能写入 MAUI App。

## Provider 平台准备

Google:

- 在 Google Cloud Console 创建 OAuth client。
- 移动端建议使用 installed app/native app flow，并开启 PKCE。
- `GOOGLE_REDIRECT_URI` 必须与 Google OAuth client 中登记的 redirect URI 完全一致，否则会出现 `redirect_uri_mismatch`。

GitHub:

- 在 GitHub Developer settings 创建 OAuth App。
- `Authorization callback URL` 填写与 `GITHUB_REDIRECT_URI` 一致的回调地址。
- GitHub OAuth App callback URL 通常是单个配置，切换开发/生产地址时要同步修改 GitHub App 设置和 `.env`。
- `GITHUB_CLIENT_SECRET` 只放后端环境变量。

## 登录流程

1. MAUI 读取 `/api/mobile-auth/external/settings`。
2. 已启用 provider 的按钮可点击。
3. MAUI 用 WebAuthenticator 打开 Google/GitHub 授权页。
4. Provider 回调 `privateclouddrive://callback` 并返回 `code/state`。
5. MAUI 调用 OpenIddict token endpoint，grant 为 `urn:privateclouddrive:external`。
6. 后端用 `code` 向 provider 换取用户身份。
7. 若已绑定，签发标准 PrivateCloudDrive token。
8. 若未绑定，返回 `external_binding_required` 和 `binding_ticket`。
9. App 使用当前登录页输入的账号密码调用 `bind-existing` 完成绑定，再走账号密码登录进入文件页。

## 验收

最低验收清单：

- 未配置时，Google/GitHub 按钮禁用或显示未启用提示，账号密码登录仍可用。
- `/api/mobile-auth/external/settings` 不包含 `ClientSecret`、access token、refresh token。
- 未绑定外部账号首次登录返回 `external_binding_required` 和 binding ticket。
- 输入正确账号密码后可绑定已有 PrivateCloudDrive 用户。
- 已绑定后再次 Google/GitHub 登录可进入文件页。
- 已登录用户可在 Settings 绑定和解绑 Google/GitHub。
- 已被其他用户绑定的外部身份不能迁移到当前用户。
- 解绑必须保留账号密码登录能力。
- 授权取消或 provider 失败不清理已有账号密码登录 token。
- 审计日志不记录授权码、provider access token、provider refresh token、client secret、业务 access token 或 refresh token。

## 参考

- [Google OAuth 2.0 for iOS & Desktop Apps](https://developers.google.com/identity/protocols/oauth2/native-app)
- [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect)
- [GitHub Authorizing OAuth Apps](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
- [GitHub Creating an OAuth App](https://docs.github.com/en/developers/apps/creating-an-oauth-app)
