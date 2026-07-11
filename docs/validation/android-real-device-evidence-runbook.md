# Android 真机验收证据轮次运行手册

> 适用：mobile-eng（主负责）、qa-eng（复核）、devops-eng（后端可用性保障）
> 目标：完成 Issue #1 闭孔前的 8 项 Android 真机验收，形成可复用的验证证据包
> 前置条件：Local backend 可用（devops-eng）、QA 测试账号可用
> **环境限制**：当前跑环境 ADB 无已连接设备/模拟器（ANDROID_HOME 未设置，无 AVD），
>   项目 1-7 真机触控验收均无法执行。Android APK 构建已验证通过（PASS 8/8），
>   项目 8 Release Gate 已 PASS（secret scan 0 findings）。
>   真机验收需有人工操作者按本手册步骤在可用 Android 设备上补填。

## 总则

1. **敏感信息红线**：截图和 logcat 中不得包含完整 Token、私有 IP（脱敏为 `[REDACTED_IP]`）、Cookie、密码、服务器绝对路径、`.env` 内容或真实用户文件内容。
2. **证据格式**：每项验收生成截图（`.png`）+ 说明书（markdown）+ 裁剪 logcat（如涉及）。截图放在 `docs/validation/screenshots/real-device/` 子目录。
3. **WARN 记录规则**：如某项不能达成 PASS（例如 OEM 省电导致后台中断），记录 WARN + 原因，不阻断整体验收。
4. **文件大小参考**：大视频指 >50MB 文件（触发分片上传逻辑，阈值 32MB）。

---

## 项目 1：Clean Install + 登录 + 连接状态

### 验证目标
在真实 Android 设备上 clean install，配置局域网可到达后端地址，完成登录，截图证明连接状态。

### 前置条件
- [ ] Android 真机（建议 Android 11+）
- [ ] APK 已构建（`dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None`）
- [ ] 后端在局域网同一网段运行
- [ ] 设备已开启 USB 调试

### 操作步骤

| 步骤 | 操作 | 预期结果 | 截图要点 |
| --- | --- | --- | --- |
| 1.1 | `adb uninstall com.companyname.privateclouddrive.app || true`；必要时在系统设置中清除 App 数据 | 确认已卸载；`-r` 安装时不保留旧状态 | — |
| 1.2 | `adb install -r maui/PrivateCloudDrive.App/bin/Debug/net10.0-android/com.companyname.privateclouddrive.app-Signed.apk` | 安装成功，exit code 0 | — |
| 1.3 | 启动 App | 可见启动页/首屏（StartupPage） | **截图 1.3**：启动页 |
| 1.4 | 进入登录页，配置服务器地址（局域网 IP:8080） | 地址输入框可编辑 | **截图 1.4**：服务器配置页，IP 脱敏为 `[REDACTED_IP]:8080` |
| 1.5 | 使用 QA 测试账号登录（用户名/密码） | 登录成功，跳转到文件（FilesPage）Tab | **截图 1.5**：登录成功后文件页 |
| 1.6 | 检查文件页右上角连接状态 | 页面正常加载，无连接错误提示 | 同 1.5 截图，确认无错误 banner |
| 1.7 | 切换到"我的"（SettingsPage）Tab | 显示"在线"标签 | **截图 1.7**：显示"在线"和容量状态 |

### 截图清单
- `real-device-01-startup-page.png`
- `real-device-01-server-config.png`（IP 字段脱敏）
- `real-device-01-files-after-login.png`
- `real-device-01-settings-online.png`

### 验收标准
- App 不崩溃，所有页面可加载
- 登录成功，无敏感信息暴露（Token 片段、密码不出现）
- 服务器地址字段脱敏

**结论（验收后填写）：** PASS / FAIL / WARN

---

## 项目 2：真机相册/媒体权限 → 照片备份并命中

### 验证目标
授予真机相册/媒体权限，从 App 中选择照片备份，确认文件出现在文件页或媒体库。

### 前置条件
- [ ] Android 真机至少有 3 张不同尺寸照片
- [ ] 项目 1 登录状态有效

### 操作步骤

| 步骤 | 操作 | 预期结果 | 截图要点 |
| --- | --- | --- | --- |
| 2.1 | 在文件页点击"上传"按钮 | 弹出 CreateActionPage 备份选项 | **截图 2.1**：备份弹层（显示"备份照片""从相册选择备份""备份本机文件"） |
| 2.2 | 选择"备份照片"或"从相册选择备份" | 系统弹出相册/文件选择器，请求权限 | **截图 2.2**：权限请求对话框 |
| 2.3 | 授予相册/媒体权限 | 权限授予成功，进入媒体选择界面 | **截图 2.3**：权限授予后媒体选择页 |
| 2.4 | 选择 2-3 张照片，点击"备份" | 照片进入上传队列 | **截图 2.4**：上传队列显示进度 |
| 2.5 | 等待上传完成 | 队列显示"完成"状态 | **截图 2.5**：上传完成状态 |
| 2.6 | 切换到文件页，定位到备份目录 | 照片文件出现在文件列表中 | **截图 2.6**：文件页显示备份的照片文件名 |
| 2.7 | 切换到"相册" Tab（PhotosPage） | 照片在媒体库中可见 | **截图 2.7**：媒体库显示上传的照片（内容缩略但不显示隐私内容） |

### 截图清单
- `real-device-02-create-action-menu.png`
- `real-device-02-permission-dialog.png`
- `real-device-02-media-picker.png`
- `real-device-02-upload-progress.png`
- `real-device-02-upload-completed.png`
- `real-device-02-files-listed.png`
- `real-device-02-media-library.png`

### 验收标准
- 权限对话框正常弹出
- 授予后媒体选择功能完整
- 备份成功后文件出现在文件页和媒体库
- 截图不显示原始照片内容（仅文件名称/列表）

**结论（验收后填写）：** PASS / FAIL / WARN

---

## 项目 3：大视频备份路径

### 验证目标
验证大视频文件（>50MB，触发分片上传）的备份路径：进度可见、失败原因可读、重试后可恢复。记录文件大小范围但不记录隐私文件内容。

### 前置条件
- [ ] 准备至少一个 >50MB 的视频文件（可用 `ffmpeg -f lavfi -i testsrc=duration=30:size=1920x1080 -c:v libx264 -preset ultrafast test_video_50mb.mp4` 生成）
- [ ] 项目 1 登录状态有效

### 操作步骤

| 步骤 | 操作 | 预期结果 | 截图要点 |
| --- | --- | --- | --- |
| 3.1 | 准备测试视频文件（>50MB），确认文件大小 | 确认 > 32MB（分片阈值） | — |
| 3.2 | 在文件页点击"上传"，选择测试视频文件 | 文件进入上传队列 | **截图 3.2**：队列显示文件名和文件大小（只显示大小范围，如 "约 50 MB"） |
| 3.3 | 观察上传进度 | 进度条持续更新，显示分片上传进度 | **截图 3.3**：上传进度条和百分比 |
| 3.4 | 中断上传：在分片上传过程中关闭 App 或断开网络 | 上传失败，队列项变为失败状态 | **截图 3.4**：失败状态及可读错误原因（不暴露原始异常、Token） |
| 3.5 | 恢复网络/重新打开 App，检查队列保留 | 失败任务仍然保留在队列中 | **截图 3.5**：App 重启后队列状态一致 |
| 3.6 | 点击"重试备份" | 重试触发，继续上传剩余分片 | **截图 3.6**：重试后进度从断点恢复 |
| 3.7 | 等待上传完成 | 队列显示"完成"状态 | **截图 3.7**：上传完成 |
| 3.8 | 确认文件出现在文件页 | 视频文件在列表中 | **截图 3.8**：文件页列表显示视频文件 |

### 脱敏说明
- 文件大小记录范围（"约 50 MB"）、不记录精确字节数或文件内容
- 错误原因使用 `UserVisibleErrorSanitizer` 处理后的文案，不记录原始 Exception
- logcat 只裁剪关键分片上传/重试日志，去除 Token、文件内容

### 截图清单
- `real-device-03-queue-with-size.png`
- `real-device-03-upload-progress.png`
- `real-device-03-upload-failed.png`
- `real-device-03-queue-persisted-after-restart.png`
- `real-device-03-retry-progress.png`
- `real-device-03-upload-completed.png`
- `real-device-03-file-listed.png`

### 验收标准
- 分片上传进度可视化
- 失败原因为用户友好文案（不暴露原始异常）
- 重试可从断点继续（或重新上传分片）
- 文件最终上传成功

**结论（验收后填写）：** PASS / FAIL / WARN

---

## 项目 4：后台/前台切换、弱网中断队列保留与重试

### 验证目标
验证 App 在后台/前台切换、弱网或服务中断后上传队列的保留与重试能力。记录 OEM 省电策略影响为 WARN。

### 前置条件
- [ ] Android 真机支持 WiFi 或蜂窝控制（切换飞行模式或关闭 WiFi）
- [ ] 项目 1 登录状态有效，有进行中的上传任务

### 操作步骤

| 步骤 | 操作 | 预期结果 | 截图要点 |
| --- | --- | --- | --- |
| 4.1 | 开始一个小文件上传任务 | 上传进行中 | **截图 4.1**：上传进度条可见 |
| 4.2 | 将 App 切换到后台（按 Home 键） | 无崩溃 | — |
| 4.3 | 等待 30 秒后切回前台 | App 恢复，队列状态不变 | **截图 4.3**：队列状态保持与切换前一致 |
| 4.4 | 关闭 WiFi / 开启飞行模式 | 上传任务失败或超时 | **截图 4.4**：队列显示失败状态，错误原因可读 |
| 4.5 | 恢复网络 | 任务停留在失败状态等待手动重试 | **截图 4.5**：网络恢复后队列状态仍保留 "重试备份" 按钮可见 |
| 4.6 | 点击"重试备份" | 重试成功，任务完成 | **截图 4.6**：重试成功后显示完成状态 |

### OEM 省电记录
- 设备型号：____________________
- Android 版本：____________________
- OEM 品牌：____________________
- 省电模式设置：____________________
- 省电对后台队列的影响说明：____________________

| OEM 场景 | 观察结果（验收后填写） |
| --- | --- |
| 标准模式 | PASS / WARN（说明：__________） |
| 省电模式 | PASS / WARN（说明：__________） |
| 超级省电模式 | PASS / WARN（说明：__________） |

### 证据清单
- `real-device-04-queue-foreground.png`
- `real-device-04-queue-after-foreground-background.png`
- `real-device-04-queue-failed-network-down.png`
- `real-device-04-queue-after-network-restore.png`
- `real-device-04-retry-success.png`
- `real-device-04-oem-power-log.txt`（OEM 省电 logcat 摘要）

### 验收标准
- 前后台切换不丢失队列状态
- 网络中断导致的任务失败有可读原因
- 网络恢复后可手动重试
- OEM 省电影响记录为 WARN

**结论（验收后填写）：** PASS / WARN（OEM 记录）

---

## 项目 5：MAUI 内下载/预览补证

### 验证目标
在 Android 真机上完成 MAUI 内的文件下载、预览链路的完整验证：图片/视频/普通文件至少一条完整可见链路。

### 前置条件
- [ ] 项目 2 或 3 已完成，服务器上有可预览的文件

### 操作步骤

| 步骤 | 操作 | 预期结果 | 截图要点 |
| --- | --- | --- | --- |
| 5.1 | 在文件页点击一个图片文件 | 进入文件详情/预览页 | **截图 5.1**：图片预览页显示缩略图 |
| 5.2 | 点击下载按钮（或长按选择下载） | 文件下载到设备，显示下载进度 | **截图 5.2**：下载进度提示 |
| 5.3 | 在设备通知栏或下载列表中确认文件下载完成 | 下载成功 | **截图 5.3**：通知栏/系统下载列表显示文件 |
| 5.4 | 返回 App，点击一个视频文件 | 视频预览/播放界面显示 | **截图 5.4**：视频播放界面（可暂停状态） |
| 5.5 | 点击一个普通文件（如 `.txt`） | 显示文件内容或触发系统打开方式 | **截图 5.5**：文件内容预览或系统分享/打开对话框 |

### 脱敏说明
- 预览截图应只显示文件名称和 UI 布局，不显示文件全部内容
- 视频预览截图只显示播放器界面帧（不播放隐私内容）

### 截图清单
- `real-device-05-image-preview.png`
- `real-device-05-download-progress.png`
- `real-device-05-download-notification.png`
- `real-device-05-video-preview.png`
- `real-device-05-file-preview.png`

### 验收标准
- 图片预览可加载
- 下载到本地成功
- 视频播放器界面可加载（内容播放待验证）
- 普通文件预览显示或正确路由到系统应用

**结论（验收后填写）：** PASS / FAIL / WARN

---

## 项目 6：删除 → 回收站 → 恢复全链路

### 验证目标
在 Android 真机上完成删除文件、回收站查看、恢复的完整 UI 链路截图。确认永久删除/清空回收站有强确认对话框。

### 前置条件
- [ ] 服务器上有至少一个可删除的文件（来自项目 2/3/5）

### 操作步骤

| 步骤 | 操作 | 预期结果 | 截图要点 |
| --- | --- | --- | --- |
| 6.1 | 在文件页选择文件→点击选择模式"选择"→勾选文件 | 文件被选中 | **截图 6.1**：选中状态，显示批量操作工具栏（含"移入回收站"） |
| 6.2 | 点击"移入回收站" | 文件移入回收站，文件页不再显示 | **截图 6.2**：移入回收站后的空文件页，或确认提示 |
| 6.3 | 切换到"我的"→点击"回收站" | 进入回收站页面，已删除文件可见 | **截图 6.3**：回收站列表显示被删除文件 |
| 6.4 | 选择文件→点击"恢复" | 文件恢复成功，回到原目录 | **截图 6.4a**：恢复确认提示；**截图 6.4b**：文件恢复后出现在文件页 |
| 6.5 | **永久删除验证**：回收站中选择文件→永久删除 | 弹出强确认对话框（如"确认永久删除？此操作不可恢复"） | **截图 6.5a**：强确认对话框；**截图 6.5b**：永久删除后文件消失 |
| 6.6 | **清空回收站验证**：点击"清空回收站" | 弹出强确认对话框 | **截图 6.6**：清空确认对话框 |

### 脱敏说明
- 文件名可保留，文件内容不显示
- 确认对话框文字完整记录

### 截图清单
- `real-device-06-file-selected.png`
- `real-device-06-move-to-trash.png`
- `real-device-06-trash-listing.png`
- `real-device-06-restore-confirmation.png`
- `real-device-06-file-restored.png`
- `real-device-06-permanent-delete-confirmation.png`
- `real-device-06-after-permanent-delete.png`
- `real-device-06-empty-trash-confirmation.png`

### 验收标准
- 删除 → 回收站可见 → 恢复成功
- 永久删除有强确认（不可撤销的提示文案）
- 清空回收站有强确认

**结论（验收后填写）：** PASS / FAIL / WARN

---

## 项目 7：容量/系统健康/恢复与隐私边界页面

### 验证目标
验证 Android App 中容量、系统健康状态、恢复说明和隐私边界页面保持可见。截图和 logcat 必须脱敏。

### 操作步骤

| 步骤 | 操作 | 预期结果 | 截图要点 |
| --- | --- | --- | --- |
| 7.1 | 切换到"我的"（SettingsPage） | 显示在线状态、文件数、存储容量、容量百分条 | **截图 7.1**：我的页整体截图 |
| 7.2 | 点击"存储用量" | 进入 StorageUsagePage，显示容量分类、存储位置、健康状态 | **截图 7.2**：存储用量页 |
| 7.3 | 验证"位元组/已用/剩余"信息 | 所有数字脱敏显示（精确值但无敏感信息） | — |
| 7.4 | 查看"存储位置"和"后端存储" | 显示 FileSystem / Aliyun OSS 信息（不暴露完整路径） | **截图 7.4**：存储位置信息（路径脱敏） |
| 7.5 | 查看"健康状态" | 显示 Healthy / Warning 等状态 | **截图 7.5**：健康状态行 |
| 7.6 | 点击"恢复边界说明"或相关链接 | 打开恢复说明页面/对话框 | **截图 7.6**：恢复边界说明内容（截取标题和关键提示） |
| 7.7 | 查看"隐私边界"信息 | 显示隐私声明 | **截图 7.7**：隐私边界内容（截取标题和关键提示） |

### 脱敏说明
- 存储路径脱敏：`FileSystem: [STORAGE_ROOT]/...` → `[REDACTED_PATH]`
- 服务器地址脱敏：`http://192.168.x.x:8080` → `[REDACTED_IP]:8080`
- 数字容量可保留（非敏感）

### 截图清单
- `real-device-07-settings-page.png`
- `real-device-07-storage-usage.png`
- `real-device-07-storage-location.png`（路径脱敏）
- `real-device-07-health-status.png`
- `real-device-07-restore-boundary.png`
- `real-device-07-privacy-boundary.png`

### 验收标准
- 所有页面正常加载，无连接错误
- 存储路径/服务器地址已脱敏
- 恢复说明和隐私边界文案完整可读

**结论（验收后填写）：** PASS / FAIL / WARN

---

## 项目 8：Release Gate — Secret/Log Scan 与 Validation Evidence Index

### 验证目标
完成上述 7 项验收后，运行 release gate 所需的 secret/log scan 和 validation evidence index，确保所有新证据文件无敏感信息泄漏。

### 操作步骤

| 步骤 | 命令 | 预期结果 |
| --- | --- | --- |
| 8.1 | `git diff --check` | 无 trailing whitespace 或冲突标记 |
| 8.2 | `python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD` | PASS，0 findings |
| 8.3 | `python scripts/validation_evidence_index.py --run-id "mobile-eng-real-device-r1" --date $(date -u +%Y%m%d)` | PASS，evidence_count=N，finding_count=0 |
| 8.4 | 人工复核截图内容：确认无完整 Token、私有 IP（未脱敏）、密码、OAuth 密钥、完整私有分享 URL | 所有截图合规 |
| 8.5 | 更新 `docs/validation/evidence-index.md` 中的真机验收结论 | 8 项状态均已填写 |

### 验收标准
- Secret/log scan：0 findings
- Validation evidence index：PASS，0 sensitive findings
- 所有截图脱敏合规
- Evidence index 更新完成

**结论（验收后填写）：** PASS / FAIL

---

## 附录 A：ADB 截图与 Logcat 命令参考

```bash
# 截图（每次 pull 后按截图清单重命名，避免默认文件名覆盖）
adb shell screencap -p /sdcard/screenshot.png
adb pull /sdcard/screenshot.png docs/validation/screenshots/real-device/real-device-xx-temp.png
adb shell rm /sdcard/screenshot.png
# 重命名示例：mv real-device-xx-temp.png real-device-01-startup-page.png

# 截取指定进程 logcat（两步过滤：先排除敏感令牌行，再按 TAG 筛选；敏感过滤规则见附录 B）
adb logcat -c
adb logcat -v brief -T 1 | grep -ivE "(Authorization|Bearer|access_token|refresh_token|client_secret|password)" | grep -iE "(privateclouddrive|startup|login|upload|backup|queue|error)" > docs/validation/logcat/real-device-xx.txt

# 构建 APK
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None

# 安装 APK
adb install -r maui/PrivateCloudDrive.App/bin/Debug/net10.0-android/com.companyname.privateclouddrive.app-Signed.apk

# 卸载 App
adb uninstall com.companyname.privateclouddrive.app

# 查看设备型号
adb shell getprop ro.product.model
adb shell getprop ro.build.version.release
```

## 附录 B：脱敏模板

### 截图脱敏规则

| 元素 | 处理方法 | 示例 |
| --- | --- | --- |
| 服务器 IP 地址 | 替换为 `[REDACTED_IP]` | `192.168.1.100` → `[REDACTED_IP]` |
| 用户名（非必要） | 保留，或替换为 `[REDACTED_USER]` | `admin` → `[REDACTED_USER]` |
| Token 片段 | 全部替换为 `[REDACTED_TOKEN]` | `eyJhbGciOi...` → `[REDACTED_TOKEN]` |
| 文件原始内容 | 仅显示文件名，不截取文件内容 | — |
| 分享链接 | 替换完整 URL | `http://[REDACTED_IP]:8080/s/abc123` → `[REDACTED_SHARE_URL]` |
| 设备 IMEI/SN | 裁剪或马赛克 | — |

### Logcat 脱敏规则

```bash
# 过滤条件：去除含敏感 Token 的行
adb logcat -v brief -T 1 | grep -ivE "(Authorization|Bearer|access_token|refresh_token|client_secret|password)"

# 只保留以下 TAG 输出
adb logcat -v brief -T 1 -s "MonoDroid-Touch" "MonoDroid-Debug" 2>/dev/null || true
```

## 附录 C：8 项验收总表（验收后填写）

| 编号 | 项目 | 结论 | 备注 |
| --- | --- | --- | --- |
| 1 | Clean Install + 登录 + 连接状态 | □ PASS □ WARN □ FAIL | **环境限制**：ADB 无设备连接，无法安装 APK 与触控验证 |
| 2 | 相册权限 → 照片备份命中 | □ PASS □ WARN □ FAIL | **环境限制**：同上 |
| 3 | 大视频备份路径 | □ PASS □ WARN □ FAIL | **环境限制**：同上 |
| 4 | 后台/前台切换 + 弱网重试 | □ PASS □ WARN □ FAIL | **环境限制**：同上 |
| 5 | MAUI 内下载/预览 | □ PASS □ WARN □ FAIL | **环境限制**：同上 |
| 6 | 删除 → 回收站 → 恢复全链路 | □ PASS □ WARN □ FAIL | **环境限制**：同上 |
| 7 | 容量/健康/恢复/隐私边界 | □ PASS □ WARN □ FAIL | **环境限制**：同上 |
| 8 | Release Gate 扫描 | □ PASS □ FAIL | **PASS** — Secret scan 0 findings，validation evidence index PASS |
