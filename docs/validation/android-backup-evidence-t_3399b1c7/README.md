# Android 备份主链路可见验收证据（t_3399b1c7）

## 范围

- 构建：MAUI Android Debug APK，`EmbedAssembliesIntoApk=true`，`AndroidFastDeploymentType=None`。
- 安装：Pixel 9 Pro API 36 模拟器 `emulator-5554`，先 `pm clear com.companyname.privateclouddrive.app` 清理 App 数据，再启动。
- 本次代码增量：备份中心补齐“查看备份队列与失败重试”、“容量/健康与恢复说明”、“恢复边界说明”入口；存储用量页新增恢复边界说明入口。

## 证据文件

- `01-clean-launch.png`：清理数据后的首次启动截图，停留在登录页。
- `01-clean-launch-window.xml`：同一页面的 UIAutomator 层级，包含默认后端 `http://10.0.2.2:8080`、账号登录入口和登录表单。
- `logcat-clean-launch.txt`：启动后 logcat 片段，未发现 App 侧 `FATAL EXCEPTION`。

## 已验证

- `dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None`：0 errors。
- APK 安装成功；Incremental Install 先返回 `INSTALL_FAILED_INVALID_APK` 后自动降级 Streamed Install 并 `Success`。
- `pm clear` 成功；`monkey -p com.companyname.privateclouddrive.app ...` 启动成功。
- 干净启动未崩溃，登录页可见。

## 未完成/阻塞

- 当前本机 `127.0.0.1:8080` 未运行后端，Android 默认 `10.0.2.2:8080` 无法登录；因此尚未完成登录后的备份中心、队列重试、容量/健康页截图验收。
- 本次 UI 文案只展示通用存储/恢复边界，不包含真实 bucket、绝对路径、连接串、AccessKey 或 Token。
