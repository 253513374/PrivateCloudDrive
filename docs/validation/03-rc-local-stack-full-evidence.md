# V1.0 RC Docker 本地栈全量健康检查验证证据

日期：2026-06-17
执行人：丁 DevOps / devops-eng
工具：`scripts/verify-local-stack.ps1 -SkipStart -TimeoutSeconds 120`

---

## 命令执行输出

```
PrivateCloudDrive V1.0 RC local stack verification
Mode: Full, SkipStart

[PASS] docker-cli - Docker CLI is available.
[PASS] docker-compose - Docker Compose is available.
[PASS] compose-config - Compose configuration is valid.
[PASS] service:postgres - Service is defined.
[PASS] service:redis - Service is defined.
[PASS] service:db-migrator - Service is defined.
[PASS] service:api - Service is defined.
[PASS] service:media-worker - Service is defined.
[PASS] .env - Found .env. Secret values are not printed.
[WARN] security:swagger-enabled - Swagger is ON (true). OK for local validation; disable (SWAGGER_ENABLED=false) for production/public exposure.
[WARN] security:allow-insecure-transport - Insecure transport allowed (true). OK for local validation; disable for production via ALLOW_INSECURE_LOCAL_VALIDATION=false.
[WARN] security:auth-server-require-https - AuthServer HTTPS metadata NOT required (false). OK for local; enable for production via AUTH_SERVER_REQUIRE_HTTPS_METADATA=*** env:auth-server-authority - AuthServer issuer configured. Value hidden.
[PASS] env:STRING_ENCRYPTION_PASSPHRASE - Configured. Value hidden.
[PASS] env:POSTGRES_PASSWORD - Configured. Value hidden.
[PASS] env:PUBLIC_URL - Configured. Value hidden.
[PASS] env:FILECENTER_STORAGE_PROVIDER - Using FileSystem.
[FAIL] qa-test-account - secret missing; user=qa_user; alt_user=qa_user_alt; role=QA.Tester; secret_id=unset; rotated_at=unknown; sanitized=true
[WARN] compose-up - Skipped stack startup; checking current containers only.
[PASS] postgres - Ready.
[PASS] redis - Ready.
[PASS] db-migrator - Ready.
[PASS] api - Ready.
[PASS] media-worker - Ready.
[PASS] swagger - Ready.
[PASS] storage:/app/storage - Available.
[PASS] ffmpeg - Available.
[PASS] ffprobe - Available.

Summary
PASS: 23
WARN: 4
FAIL: 1
```

## 覆盖范围

| 检查项 | 结果 | 说明 |
|--------|------|------|
| Docker CLI | PASS | Docker Desktop 可用 |
| Docker Compose | PASS | 插件可用 |
| Compose 配置 | PASS | docker compose config 通过 |
| 服务定义（postgres/redis/db-migrator/api/media-worker） | 5×PASS | 全部定义 |
| `.env` 存在且可读 | PASS | 敏感值未打印 |
| Swagger 安全开关 | WARN | 本地可接受；生产应关闭 |
| 不安全传输开关 | WARN | 本地可接受；生产应开启 HTTPS |
| AuthServer HTTPS 要求 | WARN | 本地可接受；生产应要求 HTTPS |
| AuthServer issuer | PASS | PUBLIC_URL 已配置 |
| 加密口令/DB密码/PUBLIC_URL | 3×PASS | 已配置，值隐藏 |
| 存储 provider | PASS | 使用 FileSystem |
| QA 测试账号 | FAIL | 已启用但密码为空（本地问题） |
| **容器健康** | | |
| PostgreSQL | PASS | healthy |
| Redis | PASS | healthy |
| db-migrator | PASS | exited 0 |
| API | PASS | running |
| media-worker | PASS | running |
| Swagger 可达性 | PASS | HTTP 200 |
| 存储卷 `/app/storage` | PASS | 存在且可写 |
| FFmpeg | PASS | 可用 |
| FFprobe | PASS | 可用 |

## FAIL 说明

- **qa-test-account**：本地 `.env` 启用了 QA 账号但未设密码。不影响基础设施健康或 RC 发布。RC 验收时设置为 `PCD_QA_TEST_ACCOUNT_ENABLED=false`（`.env.example` 默认）。

## 所有 WARN 项均为本地验证可接受

这些 WARN 对应 `TD-02`（Docker `.env` 默认值）和 `TD-11`（Swagger 暴露策略），已在 RC-FIX-02 中明确允许本地验证时 WARN。

## 回滚路径

如果脚本修改导致误判：
1. 回退 `scripts/verify-local-stack.ps1` 脚本修改至 git HEAD 版本
2. 临时验收使用手工命令链：
   - `docker compose config`
   - `docker compose up -d --build`
   - `curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/swagger/index.html`
   - `docker exec <api-container> command -v ffmpeg && command -v ffprobe`

## 脱敏说明

本文件不包含任何 secret、token、password 或 access key。
