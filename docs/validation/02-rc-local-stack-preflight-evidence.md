# V1.0 RC Docker 本地栈 Preflight 验证证据

日期：2026-06-17
执行人：丁 DevOps / devops-eng
工具：`scripts/verify-local-stack.ps1 -PreflightOnly`

---

## 命令执行输出

```
PrivateCloudDrive V1.0 RC local stack verification
Mode: PreflightOnly

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
[WARN] security:auth-server-require-https - AuthServer HTTPS metadata NOT required (false). OK for local; enable for production via AUTH_SERVER_REQUIRE_HTTPS_METADATA=true.
[PASS] env:auth-server-authority - AuthServer issuer configured. Value hidden.
[PASS] env:STRING_ENCRYPTION_PASSPHRASE - Configured. Value hidden.
[PASS] env:POSTGRES_PASSWORD - Configured. Value hidden.
[PASS] env:PUBLIC_URL - Configured. Value hidden.
[PASS] env:FILECENTER_STORAGE_PROVIDER - Using FileSystem.
[FAIL] qa-test-account - secret missing; user=qa_user; alt_user=qa_user_alt; role=QA.Tester; secret_id=unset; rotated_at=unknown; sanitized=true

Summary
PASS: 14
WARN: 3
FAIL: 1
```

## 覆盖范围检查

| 检查项 | 覆盖率 | 状态 |
|--------|--------|------|
| Docker CLI | ✅ | PASS |
| Docker Compose | ✅ | PASS |
| Compose 配置验证 | ✅ | PASS |
| 必需服务定义（postgres/redis/db-migrator/api/media-worker） | ✅ | PASS × 5 |
| `.env` 存在性 | ✅ | PASS |
| 安全开关：Swagger 启用 | ✅ | WARN（本地可接受） |
| 安全开关：不安全传输 | ✅ | WARN（本地可接受） |
| AuthServer HTTPS 元数据 | ✅ | WARN（本地可接受） |
| AuthServer issuer（PUBLIC_URL） | ✅ | PASS |
| 关键环境变量（STRING_ENCRYPTION_PASSPHRASE/POSTGRES_PASSWORD/PUBLIC_URL） | ✅ | PASS × 3 |
| 存储 provider | ✅ | PASS（FileSystem） |
| QA 测试账号 | ✅ | FAIL（已启用但密码为空 — 本地开发配置） |

## FAIL 说明

- `qa-test-account`：本地 `.env` 中 `PCD_QA_TEST_ACCOUNT_ENABLED=true` 但未设密码（`PCD_QA_TEST_ACCOUNT_PASSWORD` 为空且 `PCD_QA_TEST_ACCOUNT_PASSWORD_FILE` 文件不存在）。
  - **不影响 RC 发布**：QA 账号种子数据仅在启用时使用，密码通过外部文件管理。RC 验收时在干净栈上需禁用或提供密码文件。
  - 推荐：RC 发布前将 `PCD_QA_TEST_ACCOUNT_ENABLED=false`（`.env.example` 默认值）。

## WARN 说明

| WARN 项 | 解释 |
|---------|------|
| security:swagger-enabled | Swagger 当前为 ON（本地开发默认）。生产环境应配置 `SWAGGER_ENABLED=false` 或仅内网访问。 |
| security:allow-insecure-transport | 允许 HTTP 传输（本地验证）。生产环境需 TLS/HTTPS。 |
| security:auth-server-require-https | AuthServer 不要求 HTTPS 元数据（本地验证）。生产环境应启用。 |

## 脱敏说明

本文件不包含任何 secret、token、password 或 access key。
