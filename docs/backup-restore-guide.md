# 备份恢复指南

> **本文档是 PrivateCloudDrive 备份恢复的快速操作入口**。完整灾难恢复 Runbook 见 [disaster-recovery.md](disaster-recovery.md)，部署数据安全说明见 [deployment.md](deployment.md#存储目录与数据安全)。

## 备份三件套

PrivateCloudDrive 的最小可恢复备份由三部分组成，**缺一不可**：

| 数据资产 | 默认位置 | 备份内容 | 恢复意义 |
|----------|---------|---------|---------|
| **PostgreSQL 数据库** | `privateclouddrive_stack_postgres_data` (Docker volume) | `pg_dump --format=custom` → `postgres.dump` | 用户账号、权限、文件索引、分享、相册、媒体处理状态、审计日志 |
| **FileCenter storage** | API 容器 `/app/storage` 对应的 Docker volume | `tar.gz` 归档 → `storage.tar.gz` | 文件正文、上传临时分片、缩略图、视频封面、媒体处理临时文件 |
| **环境配置** | 仓库根目录 `.env`（主机文件系统） | `-IncludeEnv` 参数 → `.env.secret` | 数据库密码、加密短语、`PUBLIC_URL`、存储提供商凭证、外部登录密钥 |

> ⚠️ **只有三件套一起恢复才能在全新环境中还原可用实例。** 仅恢复 DB 或仅恢复 storage 都无法独立工作。

### 各数据资产独立说明

**PostgreSQL 数据库**：存储所有元数据 — 用户、文件夹结构、文件索引、分享记录、相册、媒体处理任务状态、操作审计日志。丢失后无法恢复文件索引和分享链接，即使文件实体仍在 storage 中。

**FileCenter storage**：文件实体本身。使用默认 `FileSystem` 存储时，文件保存在 Docker volume 中。使用 `AliyunOss` 时，storage volume 只存放上传临时分片和媒体处理临时文件；OSS bucket 内的对象不由 `storage.tar.gz` 覆盖，需部署者独立管理云侧备份。

**环境配置**：部署者自定义的 `.env` 文件包含数据库密码、`STRING_ENCRYPTION_PASSPHRASE`、`PUBLIC_URL`、`FILECENTER_STORAGE_PROVIDER` 及凭据。恢复时 `.env` 必须与备份时的实例匹配。**默认备份不会包含 `.env`**，因为其包含敏感信息；仅在显式使用 `-IncludeEnv` 参数时才会复制，且备份目录必须存入加密、访问受控的存储，禁止提交到 Git。

### 边界说明

| 场景 | 备份覆盖 | 不覆盖 |
|------|---------|--------|
| 默认备份 | PostgreSQL dump + storage.tar.gz + manifest | `.env`、Redis 缓存、MinIO volume、OSS bucket 对象 |
| `-IncludeEnv` | 增加 `.env.secret` 副本 | — |
| `-IncludeRedis` | 增加 `redis-dump.rdb` | — |
| `-IncludeMinio` | 增加 `minio.tar.gz` | — |
| Aliyun OSS 场景 | storage.tar.gz 只覆盖本地临时区 | OSS bucket 内对象（需云侧启用版本控制/复制） |

---

## 日常备份

创建完整备份：

```powershell
.\scripts\backup-local-stack.ps1 -OutputDirectory .\artifacts\backups
```

备份目录输出包含：

- `manifest.json` — 提交号、备份时间、文件清单、SHA256 校验、PASS/WARN/FAIL 汇总
- `postgres.dump` — PostgreSQL 逻辑备份
- `storage.tar.gz` — FileCenter storage volume 归档
- `ENVIRONMENT-REQUIRED.md` — 恢复所需环境变量说明（不含 secret 值）

如果需要同时备份 `.env`（仅限加密备份介质）：

```powershell
.\scripts\backup-local-stack.ps1 -OutputDirectory E:\EncryptedBackups\PrivateCloudDrive -IncludeEnv
```

**验证备份完整性：**

```powershell
.\scripts\run-backup-restore-drill.ps1
```

该命令会创建备份、校验 `manifest.json` / `postgres.dump` / `storage.tar.gz`、执行恢复 dry-run，并在 `docs/validation/` 生成演练报告。所有操作均非破坏性，不会覆盖任何数据。

---

## 恢复 dry-run

任何恢复前**必须先执行 dry-run**。dry-run 只校验备份文件和打印恢复计划，不写入数据库或 volume：

```powershell
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260518-193510
```

通过条件：

- `manifest.json`、`postgres.dump`、`storage.tar.gz` 均存在且非空
- Compose 配置合法
- 输出明确列出将停止的服务、将恢复的数据库、将替换的 storage volume
- 输出 `Summary: PASS ... / FAIL 0`

---

## 破坏性恢复

> ⚠️ **只允许在一次性测试栈或明确授权的目标栈上执行。** 生产恢复前必须先保留事故现场备份。

在测试栈执行：

```powershell
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260518-193510 -ConfirmDestructiveRestore
```

使用独立 Compose project 恢复：

```powershell
$env:COMPOSE_PROJECT_NAME = "pcd-drill-test"
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260518-193510 -ConfirmDestructiveRestore -UseCurrentComposeProjectVolumes
```

恢复脚本自动执行：

1. 停止 API、media-worker、db-migrator、MinIO
2. 启动 PostgreSQL/Redis
3. `pg_restore --clean --if-exists --no-owner --no-privileges`
4. 清空并恢复 storage volume
5. 默认启动完整栈并运行 `.\scripts\verify-local-stack.ps1 -SkipStart`

---

## 恢复后验收清单

| 验收项 | 操作 | 通过标准 |
|--------|------|----------|
| 栈健康 | `.\scripts\verify-local-stack.ps1 -SkipStart` | PASS 汇总，所有组件可用 |
| 登录 | 用测试账号登录 MAUI 客户端或调用 `/connect/token` | 登录成功 |
| 文件列表 | 打开根目录和至少一个子目录 | 文件/文件夹数量与预期一致 |
| 下载 | 下载一个小文件 | 文件可下载，大小符合预期 |
| 图片预览 | 打开图片或缩略图 | 图片可加载 |
| 视频播放 | 打开 MP4 并拖动进度 | Range 请求可用，播放正常 |
| 分享链接 | 打开一个有效分享 | 有效分享可访问 |
| 回收站 | 查看回收站并恢复一个测试文件 | 恢复成功，回到原位置 |
| 审计脱敏 | 查看操作日志 | 不包含密码/token/secret |

---

## 快速参考

| 操作 | 命令 | 说明 |
|------|------|------|
| 一键备份 + 演练 | `.\scripts\run-backup-restore-drill.ps1` | 非破坏性，推荐日常巡检 |
| 仅备份 | `.\scripts\backup-local-stack.ps1` | 输出到默认目录 |
| 备份 + 环境变量 | `.\scripts\backup-local-stack.ps1 -IncludeEnv` | 仅加密介质 |
| 恢复 dry-run | `.\scripts\restore-local-stack.ps1 -BackupDirectory <dir>` | 无实际写入 |
| 破坏性恢复 | 同上 + `-ConfirmDestructiveRestore` | 测试栈专用 |
| 演练证据 | 查看 `docs/validation/backup-restore-drill-*.md` | 现有演练报告 |

---

## 相关文档

- [部署说明](deployment.md) — 数据安全与 BR-ST 说明
- [灾难恢复 Runbook](disaster-recovery.md) — 完整 DR 流程
- [发布说明 V1.3](release-notes-v1.3.md) — V1.3 备份恢复新能力
- [测试说明](testing.md) — 备份恢复验证命令
- `docs/validation/backup-restore-drill-20260518-193513.md` — 非破坏性演练证据
- `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md` — 测试栈破坏性恢复证据
