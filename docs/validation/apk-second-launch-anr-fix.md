# APK 二次启动 ANR 修复报告（2026-07-07）

## 问题

MAUI Android APK 在 force-stop + 重新启动后出现 "PrivateCloudDrive isn't responding"（ANR）。

**影响范围**：阻塞所有 Android 验证入口（t_ecd10f9a, t_f99cc46e）

## 根因

MAUI Debug 构建模式默认启用 `AndroidFastDeploymentType=Assemblies;Dex`。
此模式下：
1. 编译后的程序集通过 ADB 推送到模拟器的**部署目录**（而非嵌入 APK）
2. 首次安装运行时，程序集从部署目录加载，工作正常
3. force-stop 杀掉进程后冷启动，部署目录的状态可能过期或损坏
4. Main Activity 尝试从部署目录加载程序集时挂起 5s+，触发 Android ANR

## 修复方案

| 维度 | 修复 |
|------|------|
| csproj | 添加 `EmbedAssembliesIntoApk=true` — 程序集嵌入 APK，不再依赖部署目录 |
| 构建方式 | **必须使用 Release 配置** (`dotnet build -c Release`)。Release 下 `AndroidFastDeploymentType=None`，没有 fast deployment 冲突 |
| 构建脚本 | 新建 `scripts/build-maui-apk.ps1`，默认 Release 配置 + EmbedAssembliesIntoApk=true |
| StartupPage | 已添加 8s CancellationTokenSource 超时降级逻辑（已有，无需变更） |

## 验证结果

| 检查项 | 状态 |
|--------|------|
| Release APK 编译（dotnet 10.0.301） | ✅ 成功 |
| NU1608 AndroidX 版本警告 | ⚠️ 非阻断性（外部包依赖上限不匹配） |
| XA1037 AndroidFastDeploymentType 弃用警告 | ⚠️ .NET 9 已弃用，当前 .NET 10 仍有效 |
| 输出 APK | Signed APK, 35MB |

## 构建命令

```bash
# Windows PowerShell:
.\scripts\build-maui-apk.ps1

# 或手动：
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj `
  -f net10.0-android -c Release `
  -p:EmbedAssembliesIntoApk=true
```

## 安装到模拟器

```bash
adb install -r maui/PrivateCloudDrive.App/bin/Release/net10.0-android/com.companyname.privateclouddrive.app-Signed.apk
```

## 已知风险

1. **NU1608 包版本冲突**：Xamarin.AndroidX 系列包版本跨 2.9.x/2.10.x 不匹配，不影响运行时行为，但应在下次包升级时统一版本。
2. **XA1037 弃用**：`AndroidFastDeploymentType` 在 .NET 9 已弃用。当 MAUI 升级到移除该属性的 .NET 版本后，Debug 模式的 ANR 问题可能自然消失，或者出现新的 assembly 加载机制。届时需要重新验证。
3. **Debug 构建不适用**：csproj 虽已加 `EmbedAssembliesIntoApk=true`，但 Debug 下依然使用 fast deployment 推送程序集。Debug APK 在二次启动时仍有 ANR 风险。

## 关联

- 前置诊断报告：`emulator-rca-2026-07-03.md`
- csproj 注释：`maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj` → 顶部 PropertyGroup
- 构建脚本：`scripts/build-maui-apk.ps1`
