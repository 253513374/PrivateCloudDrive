# Private Backup D7 发布闸门口径

日期：2026-05-22
适用范围：PrivateCloudDrive 手机优先私有备份 MVP / Public RC 前内部闸门

## 1. 结论

D7 不以“功能数量”作为通过标准，而以“Android 手机优先私有备份可信闭环可见证据”作为发布闸门。只有当用户可以从 App 和公开文档中理解以下事实时，才允许升级最终人工验收：

1. 手机端能连接自己部署的后端并完成账号登录。
2. 照片、视频和普通文件可以进入备份/上传队列。
3. 队列能解释等待、上传中、完成、失败、可重试和登录过期等状态。
4. 上传成功的文件能在文件页命中，并可预览或下载。
5. 后端不可达、登录过期、容量/健康异常等失败状态有可读文案，不暴露 token、cookie、私有 URL、绝对路径、bucket 或原始异常。
6. 用户能知道数据保存在哪里、默认备份覆盖什么、恢复责任由谁承担。

## 2. D7 PASS 必备项

| 编号 | 闸门项 | 通过标准 | 当前文档入口 |
| --- | --- | --- | --- |
| D7-P0-01 | 本地栈/后端健康 | Docker Compose、数据库迁移、API、Swagger、media-worker、storage、ffmpeg/ffprobe 可验证 | `docs/deployment.md`、`docs/testing.md` |
| D7-P0-02 | Android 启动与登录 | clean install 后不崩溃；能连接本地或局域网后端；登录错误不泄漏敏感值 | `docs/testing.md` |
| D7-P0-03 | 手动备份入口 | 用户能选择照片/视频/文件并看到任务进入队列 | `docs/private-backup-d2-ux-blueprint-2026-05-22.md` |
| D7-P0-04 | 队列状态与失败重试 | 等待、上传中、完成、失败、可重试、登录过期有稳定状态与文案 | `docs/private-backup-trusted-loop-scenario-matrix-2026-05-22.md` |
| D7-P0-05 | 文件页命中与下载/预览 | 备份成功后文件出现在目标目录，支持基础预览/下载或可解释失败 | `docs/testing.md` |
| D7-P0-06 | 存储位置与容量健康 | FileSystem 默认路径、Aliyun OSS 可选边界、MinIO Not Now 口径清晰 | `docs/private-backup-storage-boundary-audit-2026-05-22.md` |
| D7-P0-07 | 恢复说明 | 默认备份覆盖 PostgreSQL + FileCenter storage；恢复前必须 dry-run；生产恢复需明确授权 | `docs/disaster-recovery.md` |
| D7-P0-08 | 隐私/安全红线 | 不承诺 E2EE/零知识；不暴露密钥、token、cookie、私有 URL 或真实用户隐私内容 | `docs/private-backup-privacy-trust-copy-2026-05-22.md` |
| D7-P0-09 | 发布证据包 | Android 最终验收报告、截图/日志索引和 release gate 报告形成单一入口 | `docs/release-gate-d7-decision-2026-05-26.md` |

## 3. P1 / PASS with WARN

以下能力可以作为 P1 或 PASS with WARN，不阻断手机优先私有备份可信闭环，但必须在 release notes / known limitations 中说明：

- Aliyun OSS：可作为可选 Provider，但 bucket/object 数据不在默认 `storage.tar.gz` 内，需要部署者独立保护。
- 微信登录：依赖真实开放平台应用、包名签名、AppSecret 和真机环境；默认不作为 Private Backup D7 必备项。
- iOS/真实设备全量回归：可按发布范围补验；D7 的最低证据以 Android 模拟器/设备可见链路为主。
- 软键盘遮挡：若模拟器无法弹出软键盘，应在真机或可弹键盘环境补充记录。

## 4. Not Now

D7 明确不包含：

- NAS OS、RAID、磁盘池、SMB/NFS。
- 桌面同步客户端、后台自动相册备份。
- AI 相册、AI 搜索、OCR、人物识别。
- 多节点高可用、企业审批流、Office 在线协作。
- 端到端加密或零知识加密承诺。
- App 内一键恢复服务器；服务器恢复仍由管理员按 DR Runbook 执行。

## 5. 发布前红线

出现以下任一情况，不得升级用户最终验收：

1. main CI 或 Security Gate 红灯且无明确外部基础设施豁免说明。
2. `secret-log-scan.py --include-working-tree` 对发布范围仍有未解释 finding。
3. GitHub Issue #1/#4/#5 等发布主线问题仍为 OPEN 且无非阻断裁剪说明。
4. Android 最终验收包仍保留“登录后截图未完成”等过期阻塞结论。
5. README、deployment、DR、known limitations、release notes 与父任务口径不一致。
6. 公开文档、日志、截图或验收报告出现完整 token、cookie、AppSecret、私有 URL、真实 bucket/object key、绝对服务器路径、`.env` 原文或用户真实隐私文件内容。
