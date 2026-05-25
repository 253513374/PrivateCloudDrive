# Android App 可见验收计划（2026-05-22）

## 目标

确认 Android App 在本地验收栈中可以完成从登录到文件中心核心能力的可见路径验证，并沉淀可供后端、运维、QA 复核的脱敏证据。

本计划只记录验收步骤、页面/API 证据和脱敏规则；不得写入密码、访问 token、刷新 token、Cookie、AppSecret、连接字符串、微信 openid/unionid、完整公开分享 URL 或真实私密文件内容。

## 验收环境约定

- 后端本地栈：优先使用本机开发 API；模拟器默认通过 `http://10.0.2.2:8080` 访问宿主机。
- Android 包：Debug APK 需包含程序集，手动安装时使用 `EmbedAssembliesIntoApk=true` 与 `AndroidFastDeploymentType=None`。
- 账号：QA 使用具备 admin 角色或等价 FileCenter 全量权限的测试账号；密码仅通过安全渠道提供，不写入文档、评论或日志。
- 证据：截图、logcat 摘要、HTTP 状态码、审计时间点可以记录；敏感标识需遮罩或只保留前后少量字符。

## 可见验收路径

| 编号 | 页面/能力 | App 可见动作 | 后端/API 依赖 | 通过标准 | 证据 |
| --- | --- | --- | --- | --- | --- |
| A1 | 登录 | 打开 App，使用测试账号登录 | OpenIddict discovery/token，移动端认证审计 | 登录成功并进入首页；失败场景有明确错误提示 | 登录页/首页截图，脱敏 logcat |
| A2 | 文件列表 | 进入文件页，查看根目录/子目录 | FileCenter nodes/folders 查询 | 列表加载成功，空目录/异常状态有可见反馈 | 文件页截图 |
| A3 | 上传 | 上传小文件或创建分片上传会话 | upload-small 或 upload-sessions/chunks/complete | 上传进度与完成状态可见；失败可重试/提示 | 上传页截图，脱敏后文件名 |
| A4 | 下载/预览 | 下载或预览测试文件 | files/{id}/download、files/{id}/content，支持 Range | 下载成功；Range/预览不返回 5xx | 下载/预览截图，HTTP 状态摘要 |
| A5 | 分享 | 为测试文件创建分享并打开分享入口 | file-center/shares、public/shares | 分享创建成功；公开入口可读元数据，密码验证/限流生效 | 分享页截图；不记录完整分享 URL |
| A6 | 回收站 | 删除测试文件、进入回收站、恢复/永久删除 | nodes delete/restore/permanent，trash | 删除后出现在回收站；恢复后回到列表 | 回收站前后截图 |
| A7 | 系统健康 | 打开/调用存储健康摘要 | system-health/summary，storage/usage | 授权后返回脱敏摘要；未授权返回 401 | 状态码与脱敏摘要 |
| A8 | 移动认证审计 | 登录/失败/外部绑定等动作后查询审计 | mobile-auth/audit-logs、operation-logs | 匿名写入审计成功；管理员可按时间点查询 | 审计查询时间窗口与事件类型 |

## 脱敏与记录规则

1. 公开分享 URL 只记录路由形态或 token 已遮罩版本，例如 `api/public/shares/{token}`。
2. Header、Cookie、Authorization、Set-Cookie 不进入验收文档。
3. 测试文件使用无隐私样例文件，截图中如出现用户名、邮箱、手机号、对象 Key 需遮罩。
4. 审计证据记录时间窗口、事件类型、状态，不记录原始 token 或完整用户外部标识。

## 交付物

- 后端准备记录：`docs/validation/android-backend-acceptance-readiness-2026-05-22.md`。
- QA 可见验收证据：截图、logcat 摘要、脱敏 HTTP 状态记录。
- 如发现 5xx、权限越权或敏感信息泄露，必须先回退为阻塞问题，不进入通过结论。
