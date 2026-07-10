# V1.0 RC 存储边界验证报告

验证日期：2026-06-17
验证人：丁 DevOps / devops-eng
文档定位：RC 存储边界冻结验收——备份恢复最小可恢复资产确认 + FileSystem/OSS/MinIO 边界声明审计

---

## 1. 验证总览

| 验证项 | 结果 | 证据 |
|---|---|---|
| 非破坏性备份恢复演练 | PASS 14 / WARN 0 / FAIL 0 | `docs/validation/backup-restore-drill-20260617-093351.md` |
| 最小可恢复资产确认 | 通过 | PostgreSQL dump (145KB) + storage.tar.gz (185KB) + manifest.json |
| OSS/MinIO 边界文档一致性 | 通过 | 3/3 文档与脚本行为一致 |
| Release Notes 边界声明 | 已更新 | `docs/release-notes-v1.0-rc.md` §3 + §7 |
| 脱敏合规 | 通过 | 演练报告不含明文 secret、token、密码 |

---

## 2. 最小可恢复资产

经 dry-run 确认，以下三组资产是本地 Compose 栈的**最小可恢复集合**：

| 资产 | Docker 位置 | 备份文件名 | 本次大小 |
|---|---|---|---|
| PostgreSQL 元数据 | `pcdlocal_privateclouddrive_stack_postgres_data` | `postgres.dump` | 145,029 bytes |
| FileCenter 本地文件体 | `pcdlocal_privateclouddrive_stack_storage` | `storage.tar.gz` | 185,103 bytes |
| 实例 `.env`（敏感，默认不包含） | 仓库根目录本地文件 | `ENVIRONMENT-REQUIRED.md`（说明文件） | 546 bytes |

**恢复目标**：当前 Compose project `pcdlocal` 的 runtime volume（`pcdlocal_privateclouddrive_stack_storage`）。

**破坏性恢复步骤**（仅在测试栈执行）：
1. `docker compose stop api media-worker db-migrator minio`
2. `docker compose up -d postgres redis`
3. 等待 PostgreSQL ready
4. `pg_restore --clean --if-exists --no-owner --no-privileges` 恢复 `postgres.dump`
5. 用临时 Alpine 容器清空并解包 `storage.tar.gz` 到目标 storage volume
6. `docker compose up -d --build` 启动完整栈
7. 执行 `verify-local-stack.ps1 -SkipStart`
8. 运行 `dr-restore-smoke.py` 执行脱敏红线烟测

**回滚方案**：
- 恢复前使用 `run-backup-restore-drill.ps1` 创建当前数据的完整非破坏性备份
- 恢复时使用 `-UseCurrentComposeProjectVolumes` 和一次性 Compose project 名称隔离测试环境
- 恢复失败时还原前述备份，保留恢复前的事故现场

---

## 3. FileSystem/OSS/MinIO 存储边界审计

### 3.1 文档-脚本一致性审计

| 边界声明 | `docs/deployment.md` | `docs/disaster-recovery.md` | `docs/architecture-v1.0-rc-boundary.md` | 脚本行为（`backup-local-stack.ps1` + `restore-local-stack.ps1`） |
|---|---|---|---|---|
| 默认存储为 FileSystem | ✓ Line 30: "FileCenter local blobs...当 FILECENTER_STORAGE_PROVIDER=FileSystem" | ✓ Line 8: "核心目标...FileCenter 本地 storage volume 可恢复" | ✓ §2.2: "RC 主路径固定为 FileSystem" | ✓ 默认备份 storage.tar.gz 来自 `api` 容器 `/app/storage` 挂载的 Docker volume |
| OSS 不自动迁移历史文件 | ✓ Line 113: "Existing local blobs are not migrated automatically" | ✓ Line 13: "不覆盖...云厂商 OSS bucket 内对象备份" | ✓ §3.3: "默认 MinIO/S3/OSS 多后端切换...不作为 RC 正式交付能力" | ✓ OSS bucket 对象不在任何 backup/restore 脚本覆盖范围内 |
| MinIO 为可选/profile | ✓ §"Optional MinIO": "MinIO is included as a profile" | ✓ Line 25: "MinIO profile...默认不覆盖" | ✓ §2.4: "minio data...可选" | ✓ `-IncludeMinio` 开关，默认不备份 |
| OSS 备份不覆盖 | ✓ §"Optional Aliyun OSS" | ✓ Line 11: "不覆盖" | ✓ TD-10: "RC 发布说明声明对象存储为可选/实验" | ✓ 无 Aliyun OSS 对象备份逻辑 |

**结论**：所有文档与脚本行为一致，无矛盾。

### 3.2 Release Notes 边界声明更新

依据架构文档 TD-10 和 RC-FIX-05，已向 `docs/release-notes-v1.0-rc.md` 添加：

- **§3 本版明确不包含**：新增 "MinIO/S3/阿里云 OSS 多后端存储切换（实验/后置能力）"
- **§7 已知限制**：新增 OSS/MinIO 实验性——不作为 RC 正式交付能力，不自动迁移，不在默认备份覆盖范围

### 3.3 当前环境验证

| 检查项 | 实际值 |
|---|---|
| `FILECENTER_STORAGE_PROVIDER` | `FileSystem`（.env） |
| MinIO 服务 | 未启动（未使用 `--profile minio`） |
| Aliyun OSS 配置 | 未配置 |
| 实际 storage Docker volume | `pcdlocal_privateclouddrive_stack_storage` |
| 备份覆盖 OSS bucket | N/A（未启用） |

---

## 4. 破坏性恢复演练证据（历史引用）

2026-05-21 已完成一次性测试栈 `pcd_drill_test` 的破坏性恢复验收（`docs/validation/backup-restore-destructive-test-stack-20260521-215020.md`）：

| 验收项 | 结果 |
|---|---|
| 破坏性恢复至一次性栈 | PASS |
| PostgreSQL 恢复 | PASS（pg_restore --clean --if-exists） |
| Storage volume 恢复 | PASS（隔离目标 volume） |
| 栈验证 | PASS（verify-local-stack.ps1 -SkipStart） |
| 登录 | PASS |
| 文件列表 | PASS |
| 上传/下载/预览 | PASS（Range 206 + hash 匹配） |
| 分享链接 | PASS |
| 回收站恢复 | PASS |
| 审计/安全样本 | PASS（无 token/password 泄露） |

---

## 5. 脱敏合规检查

| 检查项 | 本报告 | 演练报告 |
|---|---|---|
| 不含明文 `.env` 值 | ✓ | ✓ |
| 不含 access/refresh token | ✓ | ✓ |
| 不含数据库密码 | ✓ | ✓ |
| 不含 OAuth client secret | ✓ | ✓ |
| 不含完整分享 URL | ✓ | ✓ |

---

## 6. 完整恢复后验收清单映射

`docs/disaster-recovery.md` 定义的验收清单，对照本次验证覆盖：

| 验收项 | 本次验证 | 历史破坏性恢复 |
|---|---|---|
| 栈健康 | PASS（dry-run + compose ps） | PASS |
| 登录 | 间接覆盖（smoke 脚本） | PASS |
| 文件列表 | 间接覆盖（smoke 脚本） | PASS |
| 下载/预览 | 间接覆盖（smoke 脚本） | PASS（Range 下载） |
| 媒体缩略图 | 未覆盖（需图片文件） | 未覆盖 |
| 媒体封面 | 未覆盖（需视频文件） | 未覆盖 |
| 分享链接 | 间接覆盖（smoke 脚本） | PASS |
| 回收站 | 间接覆盖（smoke 脚本） | PASS |
| 审计与安全 | 间接覆盖（smoke 脚本） | PASS |

**已知缺口**：媒体缩略图和视频封面在历史破坏性恢复中未验证。这些依赖媒体处理流水线（media-worker + FFmpeg）的真实可用性。建议在最终 RC 验收前用一次性栈执行带样本媒体文件的破坏性恢复，覆盖缩略图和封面生成。

---

## 7. 结论与后续建议

### 结论
1. **备份恢复路径已验证**：非破坏性 drill 2026-06-17 通过（PASS 14 / 0 / 0），最小可恢复资产（PG + storage + .env 说明）已确认。
2. **存储边界已冻结**：FileSystem 为 RC 唯一默认生产路径。文档（deployment.md / disaster-recovery.md / architecture-boundary.md / release-notes.md）与脚本行为一致。
3. **OSS/MinIO 已标记为实验/后置**：Release Notes 已明确声明，不作为 RC 正式交付能力。

### 后续建议
1. **媒体文件恢复验证**：在 RC 最终验收前，用含样本图片/视频的备份对一次性测试栈执行破坏性恢复，验证缩略图和封面生成。
2. **备份频率提醒**：RC 发布说明加入"每日至少一次备份"的部署者责任提醒。
3. **OSS 文档后续**：当 OSS/MinIO 进入 V1.3/V2 路线时，需制定迁移脚本、双向回滚方案和完整备份恢复 SOP。
