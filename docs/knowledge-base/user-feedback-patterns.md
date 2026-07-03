# 用户反馈模式与重复问题整理（SOP 建议）

> 本文档由 Support-Ops 维护，记录 PrivateCloudDrive 开发与验收过程中反复出现的阻塞模式、根因分析、临时修复方案，以及面向 Hermes 多 Agent 团队的操作规程建议。
>
> 创建日期：2026-07-03 | 来源：`docs/validation/emulator-rca-2026-07-03.md`、`docs/validation/android-acceptance-decomposition.md`、`docs/validation/v1.0-rc-to-v1.1-transition-dispatch-plan.md`
>
> 管理入口：Support-Ops 负责新增/更新，PM 负责优先级评估，QA 负责验收闭环。

---

## 1. 已知重复问题模式

### 1.1 模式分类总表

| # | 模式名称 | 类型 | 影响面 | 复现频率 | 阻塞等级 | 首次发现 | 最近一次 |
|---|----------|------|--------|----------|----------|----------|----------|
| P1 | APK 二次启动 ANR | 构建缺陷 | Android 全部验收 | 每次 | P0 (阻塞) | 2026-07-03 | 2026-07-03 |
| P2 | ADB 文本输入在 Android 16 失效 | 环境缺陷 | 自动化验收 | 每次（Gboard） | P1 | 2026-07-03 | 2026-07-03 |
| P3 | 模拟器 ScrollView 坐标偏移 | 自动化缺陷 | 模拟器 UI 验收 | 每次（含键盘弹起） | P1 | 2026-07-03 | 2026-07-03 |
| P4 | 无物理设备→验收链反复阻塞 | 资源缺陷 | Android 验收全链路 | 持续 | P0 (门禁) | 2026-06 | 2026-07-03 |
| P5 | Docker Compose 项目名污染 | 运维缺陷 | Docker 镜像/存储 | 多项目并行时 | P2 | 2026-05 | 2026-06 |
| P6 | Git 工作区隔离三层次不完整 | 协作缺陷 | 所有 Agent | 新员工首次 | P2 | 2026-05-22 | 2026-05-22 |
| P7 | 脱敏遗漏 | 合规缺陷 | 证据/截图 | 无系统前置检查 | P1 | 持续 | 持续 |
| P8 | 模拟器宿主地址默认不匹配 | 配置缺陷 | 首次登录 | 100%（新环境） | P1 | 2026-07-03 | 2026-07-03 |

---

### 1.2 模式详述与 SOP

#### P1：APK 二次启动 ANR

**现象描述：**
MAUI Debug APK 首次启动正常（登录页可见），但 force-stop + restart 后显示 "PrivateCloudDrive isn't responding"（ANR 对话框）。

**根因：**
`dotnet build -f net10.0-android` 默认生成 Debug APK 时，native assemblies 被压缩（fast deployment 模式）。App 首次启动时解压正常，但二次启动时 MAUI 运行时找不到已解压的程序集，触发 ANR。

**修复方案（已验证）：**
```bash
dotnet build -f net10.0-android \
  -p:EmbedAssembliesIntoApk=true \
  -p:AndroidFastDeploymentType=None
```

**SOP 要点：**
1. 每次 `adb uninstall` 后 clean install 仍需要正确的构建参数
2. 构建脚本需强制锁定这两个参数，不接受默认值
3. 构建后验证：`adb shell dumpsys package com.companyname.privateclouddrive.app | grep versionName` 确认安装成功
4. 验收流程中应在 2 分钟内完成首次启动 → force-stop → 二次启动验证

**是否可写入 skill：** 是。建议作为 `privateclouddrive-delivery` skill 中 "MAUI APK 构建" 章节的核心模板。

---

#### P2：Android 16 ADB 文本输入失效

**现象描述：**
在 Android 16 模拟器上，`adb shell input text <string>` 和 `adb shell input keyevent` 在执行时无报错但输入框无内容。Gboard 接管软键盘后，ADB 的 input 指令无法注入到 EditText。

**临时修复方案：**
1. 使用 `input tap <x> <y>` 先聚焦输入框，再逐字符模拟
2. 或在模拟器上安装 ADB Keyboard APK（第三方 IME）替代 Gboard
3. 或使用 ADB 的 `am broadcast` 方式而非 `input text`

**SOP 要点：**
1. 登录场景优先使用 API 直调（`curl`）而非 UI 输入，减少 ADB 依赖
2. 必须 UI 输入时，使用坐标 tap 替代 text/keyevent
3. 验收文档中标注"ADB text 在 Android 16 Gboard 环境不适用"
4. 建议为模拟器预设一个避免 Gboard 的输入法

---

#### P3：模拟器登录被 ScrollView 坐标偏移阻塞

**现象描述：**
模拟器登录页面使用 ScrollView 布局，用户名/密码输入框坐标在软键盘弹起后发生偏移。`adb shell input tap <x> <y>` 无法准确点击到目标元素，导致登录链路中断。

**临时修复方案：**
1. 先点击页面空白区域确认键盘未弹起
2. 使用 `uiautomator` dump XML 获取实时元素边界
3. 解析 XML 中的 `bounds` 属性计算准确 tap 坐标
4. 或：放弃 UI 登录验证，改由 API 层直接调用登录接口

**SOP 要点：**
1. 模拟器验收的登录步骤优先走 API 层（`curl -X POST http://10.0.2.2:8081/api/...`）
2. 仅当验证 UI 页面渲染时进行 UI 登录（此时需借助 uiautomator dump）
3. 在验收运行手册中加入 `scripts/parse_ui_xml.py` 的使用说明
4. 滑动到目标元素上方再 tap（先 `input swipe` 再 tap）

---

#### P4：无物理设备→验收链反复阻塞

**现象描述：**
QA 验收卡依赖"Android 真机"，但开发环境无可用物理 Android 设备。ADB 返回空列表。4 次 Kanban 运行均因无设备而 crashed/blocked，导致后续 5 个验收卡连锁阻塞。

**根因：**
验收运行手册从设计上要求"物理真机"，未考虑模拟器替代路径。当 ADB 无设备时没有 fallback 机制，整个验收链卡死。

**处置方案（已确认）：**
1. **决策：** 用户已接受模拟器替代真机。模拟器 Android 16 (API 36) 1280x2856 足够覆盖 90%+ 验收项
2. **原卡处理：** 标记为 `reclaimed-by-simulator` 并 blocked
3. **替代卡：** 创建新的模拟器验收卡链
4. **差异记录：** 模拟器无法测试相机硬件、传感器、OEM 省电策略——这些标记为 Known Limitation

**SOP 要点：**
1. 所有 Android 验收任务的开头必须执行 `adb devices` 检查设备状态
2. 无设备 → 自动 fallback 到模拟器验收路径，不等待人工
3. Fallback 时自动调整验证项（移除不可在模拟器上验证的条目）
4. 在验收结论中记录设备类型（真实设备 vs 模拟器）

---

#### P5：Docker Compose 项目名污染

**现象描述：**
多个 Compose 项目在未指定 `COMPOSE_PROJECT_NAME` 时创建独立镜像。例如 `knowpulse` 和 `privateclouddrive` 项目各自创建不同的 Postgres/MinIO 镜像。Docker Desktop 镜像列表膨胀，`docker compose` 命令可能混淆目标栈。

**SOP 要点：**
1. 每个 Compose 项目必须显式设置 `COMPOSE_PROJECT_NAME`
2. PCD 栈的 Compose 项目名硬编码为 `privateclouddrive`
3. 非 PCD 栈必须指定不同项目名（如 `knowpulse`）
4. 启动前先 `docker compose ls` 检查是否有冲突运行
5. 停止时使用 `docker compose -p <name> down` 确保精确

---

#### P6：Git 工作区隔离三层次不完整

**现象描述：**
2026-05-22 治理事件后建立了三层防护规则（分支策略、worktree、Kanban workspace 隔离），但新员工（新 Agent）在首次接手任务时可能绕过这些规则，直接修改 `main` 分支或跨任务共享工作区。

**SOP 要点：**
1. 每个 Kanban 任务启动时，worker 必须检查 `git branch --show-current`
2. 如果当前分支是 `main`，禁止直接修改代码文件
3. 使用 `worktree` 隔离不同功能分支的检出
4. 非代码任务（文档/验收）使用 `scratch` workspace
5. 新 worker 入职后由 Support-Ops 发送 Git 规则摘要

---

#### P7：脱敏遗漏

**现象描述：**
验收过程中截图/日志中包含 IP 地址、文件路径、文件内容等敏感信息。之前出现过截图未脱敏就提交到验收证据库的情况。

**SOP 要点：**
1. 每个验收子任务结尾必须执行 `python scripts/secret-log-scan.py`
2. 截图中的 IP 替换为 `[REDACTED_IP]`，路径替换为 `[REDACTED_PATH]`
3. 脱敏规则固化在 `secret-log-scan.py` 中（PLACEHOLDER_RE）
4. 截图脱敏人工复核是 Release Gate（Task 8）的必过项
5. Task 8 执行全量扫描做最终确认

---

#### P8：模拟器宿主地址默认不匹配

**现象描述：**
模拟器首次启动时，App 配置的后端地址默认为 `localhost:8080`，但模拟器需要通过 `http://10.0.2.2:8081`（或宿主局域网 IP）访问宿主服务。导致登录页面可以渲染但 API 调用失败，App 隐私保护不暴露详细错误，增加排查难度。

**SOP 要点：**
1. 在 App 的 Preferences/Storage 中预设 `10.0.2.2:8081` 作为模拟器环境的默认值
2. 首次验收时，必须检查 App 的 Server Config 页面确认地址正确
3. 如果地址错误，通过 ADB 修改 Preference 或重新配置
4. 验收报告记录配置步骤以便复现

---

## 2. SOP 切换点矩阵

以下表格列出每个问题模式的 SOP 触发条件和对应的处理路径：

| 触发器 | 检测方式 | 响应 SOP | 负责人 |
|--------|----------|----------|--------|
| APK 二次启动即崩溃 | `adb shell am start` 后检查进程存活 | P1: 重建 APK + 验证二次启动 | mobile-eng |
| ADB `input text` 无效果 | `input text` 后截图检查输入框 | P2: 切换至 `input tap` 或 API 模式 | qa-eng |
| Tap 坐标不可靠 | `input tap` 后检查 UI 状态不符合预期 | P3: uiautomator dump → bounds 解析 | qa-eng |
| `adb devices` 返回空 | 任务开头检查 | P4: Fallback 到模拟器验收 | qa-eng |
| Docker 镜像过多/端口冲突 | `docker compose ls` 查看多项目 | P5: 设置 COMPOSE_PROJECT_NAME | devops-eng |
| Worker 在 main 分支上修改代码 | `git branch --show-current` | P6: 拒绝修改 + 通知 PM | 所有 agent |
| Secret scan 失败 | `python scripts/secret-log-scan.py` exit=1 | P7: 修正脱敏 → 重新 scan | 当事人 |
| App 登录后 API 返回无详细信息 | UI 显示"隐私保护"提示 | P8: 检查后端地址配置 | qa-eng |

---

## 3. 需要写入 Skill 但尚未覆盖的操作经验

> **背景：** `privateclouddrive-delivery` skill 尚未创建。以下是从 3 份源文档中提取的、需要固化到 skill 中的操作经验。

### 3.1 缺失 Skill 内容清单

| # | Skill 章节 | 建议内容 | 来源文档 | 优先级 |
|---|------------|----------|----------|--------|
| S1 | `MAUI APK 构建` | 强制 `EmbedAssembliesIntoApk=true` + `AndroidFastDeploymentType=None`；构建后验证 | emulator-rca | P0 |
| S2 | `模拟器验收框架` | 模拟器启动参数（`-no-snapshot -no-audio -gpu swiftshader_indirect`）；ADB 连接验证 | emulator-rca, v1.0-transition | P0 |
| S3 | `ADB 操作兼容性` | Android 16 Gboard 的 `input text` 限制；替代方案 | emulator-rca | P1 |
| S4 | `验收子任务分解模板` | 8-task DAG 模式；截图命名规则；秘扫在每个子任务结尾 | android-acceptance-decomposition | P0 |
| S5 | `脱敏操作规范` | 截图 IP/路径脱敏规则；secret-log-scan.py 自动化使用；人工复核步骤 | android-acceptance-decomposition, v1.0-transition | P1 |
| S6 | `Docker Compose 项目隔离` | COMPOSE_PROJECT_NAME 硬编码；`docker compose ls` 检查 | v1.0-transition | P2 |
| S7 | `Git 工作区防护` | 三层隔离规则；`git branch --show-current` 检查 | v1.0-transition | P2 |
| S8 | `Release Gate 标准流程` | 证据索引更新；全量 scan；人工复核脱敏；PR 提交流程 | android-acceptance-decomposition | P1 |

### 3.2 创建 `privateclouddrive-delivery` Skill 的建议

建议 PM 同意后创建该 skill（或由 Support-Ops 直接创建），内容提纲：

```
privateclouddrive-delivery/
├── SKILL.md             # 入口文档，包含版本号、适用场景、快速检索表
├── references/
│   ├── apk-build.md     # S1: MAUI APK 构建规范
│   ├── emulator-setup.md # S2: 模拟器验收框架
│   ├── adb-compat.md    # S3: ADB 操作兼容性
│   ├── acceptance-decomposition.md # S4: 验收子任务分解模板
│   └── desensitization.md # S5: 脱敏操作规范
└── scripts/
    ├── verify-apk.sh    # 构建后验证脚本
    └── check-docker.sh  # Docker 冲突检查
```

---

## 4. 反馈分流规则（Support-Ops 参考）

当收到用户反馈时，按以下规则分流：

| 反馈类型 | 判断依据 | 分派给 | 响应时效 |
|----------|----------|--------|----------|
| **缺陷 (Bug)** | 功能与预期行为不符、崩溃、数据丢失 | mobile-eng / backend-eng | P0: 4h, P1: 24h |
| **需求 (Feature)** | 新功能请求、增强建议 | PM | 48h 内确认是否纳入路线图 |
| **体验问题 (UX)** | 界面不清晰、操作路径长、文案歧义 | PM + frontend-eng | 24h 内给初步回复 |
| **文档问题 (Docs)** | 文档缺失、描述错误、部署步骤不完整 | Support-Ops → Docs | 48h 内修正 |

**反馈受理模板（推荐）：**

```
## 反馈登记
- 来源：用户/Agent/自发现
- 分类：缺陷/需求/体验/文档
- 优先级：P0/P1/P2
- 复现环境：Android/iOS/Windows/macOS/WASM,版本号
- 复现步骤：
  1. ...
  2. ...
- 当前表现：
- 预期表现：
- 附件/截图：
```

---

## 5. 升级路径（Escalation）

| 层级 | 条件 | 负责人 | 响应时间 |
|------|------|--------|----------|
| L1 | 已知问题模式（本文档 §1 所列） | 当前 worker 根据 SOP 自助处理 | 即时 |
| L2 | SOP 无法解决、需跨团队协作 | Support-Ops 创建 Kanban 任务分派 | 2h |
| L3 | 重大安全漏洞、数据丢失、隐私合规 | Support-Ops 立即通知 PM + Security | 30min |
| L4 | 产品方向变更、功能范围取舍 | PM 上报用户 | 按需 |

---

## 6. 高频问题 FAQ（面向 Agent）

### Q: 模拟器 API 地址是什么？
A: `http://10.0.2.2:8081`。App 默认可能是 `localhost:8080`，模拟器中需要映射到宿主。

### Q: 构建 APK 后如何验证立即生效？
A: `adb install -r <apk>` → `adb shell am start -n com.companyname.privateclouddrive.app/.MainActivity` → 等待 5s → `adb shell am force-stop com.companyname.privateclouddrive.app` → 重新启动验证无 ANR。

### Q: 验收运行手册在哪里？
A: `docs/validation/android-real-device-evidence-runbook.md`（如果不存在则在创建中）。

### Q: 如何执行 secret scan？
A: `python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD`。

### Q: 没有物理 Android 设备怎么办？
A: Fallback 到模拟器验收。差异记录在验收结论中。

---

*本文档持续更新。每次发现新的复现模式或 SOP 改进，Support-Ops 负责在此记录并通知 PM。*
