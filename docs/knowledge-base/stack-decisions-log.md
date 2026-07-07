# 架构决策日志 / Stack Decisions Log

本文档记录 PrivateCloudDrive 项目阶段性架构决策、选型理由、回滚原因和落地方式。按时间倒序排列。

---

## ADR-006：Git 工作区隔离方案（2026-05-26）

**背景**

项目在 main 分支上存在多个 parallel 开发/验收任务，工作区（Working Directory）中有来自不同岗位的未提交变更（文档修改、脚本更新、测试数据）。直接提交会混入无关文件，污染提交历史。

**决策**

采用 Git Worktree 隔离发布/验收工作的提交范围，而非在脏工作区执行 `git add --interactive` 或 `git stash`。

- 每个独立发布任务基于 `origin/main` 创建独立 worktree。
- 在该 worktree 内 `git add` 仅限任务变更文件，完成后创建独立发布分支并推送 PR。
- 原 main 工作区的未提交/未跟踪改动不被带入发布分支。

**理由**

| 方案 | 优缺点 |
|---|---|
| `git add --interactive` | 步骤多，容易遗漏或误引入无关文件 |
| `git stash` | 涉及 untracked 文件时模式复杂，容易丢失进度 |
| 独立 Worktree + 分支 | 隔离干净，提交范围可审查，PR 清晰 |

**参考**

`docs/android-evidence-pack` 分支即按此方案创建（主工作区 main 保留已有未提交改动，基于 `origin/main` 创建 worktree 并发布 PR #26）。

---

## ADR-005：Secret/Log 扫描门禁选型（2026-05-22）

**背景**

GitHub Issue #5 要求建立公开仓库秘密泄露检测门禁。需要选择适合项目阶段的扫描工具和集成方式。

**选项与评估**

| 选项 | 优势 | 劣势 | 裁决 |
|---|---|---|---|
| GitGuardian (GitHub App) | 零配置，覆盖所有 push + PR | Issue #5 评估时未能成功开启；后续单独开启成功 | 不阻任务，但未作为唯一门禁 |
| Gitleaks CLI | 开源，规则可定制 | CI 集成需要额外 Action | 否决——维护成本超过项目当前需求 |
| 自研 `secret-log-scan.py` | 规则精确匹配项目模式：不打印匹配值、支持 archive guardrail、模板占位符白名单 | 需要维护 | 采纳——作为 Security Gate workflow 主扫描器 |

**决策**

- 采用 `scripts/secret-log-scan.py` 作为主扫描器。
- `.github/workflows/security-gate.yml` 在 push/PR/manual_dispatch 时运行。
- GitGuardian 作为辅助门禁，但不作为发布阻断。
- 扫描器设计约束：
  - **永不输出匹配值**，只输出 path/line/rule metadata。
  - 允许模板占位符（`<redacted>`、`PLACEHOLDER`、`${VAR}`、`CHANGEME` 等）。
  - 覆盖 working-tree 文本文件 + Git archive（release 包）路径检查。

**参考**

`docs/security-review-public-repo-p1-2026-05-22.md`、`.github/workflows/security-gate.yml`。

---

## ADR-004：Compose 项目名治理与备份脚本修正（2026-05-18）

**背景**

`backup-local-stack.ps1` 初次演练生成的 `storage.tar.gz` 仅 87 bytes，分析发现备份脚本使用显式 volume 名 `privateclouddrive_stack_storage`，而 Docker Compose 在创建 volume 时自动添加 `{project_name}_` 前缀，导致备份源为空。

**决策**

- 备份脚本改为从运行中 API 容器 `/app/storage` 挂载点动态解析真实 Docker volume 名。
- `manifest.json` 记录的 `storage.dockerVolume` 使用动态解析后的名称。
- 恢复脚本同样优先从 manifest 读取 docker volume 名，而非硬编码。

**理由**

- Compose project name 可通过环境变量 `COMPOSE_PROJECT_NAME`、目录名或 `docker-compose.yml` 中 `name:` 字段变化，硬编码 volume 名无法跨环境工作。
- 动态解析从容器挂载反推 volume 名最可靠。

**影响范围**

`scripts/backup-local-stack.ps1`、`scripts/restore-local-stack.ps1`。

---

## ADR-003：公开文档采用统一脱敏口径（2026-05-22）

**背景**

多个验收人员发布的 issue/report 中偶见暴露 token、分享 URL 或日志原文片段，需要建立统一规范。

**决策**

所有公开文档、验收报告、截图和日志采用以下脱敏规则：

| 内容类型 | 处理方式 |
|---|---|
| 密码、加密短语 | 绝不输出原始值；使用 `<redacted>` 或 `CHANGEME` |
| access_token / refresh_token | 不在任何文档、日志截图中出现完整值 |
| `Set-Cookie` 或 `Cookie` 头 | 不复制到公开文档中 |
| 分享 URL | 仅展示 `https://{host}/s/{shortId}` 格式，不输出完整可访问 URL |
| 服务器绝对路径 | 替换为 `/app/storage` 或 `{storage_root}` |
| OSS bucket / object key | 不展示原始名称 |
| `.env` 文件内容 | 禁止整体输出到文档或验收报告 |
| logcat 输出 | 过滤掉含 token/密码的行后保留 |

**验证**

每次文档变更通过 `secret-log-scan.py` 扫描；发现即阻断。

---

## ADR-002：Staged/Staged-Only 敏感扫描流程（2026-05-26）

**背景**

发布分支的提交需要快速验证 staged 文件是否有敏感泄漏，同时避免扫描整个 worktree（包含大量与提交无关的文件和生成的 APK/二进制）。

**决策**

采用 staged-only 扫描模式：

1. `git diff --cached --name-only` 获取待提交文件列表。
2. Python 脚本逐一扫描 target patterns（access_token、refresh_token、Bearer、cookie、client_secret、password 赋值等）。
3. 发现任何匹配即退出码 1 并输出 finding metadata（path:line:rule），不输出实际值。
4. PASS 后允许 `git commit`。

**理由**

- 全 worktree 扫描耗时长，且会误报无关文件（如 `artifacts/` 下生成的 APK/log）。
- staged-only 扫描将检查范围精确限定为本次提交的文件变更。

---

## ADR-001：Android 验收以模拟器 + 端到端 Token 验证为主（2026-05-15）

**背景**

真机不足且 ADB `input text` 在 Android 14+ 失效，导致 App 登录页验收困难。

**决策**

1. **以 Android 模拟器（Pixel 9 Pro API 36）作为主要验收设备**，不接受因真机缺失而阻塞验收。模拟器不可用的场景（微信登录授权、实际网络切换、指纹/生物识别）标记为 Known Limitations。
2. **绕过输入法限制**：通过 `curl http://localhost:8080/connect/token` 获取 token 验证登录链路成功，再在 App 内执行后续操作验收。登录截图不依赖 `input text` 输入的过程。
3. **验收证据**：截图展示 App 正常启动页面和登录后页面，logcat 确认无崩溃。

**理由**

- `adb shell input text` 在 Android 14+ 的默认 GMS 输入法下不可靠，改为非 GMS 镜像或 ADB Keyboard 的维护成本不值得计入产品交付。
- 产品验收重点在"用户能否完成闭环"，而非"自动化输入能否在模拟器上工作"。

---
