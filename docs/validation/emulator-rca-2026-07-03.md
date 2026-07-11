# 模拟器验收环境诊断报告（2026-07-03 13:45）

## 环境状态

| 项目 | 状态 |
|------|------|
| 模拟器 (emulator-5554) | ✅ 在线 (Android 16, 1280x2856) |
| APK (Signed) | ✅ 已构建 (16MB, 2026-07-03 11:27) |
| PCD Docker 栈 | ✅ 4/4 运行 (api:8081, postgres, redis, media-worker) |
| API 登录验证 | ✅ 成功 (qa_user) |
| ADB 设备连接 | ✅ 可用 |

## App 启动验证

- ✅ **首次启动成功**：App 正常显示登录页面（PrivateCloudDrive 品牌、用户名/密码输入框）
- ❌ **二次启动崩溃**：force-stop + restart 后显示 "PrivateCloudDrive isn't responding"
- 根因推测：MAUI Debug APK 的 native assemblies 压缩导致启动后崩溃（前一个 worker 已识别此问题）

## 登录链路阻塞

1. ADB `input tap` 坐标在 ScrollView 中不可靠——元素坐标随键盘/弹窗偏移
2. 模拟器需要通过 `http://10.0.2.2:8081` 访问宿主服务，但 App 的默认地址可能是 `localhost:8080`
3. 登录失败后 App 显示隐私保护提示（不暴露详细错误）

## 建议

1. **修复 APK 构建**：使用 `EmbedAssembliesIntoApk=true` + `AndroidFastDeploymentType=None` 重新构建
2. **预配置服务器地址**：在 App 的 Preferences/Storage 中预设 `10.0.2.2:8081` 
3. **简化验收路径**：将验收拆为 API 层（可验证）+ UI 层（需修复 MAUI 构建）
