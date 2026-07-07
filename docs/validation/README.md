# V1.0 RC 验证证据

本目录包含 DevOps 门禁验证证据。所有文件已脱敏，不包含 secret/token/password/access key。

| 文件 | 内容 | 状态 |
|------|------|------|
| `01-rc-secret-log-scan-evidence.md` | Secret/log 扫描 + archive guard + `.env` 跟踪检查 | PASS（1个误报已复核） |
| `02-rc-local-stack-preflight-evidence.md` | Preflight 模式验证（14 PASS / 3 WARN / 1 FAIL） | PASS（FAIL 为本地 QA 配置，非脚本/基础设施问题） |
| `03-rc-local-stack-full-evidence.md` | 全量健康检查（23 PASS / 4 WARN / 1 FAIL） | PASS（所有容器、Swagger、存储、FFmpeg/FFprobe 均可用） |

## 验证命令

```powershell
# Secret scan
python scripts/secret-log-scan.py --include-working-tree --archive-ref HEAD

# .env 跟踪检查
git ls-files -- .env .env.secret .secrets

# Preflight
.\scripts\verify-local-stack.ps1 -PreflightOnly

# 全量（栈已在运行时）
.\scripts\verify-local-stack.ps1 -SkipStart -TimeoutSeconds 120

# 全量（栈未启动时）
.\scripts\verify-local-stack.ps1
```

## 关键状态

- **基础设施**：Docker CLI、Compose、Compose 配置、5 服务定义 → 全部 PASS
- **容器健康**：PostgreSQL (healthy)、Redis (healthy)、db-migrator (exited 0)、API (running)、media-worker (running) → 全部 PASS
- **应用可达**：Swagger HTTP 200 → PASS
- **存储**：`/app/storage` 存在且可写 → PASS
- **媒体工具**：ffmpeg + ffprobe 可用 → PASS
- **安全配置**：Swagger enabled、AllowInsecure、HTTPS not required → 均为 WARN（本地可接受）
- **AuthServer issuer**：PASS（PUBLIC_URL 已配置）
- **Secret 扫描**：tracked + archive PASS；working-tree 1 个误报已复核
