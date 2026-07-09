# PrivateCloudDrive V1.3 发布说明

> 版本：V1.3 — 管理与运维版
> 发布日期：待定
> 前置版本：V1.2 (RC)

---

## 新能力摘要

V1.3 聚焦于**管理与运维闭环**，让非开发者的实例管理员能通过 MAUI 客户端或 API 完成日常管理操作，不再依赖 Swagger 或命令行手动介入。

### P0 — 必须交付

| 能力 | 说明 | 用户可见 |
|------|------|----------|
| 管理员用户管理 | 创建/禁用/启用用户、重置密码、设置容量配额 | 管理员在 Settings 看到用户管理入口 |
| 系统健康页 | 展示 PostgreSQL、Redis、Storage、FFmpeg 组件状态；管理员见详情和修复建议，普通用户见概要 | Settings 顶部健康状态圆点 |
| 存储状态 | 只读展示当前存储 provider、容量、可用空间 | 管理员 Settings 可见 |
| 备份恢复脚本 | 一键备份（DB + storage + 配置）、校验、默认 dry-run、显式确认破坏性恢复 | 管理员可通过 PowerShell 脚本操作 |
| 升级回滚 SOP | 升级前备份、维护窗口、数据库迁移、健康验证、失败回滚 | 部署者按文档操作 |
| Docker Compose 验证 | `verify-local-stack.ps1` 输出 PASS/WARN/FAIL，检查所有服务、volume、FFmpeg 和 `.env` 配置 | 部署者运行脚本 |
| SQL Server / IIS 部署 | Docker Compose + PostgreSQL 为默认部署路径；IIS 部署指南保持同步 | 部署者参考文档 |
| 脱敏基线 | 健康详情、存储状态、操作日志不泄露密码/token/secret/物理路径 | 安全合规 |

### P1 — 本版本交付

| 能力 | 说明 |
|------|------|
| 操作日志增强 | 管理员按用户、操作类型、时间范围组合筛选；展开日志详情，密码/token 显示为 `***` |
| 媒体任务管理 | 管理员查看所有用户的媒体处理队列、失败原因、重新处理 |
| 分享风险提示 | 分享列表页风险提示文案 |
| 回收站清理建议 | 回收站清理建议和二次确认 |
| 故障诊断清单 | Settings 诊断页，问题类别展开 |

---

## 已知限制（10 条）

以下限制在 V1.3 中已知且不会被修复，在评估部署和使用时请充分了解。

| 编号 | 限制 | 影响 | 规避/备注 |
|:----:|------|------|-----------|
| KN-V1.3-01 | 禁用用户后，已有 access_token 缓存最长 5 分钟失效；期间 API 调用可能仍成功 | 安全边界非实时 | 在五分钟后验证禁用生效；如需立即生效，可重启 API 或等待 token 自然过期 |
| KN-V1.3-02 | 系统健康检测结果有 30 秒缓存，不会实时反映组件状态变化 | 监控时效性 | 可通过间隔 30 秒以上刷新获取最新状态 |
| KN-V1.3-03 | 备份脚本依赖主机安装 `pg_dump`，且在 Docker 宿主机上执行；非 Docker 部署需手工调整 | 备份可用性 | 确保 Docker 宿主机安装 PostgreSQL 客户端工具集 |
| KN-V1.3-04 | 存储状态页仅展示当前 provider 的容量概览，不支持在线切换存储后端或自动迁移已有文件 | 运维灵活性 | 切换 FileSystem / AliyunOss / MinIO 前需制定独立迁移与回滚计划 |
| KN-V1.3-05 | 操作日志筛选结果不支持 CSV 导出，当前仅限页面查看 | 审计导出 | 需导出时可通过 API 分页自行处理 |
| KN-V1.3-06 | 创建用户时无法通过 UI 分配角色（管理员/普通用户）；新建用户默认为普通用户，分配角色需通过 API 或数据库操作 | 管理效率 | 可通过 Swagger 调用 `/api/app/admin/users/{id}/roles` 分配角色 |
| KN-V1.3-07 | Settings 页面信息架构（IA）有调整，管理员用户需要适应新的管理区入口位置 | 体验过渡 | 管理入口集中在 Settings 顶部区域，按角色可见 |
| KN-V1.3-08 | 故障诊断清单为静态内容，不会根据当前系统状态动态展开/收起 | 诊断灵活性 | 静态内容已覆盖常见问题类别 |
| KN-V1.3-09 | 管理端当前仅通过 MAUI Settings 入口和 Swagger/API 提供，无独立 Web 管理后台 | 操作平台 | 独立 Blazor/Web 管理端列为 V2 候选 |
| KN-V1.3-10 | iOS 客户端不在 V1.3 范围内；MAUI 构建仅验证 Windows 和 Android 目标 | 平台覆盖 | 如需 iOS，请参考 V1.2 已知限制并等待后续版本 |

---

## 升级注意事项

> **适用范围**：从 V1.2 (RC) 升级到 V1.3 的 Docker Compose 部署实例。
> 首次部署请直接参考 [deployment.md](deployment.md)。

### 升级前必须完成

1. **完整备份**（DB + storage + .env）：

   ```powershell
   .\scripts\backup-local-stack.ps1 -IncludeEnv
   .\scripts\run-backup-restore-drill.ps1
   ```

   确认输出无 FAIL，`postgres.dump` 和 `storage.tar.gz` 均非空。

2. **确认 `.env` 兼容性**：V1.3 新增了 `PASSWORD_LOGIN_RATE_LIMIT_*` 系列环境变量。如 `.env` 未包含，API 会使用代码默认值（限流默认开启）。建议先 diff 确认。

   ```powershell
   git diff origin/main -- .env.example
   ```

3. **阅读已知限制**：特别关注 KN-V1.3-01（token 缓存）、KN-V1.3-02（健康页缓存）、KN-V1.3-06（角色分配 UI）。

### 升级步骤

```powershell
# Step 1: 备份（已完成）
# Step 2: 拉取新版本
git pull origin main
docker compose up -d --build

# Step 3: 确认迁移完成
docker compose logs db-migrator --tail=50
# 预期看到 "DbMigrator has been successfully completed"

# Step 4: 健康验证
.\scripts\verify-local-stack.ps1 -SkipStart
.\scripts\verify-health.ps1
```

### 升级后验证清单

| 检查项 | 预期 |
|--------|------|
| 所有容器正常 | api、db-migrator (Exited 0)、postgres、redis、media-worker |
| API 可达 | HTTP 200 |
| 健康端点 | `verify-local-stack.ps1` PASS |
| 管理员登录 | SSH/MAUI 登录成功 |
| 普通用户登录 | 不受影响 |
| 文件列表、分享、回收站 | 功能正常 |
| MAUI 客户端版本 | 如使用 MAUI，需重新构建或安装新版 APK |
| 操作日志 | 管理员可查询 |
| 备份恢复脚本 | 新版脚本正常执行 backup + drill |

### 回滚方案

如果升级后出现迁移失败、API 500、核心功能不可用：

```powershell
docker compose down
git checkout <升级前的提交或标签>
docker compose up -d --build
docker compose stop api media-worker
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\<备份目录> -ConfirmDestructiveRestore
.\scripts\verify-local-stack.ps1 -SkipStart
```

> **只有在升级前完成了完整备份，回滚才可靠。**

### 已知升级风险

| 风险 | 出现条件 | 处理 |
|------|----------|------|
| db-migrator ERROR | 数据库迁移与新代码不兼容 | 立即回滚 Step 6；检查迁移脚本后重试 |
| API 502 | `.env` 未包含新增变量或值不兼容 | 检查 `.env.example` diff，补全变量后重建 |
| 禁用用户仍可登录 | token 缓存未过期 | 等待最长 5 分钟后验证；或重启 api 容器使缓存立即失效 |
| 健康页 WARN | Docker daemon 或环境配置与本地开发环境有差异 | 根据 WARN 内容排查；非核心组件 WARN 可接受 |

---

## 文档导航

- [部署说明](deployment.md) — 包含完整升级回滚 SOP
- [备份恢复指南](backup-restore-guide.md) — 三件套备份/恢复快速操作
- [灾难恢复 Runbook](disaster-recovery.md) — 完整灾难恢复流程
- [测试说明](testing.md) — V1.3 验收矩阵与验证命令
- [已知限制](known-limitations.md) — 全局已知限制
- [架构边界](architecture-v1.3-boundary.md) — V1.3 技术债务与组件修改 allowlist
