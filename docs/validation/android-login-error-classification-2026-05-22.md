# Android 登录错误分类改进验证记录（2026-05-22）

## 变更范围

- `maui/PrivateCloudDrive.App/Services/OpenIddictAuthService.cs`
  - `connect/token` 请求增加网络/超时/服务不可达的安全分类。
  - OAuth 错误响应不再把 raw response body 或 `error_description` 直接抛给 UI。
  - 5xx、401/403、400/invalid_grant、绑定要求分别转换为可分类的安全消息。
- `maui/PrivateCloudDrive.App/Views/LoginPage.xaml.cs`
  - 密码、微信、Google/GitHub 登录统一走 `GetUserFacingSignInError`。
  - UI 文案区分：服务不可达、网络错误、凭据错误、服务器错误、兜底失败。
  - 取消微信授权使用固定取消文案，不展示平台 raw error。
- `maui/PrivateCloudDrive.App/Localization/AppText.cs`
  - 增加中英文安全登录错误文案。

## 安全与隐私验收

UI 只展示固定安全文案，不展示以下内容：

- raw exception / stack trace
- private URL / 服务器完整地址
- token / refresh token / cookie
- 本地文件路径
- bucket / object key
- OAuth 原始响应体或 `error_description`

## 分类期望

| 场景 | 用户可见文案 |
| --- | --- |
| 服务不可达、DNS、连接拒绝 | 无法连接服务器，请检查服务器地址或稍后重试。 |
| 网络超时、TLS/SSL、弱网中断 | 网络连接异常，请检查网络后重试。 |
| 用户名密码错误、invalid_grant、401/403 | 用户名或密码错误，请检查大小写后重试。 |
| 服务器 5xx / 网关错误 | 服务器暂时无法完成登录，请稍后重试。 |
| 未覆盖异常 | 登录失败，请稍后重试或联系管理员。 |

## 静态验证

- `git diff --check`：通过，无空白错误。
- Android MAUI 构建：通过。
  - 命令：`dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None`
  - 结果：0 errors，25 warnings。
  - 警告均为既有依赖版本约束/AndroidFastDeploymentType 弃用警告，本次登录错误分类改动未新增编译错误。

## 兼容性说明

- Android：本次目标平台，登录页用户可见错误已收敛为安全固定文案。
- Windows / iOS / MacCatalyst：共用 MAUI 登录页和认证服务分类逻辑；浏览器/第三方登录异常同样不会直接展示 raw exception。
- 绑定流程：后端返回 `wechat_binding_required` / `external_binding_required` 时仍保留绑定票据内部流转，但 UI 不展示绑定票据。
