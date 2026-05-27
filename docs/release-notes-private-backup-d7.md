# Private Backup MVP D7 Release Notes（内部 RC / Evidence Hardening）

日期：2026-05-26
本地收口更新：2026-05-28
发布类型：Internal RC / Evidence Hardening Build
最终用户验收状态：暂不升级，等待 D7 阻塞项清零

## 1. 产品定位

本阶段聚焦“手机优先私有备份可信闭环”：用户通过 Android App 连接自托管后端，手动备份照片、视频和普通文件，理解队列状态、失败重试、存储位置、恢复责任和隐私边界。

## 2. 当前已进入文档包的发布口径

| 口径 | 文档 |
| --- | --- |
| D7 发布闸门 | `docs/private-backup-d7-release-gate-2026-05-22.md` |
| D1 可信闭环场景矩阵 | `docs/private-backup-trusted-loop-scenario-matrix-2026-05-22.md` |
| D2 UX 蓝图 | `docs/private-backup-d2-ux-blueprint-2026-05-22.md` |
| 存储边界审计 | `docs/private-backup-storage-boundary-audit-2026-05-22.md` |
| 隐私信任文案 | `docs/private-backup-privacy-trust-copy-2026-05-22.md` |
| D7 发布裁决 | `docs/release-gate-d7-decision-2026-05-26.md` |
| 灾备 Runbook | `docs/disaster-recovery.md` |
| 已知限制 | `docs/known-limitations.md` |

## 3. 本阶段能力摘要

- Docker Compose 本地私有部署路径：PostgreSQL、Redis、API、media-worker、FileCenter storage、ffmpeg/ffprobe。
- 后端文件基础能力：文件/文件夹、上传下载、Range、预览、回收站、分享、标签/收藏、审计。
- Android/MAUI 客户端基础能力：登录、文件页、上传队列、设置、容量/健康摘要入口。
- Private Backup 可信闭环口径：备份中心、队列状态、失败重试、文件页命中、恢复说明和隐私边界。
- DR 证据：默认覆盖 PostgreSQL + FileCenter storage 的备份/恢复脚本、dry-run 和一次性测试栈破坏性恢复报告。

## 4. 当前不得称为 Public RC 的原因

| 阻塞 | 状态 | 处理方向 |
| --- | --- | --- |
| main CI / Security Gate 红灯 | 2026-05-27 23:00:40 +0800 已查询 main 最新 CI run `26448677475` 与 Security Gate run `26448677495` 为 success，head SHA `0de2147e5754f2f5cddb8aaff011964eb5f8f9ae`；仍需目标提交复验 | 合并本地证据链修复后重新跑 CI / Security Gate |
| Issue #1/#4/#5 仍 OPEN | 阻断 | 收敛 issue、PR 与发布裁剪说明 |
| Issue #5 安全门禁/secret scan finding | 本地 `secret-log-scan.py --include-working-tree --archive-ref HEAD` PASS，0 findings，598 个 tracked/未忽略 working-tree 文本路径已检查；archive guard 仅覆盖当前 HEAD | 合并/关闭 stale PR，并以目标提交 CI 结果作为发布证据 |
| Android 最终可见验收包未形成单一 PASS 报告 | 已形成 `docs/validation/android-backup-release-evidence.md`，结论 PASS with WARN | 发布说明保留真机相册权限、大视频、后台续传、弱网和 OEM 差异 WARN |
| 文档断层 | 本任务收口 | 父任务关键口径已重建为 main 文档包入口 |

## 5. 已知限制和 Not Now

详见 `docs/known-limitations.md`。重点包括：不承诺 E2EE/零知识、MinIO 不是当前默认 FileCenter Provider、Aliyun OSS bucket 不由默认 `storage.tar.gz` 备份覆盖、NAS/RAID/SMB/NFS/桌面同步/AI 相册/企业协作均为 Not Now。

## 6. 回滚 / 降级

当前阶段如需展示，只能使用“内部 RC / evidence hardening build”口径。不得发布 public tag，不得要求用户最终验收。若部署验证失败，按 `docs/disaster-recovery.md` 先做 dry-run，再在一次性测试栈执行破坏性恢复验证；生产恢复必须由管理员明确授权。
