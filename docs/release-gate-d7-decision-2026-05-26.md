# D7 发布闸门裁决：V1.0 Public RC / Private Backup MVP

- 裁决时间：2026-05-26 20:21:02 +0800
- 本地收口复审：2026-05-28
- 裁决人：芮发布（release-manager）
- 目标范围：Private Backup MVP / V1.0 Public RC 阶段是否可升级用户做最终人工验收

## 1. 最终裁决

结论：PASS with WARN for local evidence chain / Public RC 仍暂不升级用户最终验收。

2026-05-28 本地复审已基于最新 `origin/main` 补齐 D7 发布可信证据链的可复跑脚本、Security Gate 工作流和 Android 统一证据包口径；但 Public RC 升级仍依赖远端 Issue/PR 收敛与目标提交上的 CI 复验。当前状态不再是“缺少本地证据链”，而是“本地证据链 PASS with WARN，远端治理项仍需合并或裁剪”。

当前仍需发布经理复核的事项：

1. GitHub main 最新 CI 与 Security Gate 已查询为 success（head SHA `0de2147e5754f2f5cddb8aaff011964eb5f8f9ae`），但本地新增的证据链修复还需进入目标提交后重新跑远端门禁。
2. GitHub Issue #1/#4/#5 仍为 OPEN，需要关闭或明确裁剪为非阻断。
3. 安全、Android、文档相关 PR 仍有多条 OPEN；与本地修复重叠的 PR 需要合并、关闭或重新基于当前方案整理。
4. Android 可见验收包已形成统一 PASS with WARN 报告；真机相册权限、大视频、后台续传、弱网和 OEM 差异仍是 WARN/后续补证项。
5. D1/D2 产品、UX、存储、隐私等父任务交付物已进入文档包入口，但仍需 release-manager 在最终提交上复核 README、testing、deployment、disaster-recovery、known limitations、release notes 口径一致。

## 2. 已验证证据摘要

| 类别 | 当前证据 | 裁决 |
| --- | --- | --- |
| Swagger / 公开分享路由冲突 | PR #25 已合并到 main；本地定向测试退出码 0 | 技术 blocker 初步清零，但需 main CI 绿灯复核 |
| CI | 2026-05-27 23:00:40 +0800 查询 main 最新 CI run [`26448677475`](https://github.com/253513374/PrivateCloudDrive/actions/runs/26448677475) 为 success，head SHA `0de2147e5754f2f5cddb8aaff011964eb5f8f9ae` | PASS，需要目标提交复验 |
| Security Gate | 2026-05-27 23:00:40 +0800 查询 main 最新 Security Gate run [`26448677495`](https://github.com/253513374/PrivateCloudDrive/actions/runs/26448677495) 为 success，head SHA `0de2147e5754f2f5cddb8aaff011964eb5f8f9ae` | PASS，需要目标提交复验 |
| Validation evidence sensitive-data gate | 本地 `validation_evidence_index.py --run-id codex-d7-gate-local-r2` dry-run：status PASS，evidence_count 17，finding_count 0 | PASS |
| Secret/log scan | 本地 `secret-log-scan.py --include-working-tree --archive-ref HEAD`：0 findings，598 个 tracked/未忽略 working-tree 文本路径已检查，archive guardrail PASS（archive guard 仅覆盖当前 HEAD） | PASS，需要目标提交复验 |
| Git diff 空白检查 | `git diff --check` PASS | PASS |
| Docker Compose 配置 | `docker compose config --quiet` PASS | PASS |
| Android 可见证据 | `docs/validation/android-backup-release-evidence.md` 汇总 slice1~slice5、容量/健康、失败重试、恢复/隐私边界 | PASS with WARN |
| 备份/恢复 | 破坏性测试栈报告与 smoke 证据存在 | PASS with WARN |

## 3. 发布阻塞项

### R1：发布基础设施目标提交复验（阻断）

main CI 与 Security Gate 当前查询为成功，但本地证据链修复尚未进入目标提交；Public RC 通过前必须在最终提交上重新跑远端 CI 与 Security Gate。当前 `--archive-ref HEAD` 只证明已提交 HEAD 的 archive path guardrail，本地新增文件进入提交后仍需重新扫描。

### R2：安全门禁未收敛（阻断）

Issue #5 仍 open，安全相关 PR 未全部合并、关闭或裁剪。本地 secret/log scan 已为 PASS，但需要以目标提交的 CI 结果作为发布证据。

### R3：Android 最终可见验收包 PASS with WARN（非阻断，需发布说明）

统一报告已覆盖登录、照片/文件备份、队列、失败重试、下载/预览、删除/恢复补强、容量/健康、恢复说明和隐私边界。发布说明必须保留真机相册权限、大视频、后台续传、弱网和 OEM 差异的 WARN 口径。

### R4：父任务关键文档未全部进入 main（阻断/治理）

D7、D2 UX、D1 场景矩阵、存储边界、隐私文案等文档必须在目标提交中从 README / release notes / known limitations / DR 入口可达，并通过最终 `git diff --check` 与敏感扫描。

## 4. 回滚/降级建议

若必须对外展示当前阶段，只能降级为“内部 RC / evidence hardening build”，不得称为 Public RC：

- 不发布 public release/tag。
- 不要求用户最终验收。
- 仅允许内部员工继续验证、修复和整理证据。
- 对外文档继续标注 Not Now、非 E2EE、FileSystem 为 P0 可交付路径、Aliyun OSS PASS with WARN、MinIO Not Now。

## 5. 下一轮复审入口

下一轮 D7 复审前，release-manager 应至少重新核对：

1. main CI / Security Gate 均为绿灯或存在可审计外部豁免。
2. Issue #1/#4/#5 可关闭或明确裁剪为非阻断。
3. Android 最终可见验收包为 PASS / PASS with WARN，且无过期阻塞文案。
4. 本文档包中的父任务口径已进入 main，且 `docs/known-limitations.md`、`docs/release-notes-private-backup-d7.md`、`docs/disaster-recovery.md` 链接一致。
5. 敏感信息扫描无未解释 finding。
