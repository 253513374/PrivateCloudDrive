# Android 备份主链路可见验收证据（t_3399b1c7）

> 状态更新（2026-05-26）：本目录最初只证明 Android clean launch/登录页可见，曾保留“登录后备份中心、队列重试、容量/健康页截图未完成”的历史阻塞说明。该阻塞已由后续 slice1～slice5 截图和最终报告收口。
>
> 当前权威结论请阅读：`../android-final-visible-acceptance-2026-05-26.md`。

## 原始范围

- 构建：MAUI Android Debug APK，`EmbedAssembliesIntoApk=true`，`AndroidFastDeploymentType=None`。
- 安装：Pixel 9 Pro API 36 模拟器 `emulator-5554`，先 `pm clear com.companyname.privateclouddrive.app` 清理 App 数据，再启动。
- 本次代码增量：备份中心补齐“查看备份队列与失败重试”、“容量/健康与恢复说明”、“恢复边界说明”入口；存储用量页新增恢复边界说明入口。

## 本目录证据文件

| 文件 | 说明 | 当前判定 |
| --- | --- | --- |
| `01-clean-launch.png` | 清理数据后的首次启动截图，停留在登录页。 | PASS |
| `01-clean-launch-window.xml` | 同一页面的 UIAutomator 层级，包含默认后端、账号登录入口和登录表单。 | PASS |
| `logcat-clean-launch.txt` | 启动后 logcat 裁剪摘要，未发现 App 侧 `FATAL EXCEPTION`。 | PASS |

## 已验证

- `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None`：0 errors。
- APK 安装成功；Incremental Install 先返回 `INSTALL_FAILED_INVALID_APK` 后自动降级 Streamed Install 并 `Success`。
- `pm clear` 成功；`monkey -p com.companyname.privateclouddrive.app ...` 启动成功。
- 干净启动未崩溃，登录页可见。

## 后续证据收口

后续可见验收已补齐并统一引用：

- 登录后文件中心：`../screenshots/private-backup-slice2-files-after-login.png`
- 备份队列与上传完成：`../screenshots/private-backup-slice4-upload-result.png`
- 失败保留与重试成功：`../screenshots/private-backup-slice5-server-down-failed-queue.png`、`../screenshots/private-backup-slice5-retry-success.png`
- 容量/健康：`../screenshots/private-backup-slice3-storage-usage.png`、`../screenshots/private-backup-slice3-settings-health.png`
- 最终 PASS/WARN/FAIL 报告：`../android-final-visible-acceptance-2026-05-26.md`

## 脱敏结论

本目录和后续报告只展示默认本地后端配置、测试文件名、容量摘要与用户级状态文案；不包含密码、访问令牌、刷新令牌、Cookie、连接串、完整公开分享 URL、真实 bucket/object key 或真实私密文件内容。
