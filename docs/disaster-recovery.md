# 备份恢复与灾难恢复 Runbook

本文档是 PrivateCloudDrive 公开仓库的灾难恢复（DR）入口，用于回答三个问题：备份了什么、恢复会改动什么、恢复后如何证明数据可用。

## 目标与边界

| 项目 | 当前策略 |
|---|---|
| 目标场景 | 单机 Docker Compose 私有部署、家庭/个人/小团队自托管实例 |
| 核心目标 | PostgreSQL 元数据 + FileCenter 本地 storage volume 可恢复 |
| 默认 RPO | 由部署者的备份频率决定；建议至少每日一次，重要数据导入后立即手动备份 |
| 默认 RTO | 小型实例按“准备环境 + 恢复 + 冒烟验证”估算；首次演练前不要承诺固定时长 |
| 不覆盖 | NAS RAID、跨机高可用、云厂商 OSS bucket 内对象备份、第三方 OAuth 平台状态 |

当前脚本面向本地 Compose 项目。生产环境可以复用脚本模式，但必须先在一次性测试栈完成破坏性恢复演练，再进入真实生产恢复。

## 数据资产清单

| 数据 | 默认位置 | 是否由默认备份覆盖 | 恢复意义 |
|---|---|---:|---|
| PostgreSQL 数据库 | `privateclouddrive_stack_postgres_data` | 是，导出为 `postgres.dump` | 账号、权限、文件索引、分享、媒体状态、审计日志 |
| FileCenter 本地文件与临时区 | API 容器 `/app/storage` 对应的真实 Docker volume | 是，归档为 `storage.tar.gz` | 文件正文、上传临时分片、缩略图、视频封面、媒体处理临时文件 |
| `.env` | 仓库根目录本地文件 | 默认不覆盖 | 数据库密码、加密短语、PUBLIC_URL、对象存储/外部登录密钥 |
| Redis | `privateclouddrive_stack_redis_data` | 默认不覆盖，可选 `-IncludeRedis` | 缓存、限流计数、临时票据；通常不是灾备核心 |
| MinIO profile | `privateclouddrive_stack_minio_data` | 默认不覆盖，可选 `-IncludeMinio` | 仅当部署者启用 MinIO profile 并将对象放入该 volume 时才需要 |
| Aliyun OSS bucket | 云厂商 bucket | 不覆盖 | 需要云厂商版本控制、复制、生命周期或独立对象备份 |

重要：`.env.secret` 只允许出现在加密、访问受控的备份介质中，禁止提交到 Git、Issue、日志或演练报告。

## 日常备份流程

1. 确认本地 Compose 栈可用：

```powershell
docker compose ps
.\scripts\verify-local-stack.ps1 -SkipStart
```

2. 执行非破坏性演练（推荐作为日常巡检）：

```powershell
.\scripts\run-backup-restore-drill.ps1
```

3. 如果只需要创建备份，不需要生成演练报告：

```powershell
.\scripts\backup-local-stack.ps1 -OutputDirectory .\artifacts\backups
```

4. 如确实需要把 `.env` 一起打包，只能在加密备份介质上执行：

```powershell
.\scripts\backup-local-stack.ps1 -OutputDirectory E:\EncryptedBackups\PrivateCloudDrive -IncludeEnv
```

5. 备份完成后检查备份目录至少包含：

```text
manifest.json
postgres.dump
storage.tar.gz
ENVIRONMENT-REQUIRED.md   # 或 .env.secret，但 .env.secret 不能提交
```

`manifest.json` 会记录实际解析到的 storage Docker volume 名，避免 Compose 逻辑 volume 名和运行时 volume 名不一致导致备份错目录。

## 恢复 dry-run

任何恢复前都先运行 dry-run。dry-run 只校验输入和打印计划，不会覆盖数据库或 volume：

```powershell
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260518-193510
```

通过条件：

- `manifest.json`、`postgres.dump`、`storage.tar.gz` 均存在且非空。
- Compose 配置合法。
- 输出明确列出将停止的服务、将恢复的数据库、将替换的 storage volume。
- 输出 `Summary: PASS ... / FAIL 0`。

## 破坏性恢复流程（只允许测试栈或明确授权的目标栈）

警告：以下命令会覆盖目标 PostgreSQL 数据和 storage volume。生产实例恢复前必须完成外部备份留存，并确认目标主机、Compose project、`.env` 与备份匹配。

1. 准备目标环境：

```powershell
Copy-Item .env.example .env
# 按备份实例重建 .env，尤其是 POSTGRES_DB / POSTGRES_USER / POSTGRES_PASSWORD / STRING_ENCRYPTION_PASSPHRASE / PUBLIC_URL / FILECENTER_STORAGE_PROVIDER
```

2. 先 dry-run：

```powershell
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260518-193510
```

3. 在测试栈执行破坏性恢复：

```powershell
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260518-193510 -ConfirmDestructiveRestore
```

如使用一次性 Compose project 名称验证恢复，请显式使用当前 Compose project 的新 volume，避免误写回备份 manifest 记录的源 volume：

```powershell
$env:COMPOSE_PROJECT_NAME = "pcd-drill-20260518"
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260518-193510 -ConfirmDestructiveRestore -UseCurrentComposeProjectVolumes
```

`-UseCurrentComposeProjectVolumes` 会忽略备份 manifest 中记录的源 Docker volume 名，并按当前 `COMPOSE_PROJECT_NAME` / `docker compose config` 解析目标 volume。该参数只影响 volume 目标选择，不会降低 `-ConfirmDestructiveRestore` 的显式确认要求。

4. 如备份中包含 Redis 或 MinIO，按需显式开启：

```powershell
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260518-193510 -ConfirmDestructiveRestore -RestoreRedis -RestoreMinio
```

脚本会停止 API、media-worker、db-migrator、MinIO，启动 PostgreSQL/Redis，执行 `pg_restore --clean --if-exists --no-owner --no-privileges`，清空并恢复 storage volume，然后默认启动完整栈并运行 `verify-local-stack.ps1 -SkipStart`。

## 恢复后验收清单

| 验收项 | 操作 | 通过标准 | 证据 |
|---|---|---|---|
| 栈健康 | `.\scripts\verify-local-stack.ps1 -SkipStart` | PASS 汇总，PostgreSQL/Redis/API/media-worker/storage/ffmpeg 均可用 | 命令输出或日志 |
| 登录 | 用测试账号登录 Web/MAUI 客户端 | 登录成功，失败审计不泄露密码/token | 截图或接口状态码 |
| 文件列表 | 打开根目录和至少一个子目录 | 文件/文件夹数量与备份前样本一致 | 截图/记录样本文件名 |
| 下载 | 下载一个小文件 | 文件可下载，大小和哈希符合预期 | 文件名、大小、SHA256 |
| 图片预览 | 打开图片或缩略图 | 图片可加载；缩略图不存在时可重新生成或显示可读状态 | 截图 |
| 视频预览 | 打开 MP4 并拖动进度 | Range 请求可用，播放不报错 | 截图/接口状态码 |
| 分享链接 | 打开一个有效分享，验证密码保护（如有） | 有效分享可访问；过期/禁用分享不可访问 | 分享 token 后四位或脱敏 URL |
| 回收站 | 查看回收站并恢复一个测试文件 | 恢复到原目录，同名冲突提示清楚 | 截图/操作记录 |
| 审计与安全 | 查看登录/下载/分享相关审计 | 不包含密码、access token、refresh token、OAuth code、client secret | 脱敏日志片段 |

## 演练证据记录规范

演练报告应放在 `docs/validation/backup-restore-drill-YYYYMMDD-HHMMSS.md`，并只记录：

- 执行时间、模式、备份目录相对/本地路径。
- PASS/WARN/FAIL 汇总。
- 备份文件名和字节数。
- dry-run 或测试栈破坏性恢复的命令输出摘要。
- 恢复后手动验收结果和脱敏证据。

禁止记录：

- `.env` 原文、`.env.secret` 内容、数据库密码、对象存储密钥、OAuth client secret。
- access token、refresh token、OAuth code、微信/Google/GitHub access token。
- 用户真实私密文件内容、完整公开分享 URL、可用于绕过权限的 Cookie/Header。

## D7 回滚/DR 发布检查清单

发布复审前请确认：

- 默认恢复链路仍以 PostgreSQL + FileCenter storage volume + 匹配 `.env` 为最小集合。
- 恢复 dry-run 先于任何破坏性恢复执行；生产恢复必须由管理员明确授权。
- Aliyun OSS bucket/object 不在默认 `storage.tar.gz` 覆盖范围内，需云侧独立保护。
- MinIO profile 仍为 Not Now / optional service 口径，除非完成 FileCenter Provider 级验证。
- 验收报告只记录脱敏证据，不包含 `.env` 原文、token、cookie、AppSecret、AccessKey、完整分享 URL、真实 bucket/object key 或用户真实隐私文件内容。
- 已知限制入口见 [known-limitations.md](known-limitations.md)，发布闸门入口见 [private-backup-d7-release-gate-2026-05-22.md](private-backup-d7-release-gate-2026-05-22.md)。

## 当前公开演练证据

- `docs/validation/backup-restore-drill-20260518-193513.md`：已完成非破坏性备份 + 恢复 dry-run，PASS 14 / WARN 0 / FAIL 0；证明当前控制路径能生成并校验 `manifest.json`、`postgres.dump`、`storage.tar.gz` 和 `ENVIRONMENT-REQUIRED.md`。
- `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md`：已完成一次性 Compose project `pcd_drill_test` 的破坏性恢复验收，restore PASS 14 / WARN 1 / FAIL 0；恢复后红线烟测覆盖登录、文件列表、上传、Range 下载/内容预览、公开分享、回收站恢复和审计脱敏样本。

以上报告证明本地 Compose 公开部署路径已具备“备份、dry-run、一次性测试栈破坏性恢复、恢复后核心链路验收”的公开证据。真实生产恢复前仍必须先保留事故现场备份，确认目标主机、Compose project、`.env` 与备份匹配，并按本 Runbook 重新执行一次面向目标环境的脱敏验收。
