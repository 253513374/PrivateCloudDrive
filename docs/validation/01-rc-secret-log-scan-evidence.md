# V1.0 RC Secret/Log Scan 验证证据

日期：2026-06-17
执行人：丁 DevOps / devops-eng
工具：`scripts/secret-log-scan.py`

---

## 验证 1：Tracked files + Archive guard — 必过门禁

命令：`python scripts/secret-log-scan.py --archive-ref HEAD`

结果：**SECRET/LOG SCAN PASS: 0 findings**
- 619 个 tracked 路径已检查
- Archive guardrail PASS（git archive HEAD 不含 .env/.secret 文件）

## 验证 2：Working tree（含 untracked）— 复核门禁

命令：`python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD`

结果：**SECRET/LOG SCAN FAIL: 1 finding**（需复核）

| Finding | 原因 | 复核结论 |
|---------|------|---------|
| `aspnet-core/src/PrivateCloudDrive.Application/Deployment/DeploymentHealthCheckService.cs:35 [SECRET_ASSIGNMENT]` | 匹配到 `"client_secret"` 字符串。该行定义安全检查哨兵列表（ForbiddenSensitiveMarkers），用于检测和脱敏敏感值，**不是实际 secret 泄露**。 | **误报（false positive）**。文件为 untracked（未跟踪），不属于当前 RC 工作区变更，不会进入 release archive。追踪后需添加 `# secret-log-scan-allowlist` 注释。 |

## 验证 3：`.env` / `.secrets` 跟踪检查

命令：`git ls-files -- .env .env.secret .secrets`

结果：**无跟踪文件**（PASS）

---

## 脱敏说明

本文件不包含任何 secret、token、password 或 access key。

## 门禁结论

| 检查项 | 状态 | 说明 |
|--------|------|------|
| Tracked files scan | PASS | 0 findings |
| Working tree scan | FAIL (1) | 仅误报（false positive），已复核 |
| Archive guard | PASS | git archive 无敏感文件 |
| `.env`/`.secrets` tracked | PASS | 无跟踪文件 |
| **总体** | **PASS（有条件）** | Working tree scan 的 1 个 finding 已复核确认为误报；文件未跟踪且不进入发布包 |
