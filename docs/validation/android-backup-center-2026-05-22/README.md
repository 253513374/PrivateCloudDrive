# Android 备份中心登录后截图验收

任务：`t_1a159e4f`
日期：2026-05-22
设备：Android Emulator `emulator-5554`
后端：`http://10.0.2.2:8080`（宿主机 `127.0.0.1:8080`）

## 结论

PASS。已在 Android 模拟器清理 App 数据后重新安装 Debug APK，使用本机 8080 后端完成账号登录，并采集登录后备份队列、容量/存储用量、系统健康相关截图。后端授权 API 均返回 200，logcat 未发现 `FATAL EXCEPTION`。

## 截图证据

| 文件 | 说明 |
|---|---|
| `01-login.png` | 清理数据后启动 App，登录页显示默认后端 `http://10.0.2.2:8080`。 |
| `06-after-login-correct-focus.png` | 登录成功后进入文件页，显示当前账号工作台/文件列表与备份入口。 |
| `07-backup-center-queue.png` | 备份队列页，显示“暂无备份任务”的空队列状态和“去文件页备份”入口。 |
| `08-my-capacity-health.png` | “我的”页顶部，显示在线状态、容量摘要、存储空间卡片。 |
| `09-storage-usage-health.png` | 存储用量页，显示 0 B / 10 GB、服务器可信状态、后端存储 FileSystem 正常、健康状态正常。 |
| `10-system-health.png` | “我的”页私有备份服务区域，显示系统健康运行正常，API/DB/Redis/存储/FFmpeg/FFprobe 正常。 |

## API 与运行证据

详见 `api-and-log-evidence.txt`：

- Swagger：HTTP 200。
- `/api/file-center/system-health/summary`：登录后 HTTP 200。
- `/api/file-center/storage/usage`：登录后 HTTP 200。
- `/api/mobile-auth/wechat/settings`：HTTP 200。
- `/api/mobile-auth/external/settings`：HTTP 200。
- Android logcat：`FATAL EXCEPTION` 计数为 0。

## 构建与安装

构建命令：

```bash
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None
```

结果：0 errors，25 warnings。警告为 Xamarin.AndroidX 依赖版本约束告警，未阻断 APK 构建与模拟器验收。

APK：`maui/PrivateCloudDrive.App/bin/Debug/net10.0-android/com.companyname.privateclouddrive.app-Signed.apk`

## 兼容性与风险说明

- 本次为 Android 模拟器验收，未覆盖真机相册/文件权限弹窗、真实蜂窝/弱网、后台上传保活与系统杀进程场景。
- 截图、日志和交接材料未写入密码、Token、连接串或密钥；API 证据仅保留 HTTP 状态码和响应字节数。
- 存储页会展示服务器文件系统/容量状态，属于运维可见信息；对外共享截图前建议按发布范围再做一次脱敏审查。
