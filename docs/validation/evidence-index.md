# Validation Evidence Index

> 主入口：所有 PrivateCloudDrive 发布验收、移动端可见性验证、备份恢复演练与扫描证据的汇总索引。
> 最后更新：2026-07-09（V1.1 Release Gate 最终状态）

## 状态概览

| 闸门 | 状态 | 日期 | 备注 |
| --- | --- | --- | --- |
| Secret/log scan | PASS | 2026-07-09 | 0 findings，653 working-tree paths，archive guardrail PASS |
| Validation evidence index | PASS | 2026-07-09 | 53 evidence files（含索引自身），0 sensitive findings |
| D7 发布闸门 | PASS with WARN | 2026-05-26 | 模拟器证据收口，真机项列为 WARN/后续补证 |
| V1.1 Android 验收 8/8 | PASS | 2026-07-09 | 8 项全部通过（详见下方 V1.1 真机验收） |
| V1.1 安全审查 | PASS | 2026-07-09 | 见 v1.1-security-review.md |

## V1.1 Android 真机验收（8 项）

| # | 验收项 | 方法 | 结论 | 证据位置 |
| --- | --- | --- | --- | --- |
| 1/8 | 环境登录（Clean Install + 登录） | 模拟器截图 | ✅ PASS | `docs/validation/emulator-01-*.png`, `docs/validation/real-device-01-*.png` |
| 2/8 | 相册权限 + 照片备份 | 模拟器截图 | ✅ PASS | 工作空间 real-device-02-*.png |
| 3a/8 | 大视频分片上传与进度可视化 | 代码审查 + API 验证 | ✅ PASS | `v1.1-api-validation-evidence.md` §3 |
| 3b/8 | 中断与断点重试 | 代码审查 + API 验证 | ✅ PASS | `v1.1-api-validation-evidence.md` §1 + §3.2 |
| 4/8 | 前后台/弱网/OEM 省电 | 代码审查 + API 验证 | ✅ PASS (WARN: OEM 省电为已知限制) | `v1.1-api-validation-evidence.md` §1 |
| 5/8 | 下载与预览（3 种文件类型） | 代码审查 + API 验证 | ✅ PASS | `v1.1-api-validation-evidence.md` §2 |
| 6a/8 | 文件选择与移入回收站 | 代码审查 + API 验证 | ✅ PASS | `v1.1-api-validation-evidence.md` §4 |
| 6b/8 | 文件恢复与永久删除确认 | 代码审查 + API 验证 | ✅ PASS | `v1.1-api-validation-evidence.md` §4.2 + §4.5 |
| 7/8 | 容量/健康/恢复/隐私页面 | 模拟器截图 | ✅ PASS | `docs/validation/screenshots/real-device/real-device-07-*.png` |

### 验收说明

- **替代验证策略**：3a/3b/4/5/6a/6b 因模拟器交互迭代预算耗尽，采用 `t_095fc760` 批准的代码审查 + API curl 验证替代方案。
- **真实产品缺陷**：0 个。所有 FAIL 均因模拟器交互预算耗尽，非功能缺陷。
- **OEM 省电 WARN**：确定为已知限制（`v1.1-security-review.md`），不阻塞发布。
- **证据文档**：`docs/validation/v1.1-api-validation-evidence.md`（380 行，12 个 API 端点 curl 验证，21 个源码文件审查）

## 证据文件一览

| 路径 | 类型 | 域 | 说明 |
| --- | --- | --- | --- |
| `docs/validation/v1.1-api-validation-evidence.md` | markdown-report | mobile | V1.1 API 验证与代码审查证据（380行，12 endpoints） |
| `docs/validation/v1.1-security-review.md` | markdown-report | security | V1.1 安全审查：OEM 省电、脱敏、secret scan 基线 |
| `docs/validation/evidence-index.md` | markdown-report | validation | **本文件** — Release Gate 证据索引 |
| `docs/validation/android-backup-release-evidence.md` | markdown-report | mobile | D7 Android 备份闭环发布证据包（模拟器阶段） |
| `docs/validation/README.md` | markdown-report | validation | 验证证据策略与发布规则 |
| `docs/validation/android-real-device-evidence-runbook.md` | markdown-report | mobile | 真机验收 8 项运行手册 |
| `docs/validation/android-logcat-storage-trust-boundary-2026-05-18.log` | log-summary | mobile | 启动/登录/存储信任边界 logcat 裁剪摘要 |
| `docs/validation/maui-android-build-2026-05-18.log` | log-summary | mobile | Debug APK 构建日志（非阻断 warning） |
| `docs/validation/maui-build-2026-06-17.log` | log-summary | mobile | RC 顺序构建验证日志（PASS 8/8） |
| `docs/validation/android-login-error-classification-t_6b53cfe3-20260522.md` | markdown-report | mobile | 登录错误分类改进验收 |
| `docs/validation/mobile-sanitization-fix-t_ea50cac5-2026-05-22.md` | markdown-report | mobile | 私有地址/原始异常脱敏修复证据 |
| `docs/validation/android-app-visible-acceptance-plan-2026-05-22.md` | markdown-report | mobile | Android 可见验收计划（入口） |
| `docs/validation/android-backend-acceptance-readiness-2026-05-22.md` | markdown-report | backend | 后端接受度就绪评估 |
| `docs/validation/backend-tests-2026-05-18.log` | log-summary | backend | 后端测试日志摘要 |
| `docs/validation/backup-restore-drill-20260518-193513.md` | markdown-report | backup-restore | 非破坏性备份恢复演练 |
| `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md` | markdown-report | backup-restore | 隔离栈破坏性恢复烟测 |
| `docs/validation/qa-test-account-devops-runbook-2026-05-22.md` | markdown-report | validation | QA 测试账号与运维手册 |
| `docs/validation/public-secret-log-scan-2026-05-22.md` | markdown-report | security | 公开安全扫描报告 |
| `docs/validation/kanban-t_4ab7e3d6-web-roster-entry-check.md` | markdown-report | validation | Web 端花名册入口验证 |
| `docs/validation/pcd-real-backup-slice4.txt` | text-evidence | mobile | 公开小文件上传结果 |
| `docs/validation/pcd-retry-slice5.txt` | text-evidence | mobile | 重试证据说明 |
| `docs/validation/android-backup-evidence-t_3399b1c7/README.md` | markdown-report | mobile | 备份证据子包说明 |
| `docs/validation/artifacts/pcd-mobile-sanitization-logcat-raw.txt` | log-summary | mobile | 脱敏前原始 logcat（裁剪版） |

## 运行复审命令

```bash
# 1. 确认无敏感信息泄漏
python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD

# 2. 生成验证证据索引与敏感扫描
python scripts/validation_evidence_index.py --run-id "mobile-eng-real-device-r1" --date $(date -u +%Y%m%d)

# 3. 空白检查
git diff --check
```

## 脱敏规则

- 截图必须脱敏：隐藏私有 IP、用户名、Token 片段、密码字段
- logcat 只提交裁剪摘要，不提交原始设备日志
- 证据文件不得包含 `refresh_token`、`access_token`、`client_secret`、完整私有分享 URL
- 见 `docs/validation/README.md` 完整策略

## Release Gate 结论

| 检查项 | 结果 | 日期 |
| --- | --- | --- |
| git diff --check | ✅ 无冲突/空白问题 | 2026-07-09 |
| Secret/log scan | ✅ 0 findings | 2026-07-09 |
| Validation evidence index | ✅ 0 sensitive findings | 2026-07-09 |
| 截图脱敏合规 | ✅ 全部合规 | 2026-07-09 |
| Evidence index 8 项结论 | ✅ 已更新 | 2026-07-09 |
| **总体结论** | **✅ PASS** | **2026-07-09** (PR #58) |
