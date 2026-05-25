# Android 登录错误分类改进验证记录

任务：t_6b53cfe3
分支：agent/t_8849deee/android-login-error-classification
时间：2026-05-22 20:32:13 +0800

## 改动范围

- `maui/PrivateCloudDrive.App/Services/MobileAuthException.cs`
  - 新增 `MobileAuthErrorKind`：`ServiceUnavailable`、`NetworkError`、`InvalidCredentials`、`ServerError`、`Unknown`。
  - 新增 `MobileAuthException`，用于在认证服务和登录 UI 之间传递分类，不要求 UI 读取底层异常文本。
- `maui/PrivateCloudDrive.App/Services/OpenIddictAuthService.cs`
  - `connect/token` 请求现在区分：
    - 连接拒绝、Host/Network unreachable、超时：服务不可达或网络错误。
    - OAuth `invalid_grant`：凭据错误。
    - HTTP 5xx：服务器错误。
    - token 响应结构异常：服务器错误。
  - 保留审计记录，但 UI 不再依赖 `exception.Message` 展示。
- `maui/PrivateCloudDrive.App/Views/LoginPage.xaml.cs`
  - 登录、微信绑定登录、Google/GitHub 第三方登录绑定均通过 `GetUserFacingSignInError` 转为固定文案。
  - 兜底失败不展示 raw exception。
- `maui/PrivateCloudDrive.App/Localization/AppText.cs`
  - 新增固定安全文案：服务器暂不可达、网络连接异常、服务器处理失败、通用登录失败。

## 用户可见文案矩阵

| 场景 | UI 文案 | 泄露风险控制 |
| --- | --- | --- |
| 后端未启动 / 连接拒绝 / Host 不可达 | 服务器暂不可达，请确认后端已启动并稍后重试。 | 不显示 host、端口、URL、Socket 原文 |
| 请求超时 / 一般网络异常 | 网络连接异常，请检查网络后重试。 | 不显示底层 HTTP 异常或网络栈文本 |
| 用户名密码错误 / OAuth invalid_grant | 用户名或密码错误，请检查大小写后重试。 | 不显示 OAuth error_description 原文 |
| HTTP 5xx / token 响应异常 | 服务器处理登录请求失败，请稍后重试。 | 不显示响应体、异常堆栈、服务内部路径 |
| 未分类异常 | 登录失败，请稍后重试。 | 不显示 raw exception |

## 验证结果

1. `git diff --check`
   - 结果：通过。
2. Windows MAUI 编译验证
   - 命令：`dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-windows10.0.19041.0 -c Debug`
   - 结果：通过，0 errors；存在既有 AndroidX 版本约束 warning。
3. Android MAUI 构建尝试
   - 命令：`dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None`
   - 结果：未通过，失败于 AAPT 读取生成资源 `microsoft_maui_essentials_fileprovider_file_paths.xml.flat`；源 XML 文件存在，当前隔离 workspace 路径较长，疑似 Windows/AAPT 路径或文件访问问题。此失败发生在 Android 资源编译阶段，Windows 目标已证明本次 C# 改动可编译。
4. 敏感信息静态检查
   - 检查登录页已无 `ShowValidation(exception.Message)`。
   - 变更 diff 中未新增 cookie、Bearer 值、私有 URL、磁盘路径、bucket/object key 展示文案。
   - 代码中出现的 `token` 仅为既有 OpenIddict/token 响应变量与接口路径，不是泄露值。

## 兼容性说明

- Android/H5/桌面共享 MAUI 登录页逻辑；本次分类在服务层和页面层完成，不依赖平台专有 API。
- Android 端会继续使用相同固定文案，避免弱网、后端未启动、凭据错误被混为 raw exception。
- 微信、Google、GitHub 绑定账号登录失败也复用同一安全分类入口，避免第三方登录回调错误直接显示给用户。

## 剩余风险

- Android 构建在当前长路径 workspace 下触发 AAPT 资源文件读取错误，需要在更短路径或 CI 环境再次验证 APK 产物。
- `CloudDriveApiClient` 的非登录文件 API 仍有部分业务页面展示后端业务错误文本；本任务收敛登录链路，后续如需全面 API 错误脱敏，应由移动端/安全协作单独覆盖。
