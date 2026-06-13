# Private Backup 存储边界审计

日期：2026-05-22
目标：明确容量、数据位置、生命周期与恢复范围。

## 1. 发布结论

| Provider / 组件 | D7 口径 | 发布判断 |
| --- | --- | --- |
| FileSystem / Docker volume | 默认 P0 可交付路径；文件正文、缩略图、视频封面和上传临时分片位于 API `/app/storage` 对应 volume | PASS candidate |
| PostgreSQL | 账号、权限、文件索引、分享、媒体状态、审计日志等元数据 | P0 必备 |
| Redis | 缓存、限流、临时状态；默认不是恢复核心 | PASS with WARN |
| Aliyun OSS | 可选对象存储；bucket/object 不在默认 `storage.tar.gz` 中 | PASS with WARN，需云侧备份 |
| MinIO profile | Compose 可选服务，但当前不应写成已交付 FileCenter Provider 主路径 | Not Now |
| 手机本地缓存 | 仅为 App 会话/预览/临时状态，不能恢复服务器数据 | Not recovery source |

## 2. 用户可见边界

- 可以说：默认本地部署会把文件数据保存在 Docker storage volume，数据库保存索引和权限。
- 必须说：恢复需要 PostgreSQL、FileCenter storage volume 和匹配的 `.env` / 加密短语等环境配置。
- 不得说：手机缓存可以恢复服务器文件。
- 不得说：默认备份脚本会备份 Aliyun OSS bucket 对象。
- 不得把 MinIO 描述为当前已完成的 FileCenter Provider，除非后端配置、验证和文档同时收口。

## 3. 容量与健康口径

| 项目 | 对用户展示 | 对管理员展示/文档 |
| --- | --- | --- |
| 已用容量 | 已使用、配额、剩余、进度条 | 可记录统计来源与验证命令 |
| 存储健康 | “存储可用/异常/未知”摘要 | 可说明检查 API storage volume、对象存储连接或 media-worker |
| 错误详情 | 可行动文案 | 内部日志必须脱敏，不能进公开验收包 |
| Provider | FileSystem / Aliyun OSS / Not Now 口径 | 不展示 AccessKey、bucket 真实名称、对象 key、连接串 |

## 4. 生命周期与删除恢复

1. 上传临时分片应在完成、取消或清理任务后释放。
2. 普通删除进入回收站；回收站恢复回原目录或按冲突规则提示。
3. 永久删除/清空回收站不可恢复，必须有强确认。
4. 备份恢复脚本恢复的是服务器侧数据库和 storage，不是手机侧缓存。
5. 媒体缩略图/视频封面可作为派生数据；恢复后可重新生成或按报告说明缺口。

## 5. DR 入口

- 部署说明：`docs/deployment.md`
- 灾备 Runbook：`docs/disaster-recovery.md`
- 破坏性恢复测试栈证据：`docs/validation/backup-restore-destructive-test-stack-20260521-215020.md`
- 已知限制：`docs/known-limitations.md`

## 6. D7 存储门禁

发布前必须确认：

- `docs/deployment.md` 没有把 MinIO 写成默认已交付存储后端。
- `docs/disaster-recovery.md` 明确 Aliyun OSS 不在默认 `storage.tar.gz` 覆盖范围内。
- App/公开文档不展示 AccessKey、连接串、完整私有 URL、真实 bucket/object key 或服务器绝对路径。
- 验收报告不记录用户真实私密文件内容。
