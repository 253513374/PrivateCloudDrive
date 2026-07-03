# Private Deployment

## Docker Compose

1. Copy `.env.example` to `.env` and change all secrets:

```powershell
Copy-Item .env.example .env
```

2. Set `PUBLIC_URL` to the address users and the MAUI client can reach.
3. Start the backend stack:

```powershell
docker compose up -d --build
```

4. For local validation, open Swagger at `http://localhost:8080/swagger`. For production, set `SWAGGER_ENABLED=false` unless Swagger is protected by an internal network, VPN, or administrator-only gateway.

The Compose stack contains PostgreSQL, Redis, an API host, a database migrator, a media-worker process for ABP background jobs, and an optional MinIO service behind the `minio` profile. The checked-in Compose defaults are production-safe for Swagger and insecure-local-validation switches; `.env.example` explicitly enables the local HTTP/Swagger settings needed by the bundled health checks.

The `db-migrator` service runs before the API and applies database migrations plus ABP data seed. The seed creates the OpenIddict Swagger client and the MAUI client `PrivateCloudDrive_App`. The Compose service explicitly provides `PrivateCloudDrive_Swagger` and `PrivateCloudDrive_App` ClientId values and runs from `/app/migrator`, so a fresh empty-volume deployment does not depend on the container entry working directory to discover the published DbMigrator `appsettings.json` before seeding OpenIddict clients.

V1 WeChat, Google, and GitHub login stay disabled by default. When enabling WeChat, set the `WECHAT_*` variables in `.env`; `WECHAT_APP_SECRET` is passed only to the backend API container and must not be copied into the MAUI app. When enabling Google or GitHub, set the `GOOGLE_*` or `GITHUB_*` variables; provider client secrets are backend-only and are never returned by mobile settings endpoints.

## Persistent Data

- PostgreSQL data: `privateclouddrive_stack_postgres_data`
- Redis data: `privateclouddrive_stack_redis_data`
- FileCenter local blobs, upload temp files, thumbnails, and video covers when `FILECENTER_STORAGE_PROVIDER=FileSystem`: Compose logical volume `privateclouddrive_stack_storage`（实际 Docker volume 名会带 Compose project 前缀；脚本会从运行中容器挂载解析真实名称）
- FileCenter upload temp files and media-processing temp files when `FILECENTER_STORAGE_PROVIDER=AliyunOss`: Compose logical volume `privateclouddrive_stack_storage`（实际 Docker volume 名会带 Compose project 前缀；脚本会从运行中容器挂载解析真实名称）
- Optional MinIO data: `privateclouddrive_stack_minio_data`

## Backup and Restore Drill

灾难恢复总入口见 [docs/disaster-recovery.md](disaster-recovery.md)。该 Runbook 定义数据资产、RPO/RTO 边界、恢复 dry-run、破坏性测试栈恢复、恢复后登录/文件/预览/分享验收清单，以及演练证据记录规范。

PrivateCloudDrive 的最小可恢复备份由三部分组成：

1. PostgreSQL 逻辑备份：账号、权限、文件索引、分享、相册、媒体处理状态和审计日志。
2. FileCenter storage volume：本地文件、上传临时文件、缩略图、视频封面和媒体处理临时文件。
3. 与实例匹配的 `.env`：数据库密码、加密短语、公开 URL、存储提供商和外部登录密钥。默认备份命令不会复制 `.env`，因为它包含敏感信息。

推荐先执行一键非破坏性演练。该命令会创建备份、校验 `manifest.json` / `postgres.dump` / `storage.tar.gz`、执行恢复 dry-run，并在 `docs/validation/` 生成演练报告；默认不会复制 `.env` 或覆盖任何数据：

```powershell
.\scripts\run-backup-restore-drill.ps1
```

创建本地栈备份：

```powershell
.\scripts\backup-local-stack.ps1
```

可选参数：

```powershell
.\scripts\backup-local-stack.ps1 -OutputDirectory .\artifacts\backups -IncludeRedis -IncludeMinio
.\scripts\backup-local-stack.ps1 -IncludeEnv
```

`-IncludeRedis` 只用于需要缓存、限流计数或临时登录票据时间点快照的排障场景；正常灾备恢复可以不包含 Redis。`-IncludeEnv` 会把 `.env` 复制为 `.env.secret`，该备份目录必须放入加密、访问受控的存储，且禁止提交到 Git。

备份目录包含：

- `postgres.dump`：`pg_dump --format=custom` 生成的数据库备份。
- `storage.tar.gz`：FileCenter storage 真实 Docker volume 归档；脚本会优先读取 API 容器 `/app/storage` 挂载到的实际 volume 名，并在 `manifest.json` 的 `storage.dockerVolume` 中记录。
- `manifest.json`：提交号、备份时间、文件清单和 PASS/WARN/FAIL 汇总，不包含明文 secret。
- `ENVIRONMENT-REQUIRED.md` 或 `.env.secret`：恢复时的环境变量说明或敏感环境文件副本。

恢复演练先执行 dry-run，确认备份文件、Compose 配置和破坏性操作范围：

```powershell
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260515-141611
```

确认要覆盖目标测试栈后，再显式加上确认开关：

```powershell
.\scripts\restore-local-stack.ps1 -BackupDirectory .\artifacts\backups\20260515-141611 -ConfirmDestructiveRestore
```

恢复建议在全新的测试机器或测试 Compose project 中执行，不要直接覆盖生产实例。生产恢复前必须先保留事故现场备份，并确认目标主机、Compose project、`.env` 与备份来源一致。`restore-local-stack.ps1` 的破坏性恢复会：

1. 停止 API、media-worker、db-migrator 和 MinIO，避免恢复时仍有进程读写数据库或 storage volume。
2. 启动 PostgreSQL/Redis。
3. 将 `postgres.dump` 复制进 PostgreSQL 容器，并用 `pg_restore --clean --if-exists --no-owner --no-privileges` 恢复到当前 Compose 目标数据库。
4. 用临时 Alpine 容器清空并解包 `storage.tar.gz` 到备份 manifest 记录或当前 API 容器 `/app/storage` 挂载的真实 FileCenter storage volume。
5. 可选恢复 `redis-dump.rdb` 和 `minio.tar.gz`；Redis 恢复后会用 `redis-cli ping` 验证。
6. 默认启动完整栈并运行 `.\scripts\verify-local-stack.ps1 -SkipStart`。
7. 恢复后仍需用测试账号验证登录、文件列表、下载/预览、媒体缩略图、回收站恢复和分享链接，并把脱敏证据写入 `docs/validation/`。

如果 `FILECENTER_STORAGE_PROVIDER=AliyunOss`，`storage.tar.gz` 只覆盖本地临时区，不包含 OSS bucket 内对象。OSS bucket 需要按云厂商能力单独开启版本控制、跨区域复制或定期对象备份；切换本地/OSS 存储前必须先制定迁移与回滚计划。

## Media Processing

The Docker image installs `ffmpeg` and `ffprobe`. The API host disables background job execution, while the `media-worker` service enables it so media thumbnail and metadata jobs are handled separately from HTTP traffic.

## Optional MinIO

MinIO is included as a profile for later object-storage wiring:

```powershell
docker compose --profile minio up -d --build
```

The current default FileCenter storage still uses the local filesystem volume at `/app/storage`.

## Optional Aliyun OSS

FileCenter can store new blobs in a private Aliyun OSS bucket while keeping the same backend upload, download, thumbnail, video cover, and MAUI API flow. Existing local blobs are not migrated automatically; do not switch a production database with existing local files to OSS without a separate migration plan.

Set these variables in `.env`:

```env
FILECENTER_STORAGE_PROVIDER=AliyunOss
ALIYUN_OSS_ACCESS_KEY_ID=your-ram-access-key-id
ALIYUN_OSS_ACCESS_KEY_SECRET=your-ram-access-key-secret
ALIYUN_OSS_ENDPOINT=oss-cn-hangzhou.aliyuncs.com
ALIYUN_OSS_REGION_ID=cn-hangzhou
ALIYUN_OSS_BUCKET=privateclouddrive
ALIYUN_OSS_CREATE_BUCKET=false
```

The bucket should stay private. File access still goes through the backend API so authentication, permissions, quota checks, public-share rules, and HTTP Range playback remain enforced by PrivateCloudDrive. The Aliyun AccessKey values are passed only to the API and media-worker containers and must not be copied into the MAUI app.

Recommended RAM policy scope for the bucket is limited to the configured bucket and should include object read/write/delete plus bucket existence checks. If `ALIYUN_OSS_CREATE_BUCKET=true`, the credential also needs permission to create the bucket; production deployments should normally create the bucket explicitly and keep this value `false`.

Keep the `/app/storage` volume mounted even when OSS is enabled. It remains the local work area for chunk upload merge files and FFmpeg/FFprobe temporary media-processing files.

## MAUI Client

The seeded mobile OAuth client is `PrivateCloudDrive_App` and uses `privateclouddrive://callback`. Keep `PUBLIC_URL` aligned with the address the mobile device can reach.

For MVP inner testing, `maui/PrivateCloudDrive.App/Services/AppSettings.cs` targets the local Compose API by default: Windows uses `http://localhost:8080`, and the Android emulator uses `http://10.0.2.2:8080`. For physical device testing, update `ApiBaseUrl` to the LAN URL that can reach the API host. If the callback URI changes, update the DbMigrator OpenIddict application setting and rerun the migrator.

The MVP app enters Trash from Settings. WeChat, Google, and GitHub login are optional paths. Android now has a native WeChat SDK authorization bridge, while Google/GitHub use MAUI WebAuthenticator with the configured provider redirect URI. Keep account-password login available regardless of external provider configuration.

## Configuration Reference

| Variable | Purpose |
| --- | --- |
| `POSTGRES_DB` | PostgreSQL database name |
| `POSTGRES_USER` | PostgreSQL user |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `STRING_ENCRYPTION_PASSPHRASE` | ABP string encryption passphrase; replace the template value before production use |
| `PUBLIC_URL` | Public API/AuthServer URL used by Swagger and OpenIddict; production/RC network deployments must use `https://` |
| `AUTH_SERVER_REQUIRE_HTTPS_METADATA` | Whether OpenIddict metadata must be fetched over HTTPS. Local `.env.example` sets `false`; production should set or inherit `true` |
| `SWAGGER_ENABLED` | Enables Swagger UI/API docs. Local `.env.example` sets `true` for validation; production should set or inherit `false` unless access is otherwise protected |
| `ALLOW_INSECURE_LOCAL_VALIDATION` | Explicitly allows local HTTP/default-secret validation. Local `.env.example` sets `true`; production must set or inherit `false` so the API fail-fast security gate is active |
| `PUBLIC_SHARE_PASSWORD_RATE_LIMIT_PERMIT_LIMIT` | Max attempts per IP+share-token window for public share password verification/download |
| `PUBLIC_SHARE_PASSWORD_RATE_LIMIT_WINDOW_MINUTES` | Rate-limit window for password-protected public share endpoints |
| `FILECENTER_STORAGE_PROVIDER` | `FileSystem` by default; set `AliyunOss` to store new FileCenter blobs in Aliyun OSS |
| `FILECENTER_STORAGE_PATH` | Container path for FileCenter blob, thumbnail, cover, and temp upload storage |
| `ALIYUN_OSS_ACCESS_KEY_ID` | Aliyun RAM AccessKey ID, backend only |
| `ALIYUN_OSS_ACCESS_KEY_SECRET` | Aliyun RAM AccessKey secret, backend only |
| `ALIYUN_OSS_ENDPOINT` | OSS endpoint, for example `oss-cn-hangzhou.aliyuncs.com` |
| `ALIYUN_OSS_REGION_ID` | STS region ID used by the ABP Aliyun provider, for example `cn-hangzhou` |
| `ALIYUN_OSS_BUCKET` | Private OSS bucket used by FileCenter |
| `ALIYUN_OSS_CREATE_BUCKET` | Whether the provider may create the bucket if missing; keep `false` for production |
| `PASSWORD_LOGIN_RATE_LIMIT_ENABLED` | Enables account-password login failure rate limiting |
| `PASSWORD_LOGIN_RATE_LIMIT_MAX_FAILED_ATTEMPTS` | Maximum failed password-login attempts per username and IP window |
| `PASSWORD_LOGIN_RATE_LIMIT_WINDOW_MINUTES` | Password-login failure rate-limit window |
| `WECHAT_ENABLED` | Optional V1 WeChat login switch; keep `false` unless official mobile app credentials are ready |
| `WECHAT_APP_ID` | WeChat Open Platform mobile application AppId |
| `WECHAT_APP_SECRET` | WeChat Open Platform AppSecret; backend only |
| `WECHAT_SCOPE` | WeChat OAuth scope, usually `snsapi_userinfo` |
| `WECHAT_CALLBACK_SCHEME` | App callback scheme used by the MAUI platform implementation |
| `WECHAT_ANDROID_PACKAGE_NAME` | Android package name registered in WeChat Open Platform |
| `WECHAT_ANDROID_SIGNATURE` | Android application signature registered in WeChat Open Platform |
| `WECHAT_IOS_BUNDLE_ID` | iOS Bundle Identifier registered in WeChat Open Platform |
| `WECHAT_IOS_URL_SCHEME` | iOS URL Scheme registered for WeChat callback |
| `WECHAT_BINDING_TICKET_LIFETIME_MINUTES` | Lifetime of first-login binding tickets |
| `WECHAT_REQUEST_TIMEOUT_SECONDS` | Backend timeout when calling WeChat APIs |
| `WECHAT_RATE_LIMIT_WINDOW_SECONDS` | WeChat login, bind, and unbind rate-limit window |
| `WECHAT_RATE_LIMIT_MAX_ATTEMPTS` | Maximum WeChat login, bind, or unbind attempts in one window |
| `EXTERNAL_BINDING_TICKET_LIFETIME_MINUTES` | Lifetime of Google/GitHub first-login binding tickets |
| `EXTERNAL_REQUEST_TIMEOUT_SECONDS` | Backend timeout when calling Google/GitHub APIs |
| `EXTERNAL_RATE_LIMIT_WINDOW_SECONDS` | Google/GitHub login, bind, and unbind rate-limit window |
| `EXTERNAL_RATE_LIMIT_MAX_ATTEMPTS` | Maximum Google/GitHub login, bind, or unbind attempts in one window |
| `GOOGLE_LOGIN_ENABLED` | Optional Google login switch |
| `GOOGLE_CLIENT_ID` | Google OAuth client ID returned to the MAUI app through settings when enabled |
| `GOOGLE_CLIENT_SECRET` | Optional Google OAuth client secret; backend only |
| `GOOGLE_REDIRECT_URI` | Google OAuth redirect URI, default `privateclouddrive://callback` |
| `GOOGLE_SCOPE` | Google OAuth scopes, default `openid profile email` |
| `GOOGLE_USE_PKCE` | Whether the MAUI app sends a PKCE code challenge for Google |
| `GITHUB_LOGIN_ENABLED` | Optional GitHub login switch |
| `GITHUB_CLIENT_ID` | GitHub OAuth app client ID returned to the MAUI app through settings when enabled |
| `GITHUB_CLIENT_SECRET` | GitHub OAuth app client secret; backend only |
| `GITHUB_REDIRECT_URI` | GitHub OAuth callback URL, default `privateclouddrive://callback` |
| `GITHUB_SCOPE` | GitHub OAuth scopes, default `read:user user:email` |
| `GITHUB_USE_PKCE` | Whether the MAUI app sends a PKCE code challenge for GitHub |
| `MINIO_ROOT_USER` | Optional MinIO root user |
| `MINIO_ROOT_PASSWORD` | Optional MinIO root password |

The Docker variables above enable the backend side and expose only public settings to the app. Android includes the official WeChat SDK authorization bridge, but still needs a real Open Platform mobile AppId/AppSecret, matching package signature, installed WeChat client, and real-device authorization validation before it can be accepted. iOS still needs its platform SDK implementation and URL Scheme validation.
WeChat operation rate limits are stored in Redis through ABP distributed cache, so API replicas share the same counters.
Google/GitHub operation rate limits and first-login binding tickets also use Redis through ABP distributed cache.
Password-login failure limits are also stored in Redis and are checked for both username and request IP before the password grant is processed.

## Validation

Before first deployment, run:

```powershell
docker compose config
```

For V1.0 RC preflight validation, run:

```powershell
.\scripts\verify-local-stack.ps1 -PreflightOnly
```

Preflight mode validates Docker, Compose configuration, required service definitions, and `.env` readiness without printing secret values. WARN results are acceptable for local-only validation when `.env` is intentionally absent or `PUBLIC_URL` still points to localhost; production-like RC deployment should replace template passwords and encryption passphrases before release.

After Docker can pull or build the required images, run the full local stack verification:

```powershell
.\scripts\verify-local-stack.ps1
```

Full mode starts the stack unless `-SkipStart` is passed, then confirms:

- `postgres` and `redis` are healthy.
- `db-migrator` completed successfully.
- `api` is running and exposes `http://localhost:8080/swagger`.
- `media-worker` stays running and handles background media jobs.
- FileCenter local/temp storage volume is mounted and writable at `/app/storage`.
- `ffmpeg` and `ffprobe` are available in the API container.

When `.env` sets `FILECENTER_STORAGE_PROVIDER=AliyunOss`, preflight validation also checks that all required Aliyun OSS variables are present without printing secret values. Full OSS smoke validation still requires real credentials and should confirm upload, thumbnail generation, video cover generation, download, HTTP Range playback, and permanent delete against the configured bucket.

The legacy `scripts/verify-docker-stack.ps1` remains available for basic Compose startup checks, but V1.0 RC acceptance should use `scripts/verify-local-stack.ps1` because it also covers storage, media tooling, and release-configuration boundaries.

## Troubleshooting

If Docker Desktop cannot pull images from Docker Hub or Microsoft Container Registry, configure the Docker Desktop HTTPS proxy first, then rerun:

```powershell
docker compose up -d --build
```

The Compose file uses `postgres:17-alpine` and `redis:7-alpine` because they are stable Alpine images and are enough for the current PostgreSQL and Redis requirements.

The full stack uses `privateclouddrive_stack_*` volumes so it does not reuse the development-only PostgreSQL volume from `docker-compose.postgres.yml`.

## MAUI 客户端构建

MAUI 客户端使用 `.NET MAUI` 框架，目标平台为 Windows（`net10.0-windows10.0.19041.0`）和 Android（`net10.0-android`）。

### 前置条件

- .NET SDK 10.0（`global.json` 要求 10.0.203+）
- `maui-windows` workload（Windows 构建必需）
- `android` workload（Android 构建必需）
- Android SDK（JDK + Android SDK，Android 构建必需）

检查 workload：

```powershell
dotnet workload list
```

### 一键构建验证

完整的顺序构建（Windows → Android）：

```powershell
.\scripts\verify-maui-build.ps1
```

Bash 环境：

```bash
bash scripts/verify-maui-build.sh
```

脚本输出示例：

```
================================================================
  PrivateCloudDrive MAUI Sequential Build Verification
================================================================
  Project:  D:\...\PrivateCloudDrive.App.csproj
  Config:   Debug
  Windows:  yes
  Android:  yes
================================================================

[PASS] dotnet-cli         dotnet CLI is available (10.0.204).
[PASS] maui-project       Project file found.
[PASS] maui-windows-wl    maui-windows workload detected.
[PASS] android-wl         android workload detected.

==> Building maui-windows
[PASS] maui-windows       Build completed.
[PASS] maui-windows-artifact Found: ...\PrivateCloudDrive.App.exe (0.28 MB)

==> Building maui-android
[PASS] maui-android       Build completed.
[PASS] maui-android-artifact Found: ...\com.companyname.privateclouddrive.app-Signed.apk (19.06 MB)

================================================================
  Summary
================================================================
  PASS: 8  WARN: 0  FAIL: 0
================================================================

[PASS] All MAUI build checks passed.
```

输出构件：

| 平台 | 输出路径 | 格式 |
| --- | --- | --- |
| Windows | `maui/PrivateCloudDrive.App/bin/Release/net10.0-windows10.0.19041.0/win-x64/` | `.exe` |
| Android | `maui/PrivateCloudDrive.App/bin/Release/net10.0-android/` | `-Signed.apk` |

### 参数说明

| 参数 | 说明 |
| --- | --- |
| `-Configuration Release` | Release 构建（默认 Debug） |
| `-SkipWindows` | 跳过 Windows |
| `-SkipAndroid` | 跳过 Android |
| `-NoRestore` | 跳过 NuGet restore（预 restore 后 CI 使用） |

### 发布前一键验证

在发布新的 RC 或 Release 之前：

```powershell
.\scripts\verify-maui-build.ps1 -Configuration Release
```

确认输出：

1. Windows `.exe` 生成且大小合理
2. Android `-Signed.apk` 生成且大小合理
3. 无 NuGet 依赖冲突或目标框架不匹配报错

### 回滚方案

如果某个变更导致 MAUI 构建失败：

1. 按脚本输出定位失败的平台（Windows 或 Android）
2. 检查最近变更的 `maui/` 目录文件
3. 回退变更后重新运行：
   ```powershell
   git checkout -- maui/
   .\scripts\verify-maui-build.ps1
   ```
