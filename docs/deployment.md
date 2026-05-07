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

4. Open Swagger at `http://localhost:8080/swagger`.

The Compose stack contains PostgreSQL, Redis, an API host, a database migrator, a media-worker process for ABP background jobs, and an optional MinIO service behind the `minio` profile.

The `db-migrator` service runs before the API and applies database migrations plus ABP data seed. The seed creates the OpenIddict Swagger client and the MAUI client `PrivateCloudDrive_App`.

## Persistent Data

- PostgreSQL data: `privateclouddrive_stack_postgres_data`
- Redis data: `privateclouddrive_stack_redis_data`
- FileCenter blobs, upload temp files, thumbnails, and video covers: `privateclouddrive_stack_storage`
- Optional MinIO data: `privateclouddrive_stack_minio_data`

## Media Processing

The Docker image installs `ffmpeg` and `ffprobe`. The API host disables background job execution, while the `media-worker` service enables it so media thumbnail and metadata jobs are handled separately from HTTP traffic.

## Optional MinIO

MinIO is included as a profile for later object-storage wiring:

```powershell
docker compose --profile minio up -d --build
```

The current default FileCenter storage still uses the local filesystem volume at `/app/storage`.

## MAUI Client

The seeded mobile OAuth client is `PrivateCloudDrive_App` and uses `privateclouddrive://callback`. Keep `PUBLIC_URL` aligned with the address the mobile device can reach.

For Android emulator or physical device testing, update `maui/PrivateCloudDrive.App/Services/AppSettings.cs` so `ApiBaseUrl` points to the reachable API URL. If the callback URI changes, update the DbMigrator OpenIddict application setting and rerun the migrator.

## Configuration Reference

| Variable | Purpose |
| --- | --- |
| `POSTGRES_DB` | PostgreSQL database name |
| `POSTGRES_USER` | PostgreSQL user |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `STRING_ENCRYPTION_PASSPHRASE` | ABP string encryption passphrase; replace the template value before production use |
| `PUBLIC_URL` | Public API/AuthServer URL used by Swagger and OpenIddict |
| `FILECENTER_STORAGE_PATH` | Container path for FileCenter blob, thumbnail, cover, and temp upload storage |
| `MINIO_ROOT_USER` | Optional MinIO root user |
| `MINIO_ROOT_PASSWORD` | Optional MinIO root password |

## Validation

Before first deployment, run:

```powershell
docker compose config
```

For a full preflight check:

```powershell
.\scripts\verify-docker-stack.ps1 -PreflightOnly
```

After Docker can pull the required base images, run the full stack verification:

```powershell
.\scripts\verify-docker-stack.ps1
```

After startup, confirm:

- `postgres` and `redis` are healthy.
- `db-migrator` completed successfully.
- `api` exposes `http://localhost:8080/swagger`.
- `media-worker` stays running and handles background media jobs.

## Troubleshooting

If Docker Desktop cannot pull images from Docker Hub or Microsoft Container Registry, configure the Docker Desktop HTTPS proxy first, then rerun:

```powershell
docker compose up -d --build
```

The Compose file uses `postgres:17-alpine` and `redis:7-alpine` because they are stable Alpine images and are enough for the current PostgreSQL and Redis requirements.

The full stack uses `privateclouddrive_stack_*` volumes so it does not reuse the development-only PostgreSQL volume from `docker-compose.postgres.yml`.
